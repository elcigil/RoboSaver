using System.Text.Json;
using System.Text.Json.Serialization;

namespace RobotSaver;

public enum CleanMode { Clean, Dirty, Mixed }
public enum MoveMode { Roomba, Spiral, WallFollow, Wander }
public enum DirtType { Dust, Crumbs, Mud, Confetti }
public enum BgMode { Desktop, SolidColor, Image }

public sealed class Settings
{
    public int RobotCount { get; set; } = 1;
    public int RobotSize { get; set; } = 120;
    public float Speed { get; set; } = 160;
    public CleanMode Mode { get; set; } = CleanMode.Mixed;
    public MoveMode Movement { get; set; } = MoveMode.Roomba;
    public int DirtDensity { get; set; } = 50;
    public DirtType Dirt { get; set; } = DirtType.Crumbs;
    public float FadeSeconds { get; set; }
    public int BinCapacity { get; set; } = 300;
    public float BatteryMinutes { get; set; } = 20;
    public BgMode Background { get; set; } = BgMode.Desktop;
    public string BgColor { get; set; } = "#1E1E2E";
    public string BgImage { get; set; } = "";
    public bool Bubbles { get; set; } = true;
    public bool Notifications { get; set; } = true;
    public bool StuckEvents { get; set; } = true;
    public bool Sound { get; set; }
    public bool ShowDock { get; set; } = true;
    public string Language { get; set; } = "tr";
    public int Fps { get; set; } = 60;
    public int MouseSensitivity { get; set; } = 10;
    public int TimeoutMinutes { get; set; } = 5;

    [JsonIgnore] public static Settings Current { get; private set; } = new();
    [JsonIgnore] public static string DataDirectory => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RobotSaver");
    [JsonIgnore] public static string FilePath => Path.Combine(DataDirectory, "settings.json");
    [JsonIgnore] public static string? LastLoadWarning { get; private set; }
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(allowIntegerValues: false) }
    };

    // An explicit path lets diagnostics use temporary files without changing user settings.
    public static Settings Load(string? path = null)
    {
        LastLoadWarning = null;
        try
        {
            using var stream = File.OpenRead(path ?? FilePath);
            Current = JsonSerializer.Deserialize<Settings>(stream, JsonOptions) ?? throw new JsonException("Settings cannot be null.");
            Current.Normalize();
        }
        catch (FileNotFoundException) { Current = new(); }
        catch (DirectoryNotFoundException) { Current = new(); }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            Current = new();
            LastLoadWarning = ex.Message; // Preserve the original file; never silently overwrite it on load.
        }
        return Current;
    }

    public void Save(string? path = null)
    {
        var validated = Clone();
        validated.Normalize();
        var destination = Path.GetFullPath(path ?? FilePath);
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        var temporary = destination + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                JsonSerializer.Serialize(stream, validated, JsonOptions);
                stream.Flush(flushToDisk: true);
            }
            File.Move(temporary, destination, overwrite: true);
            Current = validated;
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }

    public Settings Clone() => (Settings)MemberwiseClone();

    public void Normalize()
    {
        RobotCount = Math.Clamp(RobotCount, 1, 8);
        RobotSize = Math.Clamp(RobotSize, 40, 400);
        Speed = ClampFinite(Speed, 10, 1000, 160);
        DirtDensity = Math.Clamp(DirtDensity, 0, 100);
        FadeSeconds = ClampFinite(FadeSeconds, 0, 3600, 0);
        BinCapacity = Math.Clamp(BinCapacity, 1, 10000);
        BatteryMinutes = ClampFinite(BatteryMinutes, 0.1f, 120, 20);
        Fps = Math.Clamp(Fps, 15, 144);
        MouseSensitivity = Math.Clamp(MouseSensitivity, 1, 200);
        TimeoutMinutes = Math.Clamp(TimeoutMinutes, 1, 240);
        if (!Enum.IsDefined(Mode)) Mode = CleanMode.Mixed;
        if (!Enum.IsDefined(Movement)) Movement = MoveMode.Roomba;
        if (!Enum.IsDefined(Dirt)) Dirt = DirtType.Crumbs;
        if (!Enum.IsDefined(Background)) Background = BgMode.Desktop;
        Language = string.Equals(Language, "en", StringComparison.OrdinalIgnoreCase) ? "en" : "tr";
        BgImage ??= "";
        if (BgColor is not { Length: 7 } || BgColor[0] != '#' || !BgColor.AsSpan(1).ToString().All(Uri.IsHexDigit)) BgColor = "#1E1E2E";
    }

    private static float ClampFinite(float value, float min, float max, float fallback) => float.IsFinite(value) ? Math.Clamp(value, min, max) : fallback;
}
