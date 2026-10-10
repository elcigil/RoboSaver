using System.Reflection;
using System.Runtime.InteropServices;
using RobotSaver;

internal static class KeyboardChecks
{
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool PostMessage(IntPtr handle, uint message, IntPtr wParam, IntPtr lParam);

    public static void Run(Action<bool, string> check, string? artifacts = null)
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                using var form = new DemoForm(new Settings { DirtDensity = 0, StuckEvents = false, Notifications = false, Bubbles = false }, captureDesktop: false)
                { StartPosition = FormStartPosition.Manual, Location = new Point(-30000, -30000), ShowInTaskbar = false };
                var world = (World)typeof(DemoForm).GetField("world", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(form)!;
                var robot = world.Robots[0];
                world.KeyDown(Keys.Space); world.KeyUp(Keys.Space); // Stable fixture; all checks below use queued native messages.
                var steps = new Queue<Action>();
                void Key(Keys key, bool down)
                {
                    if (!PostMessage(form.Handle, down ? 0x100u : 0x101u, (IntPtr)(int)key, down ? (IntPtr)1 : new IntPtr(unchecked((int)0xC0000001)))) throw new System.ComponentModel.Win32Exception();
                }
                void Press(Keys key) { Key(key, true); Key(key, false); }
                void Step(Action action) => steps.Enqueue(action);
                Step(() => { robot.Bin = 30; Press(Keys.E); });
                Step(() => check(robot.Bin == 0, "Native E key empties a nonempty bin"));
                if (artifacts is not null) Step(() =>
                {
                    using var frame = new Bitmap(form.Width, form.Height);
                    form.DrawToBitmap(frame, new Rectangle(Point.Empty, frame.Size));
                    frame.Save(Path.Combine(artifacts, "keyboard-empty-bin.png"), System.Drawing.Imaging.ImageFormat.Png);
                });
                Step(() => { robot.Bin = 40; Press(Keys.E); });
                Step(() => check(robot.Bin == 0, "Native E key works again after key release"));
                Step(() => check(world.ActionMessage?.StartsWith("Hazne boşaltıldı") == true, "E gives feedback even when bubbles are disabled"));
                Step(() => Press(Keys.E));
                Step(() => check(world.ActionMessage?.StartsWith("Hazne zaten boş") == true, "E explains an already empty bin"));
                Step(() => { robot.Bin = 80; robot.SetState(RobotState.Dumping); Press(Keys.E); });
                Step(() => check(robot.Bin == 0 && robot.State == RobotState.Roaming, "E cancels active dumping"));
                foreach (var key in new[] { Keys.M, Keys.C, Keys.F1, Keys.Enter, Keys.Space })
                {
                    bool Read() => key switch { Keys.M => world.ShowMap, Keys.C => world.DropCrumbs, Keys.F1 => world.ShowKeys, Keys.Enter => robot.ManualControl, _ => world.Paused };
                    bool original = false;
                    Step(() => { original = Read(); Press(key); });
                    Step(() => check(Read() != original, $"Native {key} toggles"));
                    Step(() => Press(key));
                    Step(() => check(Read() == original, $"Native {key} toggles back after release"));
                }
                Step(() => Press(Keys.K));
                Step(() => check(world.Particles.Count > 0, "Native K creates particles"));
                Step(() => Press(Keys.H));
                Step(() => check(robot.State == RobotState.ReturningHome, "Native H requests home"));
                Step(() => Press(Keys.R));
                Step(() => check(world.Collected == 0 && world.Robots[0].State == RobotState.Roaming, "Native R resets world"));
                Step(() => { robot = world.Robots[0]; robot.Pos = new System.Numerics.Vector2(640, 400); robot.ManualControl = true; });
                System.Numerics.Vector2 before = default, stopped = default;
                foreach (var key in new[] { Keys.W, Keys.A, Keys.S, Keys.D, Keys.Up, Keys.Left, Keys.Down, Keys.Right })
                {
                    Step(() => { before = robot.Pos; Key(key, true); });
                    Step(() =>
                    {
                        var delta = robot.Pos - before;
                        bool moved = key switch { Keys.W or Keys.Up => delta.Y < -1, Keys.S or Keys.Down => delta.Y > 1, Keys.A or Keys.Left => delta.X < -1, _ => delta.X > 1 };
                        check(moved, $"Native {key} drives in the correct direction");
                        Key(key, false);
                    });
                    Step(() => stopped = robot.Pos);
                    Step(() => check(robot.Pos == stopped, $"Native {key} release stops movement"));
                }
                double started = 0;
                Step(() => { before = robot.Pos; started = world.Time; Key(Keys.ShiftKey, true); Key(Keys.D, true); });
                Step(() =>
                {
                    double speed = (robot.Pos.X - before.X) / (world.Time - started);
                    check(Math.Abs(speed - world.Configuration.Speed * 2) < 5, "Native Shift doubles manual speed");
                    Key(Keys.D, false); Key(Keys.ShiftKey, false);
                });
                Step(() => Press(Keys.N));
                Step(() => check(world.ActionMessage?.Contains("henüz hazır değil") == true, "N reports unavailable sound instead of doing nothing"));
                Step(() => Press(Keys.Escape));
                using var timer = new System.Windows.Forms.Timer { Interval = 70 };
                timer.Tick += (_, _) =>
                {
                    try { if (steps.TryDequeue(out var action)) action(); else { timer.Stop(); form.Close(); } }
                    catch (Exception ex) { failure = ex; timer.Stop(); form.Close(); }
                };
                form.Shown += (_, _) => timer.Start();
                Application.Run(form);
                if (failure is null) check(form.IsDisposed && steps.Count == 0, "Native Escape closes window");
            }
            catch (Exception ex) { failure = ex; }
        }) { IsBackground = true };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        if (!thread.Join(TimeSpan.FromSeconds(15))) throw new TimeoutException("Native keyboard test timed out.");
        if (failure is not null) throw new InvalidOperationException("Native keyboard test failed", failure);
    }
}
