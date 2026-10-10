using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace RobotSaver;

public sealed class Surface : IDisposable
{
    public Rectangle Bounds { get; }
    public Rectangle WorkArea { get; }
    public bool Primary { get; }
    public Bitmap Background { get; }
    public Bitmap Dirt { get; }
    private Graphics? dirtGraphics;
    private float fadeRemainder;
    private byte[]? pixels;
    public Graphics DirtGraphics => dirtGraphics ??= Graphics.FromImage(Dirt);

    public Surface(Rectangle bounds, Rectangle workArea, bool primary, Color color)
    {
        if (bounds.Width < 1 || bounds.Height < 1) throw new ArgumentException("Surface must have a positive size.");
        Bounds = bounds;
        WorkArea = workArea;
        Primary = primary;
        Background = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppPArgb);
        try { Dirt = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb); }
        catch { Background.Dispose(); throw; }
        using var g = Graphics.FromImage(Background);
        g.Clear(color);
    }

    public void ClearDirt()
    {
        DirtGraphics.Clear(Color.Transparent);
        fadeRemainder = 0;
    }

    public void Erase(PointF center, float radius)
    {
        var g = DirtGraphics;
        var state = g.Save();
        try
        {
            g.CompositingMode = CompositingMode.SourceCopy;
            using var brush = new SolidBrush(Color.Transparent);
            g.FillEllipse(brush, center.X - Bounds.X - radius, center.Y - Bounds.Y - radius, radius * 2, radius * 2);
        }
        finally { g.Restore(state); }
    }

    public void Fade(float seconds, float duration)
    {
        if (duration <= 0) return;
        fadeRemainder += seconds * 255 / duration;
        int decrement = (int)fadeRemainder;
        if (decrement == 0) return;
        fadeRemainder -= decrement;
        // GDI+ must release its graphics context before LockBits.
        dirtGraphics?.Dispose();
        dirtGraphics = null;
        var data = Dirt.LockBits(new Rectangle(Point.Empty, Dirt.Size), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
        try
        {
            int rowBytes = Math.Abs(data.Stride);
            pixels ??= new byte[rowBytes * Dirt.Height];
            for (int y = 0; y < Dirt.Height; y++)
            {
                var row = IntPtr.Add(data.Scan0, y * data.Stride);
                int offset = y * rowBytes;
                Marshal.Copy(row, pixels, offset, rowBytes);
                for (int x = 3; x < Dirt.Width * 4; x += 4) pixels[offset + x] = (byte)Math.Max(0, pixels[offset + x] - decrement);
                Marshal.Copy(pixels, offset, row, rowBytes);
            }
        }
        finally { Dirt.UnlockBits(data); }
    }

    public void Dispose()
    {
        dirtGraphics?.Dispose();
        Background.Dispose();
        Dirt.Dispose();
    }
}
