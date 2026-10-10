using System.Diagnostics;
using Microsoft.Win32;

namespace RobotSaver;

public sealed class SaverSession : ApplicationContext
{
    private readonly World world;
    private readonly List<SaverForm> windows = [];
    private readonly System.Windows.Forms.Timer timer = new();
    private readonly Stopwatch clock = new();
    private readonly bool play;
    private bool closing, disposed, cursorHidden;
    private double previous;
    private Point mouse;
    public IReadOnlyList<SaverForm> Windows => windows;

    // Ownership of the world transfers to the session.
    public SaverSession(World world, bool play)
    {
        this.world = world;
        this.play = play;
        try
        {
            foreach (var surface in world.Surfaces)
            {
                var form = new SaverForm(world, surface, play, () => Input(false), () => ExitThread());
                windows.Add(form);
                form.FormClosed += (_, _) => ExitThread();
            }
            timer.Interval = Math.Max(1, 1000 / world.Configuration.Fps);
            timer.Tick += (_, _) => Tick();
            SystemEvents.DisplaySettingsChanged += DisplayChanged;
            foreach (var form in windows) form.Show();
            windows.FirstOrDefault(f => f.Surface.Primary)?.Activate();
            if (!play) { Cursor.Hide(); cursorHidden = true; }
            mouse = Cursor.Position;
            clock.Start();
            timer.Start();
        }
        catch { Dispose(); throw; }
    }

    private void Tick()
    {
        double now = clock.Elapsed.TotalSeconds;
        if (!play)
        {
            var position = Cursor.Position;
            if (now < .6) mouse = position;
            else if (Math.Abs((long)position.X - mouse.X) > world.Configuration.MouseSensitivity || Math.Abs((long)position.Y - mouse.Y) > world.Configuration.MouseSensitivity)
            { ExitThread(); return; }
        }
        world.Update(Math.Min(.1, now - previous));
        previous = now;
        foreach (var form in windows) form.Invalidate();
    }

    private void Input(bool immediate)
    {
        if (immediate || clock.Elapsed.TotalSeconds >= .6) ExitThread();
    }

    private void DisplayChanged(object? sender, EventArgs e)
    {
        var form = windows.FirstOrDefault();
        if (form is { IsDisposed: false, IsHandleCreated: true })
        {
            try { form.BeginInvoke((Action)(() => ExitThread())); }
            catch (InvalidOperationException) { }
        }
    }

    protected override void ExitThreadCore()
    {
        if (closing) return;
        closing = true;
        timer.Stop();
        foreach (var form in windows.ToArray())
        {
            if (form.IsDisposed) continue;
            form.Close();
            if (!form.IsDisposed) form.Dispose();
        }
        if (cursorHidden) { Cursor.Show(); cursorHidden = false; }
        base.ExitThreadCore();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && !disposed)
        {
            disposed = true;
            closing = true;
            SystemEvents.DisplaySettingsChanged -= DisplayChanged;
            timer.Dispose();
            foreach (var form in windows) form.Dispose();
            if (cursorHidden) { Cursor.Show(); cursorHidden = false; }
            world.Dispose();
        }
        base.Dispose(disposing);
    }
}

public sealed class SaverForm : Form
{
    private readonly World world;
    private readonly bool play;
    private readonly Action exitOnInput, escape;
    public Surface Surface { get; }

    public SaverForm(World world, Surface surface, bool play, Action exitOnInput, Action escape)
    {
        this.world = world; Surface = surface; this.play = play;
        this.exitOnInput = exitOnInput; this.escape = escape;
        Text = "RobotSaver";
        AutoScaleMode = AutoScaleMode.None;
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        Bounds = surface.Bounds;
        TopMost = true;
        ShowInTaskbar = false;
        DoubleBuffered = true;
        KeyPreview = true;
        Deactivate += (_, _) => world.ReleaseKeys();
    }
    protected override bool IsInputKey(Keys keyData) => true;
    protected override void OnShown(EventArgs e) { base.OnShown(e); Bounds = Surface.Bounds; }
    protected override void OnDpiChanged(DpiChangedEventArgs e) { e.Cancel = true; base.OnDpiChanged(e); }
    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Escape) escape();
        else if (play) world.KeyDown(e.KeyCode);
        else exitOnInput();
        e.Handled = e.SuppressKeyPress = true;
        base.OnKeyDown(e);
    }
    protected override void OnKeyUp(KeyEventArgs e) { if (play) world.KeyUp(e.KeyCode); base.OnKeyUp(e); }
    protected override void OnMouseDown(MouseEventArgs e) { if (!play) exitOnInput(); base.OnMouseDown(e); }
    protected override void OnPaint(PaintEventArgs e) { world.Draw(e.Graphics, Surface, controls: play); }
}
