namespace MerryChristmasTree.Rendering;

public struct TreePoint
{
    public float NormalizedH; // 0.0 (основание) .. 1.0 (макушка)
    public float Radius;      // Радиус от оси ствола (0..1)
    public float BaseAngle;   // Базовый угол точки в 3D
    public int Type;          // 0 = ствол, 1 = хвоя, 2 = гирлянда
    public int LightColorIdx; // Индекс цвета огонька гирлянды
    public float TwinklePhase;
    public char Glyph;
}

public struct GlowEmitter
{
    public int X;
    public int Y;
    public int R;
    public int G;
    public int B;
    public float Intensity;
}
