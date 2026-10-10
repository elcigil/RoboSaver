using System.Drawing.Imaging;
using System.Numerics;
using RobotSaver;

internal static class WorldChecks
{
    public static void Run(Action<bool, string> check, string directory)
    {
        Settings Config(CleanMode mode = CleanMode.Clean, MoveMode movement = MoveMode.Roomba) => new()
        {
            Mode = mode, Movement = movement, StuckEvents = false, DirtDensity = 0,
            Bubbles = false, Notifications = false, RobotSize = 80, Speed = 140
        };
        using (var world = World.CreatePreview(new Size(640, 480), Config()))
        {
            var robot = world.Robots[0];
            robot.ManualControl = true;
            world.AddDirt(robot.Pos, DirtType.Crumbs, 8);
            world.Update(.1);
            check(world.Particles.Count == 0 && robot.Bin == 1 && world.Collected == 1, "Cleaning removes dirt and fills bin");
            check(world.Surfaces[0].Dirt.GetPixel((int)robot.Pos.X, (int)robot.Pos.Y).A == 0, "Cleaning clears actual bitmap alpha");
            double before = world.Time;
            world.KeyDown(Keys.Space); world.KeyUp(Keys.Space); world.Update(2);
            check(world.Time == before, "Pause freezes simulation time");
            world.KeyDown(Keys.Space); world.KeyUp(Keys.Space);
            var position = robot.Pos;
            world.KeyDown(Keys.D); world.Update(.3); world.KeyUp(Keys.D);
            check(robot.Pos.X > position.X + 35 && Math.Abs(robot.Pos.Y - position.Y) < .01, "Manual drive handles held/released keys");
            position = robot.Pos;
            world.Update(.1);
            check(robot.Pos == position, "Releasing movement stops manual robot");
            world.KeyDown(Keys.K); world.KeyUp(Keys.K);
            check(world.Particles.Any(d => d.Type == DirtType.Confetti), "Cake command creates confetti");
            world.KeyDown(Keys.M); world.KeyUp(Keys.M);
            check(world.ShowMap, "Map toggles");
            world.KeyDown(Keys.R); world.Update(.5); world.KeyDown(Keys.R);
            check(world.Time > .4 && world.Collected == 0, "Reset clears counters and ignores key autorepeat");
            world.KeyUp(Keys.R);
            check(!world.AddDirt(new Vector2(float.NaN, 0), DirtType.Dust), "Nonfinite dirt coordinates rejected");
        }
        using (var dirty = World.CreatePreview(new Size(640, 480), Config(CleanMode.Dirty)))
        {
            dirty.Update(2);
            check(dirty.Particles.Count > 40 && dirty.Particles.Any(d => d.Type == DirtType.Mud), "Dirty mode leaves paired mud trails and crumbs");
            dirty.KeyDown(Keys.C); dirty.KeyUp(Keys.C);
            int count = dirty.Particles.Count;
            dirty.Update(1);
            check(dirty.Particles.Count == count, "Crumb toggle stops dirt emission");
        }
        foreach (var mode in new[] { CleanMode.Clean, CleanMode.Mixed })
        {
            var config = Config(mode); config.BinCapacity = 1;
            using var world = World.CreatePreview(new Size(640, 480), config);
            world.Robots[0].ManualControl = true;
            world.AddDirt(world.Robots[0].Pos, DirtType.Crumbs);
            world.Update(.01);
            check(world.Robots[0].State == (mode == CleanMode.Clean ? RobotState.ReturningHome : RobotState.Dumping), mode + " full bin transition");
            world.Update(3);
            check(mode == CleanMode.Clean ? world.DockVisits == 1 && world.Robots[0].Bin == 0 : world.Dumps == 1 && world.Particles.Count > 0 && world.Robots[0].Bin == 0, mode + " bin cycle completes");
        }
        using (var battery = World.CreatePreview(new Size(640, 480), Config()))
        {
            battery.Update(3);
            battery.Robots[0].Battery = .14f;
            battery.Update(2);
            check(battery.Robots[0].State == RobotState.Docked && Vector2.Distance(battery.Robots[0].Pos, battery.Robots[0].Dock) < .1f, "Low battery follows route back to dock");
            battery.Update(9);
            check(battery.DockVisits == 1 && battery.Robots[0].Battery > .9f && battery.Robots[0].State == RobotState.Roaming, "Dock charges and robot leaves");
        }
        var limitedConfig = Config(); limitedConfig.BinCapacity = 1;
        using (var limited = World.CreatePreview(new Size(640, 480), limitedConfig))
        {
            var robot = limited.Robots[0]; robot.ManualControl = true;
            for (int i = 0; i < 60; i++) limited.AddDirt(robot.Pos, DirtType.Crumbs, 6);
            limited.Update(.01);
            check(robot.Bin == 1 && limited.Collected == 1 && limited.Particles.Count == 59, "Pickup cannot exceed available bin capacity");
            check(limited.Surfaces[0].Dirt.GetPixel((int)robot.Pos.X, (int)robot.Pos.Y).A > 0, "Uncollected overlapping crumbs stay visible");
        }
        var blockedConfig = Config(); blockedConfig.RobotCount = 2;
        using (var blocked = World.CreatePreview(new Size(800, 600), blockedConfig))
        {
            var returning = blocked.Robots[0];
            var obstacle = blocked.Robots[1];
            // Build a real outbound breadcrumb route, then obstruct it with a stationary robot.
            obstacle.Pos = new Vector2(500, 400); obstacle.ManualControl = true;
            blocked.KeyDown(Keys.D); blocked.Update(2); blocked.KeyUp(Keys.D);
            obstacle.Pos = new Vector2(180, 54);
            blocked.GoHome(returning);
            blocked.Update(5);
            check(blocked.DockVisits >= 1 && Vector2.Distance(returning.Pos, returning.Dock) < .1f, "Blocked return replans around stationary robot");
        }
        using (var adjacent = World.CreateLayout([new Rectangle(-400, 0, 400, 400), new Rectangle(0, 0, 400, 400)], Config()))
        {
            check(adjacent.IsValidPosition(new Vector2(0, 200), 40), "Adjacent monitor seam is traversable with negative coordinates");
            check(!adjacent.IsValidPosition(new Vector2(-380, 200), 40), "Outer edge rejects robot footprint");
            var robot = adjacent.Robots[0];
            adjacent.KeyDown(Keys.D); adjacent.Update(3); adjacent.KeyUp(Keys.D);
            check(robot.Pos.X > 0, "Robot actually crosses adjacent monitor seam");
            adjacent.GoHome(robot); adjacent.Update(3);
            check(robot.State == RobotState.Docked && Vector2.Distance(robot.Pos, robot.Dock) < .1f, "Cross-monitor route returns to original dock");
        }
        using (var peers = World.CreatePreview(new Size(800, 600), blockedConfig))
        {
            peers.Robots[0].Pos = new Vector2(300, 54);
            peers.Robots[1].Pos = new Vector2(50, 54);
            foreach (var robot in peers.Robots) { robot.ManualControl = true; peers.GoHome(robot); }
            peers.Update(5);
            check(peers.DockVisits == 2 && peers.Robots.All(r => Vector2.Distance(r.Pos, r.Dock) < .1f), "Opposing returning robots both reach their homes: " + string.Join("; ", peers.Robots.Select(r => $"{r.State} {r.Pos} -> {r.Dock}")));
        }
        using (var gap = World.CreateLayout([new Rectangle(0, 0, 400, 400), new Rectangle(410, 0, 400, 400)], Config()))
        {
            check(!gap.IsValidPosition(new Vector2(400, 200), 40), "Monitor gap rejects footprint");
            gap.KeyDown(Keys.D); gap.Update(6);
            check(gap.Robots[0].Pos.X <= 360.01f, "Robot cannot jump monitor gap");
        }
        using (var corner = World.CreateLayout([new Rectangle(0, 0, 400, 400), new Rectangle(400, 200, 400, 400)], Config()))
        {
            check(!corner.IsValidPosition(new Vector2(390, 200), 40), "Concave monitor corner rejected");
            check(corner.IsValidPosition(new Vector2(400, 300), 40), "Partial-height adjacent seam works");
        }
        var fadeConfig = Config(CleanMode.Dirty); fadeConfig.FadeSeconds = 1;
        using (var fade = World.CreatePreview(new Size(640, 480), fadeConfig))
        {
            fade.Robots[0].ManualControl = true;
            fade.AddDirt(new Vector2(300, 300), DirtType.Crumbs, 10);
            fade.Update(.5);
            int alpha = fade.Surfaces[0].Dirt.GetPixel(300, 300).A;
            check(alpha > 0 && alpha < 255, "Fade reduces actual bitmap alpha");
            fade.Update(.7);
            check(fade.Particles.Count == 0 && fade.Surfaces[0].Dirt.GetPixel(300, 300).A == 0, "Fade expires particles and pixels");
        }
        using (var one = World.CreatePreview(new Size(640, 480), Config(CleanMode.Dirty)))
        using (var many = World.CreatePreview(new Size(640, 480), Config(CleanMode.Dirty)))
        {
            one.Update(5);
            for (int i = 0; i < 300; i++) many.Update(1.0 / 60);
            check(Vector2.Distance(one.Robots[0].Pos, many.Robots[0].Pos) < .001f && one.Particles.Count == many.Particles.Count, "Fixed timestep independent of frame partitioning");
        }
        var multiConfig = Config(); multiConfig.RobotCount = 8;
        using (var multi = World.CreatePreview(new Size(1280, 800), multiConfig))
        {
            bool valid = multi.Robots.Count == 8;
            for (int step = 0; step < 1200; step++)
            {
                multi.Update(1.0 / 60);
                for (int i = 0; i < multi.Robots.Count; i++)
                {
                    var a = multi.Robots[i];
                    valid &= multi.IsValidPosition(a.Pos, a.Radius);
                    for (int j = i + 1; j < multi.Robots.Count; j++)
                        valid &= Vector2.Distance(a.Pos, multi.Robots[j].Pos) >= a.Radius + multi.Robots[j].Radius;
                }
            }
            check(valid, "Eight robots remain inside bounds without overlapping for 20 seconds");
        }
        foreach (var mode in Enum.GetValues<CleanMode>())
        foreach (var movement in Enum.GetValues<MoveMode>())
        {
            var config = Config(mode, movement); config.DirtDensity = 35;
            using var world = World.CreatePreview(new Size(1280, 800), config);
            var start = world.Robots[0].Pos;
            world.Update(20);
            check(world.Time > 19.99 && world.IsValidPosition(world.Robots[0].Pos, world.Robots[0].Radius) && Vector2.Distance(start, world.Robots[0].Pos) > 1, $"20-second simulation: {mode}/{movement}");
            string frame = Path.Combine(directory, $"{mode}-{movement}.png");
            world.SaveFrame(frame);
            using var image = Image.FromFile(frame);
            check(image.Width == 1280 && image.Height == 800, $"Readable simulation frame: {mode}/{movement}");
        }
    }
}
