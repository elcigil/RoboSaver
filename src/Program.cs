using System.Globalization;

namespace RobotSaver;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Settings.Load();
        bool interactive = args.Length == 0 || args[0].Equals("/s", StringComparison.OrdinalIgnoreCase) || args[0].Equals("/play", StringComparison.OrdinalIgnoreCase) || args[0].Equals("/demo", StringComparison.OrdinalIgnoreCase) || args[0].StartsWith("/c", StringComparison.OrdinalIgnoreCase);
        try
        {
            Application.EnableVisualStyles();
            string command = args.Length == 0 ? "/c" : args[0].ToLowerInvariant();
            if (args.Length == 1 && command is "/s" or "/play")
            {
                using var session = new SaverSession(World.CreateForScreens(command == "/play"), command == "/play");
                Application.Run(session);
                return 0;
            }
            if (command == "/p" || command.StartsWith("/p:", StringComparison.Ordinal))
            {
                string handle = command == "/p" && args.Length == 2 ? args[1] : command.StartsWith("/p:", StringComparison.Ordinal) && args.Length == 1 ? command[3..] : "";
                if (!long.TryParse(handle, out long value) || value <= 0) throw new ArgumentException("A valid preview window handle is required.");
                using var preview = new PreviewForm(new IntPtr(value));
                Application.Run(preview);
                return 0;
            }
            if ((command == "/c" && args.Length <= 2) || (command.StartsWith("/c:", StringComparison.Ordinal) && args.Length == 1))
            {
                Application.Run(new ConfigForm());
                return 0;
            }
            if (args.Length >= 2 && args.Length <= 5 && (args[0].Equals("/render", StringComparison.OrdinalIgnoreCase) || args[0].Equals("/artwork", StringComparison.OrdinalIgnoreCase)))
            {
                if (args[0].Equals("/artwork", StringComparison.OrdinalIgnoreCase) && args.Length > 3) throw new ArgumentException("Artwork accepts only an output path and optional time.");
                double seconds = 0;
                if (args.Length >= 3 && (!double.TryParse(args[2], NumberStyles.Float, CultureInfo.InvariantCulture, out seconds) || !double.IsFinite(seconds) || seconds < 0 || seconds > 3600))
                    throw new ArgumentException("Render time must be between 0 and 3600 seconds (decimal separator: '.').");
                var configuration = ReadModes(args, 3);
                if (args[0].Equals("/artwork", StringComparison.OrdinalIgnoreCase)) RenderPreview.Save(args[1], seconds);
                else
                {
                    using var world = World.CreatePreview(new Size(1280, 800), configuration);
                    world.Update(seconds);
                    world.SaveFrame(args[1]);
                }
                return 0;
            }
            if (args.Length <= 3 && args[0].Equals("/demo", StringComparison.OrdinalIgnoreCase))
            {
                Application.EnableVisualStyles();
                Application.Run(new DemoForm(ReadModes(args, 1)));
                return 0;
            }
            throw new ArgumentException("Supported: /demo [Clean|Dirty|Mixed] [Roomba|Spiral|WallFollow|Wander], /render <output.png> [seconds] [mode] [movement], /artwork <output.png> [seconds].");
        }
        catch (Exception ex) when (ex is ArgumentException or IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception or System.Runtime.InteropServices.ExternalException)
        {
            if (interactive) MessageBox.Show(ex.Message, "RobotSaver", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }

    private static Settings ReadModes(string[] args, int start)
    {
        var configuration = Settings.Current.Clone();
        if (args.Length > start)
        {
            if (!Enum.TryParse<CleanMode>(args[start], true, out var mode) || !Enum.IsDefined(mode)) throw new ArgumentException("Invalid cleaning mode.");
            configuration.Mode = mode;
        }
        if (args.Length > start + 1)
        {
            if (!Enum.TryParse<MoveMode>(args[start + 1], true, out var movement) || !Enum.IsDefined(movement)) throw new ArgumentException("Invalid movement mode.");
            configuration.Movement = movement;
        }
        return configuration;
    }
}
