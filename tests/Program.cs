using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Numerics;
using RobotSaver;

var directory = Path.Combine(Path.GetTempPath(), "RobotSaver.Checks." + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(directory);
int count = 0;
void Check(bool condition, string description)
{
    if (!condition) throw new InvalidOperationException(description);
    Console.WriteLine("PASS " + description);
    count++;
}
try
{
    if (args.Contains("--keyboard-only")) { KeyboardChecks.Run(Check); return 0; }
    if (args.Contains("--saver-only")) { SaverChecks.Run(Check); return 0; }
    var path = Path.Combine(directory, "settings.json");
    var settings = Settings.Load(path);
    Check(settings.RobotCount == 1 && settings.Mode == CleanMode.Mixed && settings.BatteryMinutes == 20, "Missing settings uses defaults with 20-minute battery");
    settings.Language = "en";
    settings.Mode = CleanMode.Dirty;
    settings.BgImage = "C:\\örnek klasör\\resim.png";
    settings.Save(path);
    var saved = File.ReadAllText(path);
    Check(saved.Contains("\"Dirty\"") && !saved.Contains("Current"), "JSON persists named enums and excludes runtime state");
    var read = Settings.Load(path);
    Check(read.Language == "en" && read.Mode == CleanMode.Dirty && read.BgImage == settings.BgImage, "Settings round trip including Unicode path");
    var clone = read.Clone();
    clone.RobotCount = 8;
    Check(read.RobotCount == 1, "Clone edits are independent");
    read.Save(path);
    Check(Directory.GetFiles(directory, "*.tmp").Length == 0, "Replacing settings leaves no temporary files");
    File.WriteAllText(path, "{bad json");
    Settings.Load(path);
    Check(Settings.LastLoadWarning is not null && File.ReadAllText(path) == "{bad json", "Invalid JSON is reported and preserved");
    File.WriteAllText(path, "{\"Mode\":\"Unknown\"}");
    Check(Settings.Load(path).Mode == CleanMode.Mixed && Settings.LastLoadWarning is not null, "Unknown enum fails safely");
    File.WriteAllText(path, "{\"RobotCount\":99,\"Speed\":-1,\"Language\":null,\"BgColor\":null,\"BgImage\":null,\"Fps\":0,\"BinCapacity\":0}");
    read = Settings.Load(path);
    Check(read.RobotCount == 8 && read.Speed == 10 && read.Fps == 15 && read.BinCapacity == 1 && read.BgColor == "#1E1E2E" && read.BgImage == "" && read.Language == "tr", "Invalid ranges and null strings are normalized");
    read.Speed = float.NaN;
    read.FadeSeconds = float.PositiveInfinity;
    read.Save(path);
    Check(Settings.Load(path).Speed == 160 && Settings.Current.FadeSeconds == 0, "Nonfinite settings recover before serialization");
    using (var locked = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
    {
        Settings.Load(path);
        Check(Settings.LastLoadWarning is not null, "Locked settings returns a warning");
        bool failed = false;
        try { new Settings().Save(path); } catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { failed = true; }
        Check(failed, "Save failure propagates to caller");
    }
    Check(Directory.GetFiles(directory, "*.tmp").Length == 0, "Failed save cleans temporary files");
    foreach (var language in new[] { "tr", "en" })
    {
        new Settings { Language = language }.Save(path);
        Check(L.T("Türkçe", "English") == (language == "tr" ? "Türkçe" : "English"), language + " language selector");
        Check(L.Notifications.Length == 7 && L.Notifications.All(n => !n.Format(86, 30).Contains('{')), language + " notification placeholders");
        Check(new[] { L.Bump, L.Stuck, L.BinFull, L.BinFullHome, L.AfterDump, L.LowBattery, L.Docked, L.DirtyLines }.All(lines => lines.Length > 0 && lines.All(s => !string.IsNullOrWhiteSpace(s))), language + " dialogue groups");
    }
    var robot = new Robot { Pos = new Vector2(128, 128), StateTime = 12 };
    robot.SetState(RobotState.ReturningHome);
    Check(robot.State == RobotState.ReturningHome && robot.StateTime == 0, "Robot state transition resets timer");
    using var bitmap = new Bitmap(256, 256);
    using var g = Graphics.FromImage(bitmap);
    g.Clear(Color.Transparent);
    g.TranslateTransform(2, 3);
    var transform = g.Transform.Elements;
    using (var renderer = new RobotRenderer(loadSprite: false))
    {
        renderer.Draw(g, robot);
        Check(g.Transform.Elements.SequenceEqual(transform), "Robot draw restores graphics transform");
        Check(bitmap.GetPixel(130, 131).A > 0 && bitmap.GetPixel(0, 0).A == 0, "Vector robot draws with transparent exterior");
        renderer.DrawDock(g, new PointF(128, 200), 0, 1);
        Check(g.Transform.Elements.SequenceEqual(transform), "Dock draw restores graphics transform");
    }
    var badSprite = Path.Combine(directory, "invalid.png");
    File.WriteAllText(badSprite, "not an image");
    using (var fallback = new RobotRenderer(spritePaths: [badSprite]))
    {
        fallback.Draw(g, robot);
        Check(fallback.SpritePath is null && fallback.SpriteWarning is not null, "Corrupt sprite falls back to vector");
    }
    var spritePath = Path.Combine(directory, "robot.png");
    using (var sprite = new Bitmap(40, 40))
    {
        using var sg = Graphics.FromImage(sprite);
        sg.Clear(Color.Transparent);
        sg.FillRectangle(Brushes.Magenta, 15, 0, 10, 10);
        sprite.Save(spritePath, ImageFormat.Png);
    }
    using (var external = new RobotRenderer(spritePaths: [badSprite, spritePath]))
    {
        Check(external.SpritePath == spritePath, "Valid sprite follows invalid candidate");
        Check(external.SpriteWarning is null, "Successful sprite fallback clears obsolete warning");
        File.Delete(spritePath);
        Check(!File.Exists(spritePath), "Sprite file is not locked");
        g.ResetTransform();
        g.Clear(Color.Transparent);
        robot.Angle = 0;
        external.Draw(g, robot);
        var pixel = bitmap.GetPixel(180, 128);
        Check(pixel.R > 200 && pixel.B > 200 && pixel.G < 20, "Up-facing sprite rotates to +X for zero angle");
    }
    new Settings { Language = "tr" }.Save(path);
    var output = Path.Combine(directory, "preview.png");
    RenderPreview.Save(output, 20);
    using (var preview = Image.FromFile(output)) Check(preview.Width == 1280 && preview.Height == 800, "Render writes a readable full-size PNG");
    WorldChecks.Run(Check, directory);
    string? artifacts = args.Length == 2 && args[0] == "--artifacts" ? Path.GetFullPath(args[1]) : null;
    if (artifacts is not null) Directory.CreateDirectory(artifacts);
    DemoChecks.Run(Check, artifacts);
    KeyboardChecks.Run(Check, artifacts);
    SaverChecks.Run(Check);
    Console.WriteLine($"All {count} checks passed.");
    return 0;
}
finally
{
    // Only this uniquely-created test directory is removed.
    Directory.Delete(directory, recursive: true);
}
