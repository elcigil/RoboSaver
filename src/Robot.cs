using System.Numerics;

namespace RobotSaver;

public enum RobotState { Roaming, ReturningHome, Docked, Leaving, Stuck, Dumping }

public sealed class Robot
{
    public Vector2 Pos;
    public Vector2 LastPos;
    public Vector2 Dock;
    public float Angle;
    public float Radius = 60;
    public float Battery = 1;
    public float Bin;
    public float BrushAngle;
    public float BumpFlash;
    public float StateTime;
    public float SpiralR;
    public float StraightTime;
    public float AvoidTime;
    public float SayCooldown;
    public float WanderPhase;
    public bool ManualControl;
    public RobotState State { get; private set; } = RobotState.Roaming;

    public void SetState(RobotState state)
    {
        State = state;
        StateTime = 0;
    }
}
