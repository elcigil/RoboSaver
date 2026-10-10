using System.Drawing.Drawing2D;

namespace RobotSaver;

public sealed class RobotRenderer : IDisposable
{
    private Bitmap? sprite;
    public string? SpritePath { get; private set; }
    public string? SpriteWarning { get; private set; }

    public RobotRenderer(bool loadSprite = true, IEnumerable<string>? spritePaths = null)
    {
        if (!loadSprite) return;
        foreach (var path in spritePaths ?? [Path.Combine(AppContext.BaseDirectory, "robot.png"), Path.Combine(Settings.DataDirectory, "robot.png")])
        {
            if (!File.Exists(path)) continue;
            try
            {
                // Clone to release the source file immediately (the user can replace it while running).
                using var source = Image.FromFile(path);
                if (source.Width > 4096 || source.Height > 4096) throw new ArgumentException("Robot image must be at most 4096 × 4096.");
                sprite = new Bitmap(source);
                SpritePath = path;
                SpriteWarning = null;
                break;
            }
            catch (Exception ex) when (ex is ArgumentException or IOException or UnauthorizedAccessException or OutOfMemoryException)
            {
                SpriteWarning = $"{Path.GetFileName(path)}: {ex.Message}";
            }
        }
    }

    public void Draw(Graphics graphics, Robot robot)
    {
        var saved = graphics.Save();
        try
        {
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.TranslateTransform(robot.Pos.X, robot.Pos.Y);
            graphics.ScaleTransform(robot.Radius / 60, robot.Radius / 60);
            for (int i = 5; i >= 1; i--)
            {
                using var shadow = new SolidBrush(Color.FromArgb(9 + (5 - i) * 3, 0, 0, 0));
                graphics.FillEllipse(shadow, -60 - i, -56 - i, 120 + i * 2, 120 + i * 2);
            }
            // Artwork faces up, while the simulation's zero angle faces right.
            graphics.RotateTransform(robot.Angle * 180 / MathF.PI + 90);
            DrawBrush(graphics, -45, -38, robot.BrushAngle);
            DrawBrush(graphics, 45, -38, -robot.BrushAngle);
            if (sprite is not null)
            {
                var scale = 120f / Math.Max(sprite.Width, sprite.Height);
                var width = sprite.Width * scale;
                var height = sprite.Height * scale;
                graphics.DrawImage(sprite, -width / 2, -height / 2, width, height);
                return;
            }
            using var bodyPath = new GraphicsPath();
            bodyPath.AddEllipse(-60, -60, 120, 120);
            using var body = new PathGradientBrush(bodyPath)
            {
                CenterPoint = new PointF(-20, -30),
                CenterColor = Color.FromArgb(185, 190, 197),
                SurroundColors = [Color.FromArgb(39, 43, 49)]
            };
            graphics.FillPath(body, bodyPath);
            using var rim = new Pen(Color.FromArgb(22, 25, 29), 3);
            graphics.DrawEllipse(rim, -58, -58, 116, 116);
            using var highlight = new Pen(Color.FromArgb(170, 222, 229, 235), 1.2f);
            graphics.DrawArc(highlight, -55, -55, 110, 110, 190, 130);
            using var bumper = new Pen(robot.BumpFlash > 0 ? Color.Coral : Color.FromArgb(24, 28, 33), 5);
            graphics.DrawArc(bumper, -54, -54, 108, 108, 198, 144);
            using var plate = new LinearGradientBrush(new Point(-36, -42), new Point(40, 45), Color.FromArgb(117, 124, 133), Color.FromArgb(51, 56, 64));
            graphics.FillEllipse(plate, -44, -44, 88, 88);
            using var seam = new Pen(Color.FromArgb(42, 46, 51), 1.2f);
            graphics.DrawEllipse(seam, -44, -44, 88, 88);
            using var buttonShadow = new SolidBrush(Color.FromArgb(34, 38, 44));
            graphics.FillEllipse(buttonShadow, -17, -16, 34, 34);
            using var button = new LinearGradientBrush(new Point(-12, -15), new Point(13, 15), Color.FromArgb(178, 185, 192), Color.FromArgb(83, 91, 101));
            graphics.FillEllipse(button, -14, -14, 28, 28);
            using var arrow = new Pen(Color.FromArgb(224, 232, 237), 1.8f) { EndCap = LineCap.ArrowAnchor };
            graphics.DrawArc(arrow, -7, -7, 14, 14, 30, 285);
            using var sensorRim = new SolidBrush(Color.FromArgb(28, 31, 36));
            graphics.FillEllipse(sensorRim, 16, 17, 25, 25);
            using var sensor = new LinearGradientBrush(new Point(20, 19), new Point(37, 38), Color.FromArgb(120, 128, 138), Color.FromArgb(32, 36, 43));
            graphics.FillEllipse(sensor, 19, 19, 19, 19);
            graphics.DrawArc(highlight, 20, 20, 17, 17, 195, 100);
            var ledColor = robot.Battery > 0.4f ? Color.FromArgb(93, 240, 155) : robot.Battery > 0.15f ? Color.Orange : Color.Tomato;
            using var glow = new SolidBrush(Color.FromArgb(35, ledColor));
            using var led = new SolidBrush(ledColor);
            graphics.FillEllipse(glow, -10, -35, 20, 10);
            graphics.FillRectangle(led, -6, -32, 12, 3);
            using var vent = new Pen(Color.FromArgb(35, 40, 47), 1.4f);
            for (int i = -3; i <= 3; i++) graphics.DrawLine(vent, i * 5, 39, i * 5, 45);
        }
        finally { graphics.Restore(saved); }
    }

    private static void DrawBrush(Graphics graphics, float x, float y, float angle)
    {
        var saved = graphics.Save();
        try
        {
            graphics.TranslateTransform(x, y);
            graphics.RotateTransform(angle * 180 / MathF.PI);
            using var bristle = new Pen(Color.FromArgb(165, 135, 139, 147), 1.3f);
            for (int arm = 0; arm < 3; arm++)
            {
                for (int strand = -2; strand <= 2; strand++) graphics.DrawBezier(bristle, 0, 0, 8, strand, 15, strand + 4, 21, strand + 8);
                graphics.RotateTransform(120);
            }
            using var hub = new SolidBrush(Color.FromArgb(42, 47, 54));
            graphics.FillEllipse(hub, -4, -4, 8, 8);
        }
        finally { graphics.Restore(saved); }
    }

    public void DrawDock(Graphics graphics, PointF position, float angle, double seconds)
    {
        var saved = graphics.Save();
        try
        {
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.TranslateTransform(position.X, position.Y);
            graphics.RotateTransform(angle * 180 / MathF.PI + 90);
            using var shadow = new SolidBrush(Color.FromArgb(70, 0, 0, 0));
            graphics.FillRoundedRectangle(shadow, new RectangleF(-49, -24, 102, 67), 10);
            using var dock = new LinearGradientBrush(new Point(0, -25), new Point(0, 38), Color.FromArgb(64, 69, 80), Color.FromArgb(26, 29, 35));
            graphics.FillRoundedRectangle(dock, new RectangleF(-50, -29, 100, 64), 8);
            using var pad = new SolidBrush(Color.FromArgb(18, 21, 26));
            graphics.FillRectangle(pad, -41, -20, 82, 22);
            using var metal = new SolidBrush(Color.FromArgb(181, 162, 107));
            graphics.FillRectangle(metal, -27, -12, 10, 7);
            graphics.FillRectangle(metal, 17, -12, 10, 7);
            using var led = new SolidBrush(Math.Sin(seconds * 3) > 0 ? Color.SpringGreen : Color.FromArgb(34, 99, 69));
            graphics.FillEllipse(led, -3, 15, 6, 4);
        }
        finally { graphics.Restore(saved); }
    }

    public void Dispose() { sprite?.Dispose(); sprite = null; }
}

internal static class DrawingExtensions
{
    public static void FillRoundedRectangle(this Graphics graphics, Brush brush, RectangleF bounds, float radius)
    {
        using var path = new GraphicsPath();
        var d = radius * 2;
        path.AddArc(bounds.Left, bounds.Top, d, d, 180, 90);
        path.AddArc(bounds.Right - d, bounds.Top, d, d, 270, 90);
        path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        graphics.FillPath(brush, path);
    }
}
