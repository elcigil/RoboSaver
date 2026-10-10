using System.Reflection;
using System.Security.Cryptography;
using RobotSaver;

internal static class DemoChecks
{
    public static void Run(Action<bool, string> check, string? artifacts)
    {
        Exception? failure = null;
        bool changed = false, frozen = false, closed = false;
        var thread = new Thread(() =>
        {
            try
            {
                using var form = new DemoForm(new Settings { Mode = CleanMode.Dirty, StuckEvents = false }, captureDesktop: false)
                {
                    StartPosition = FormStartPosition.Manual,
                    Location = new Point(-30000, -30000),
                    ShowInTaskbar = false
                };
                using var timer = new System.Windows.Forms.Timer { Interval = 500 };
                byte[]? first = null, paused = null;
                int phase = 0;
                byte[] Capture(string? output = null)
                {
                    using var frame = new Bitmap(form.Width, form.Height);
                    form.DrawToBitmap(frame, new Rectangle(Point.Empty, frame.Size));
                    if (output is not null) frame.Save(output, System.Drawing.Imaging.ImageFormat.Png);
                    using var stream = new MemoryStream();
                    frame.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                    return SHA256.HashData(stream.ToArray());
                }
                void Key(string method, Keys key) => typeof(DemoForm).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(form, [new KeyEventArgs(key)]);
                timer.Tick += (_, _) =>
                {
                    try
                    {
                        switch (phase++)
                        {
                            case 0: first = Capture(); break;
                            case 1:
                                changed = !first!.SequenceEqual(Capture());
                                Key("OnKeyDown", Keys.Space); Key("OnKeyUp", Keys.Space);
                                paused = Capture(artifacts is null ? null : Path.Combine(artifacts, "demo-window.png"));
                                break;
                            case 2:
                                frozen = paused!.SequenceEqual(Capture());
                                Key("OnKeyDown", Keys.Space); Key("OnKeyUp", Keys.Space);
                                Key("OnKeyDown", Keys.D);
                                break;
                            case 3:
                                Key("OnKeyUp", Keys.D);
                                Key("OnKeyDown", Keys.Escape);
                                closed = form.IsDisposed;
                                timer.Stop();
                                break;
                        }
                    }
                    catch (Exception ex) { failure = ex; timer.Stop(); form.Close(); }
                };
                form.Shown += (_, _) => timer.Start();
                Application.Run(form);
            }
            catch (Exception ex) { failure = ex; }
        }) { IsBackground = true };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        if (!thread.Join(TimeSpan.FromSeconds(12))) throw new TimeoutException("Demo message-loop test did not close.");
        if (failure is not null) throw new InvalidOperationException("Demo message-loop test failed", failure);
        check(changed, "WinForms timer advances actual rendered demo frames");
        check(frozen, "Space key freezes the actual demo frame");
        check(closed, "Escape key closes and disposes demo form");
    }
}
