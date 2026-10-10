using System.Diagnostics;
using System.Runtime.InteropServices;
using RobotSaver;

internal static class SaverChecks
{
    [DllImport("user32.dll")] private static extern IntPtr GetParent(IntPtr handle);
    [DllImport("user32.dll")] private static extern int GetWindowLong(IntPtr handle, int index);
    [DllImport("user32.dll")] private static extern bool PostMessage(IntPtr handle, uint message, IntPtr wParam, IntPtr lParam);

    public static void Run(Action<bool, string> check)
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                var screen = Screen.PrimaryScreen!;
                // Compare with an independent in-memory capture. Nothing is written to disk.
                using (var baseline = new Bitmap(screen.Bounds.Width, screen.Bounds.Height))
                {
                    using (var graphics = Graphics.FromImage(baseline)) graphics.CopyFromScreen(screen.Bounds.Location, Point.Empty, baseline.Size);
                    using var capture = World.CreateDesktopPreview(screen.Bounds.Size, new Settings { Background = BgMode.Desktop, DirtDensity = 0 });
                    var points = new[] { new Point(2, 2), new Point(screen.Bounds.Width / 2, screen.Bounds.Height / 2), new Point(screen.Bounds.Width - 3, screen.Bounds.Height - 3) };
                    check(points.All(point => baseline.GetPixel(point.X, point.Y).ToArgb() == capture.Surfaces[0].Background.GetPixel(point.X, point.Y).ToArgb()), "Desktop background matches independent screen capture");
                }

                using (var host = new Form { StartPosition = FormStartPosition.Manual, Location = new Point(-30000, -30000), ClientSize = new Size(240, 150), ShowInTaskbar = false })
                {
                    host.Show();
                    using var preview = new PreviewForm(host.Handle, World.CreatePreview(new Size(640, 400), new Settings { DirtDensity = 0 }));
                    preview.Show();
                    Application.DoEvents();
                    check(GetParent(preview.Handle) == host.Handle && (GetWindowLong(preview.Handle, -16) & 0x40000000) != 0, "Windows preview is a real WS_CHILD window of supplied HWND");
                    check(preview.Bounds == new Rectangle(0, 0, 240, 150), "Preview fits parent client rectangle");
                    host.ClientSize = new Size(310, 180);
                    Pump(150);
                    check(preview.Size == host.ClientSize, "Preview follows parent resize");
                    host.Close();
                    Pump(150);
                    check(preview.IsDisposed || !preview.IsHandleCreated, "Preview releases native window when host closes");
                }
                foreach (var input in new[] { "escape", "key", "click" })
                {
                    var settings = new Settings { DirtDensity = 0, MouseSensitivity = 200, StuckEvents = false };
                    using var session = new SaverSession(World.CreateLayout([new Rectangle(-30000, -30000, 400, 300), new Rectangle(-29600, -30000, 400, 300)], settings), false);
                    check(session.Windows.All(f => f.FormBorderStyle == FormBorderStyle.None && f.TopMost && f.Bounds == f.Surface.Bounds), "Saver uses borderless monitor bounds: " + input);
                    using var timer = new System.Windows.Forms.Timer { Interval = input == "escape" ? 100 : 750 };
                    bool sent = false;
                    timer.Tick += (_, _) =>
                    {
                        timer.Stop(); sent = true;
                        var handle = session.Windows[0].Handle;
                        if (input == "click") PostMessage(handle, 0x201, (IntPtr)1, (IntPtr)655370);
                        else PostMessage(handle, 0x100, (IntPtr)(int)(input == "escape" ? Keys.Escape : Keys.A), (IntPtr)1);
                    };
                    timer.Start();
                    Application.Run(session);
                    check(sent, "Saver input message was posted: " + input);
                    check(session.Windows.All(f => f.IsDisposed), "Saver input closes all monitor windows: " + input + " (" + string.Join(", ", session.Windows.Select(f => $"disposed={f.IsDisposed},handle={f.IsHandleCreated}")) + ")");
                }
            }
            catch (Exception ex) { failure = ex; }
        }) { IsBackground = true };
        thread.SetApartmentState(ApartmentState.STA); thread.Start();
        if (!thread.Join(TimeSpan.FromSeconds(15))) throw new TimeoutException("Screensaver tests timed out.");
        if (failure is not null) throw new InvalidOperationException("Screensaver tests failed", failure);
        using var batteryWorld = World.CreatePreview(new Size(400, 300), new Settings { DirtDensity = 0, StuckEvents = false });
        batteryWorld.Robots[0].ManualControl = true;
        batteryWorld.Update(60);
        check(batteryWorld.Robots[0].Battery > .94f && batteryWorld.Robots[0].Battery < .96f, "20-minute battery loses about 5 percent per minute");
    }

    private static void Pump(int milliseconds)
    {
        var clock = Stopwatch.StartNew();
        while (clock.ElapsedMilliseconds < milliseconds) { Application.DoEvents(); Thread.Sleep(5); }
    }
}
