using System.Diagnostics;
using System.Runtime.InteropServices;

namespace RobotSaver;

public sealed class PreviewForm : Form
{
    [DllImport("user32.dll")] private static extern bool IsWindow(IntPtr hwnd);
    [DllImport("user32.dll")] private static extern bool GetClientRect(IntPtr hwnd, out NativeRect rect);
    [StructLayout(LayoutKind.Sequential)] private struct NativeRect { public int Left, Top, Right, Bottom; }
    private readonly IntPtr parent;
    private readonly World world;
    private readonly System.Windows.Forms.Timer timer = new() { Interval = 50 };
    private readonly Stopwatch clock = new();
    private double previous;

    public PreviewForm(IntPtr parent, World? previewWorld = null)
    {
        if (parent == IntPtr.Zero || !IsWindow(parent)) throw new ArgumentException("Invalid preview parent window.");
        this.parent = parent;
        Text = "RobotSaver Preview";
        TopLevel = false;
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        AutoScaleMode = AutoScaleMode.None;
        DoubleBuffered = true;
        world = previewWorld ?? World.CreateDesktopPreview(new Size(640, 400));
        ResizeToParent();
        timer.Tick += (_, _) =>
        {
            if (!IsWindow(parent)) { Close(); return; }
            ResizeToParent();
            double now = clock.Elapsed.TotalSeconds;
            world.Update(Math.Min(.1, now - previous)); previous = now;
            Invalidate();
        };
        Shown += (_, _) => { clock.Start(); timer.Start(); };
    }
    protected override CreateParams CreateParams
    {
        get
        {
            var parameters = base.CreateParams;
            parameters.Parent = parent;
            parameters.Style = (parameters.Style & ~unchecked((int)0x80000000)) | 0x40000000;
            return parameters;
        }
    }
    private void ResizeToParent()
    {
        if (GetClientRect(parent, out var rect)) Bounds = new Rectangle(0, 0, Math.Max(1, rect.Right), Math.Max(1, rect.Bottom));
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        var surface = world.Surfaces[0];
        e.Graphics.ScaleTransform((float)ClientSize.Width / surface.Bounds.Width, (float)ClientSize.Height / surface.Bounds.Height);
        world.Draw(e.Graphics, surface, controls: false);
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing) { timer.Dispose(); world.Dispose(); }
        base.Dispose(disposing);
    }
}
