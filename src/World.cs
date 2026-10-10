using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Numerics;

namespace RobotSaver;

public sealed record DirtParticle(Vector2 Position, DirtType Type, float Size, double Born, Color Color);

public sealed class World : IDisposable
{
    public const int MaxParticles = 8000;
    private readonly List<Surface> surfaces;
    private readonly List<Robot> robots = [];
    private readonly List<DirtParticle> dirt = [];
    private readonly Dictionary<Robot, List<Vector2>> routes = [];
    private readonly Dictionary<Robot, (string Text, double Until)> bubbles = [];
    private readonly RobotRenderer renderer = new();
    private readonly Random random;
    private readonly Settings settings;
    private readonly HashSet<Keys> pressed = [];
    private double accumulator, dirtTimer, toastTimer, fadeTimer;
    private (ToastText Text, double Born)? toast;
    private double actionUntil;
    public string? ActionMessage { get; private set; }
    public IReadOnlyList<Surface> Surfaces => surfaces;
    public IReadOnlyList<Robot> Robots => robots;
    public IReadOnlyList<DirtParticle> Particles => dirt;
    public double Time { get; private set; }
    public bool Paused { get; private set; }
    public bool ShowKeys { get; private set; } = true;
    public bool ShowMap { get; private set; }
    public bool DropCrumbs { get; private set; } = true;
    public string? BackgroundWarning { get; private set; }
    public int Collected { get; private set; }
    public int Dumps { get; private set; }
    public int DockVisits { get; private set; }
    public Settings Configuration => settings.Clone();

    private World(List<Surface> surfaces, Settings configuration, int seed)
    {
        this.surfaces = surfaces;
        settings = configuration.Clone();
        settings.Normalize();
        random = new Random(seed);
        try { Reset(); }
        catch { Dispose(); throw; }
    }

    public static World CreatePreview(Size size, Settings? settings = null, int seed = 42)
        => CreateLayout([new Rectangle(Point.Empty, size)], settings, seed);

    public static World CreateDesktopPreview(Size size, Settings? configuration = null)
    {
        var config = configuration ?? Settings.Current;
        var world = CreatePreview(size, config);
        try
        {
            var screen = Screen.PrimaryScreen ?? Screen.AllScreens[0];
            using var g = Graphics.FromImage(world.surfaces[0].Background);
            if (config.Background == BgMode.Desktop)
            {
                using var snapshot = new Bitmap(screen.Bounds.Width, screen.Bounds.Height);
                using (var capture = Graphics.FromImage(snapshot)) capture.CopyFromScreen(screen.Bounds.Location, Point.Empty, snapshot.Size);
                g.DrawImage(snapshot, new Rectangle(Point.Empty, size));
            }
            else if (config.Background == BgMode.Image)
            {
                using var source = Image.FromFile(config.BgImage);
                g.DrawImage(source, new Rectangle(Point.Empty, size));
            }
            return world;
        }
        catch { world.Dispose(); throw; } // Do not silently substitute a black background.
    }

    public static World CreateLayout(IEnumerable<Rectangle> bounds, Settings? configuration = null, int seed = 42)
    {
        var config = (configuration ?? Settings.Current).Clone();
        config.Normalize();
        var surfaces = new List<Surface>();
        try
        {
            foreach (var rectangle in bounds) surfaces.Add(new Surface(rectangle, rectangle, surfaces.Count == 0, ColorTranslator.FromHtml(config.BgColor)));
            if (surfaces.Count == 0) throw new ArgumentException("At least one surface is required.");
        }
        catch { foreach (var surface in surfaces) surface.Dispose(); throw; }
        return new World(surfaces, config, seed);
    }

    public static World CreateForScreens(bool play)
    {
        var config = Settings.Current.Clone();
        config.Normalize();
        var surfaces = new List<Surface>();
        try
        {
            foreach (var screen in Screen.AllScreens)
            {
                var surface = new Surface(screen.Bounds, screen.WorkingArea, screen.Primary, ColorTranslator.FromHtml(config.BgColor));
                surfaces.Add(surface);
                using var g = Graphics.FromImage(surface.Background);
                if (config.Background == BgMode.Desktop) g.CopyFromScreen(screen.Bounds.Location, Point.Empty, screen.Bounds.Size);
                else if (config.Background == BgMode.Image)
                {
                    using var source = Image.FromFile(config.BgImage);
                    g.DrawImage(source, new Rectangle(Point.Empty, surface.Background.Size));
                }
            }
        }
        catch { foreach (var surface in surfaces) surface.Dispose(); throw; }
        var world = new World(surfaces, config, Environment.TickCount);
        if (play) world.robots[0].ManualControl = true;
        return world;
    }

    public void Reset()
    {
        random.Next(); // Random stream remains local to this world; reset need not duplicate the scene.
        foreach (var surface in surfaces) surface.ClearDirt();
        robots.Clear(); dirt.Clear(); routes.Clear(); bubbles.Clear(); pressed.Clear();
        Time = accumulator = dirtTimer = fadeTimer = 0;
        toastTimer = 8;
        toast = null;
        ActionMessage = null;
        Paused = false;
        Collected = Dumps = DockVisits = 0;
        float radius = Math.Min(settings.RobotSize / 2f, surfaces.Max(s => Math.Min(s.Bounds.Width, s.Bounds.Height)) / 5f);
        if (radius < 3) throw new ArgumentException("Preview is too small for a robot.");
        // Pack nonoverlapping homes. Smaller previews can display fewer robots than configured.
        foreach (var surface in surfaces)
        {
            for (float y = surface.Bounds.Top + radius + 14; y <= surface.Bounds.Bottom - radius - 4; y += radius * 2 + 20)
            for (float x = surface.Bounds.Left + radius + 14; x <= surface.Bounds.Right - radius - 4; x += radius * 2 + 20)
            {
                if (robots.Count >= settings.RobotCount) break;
                var position = new Vector2(x, y);
                if (!IsValidPosition(position, radius) || robots.Any(r => Vector2.Distance(r.Pos, position) < r.Radius + radius + 4)) continue;
                var robot = new Robot { Pos = position, LastPos = position, Dock = position, Radius = radius, Angle = .6f + robots.Count * .65f, SpiralR = radius };
                robots.Add(robot);
                routes.Add(robot, [position]);
            }
        }
        if (robots.Count == 0) throw new ArgumentException("No surface has enough room for a robot.");
        int initial = Math.Min(3000, (int)(surfaces.Sum(s => (double)s.Bounds.Width * s.Bounds.Height) / 100000 * settings.DirtDensity * 2));
        for (int i = 0; i < initial; i++) Sprinkle();
        Say(robots[0], L.T("Temizlik mesaisi başladı!", "Time to clean!"));
    }

    // Exact circular footprint against a union of axis-aligned screens. Five probes alone
    // miss narrow gaps and concave corners, so inspect vertical slabs at every screen edge.
    public bool IsValidPosition(Vector2 p, float radius)
    {
        if (!float.IsFinite(p.X) || !float.IsFinite(p.Y) || !float.IsFinite(radius) || radius < 0) return false;
        if (radius == 0) return surfaces.Any(s => p.X >= s.Bounds.Left && p.X <= s.Bounds.Right && p.Y >= s.Bounds.Top && p.Y <= s.Bounds.Bottom);
        if (surfaces.Any(s => p.X - radius >= s.Bounds.Left && p.X + radius <= s.Bounds.Right && p.Y - radius >= s.Bounds.Top && p.Y + radius <= s.Bounds.Bottom)) return true;
        var cuts = new List<float> { p.X - radius, p.X + radius };
        foreach (var surface in surfaces)
        {
            if (surface.Bounds.Left > p.X - radius && surface.Bounds.Left < p.X + radius) cuts.Add(surface.Bounds.Left);
            if (surface.Bounds.Right > p.X - radius && surface.Bounds.Right < p.X + radius) cuts.Add(surface.Bounds.Right);
        }
        cuts.Sort();
        for (int i = 1; i < cuts.Count; i++)
        {
            float left = cuts[i - 1], right = cuts[i];
            if (right - left < .0001f) continue;
            float middle = (left + right) / 2;
            float closest = Math.Clamp(p.X, left, right);
            float height = MathF.Sqrt(Math.Max(0, radius * radius - (closest - p.X) * (closest - p.X)));
            float covered = p.Y - height;
            foreach (var rectangle in surfaces.Where(s => middle >= s.Bounds.Left && middle < s.Bounds.Right).Select(s => s.Bounds).OrderBy(b => b.Top))
            {
                if (rectangle.Top > covered + .001f) break;
                covered = Math.Max(covered, rectangle.Bottom);
            }
            if (covered < p.Y + height - .001f) return false;
        }
        return true;
    }

    public bool AddDirt(Vector2 position, DirtType type, float size = 4)
    {
        if (dirt.Count >= MaxParticles || !float.IsFinite(position.X) || !float.IsFinite(position.Y) || !float.IsFinite(size) || size <= 0 || !surfaces.Any(s => position.X >= s.Bounds.Left && position.X < s.Bounds.Right && position.Y >= s.Bounds.Top && position.Y < s.Bounds.Bottom)) return false;
        size = Math.Min(size, 80);
        Color color = type switch
        {
            DirtType.Dust => Color.FromArgb(160, 182, 169, 147),
            DirtType.Mud => Color.FromArgb(210, 113, 73, 41),
            DirtType.Confetti => new[] { Color.Coral, Color.Gold, Color.Turquoise, Color.MediumPurple }[random.Next(4)],
            _ => Color.FromArgb(255, 211 + random.Next(30), 156 + random.Next(30), 81)
        };
        var particle = new DirtParticle(position, type, size, Time, color);
        dirt.Add(particle);
        PaintParticle(particle);
        return true;
    }

    private void PaintParticle(DirtParticle particle)
    {
        var (position, type, size, born, color) = particle;
        if (settings.FadeSeconds > 0) color = Color.FromArgb(Math.Max(0, color.A - (int)(255 * (Time - born) / settings.FadeSeconds)), color);
        using var brush = new SolidBrush(color);
        foreach (var surface in surfaces)
        {
            if (!surface.Bounds.IntersectsWith(Rectangle.Ceiling(new RectangleF(position.X - size, position.Y - size, size * 2, size * 2)))) continue;
            var g = surface.DirtGraphics;
            float x = position.X - surface.Bounds.X, y = position.Y - surface.Bounds.Y;
            if (type == DirtType.Confetti) g.FillRectangle(brush, x - size / 2, y - size / 4, size, size / 2);
            else g.FillEllipse(brush, x - size / 2, y - size / 2, size, type == DirtType.Mud ? size * .8f : size);
        }
    }

    private void Sprinkle()
    {
        var surface = surfaces[random.Next(surfaces.Count)];
        AddDirt(new Vector2(surface.Bounds.Left + random.Next(surface.Bounds.Width), surface.Bounds.Top + random.Next(surface.Bounds.Height)), settings.Dirt, settings.Dirt == DirtType.Mud ? 10 + random.Next(16) : 2 + random.Next(5));
    }

    public void Update(double seconds)
    {
        if (!double.IsFinite(seconds) || seconds < 0 || seconds > 3600) throw new ArgumentOutOfRangeException(nameof(seconds));
        if (Paused) return;
        accumulator += seconds;
        const double step = 1.0 / 120;
        while (accumulator + 1e-10 >= step)
        {
            Tick((float)step);
            accumulator -= step;
        }
    }

    private void Tick(float dt)
    {
        Time += dt;
        dirtTimer += dt;
        fadeTimer += dt;
        if (dirtTimer >= .25)
        {
            dirtTimer = 0;
            if (DropCrumbs && random.NextDouble() < settings.DirtDensity / 100.0) Sprinkle();
        }
        if (fadeTimer >= .1)
        {
            if (settings.FadeSeconds > 0)
            {
                foreach (var surface in surfaces) surface.Fade((float)fadeTimer, settings.FadeSeconds);
                dirt.RemoveAll(d => Time - d.Born >= settings.FadeSeconds);
            }
            fadeTimer = 0;
        }
        if (settings.Notifications && Time >= toastTimer)
        {
            toast = (L.Notifications[random.Next(L.Notifications.Length)], Time);
            toastTimer = Time + 12;
        }
        foreach (var robot in robots) UpdateRobot(robot, dt);
    }

    private void UpdateRobot(Robot robot, float dt)
    {
        robot.StateTime += dt;
        robot.BumpFlash = Math.Max(0, robot.BumpFlash - dt);
        robot.SayCooldown = Math.Max(0, robot.SayCooldown - dt);
        robot.LastPos = robot.Pos;
        robot.BrushAngle = (robot.BrushAngle + dt * 14) % MathF.Tau;
        if (robot.State == RobotState.Docked)
        {
            robot.Battery = Math.Min(1, robot.Battery + dt / 8);
            robot.Bin = Math.Max(0, robot.Bin - settings.BinCapacity * dt / 2);
            if (robot.Battery >= 1 && robot.Bin <= 0 && robot.StateTime > 2)
            {
                robot.Angle = .6f;
                robot.SetState(RobotState.Leaving);
            }
            return;
        }
        // Returning robots retain reserve power, so long return routes cannot strand them.
        if (robot.State != RobotState.ReturningHome) robot.Battery = Math.Max(.05f, robot.Battery - dt / (settings.BatteryMinutes * 60));
        if (robot.Battery <= .15f && robot.State != RobotState.ReturningHome) GoHome(robot);
        if (robot.State == RobotState.ReturningHome) { ReturnHome(robot, dt); return; }
        if (robot.State == RobotState.Stuck)
        {
            if (robot.StateTime > 2.5f) { robot.Angle += 1.3f; robot.SetState(RobotState.Roaming); }
            return;
        }
        if (robot.State == RobotState.Dumping)
        {
            robot.Angle += dt * 5;
            if ((int)(robot.StateTime / dt) % 6 == 0) Scatter(robot, 3);
            robot.Bin = Math.Max(0, robot.Bin - settings.BinCapacity * dt / 2);
            if (robot.StateTime >= 2) { robot.Bin = 0; robot.SetState(RobotState.Roaming); Say(robot, L.AfterDump[random.Next(L.AfterDump.Length)]); }
            return;
        }
        if (robot.State == RobotState.Leaving && robot.StateTime > 1) robot.SetState(RobotState.Roaming);
        if (robot.State == RobotState.Roaming && settings.StuckEvents && random.NextDouble() < dt / 100)
        { robot.SetState(RobotState.Stuck); Say(robot, L.Stuck[random.Next(L.Stuck.Length)]); return; }

        float speed = settings.Speed;
        bool moving = true;
        if (robot.ManualControl)
        {
            Vector2 direction = new((Down(Keys.D) || Down(Keys.Right) ? 1 : 0) - (Down(Keys.A) || Down(Keys.Left) ? 1 : 0), (Down(Keys.S) || Down(Keys.Down) ? 1 : 0) - (Down(Keys.W) || Down(Keys.Up) ? 1 : 0));
            moving = direction.LengthSquared() > 0;
            if (moving) robot.Angle = MathF.Atan2(direction.Y, direction.X);
            if (Down(Keys.ShiftKey)) speed *= 2;
        }
        else if (robot.State != RobotState.Leaving)
        {
            switch (settings.Movement)
            {
                case MoveMode.Spiral:
                    robot.SpiralR = Math.Min(600, robot.SpiralR + dt * 7);
                    robot.Angle += speed / robot.SpiralR * dt;
                    break;
                case MoveMode.Wander:
                    robot.WanderPhase += dt;
                    robot.Angle += MathF.Sin(robot.WanderPhase * 1.7f) * dt * 1.8f;
                    break;
                case MoveMode.WallFollow:
                    var right = new Vector2(MathF.Cos(robot.Angle + MathF.PI / 2), MathF.Sin(robot.Angle + MathF.PI / 2));
                    bool wall = !IsValidPosition(robot.Pos + right * (robot.Radius + 16), robot.Radius);
                    if (wall) robot.AvoidTime = 1;
                    else if (robot.AvoidTime > 0) { robot.Angle += dt * 2; robot.AvoidTime -= dt; }
                    break;
            }
        }
        if (moving)
        {
            var next = robot.Pos + new Vector2(MathF.Cos(robot.Angle), MathF.Sin(robot.Angle)) * speed * dt;
            if (CanMove(robot, next))
            {
                robot.Pos = next;
                routes[robot].Add(next);
                if (routes[robot].Count > 100000) GoHome(robot);
            }
            else
            {
                robot.BumpFlash = .2f;
                if (!robot.ManualControl)
                {
                    robot.Angle += settings.Movement == MoveMode.WallFollow ? -MathF.PI / 2 : 1.3f + (float)random.NextDouble() * 2;
                    robot.SpiralR = robot.Radius;
                }
                if (robot.SayCooldown <= 0) Say(robot, L.Bump[random.Next(L.Bump.Length)]);
            }
        }
        if (settings.Mode == CleanMode.Dirty)
        {
            if (DropCrumbs && Vector2.DistanceSquared(robot.LastPos, robot.Pos) > 0)
            {
                robot.StraightTime += Vector2.Distance(robot.LastPos, robot.Pos);
                if (robot.StraightTime >= 8) { LeaveTracks(robot); robot.StraightTime = 0; }
            }
        }
        else Clean(robot);
        robot.Angle = MathF.IEEERemainder(robot.Angle, MathF.Tau);
    }

    private bool CanMove(Robot robot, Vector2 next)
        => CanTravel(robot, robot.Pos, next);

    private bool CanTravel(Robot robot, Vector2 start, Vector2 next)
    {
        // Check the swept path too, including small previews at maximum turbo speed.
        int steps = Math.Max(1, (int)MathF.Ceiling(Vector2.Distance(start, next) / Math.Max(1, robot.Radius / 2)));
        for (int i = 1; i <= steps; i++)
            if (!IsValidPosition(Vector2.Lerp(start, next, (float)i / steps), robot.Radius)) return false;
        var segment = next - start;
        foreach (var other in robots)
        {
            if (ReferenceEquals(other, robot)) continue;
            float fraction = segment.LengthSquared() == 0 ? 0 : Math.Clamp(Vector2.Dot(other.Pos - start, segment) / segment.LengthSquared(), 0, 1);
            if (Vector2.DistanceSquared(start + segment * fraction, other.Pos) < MathF.Pow(robot.Radius + other.Radius + 1, 2)) return false;
        }
        return true;
    }

    private void Clean(Robot robot)
    {
        if (robot.Bin >= settings.BinCapacity) { FullBin(robot); return; }
        float radius = robot.Radius * .85f;
        int available = (int)(settings.BinCapacity - robot.Bin);
        int removed = 0;
        bool leftBehind = false;
        for (int i = dirt.Count - 1; i >= 0; i--)
        {
            if (Vector2.DistanceSquared(dirt[i].Position, robot.Pos) > radius * radius) continue;
            if (removed >= available) { leftBehind = true; continue; }
            dirt.RemoveAt(i); removed++;
        }
        if (leftBehind)
        {
            // A partial pickup must leave uncollected crumbs visible, including overlapping crumbs.
            foreach (var surface in surfaces) surface.ClearDirt();
            foreach (var particle in dirt) PaintParticle(particle);
        }
        else foreach (var surface in surfaces) surface.Erase(new PointF(robot.Pos.X, robot.Pos.Y), radius);
        robot.Bin += removed;
        Collected += removed;
        if (robot.Bin >= settings.BinCapacity) FullBin(robot);
    }

    private void FullBin(Robot robot)
    {
        if (settings.Mode == CleanMode.Mixed)
        {
            robot.SetState(RobotState.Dumping); Dumps++;
            Say(robot, L.BinFull[random.Next(L.BinFull.Length)]);
        }
        else GoHome(robot);
    }

    private void LeaveTracks(Robot robot)
    {
        var forward = new Vector2(MathF.Cos(robot.Angle), MathF.Sin(robot.Angle));
        var side = new Vector2(-forward.Y, forward.X) * robot.Radius * .65f;
        var behind = robot.Pos - forward * robot.Radius * .8f;
        AddDirt(behind + side, DirtType.Mud, 9);
        AddDirt(behind - side, DirtType.Mud, 9);
        AddDirt(behind, DirtType.Mud, 18);
        AddDirt(behind + new Vector2(random.Next(-14, 15), random.Next(-14, 15)), settings.Dirt, 4);
    }

    private void Scatter(Robot robot, int count)
    {
        for (int i = 0; i < count; i++)
        {
            float angle = (float)random.NextDouble() * MathF.Tau;
            float radius = robot.Radius * (1.1f + (float)random.NextDouble());
            AddDirt(robot.Pos + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius, settings.Dirt, 3 + random.Next(6));
        }
    }

    public void GoHome(Robot robot)
    {
        if (!routes.ContainsKey(robot)) throw new ArgumentException("Robot is not in this world.");
        if (robot.State is RobotState.Docked or RobotState.ReturningHome) return;
        robot.SetState(RobotState.ReturningHome);
        robot.AvoidTime = 0;
        Say(robot, robot.Battery <= .15f ? L.LowBattery[0] : L.BinFullHome[0]);
    }

    private void ReturnHome(Robot robot, float dt)
    {
        var route = routes[robot];
        float budget = settings.Speed * 2 * dt;
        while (route.Count > 0 && budget > 0)
        {
            var target = route[^1];
            var delta = target - robot.Pos;
            float distance = delta.Length();
            if (distance < .001f) { route.RemoveAt(route.Count - 1); continue; }
            float step = Math.Min(distance, budget);
            var next = robot.Pos + delta / distance * step;
            if (!CanMove(robot, next))
            {
                robot.AvoidTime += dt;
                if (robot.AvoidTime >= .5f)
                {
                    var alternative = FindHomeRoute(robot);
                    if (alternative is not null) routes[robot] = alternative;
                    else if (robot.SayCooldown <= 0) Say(robot, L.T("Yol kapalı, geçiş bekliyorum.", "Path blocked. Waiting for room."));
                    robot.AvoidTime = 0;
                }
                return;
            }
            robot.AvoidTime = 0;
            robot.Angle = MathF.Atan2(delta.Y, delta.X);
            robot.Pos = next;
            budget -= step;
            if (distance <= step + .001f) route.RemoveAt(route.Count - 1);
        }
        if (route.Count == 0)
        {
            if (Vector2.DistanceSquared(robot.Pos, robot.Dock) > .01f)
            {
                // A temporary yielding route is not the charging dock.
                route.Add(robot.Dock);
                return;
            }
            robot.SetState(RobotState.Docked);
            DockVisits++;
            route.Add(robot.Pos);
            Say(robot, L.Docked[0]);
        }
    }

    // Replan only after an obstruction. Original breadcrumbs remain the reliable
    // fallback for narrow or disconnected screen layouts with no grid route.
    private List<Vector2>? FindHomeRoute(Robot robot)
    {
        if (CanTravel(robot, robot.Pos, robot.Dock)) return [robot.Dock];
        bool homeBlocked = robots.Any(other => other != robot && Vector2.Distance(other.Pos, robot.Dock) < other.Radius + robot.Radius + 1);
        bool obstructsPeer = robots.Any(other => other != robot && other.State == RobotState.ReturningHome &&
            (Vector2.Distance(robot.Pos, other.Dock) < other.Radius + robot.Radius + 10 || Vector2.Distance(robot.Pos, other.Pos) < other.Radius + robot.Radius + 10));
        if (homeBlocked && obstructsPeer)
        {
            // Move aside before planning to an occupied goal, breaking reciprocal waits.
            for (int i = 0; i < 8; i++)
            {
                float angle = MathF.PI / 2 + i * MathF.PI / 4;
                var holding = robot.Pos + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * robot.Radius * 3;
                if (CanTravel(robot, robot.Pos, holding)) return [holding];
            }
        }
        if (homeBlocked) return null;
        var bounds = surfaces.Select(s => s.Bounds).Aggregate(Rectangle.Union);
        float cell = Math.Clamp(robot.Radius, 12, 40);
        int width = (int)MathF.Ceiling(bounds.Width / cell) + 1;
        int height = (int)MathF.Ceiling(bounds.Height / cell) + 1;
        var frontier = new PriorityQueue<(int X, int Y), float>();
        var cost = new Dictionary<(int X, int Y), float>();
        var parent = new Dictionary<(int X, int Y), (int X, int Y)?>();
        var closed = new HashSet<(int X, int Y)>();
        Vector2 Point((int X, int Y) node) => new(bounds.Left + node.X * cell, bounds.Top + node.Y * cell);
        var start = ((int)MathF.Round((robot.Pos.X - bounds.Left) / cell), (int)MathF.Round((robot.Pos.Y - bounds.Top) / cell));
        for (int dx = -1; dx <= 1; dx++)
        for (int dy = -1; dy <= 1; dy++)
        {
            var node = (start.Item1 + dx, start.Item2 + dy);
            if (!CanTravel(robot, robot.Pos, Point(node))) continue;
            float value = Vector2.Distance(robot.Pos, Point(node));
            cost[node] = value; parent[node] = null;
            frontier.Enqueue(node, value + Vector2.Distance(Point(node), robot.Dock));
        }
        while (frontier.TryDequeue(out var current, out _) && closed.Count < 12000)
        {
            if (!closed.Add(current)) continue;
            var position = Point(current);
            if (Vector2.Distance(position, robot.Dock) < cell * 2 && CanTravel(robot, position, robot.Dock))
            {
                var route = new List<Vector2> { robot.Dock };
                (int X, int Y)? node = current;
                while (node is { } value) { route.Add(Point(value)); node = parent[value]; }
                return route;
            }
            for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                var next = (X: current.X + dx, Y: current.Y + dy);
                if (next.X < 0 || next.Y < 0 || next.X >= width || next.Y >= height || closed.Contains(next)) continue;
                float value = cost[current] + cell * (dx != 0 && dy != 0 ? 1.414214f : 1);
                if (cost.TryGetValue(next, out var old) && old <= value) continue;
                if (!CanTravel(robot, position, Point(next))) continue;
                cost[next] = value; parent[next] = current;
                frontier.Enqueue(next, value + Vector2.Distance(Point(next), robot.Dock));
            }
        }
        return null;
    }

    private bool Down(Keys key) => pressed.Contains(key);
    public void ReleaseKeys() => pressed.Clear();
    public void KeyUp(Keys key) => pressed.Remove(key);
    public void KeyDown(Keys key)
    {
        if (!pressed.Add(key)) return;
        var robot = robots[0];
        switch (key)
        {
            case Keys.Space:
                Paused = !Paused;
                Report(Paused ? L.T("Duraklatıldı.", "Paused.") : L.T("Simülasyon devam ediyor.", "Simulation resumed.")); break;
            case Keys.Enter:
                robot.ManualControl = !robot.ManualControl;
                Report(robot.ManualControl ? L.T("Manuel sürüş açık.", "Manual driving enabled.") : L.T("Otopilot açık.", "Autopilot enabled.")); break;
            case Keys.H:
                GoHome(robot);
                Report(robot.State == RobotState.Docked ? L.T("Robot zaten yuvada.", "Robot is already docked.") : L.T("Robot yuvaya dönüyor.", "Robot is returning home.")); break;
            case Keys.C:
                DropCrumbs = !DropCrumbs;
                Report(DropCrumbs ? L.T("Yeni kir ve iz bırakma açık.", "New dirt and trails enabled.") : L.T("Yeni kir ve iz bırakma kapalı.", "New dirt and trails disabled.")); break;
            case Keys.M:
                ShowMap = !ShowMap;
                Report(ShowMap ? L.T("Harita açıldı.", "Map shown.") : L.T("Harita gizlendi.", "Map hidden.")); break;
            case Keys.F1:
                ShowKeys = !ShowKeys;
                Report(ShowKeys ? L.T("Tuş yardımı açıldı.", "Key help shown.") : L.T("Tuş yardımı gizlendi. F1 ile açabilirsiniz.", "Key help hidden. Press F1 to show it.")); break;
            case Keys.E:
                bool hadDirt = robot.Bin > 0;
                robot.Bin = 0;
                if (robot.State == RobotState.Dumping) robot.SetState(RobotState.Roaming);
                bubbles.Remove(robot);
                Report(hadDirt ? L.T("Hazne boşaltıldı. Yerdeki kirler değişmedi.", "Bin emptied. Dirt on the floor is unchanged.") : L.T("Hazne zaten boş. E yalnızca hazneyi boşaltır.", "Bin is already empty. E only empties the bin."));
                break;
            case Keys.K:
                Scatter(robot, 60);
                for (int i = 0; i < 40; i++) AddDirt(robot.Pos + new Vector2(random.Next(-140, 141), random.Next(-140, 141)), DirtType.Confetti, 6);
                Report(L.T("Pasta ve konfeti!", "Cake and confetti!"));
                break;
            case Keys.R: Reset(); pressed.Add(key); Report(L.T("Simülasyon sıfırlandı.", "Simulation reset.")); break;
            case Keys.N: Report(L.T("Ses bu sürümde henüz hazır değil.", "Sound is not available in this version yet.")); break;
        }
        if (key is Keys.W or Keys.A or Keys.S or Keys.D or Keys.Up or Keys.Left or Keys.Down or Keys.Right) robot.ManualControl = true;
    }

    private void Report(string message)
    {
        ActionMessage = message;
        actionUntil = Time + 4;
    }

    private void Say(Robot robot, string text)
    {
        if (settings.Bubbles) bubbles[robot] = (text, Time + 3);
        robot.SayCooldown = 4;
    }

    public void Draw(Graphics g, Surface surface, bool controls = true)
    {
        g.DrawImageUnscaled(surface.Background, 0, 0);
        g.DrawImageUnscaled(surface.Dirt, 0, 0);
        var saved = g.Save();
        try
        {
            g.TranslateTransform(-surface.Bounds.X, -surface.Bounds.Y);
            foreach (var robot in robots)
            {
                if (settings.ShowDock) renderer.DrawDock(g, new PointF(robot.Dock.X, robot.Dock.Y + robot.Radius * .7f), -MathF.PI / 2, Time);
                var pose = g.Save();
                try
                {
                    if (robot.State == RobotState.Stuck) g.TranslateTransform(MathF.Sin(robot.StateTime * 70) * 2, MathF.Cos(robot.StateTime * 61) * 2);
                    renderer.Draw(g, robot);
                }
                finally { g.Restore(pose); }
            }
        }
        finally { g.Restore(saved); }
        using var font = new Font("Segoe UI", 14, FontStyle.Regular, GraphicsUnit.Pixel);
        using var bold = new Font("Segoe UI", 16, FontStyle.Bold, GraphicsUnit.Pixel);
        using var text = new SolidBrush(Color.FromArgb(231, 235, 247));
        using var panel = new SolidBrush(Color.FromArgb(225, 28, 32, 46));
        using var accent = new SolidBrush(Color.FromArgb(180, 165, 255));
        foreach (var robot in robots)
        {
            if (!bubbles.TryGetValue(robot, out var bubble) || bubble.Until < Time || !surface.Bounds.Contains((int)robot.Pos.X, (int)robot.Pos.Y)) continue;
            var size = g.MeasureString(bubble.Text, font);
            float x = Math.Clamp(robot.Pos.X - surface.Bounds.X - size.Width / 2, 4, Math.Max(4, surface.Bounds.Width - size.Width - 20));
            float y = Math.Max(4, robot.Pos.Y - surface.Bounds.Y - robot.Radius - 44);
            if (surface.Primary && controls && x < 500 && y < 84) y = 88;
            g.FillRoundedRectangle(panel, new RectangleF(x, y, size.Width + 16, 32), 10);
            g.DrawString(bubble.Text, font, text, x + 8, y + 6);
        }
        if (surface.Primary && controls)
        {
            g.FillRoundedRectangle(panel, new RectangleF(20, 20, 480, 62), 12);
            g.DrawString("ROBOT / SAVER", bold, accent, 34, 28);
            g.DrawString($"{settings.Mode}  /  {settings.Movement}   ·   {Time:0.0}s   ·   " + (Paused ? L.T("DURAKLATILDI", "PAUSED") : L.T("Simülasyon", "Simulation")), font, text, 34, 53);
            var robot = robots[0];
            float y = surface.Bounds.Height - (ShowKeys ? 132 : 57);
            if (ActionMessage is not null && Time <= actionUntil)
            {
                g.FillRoundedRectangle(panel, new RectangleF(20, y - 44, Math.Min(650, surface.Bounds.Width - 40), 36), 10);
                g.DrawString(ActionMessage, font, accent, 34, y - 36);
            }
            g.FillRoundedRectangle(panel, new RectangleF(20, y, Math.Min(650, surface.Bounds.Width - 40), ShowKeys ? 112 : 40), 12);
            g.DrawString($"{(robot.ManualControl ? L.T("MANUEL", "MANUAL") : L.T("OTOPİLOT", "AUTOPILOT"))}   |   {L.T("Pil", "Battery")} {robot.Battery:P0}   |   {L.T("Hazne", "Bin")} {robot.Bin:0}/{settings.BinCapacity}   |   {robot.State}", bold, text, 34, y + 10);
            if (ShowKeys)
            {
                g.DrawString(L.T("WASD / Oklar: sür   Shift: turbo   Enter: otopilot   H: eve", "WASD / Arrows: drive   Shift: turbo   Enter: autopilot   H: home"), font, text, 34, y + 39);
                g.DrawString(L.T("Space: duraklat   C: kırıntı   E: hazne boşalt   K: pasta", "Space: pause   C: crumbs   E: empty bin   K: cake"), font, text, 34, y + 62);
                g.DrawString(L.T("M: harita   R: sıfırla   F1: tuşları gizle   Esc: çıkış", "M: map   R: reset   F1: hide keys   Esc: exit"), font, text, 34, y + 85);
            }
        }
        if (surface.Primary && toast is { } notification && Time - notification.Born < 6)
        {
            float slide = (float)Math.Clamp((Time - notification.Born) * 3, 0, 1);
            float x = surface.Bounds.Width - 380 * slide;
            float y = Math.Max(90, surface.Bounds.Height - 250);
            g.FillRoundedRectangle(panel, new RectangleF(x, y, 360, 100), 14);
            g.DrawString(L.T("RobotSaver · Şaka bildirimi", "RobotSaver · Pretend notification"), font, accent, x + 14, y + 10);
            g.DrawString(notification.Text.Title, bold, text, x + 14, y + 34);
            g.DrawString(notification.Text.Format((int)(robots[0].Battery * 100), (int)(robots[0].Bin / settings.BinCapacity * 100)), font, text, new RectangleF(x + 14, y + 59, 330, 40));
        }
        if (ShowMap && surface.Primary) DrawMap(g, surface, panel, accent);
    }

    private void DrawMap(Graphics g, Surface surface, Brush panel, Brush accent)
    {
        var bounds = surfaces.Select(s => s.Bounds).Aggregate(Rectangle.Union);
        float scale = Math.Min(180f / bounds.Width, 100f / bounds.Height);
        float x = surface.Bounds.Width - 220, y = 30;
        g.FillRoundedRectangle(panel, new RectangleF(x - 10, y - 10, 200, 120), 10);
        using var pen = new Pen(Color.SlateGray);
        foreach (var item in surfaces) g.DrawRectangle(pen, x + (item.Bounds.X - bounds.X) * scale, y + (item.Bounds.Y - bounds.Y) * scale, item.Bounds.Width * scale, item.Bounds.Height * scale);
        foreach (var robot in robots) g.FillEllipse(accent, x + (robot.Pos.X - bounds.X) * scale - 3, y + (robot.Pos.Y - bounds.Y) * scale - 3, 6, 6);
    }

    public void SaveFrame(string path)
    {
        var surface = surfaces.FirstOrDefault(s => s.Primary) ?? surfaces[0];
        using var frame = new Bitmap(surface.Bounds.Width, surface.Bounds.Height);
        using (var g = Graphics.FromImage(frame)) Draw(g, surface);
        frame.Save(Path.GetFullPath(path), ImageFormat.Png);
    }

    public void Dispose()
    {
        renderer.Dispose();
        foreach (var surface in surfaces) surface.Dispose();
    }
}
