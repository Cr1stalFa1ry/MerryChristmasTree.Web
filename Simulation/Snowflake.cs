namespace MerryChristmasTree.Simulation;

public struct Snowflake
{
    public float X;
    public float Y;
    public float SpeedY;
    public float WindPhase;
    public float WindSensitivity;
    public int Depth;     // 0 = far, 1 = mid, 2 = near
    public float TargetY; // Точка приземления на плоскости земли
}
