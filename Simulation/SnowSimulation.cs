using MerryChristmasTree.Core;

namespace MerryChristmasTree.Simulation;

public sealed class SnowSimulation
{
    private readonly List<Snowflake> _flakes = new();
    private readonly Random _rand = new();

    public float UserWindOffset { get; set; } = 0f;
    public float SnowDensityFactor { get; set; } = 1.0f;

    public void Resize(GroundField ground, int width, int height)
    {
        _flakes.Clear();
        int maxFlakes = (int)((width * height) / 6 * SnowDensityFactor);

        for (int i = 0; i < maxFlakes; i++)
        {
            _flakes.Add(CreateFlake(ground, width, height, randomY: true));
        }
    }

    public void UpdateAndRender(
        ScreenBuffer buffer,
        GroundField ground,
        float globalTime,
        float dt)
    {
        int width = buffer.Width;
        int height = buffer.Height;

        // Плавное затухание пользовательского ветра
        UserWindOffset = MathF.Sign(UserWindOffset) * MathF.Max(0f, MathF.Abs(UserWindOffset) - dt * 1.5f);

        float ambientWind = MathF.Sin(globalTime * 0.6f) * 2.0f;
        float totalWind = ambientWind + UserWindOffset;

        for (int i = 0; i < _flakes.Count; i++)
        {
            var f = _flakes[i];

            f.Y += f.SpeedY * dt;
            float localWind = totalWind * f.WindSensitivity + MathF.Sin(globalTime * 1.8f + f.WindPhase) * 1.0f;
            f.X += localWind * dt;

            if (f.X < 0) f.X += width;
            if (f.X >= width) f.X -= width;

            int ix = Math.Clamp((int)f.X, 0, width - 1);
            int iy = (int)f.Y;

            float currentSurfaceY = ground.GetSurfaceY(f.X);

            // Снежинка приземлилась на округлую поверхность земли
            if (f.Y >= currentSurfaceY && f.Y >= f.TargetY)
            {
                int ty = Math.Clamp((int)f.TargetY, (int)currentSurfaceY, height - 1);
                float depositAmount = f.Depth == 2 ? 0.9f : 0.55f;
                ground.AddSnowDeposit(ix, ty, depositAmount, _rand);

                _flakes[i] = CreateFlake(ground, width, height, randomY: false);
                continue;
            }

            // Отрисовка снежинки в воздухе
            if (iy >= 0 && iy < height)
            {
                float flakeZ = f.Depth switch { 0 => -5f, 1 => -0.5f, _ => 3.0f };
                int idx = iy * width + ix;

                if (flakeZ >= buffer.Depths[idx])
                {
                    char glyph = GetFlakeGlyph(f.Depth, f.Y, f.WindPhase);
                    int brightness = f.Depth switch { 0 => 95, 1 => 165, _ => 245 };

                    buffer.Chars[idx] = glyph;
                    buffer.Colors[idx] = ColorRgb.PackSnow(brightness);
                    buffer.BgColors[idx] = 0;
                    buffer.Depths[idx] = flakeZ;
                }
            }

            _flakes[i] = f;
        }
    }

    private Snowflake CreateFlake(GroundField ground, int width, int height, bool randomY)
    {
        int depth = _rand.Next(0, 3);
        float x = (float)(_rand.NextDouble() * width);
        float surfaceY = ground.GetSurfaceY(x);

        float minNorm = depth switch { 0 => 0.02f, 1 => 0.25f, _ => 0.55f };
        float maxNorm = depth switch { 0 => 0.40f, 1 => 0.75f, _ => 1.00f };
        float normTarget = minNorm + (float)_rand.NextDouble() * (maxNorm - minNorm);
        float targetY = surfaceY + normTarget * (height - 1 - surfaceY);

        return new Snowflake
        {
            X = x,
            Y = randomY ? (float)(_rand.NextDouble() * targetY) : -1f,
            SpeedY = depth switch
            {
                0 => 2.2f + (float)_rand.NextDouble() * 1.3f,
                1 => 4.2f + (float)_rand.NextDouble() * 1.8f,
                _ => 6.8f + (float)_rand.NextDouble() * 2.5f
            },
            WindPhase = (float)(_rand.NextDouble() * Math.PI * 2),
            WindSensitivity = 0.4f + depth * 0.35f,
            Depth = depth,
            TargetY = targetY
        };
    }

    private static char GetFlakeGlyph(int depth, float y, float phase)
    {
        if (depth == 0) return '.';
        if (depth == 1) return ((int)(y * 1.5f + phase) % 3 == 0) ? '.' : '+';
        return ((int)(y * 2.0f + phase) % 2 == 0) ? '*' : '+';
    }
}
