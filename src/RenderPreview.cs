using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Numerics;

namespace RobotSaver;

// Artwork inspection only. Time drives brushes/LEDs, not a simulated world.
public static class RenderPreview
{
    public static void Save(string path, double seconds)
    {
        using var bitmap = Create(seconds);
        bitmap.Save(Path.GetFullPath(path), ImageFormat.Png);
    }

    public static Bitmap Create(double seconds)
    {
        var bitmap = new Bitmap(1280, 800, PixelFormat.Format32bppPArgb);
        try
        {
            using var g = Graphics.FromImage(bitmap);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
            using var background = new LinearGradientBrush(new Point(0, 0), new Point(1280, 800), Color.FromArgb(19, 24, 35), Color.FromArgb(33, 37, 52));
            g.FillRectangle(background, 0, 0, 1280, 800);
            using var title = new Font("Segoe UI", 40, FontStyle.Bold, GraphicsUnit.Pixel);
            using var body = new Font("Segoe UI", 17, FontStyle.Regular, GraphicsUnit.Pixel);
            using var label = new Font("Segoe UI", 14, FontStyle.Bold, GraphicsUnit.Pixel);
            using var white = new SolidBrush(Color.FromArgb(235, 240, 248));
            using var muted = new SolidBrush(Color.FromArgb(160, 173, 194));
            using var accent = new SolidBrush(Color.FromArgb(175, 158, 255));
            using var panel = new SolidBrush(Color.FromArgb(39, 45, 61));
            g.DrawString("ROBOT / SAVER", label, accent, 56, 36);
            g.DrawString(L.T("Masaüstünün yeni sakini.", "Your desktop's new resident."), title, white, 52, 64);
            g.DrawString(L.T("Çizim önizlemesi • Simülasyon henüz uygulanmadı", "Artwork preview • Simulation not implemented yet"), body, muted, 56, 125);
            g.FillRoundedRectangle(panel, new RectangleF(56, 188, 720, 514), 24);
            g.FillRoundedRectangle(panel, new RectangleF(800, 188, 424, 514), 24);
            using var renderer = new RobotRenderer();
            renderer.DrawDock(g, new PointF(302, 554), -MathF.PI / 2, seconds);
            renderer.Draw(g, new Robot { Pos = new Vector2(425, 425), Radius = 145, Angle = -MathF.PI / 2 + .3f, Battery = .86f, BrushAngle = (float)(seconds % (Math.PI * 2)) * 8 });
            g.DrawString(L.T("01 / GÖVDE VE ŞARJ İSTASYONU", "01 / BODY AND CHARGING DOCK"), label, accent, 88, 218);
            g.DrawString(renderer.SpritePath is null ? L.T("GDI+ vektör çizim • Harici görsel gerektirmez", "GDI+ vector artwork • No external image required") : L.T("Harici robot.png görseli", "External robot.png artwork"), body, muted, 88, 651);
            g.DrawString(L.T("02 / PİL DURUMLARI", "02 / BATTERY STATES"), label, accent, 832, 218);
            float[] levels = [.86f, .3f, .1f];
            string[] captions = [L.T("Çalışmaya hazır", "Ready to roll"), L.T("Şarj azalıyor", "Battery getting low"), L.T("Yuvaya dönüş", "Return to dock")];
            for (int i = 0; i < 3; i++)
            {
                var y = 318 + i * 140;
                renderer.Draw(g, new Robot { Pos = new Vector2(897, y), Radius = 47, Angle = -MathF.PI / 2, Battery = levels[i], BrushAngle = (float)seconds + i });
                g.DrawString($"{levels[i] * 100:0}%", body, white, 976, y - 24);
                g.DrawString(captions[i], body, muted, 976, y + 3);
            }
            g.DrawString(L.T("AŞAMA 1   /   Ayarlar · Dil · Robot çizimi", "STAGE 1   /   Settings · Language · Robot artwork"), label, muted, 56, 744);
            if (renderer.SpriteWarning is not null) g.DrawString(L.T("robot.png okunamadı; yedek çizim kullanılıyor.", "robot.png could not be read; using fallback artwork."), label, accent, 680, 744);
            return bitmap;
        }
        catch { bitmap.Dispose(); throw; }
    }
}
