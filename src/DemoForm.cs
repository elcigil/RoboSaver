using System.Diagnostics;
using System.Drawing.Drawing2D;

namespace RobotSaver;

public sealed class DemoForm : Form
{
    private readonly World world;
    private readonly System.Windows.Forms.Timer timer = new();
    private readonly Stopwatch watch = new();
    private double previous;

    public DemoForm(Settings configuration, bool captureDesktop = true)
    {
        configuration = configuration.Clone();
        configuration.Normalize();
        Text = L.T("RobotSaver — Canlı simülasyon", "RobotSaver — Live simulation");
        ClientSize = new Size(1100, 700);
        MinimumSize = new Size(680, 460);
        StartPosition = FormStartPosition.CenterScreen;
        DoubleBuffered = true;
        KeyPreview = true;
        world = captureDesktop ? World.CreateDesktopPreview(new Size(1280, 800), configuration) : World.CreatePreview(new Size(1280, 800), configuration);
        timer.Interval = Math.Max(1, 1000 / configuration.Fps);
        timer.Tick += (_, _) =>
        {
            double now = watch.Elapsed.TotalSeconds;
            world.Update(Math.Min(.1, now - previous));
            previous = now;
            Invalidate();
        };
        Shown += (_, _) => { watch.Start(); timer.Start(); };
        Deactivate += (_, _) => world.ReleaseKeys();
    }

    protected override bool IsInputKey(Keys keyData) => true;
    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Escape) Close();
        else world.KeyDown(e.KeyCode);
        Invalidate();
        e.Handled = e.SuppressKeyPress = true;
        base.OnKeyDown(e);
    }
    protected override void OnKeyUp(KeyEventArgs e) { world.KeyUp(e.KeyCode); base.OnKeyUp(e); }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.Clear(Color.FromArgb(18, 20, 29));
        float scale = Math.Min(ClientSize.Width / 1280f, ClientSize.Height / 800f);
        e.Graphics.TranslateTransform((ClientSize.Width - 1280 * scale) / 2, (ClientSize.Height - 800 * scale) / 2);
        e.Graphics.ScaleTransform(scale, scale);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        world.Draw(e.Graphics, world.Surfaces[0]);
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing) { timer.Dispose(); world.Dispose(); }
        base.Dispose(disposing);
    }
}
