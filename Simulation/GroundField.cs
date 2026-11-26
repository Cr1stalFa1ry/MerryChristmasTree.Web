using MerryChristmasTree.Core;

namespace MerryChristmasTree.Simulation;

public sealed class GroundField
{
    public int Width { get; private set; }
    public int Height { get; private set; }

    private char[] _groundChars = Array.Empty<char>();
    private int[] _groundBri = Array.Empty<int>();
    private float[] _settledSnow = Array.Empty<float>();

    // Округлая форма холма: холм приподнят в центре и плавно опускается по краям
    public float GetSurfaceY(float x)
    {
        float centerX = Width * 0.5f;
        float nx = (x - centerX) / (Width * 0.5f);
        nx = Math.Clamp(nx, -1.0f, 1.0f);

        // Плавный округлый купол холма (арка косинуса)
        float arch = MathF.Cos(nx * (MathF.PI * 0.46f));

        // Мягкие естественные неровности сугробов
        float wave = MathF.Sin(nx * 7.0f) * 0.65f + MathF.Cos(nx * 13.0f) * 0.35f;

        float peakY = Height * 0.58f; // Вершина холма под ёлкой
        float edgeY = Height * 0.88f; // Округлый спуск к краям экрана

        float surfaceY = edgeY - arch * (edgeY - peakY) + wave * arch;
        return Math.Clamp(surfaceY, 2f, Height - 1f);
    }

    public void Resize(int width, int height, Random rand)
    {
        Width = width;
        Height = height;
        int total = width * height;

        _groundChars = new char[total];
        _groundBri = new int[total];
        _settledSnow = new float[total];

        InitTerrain(rand);
    }

    private void InitTerrain(Random rand)
    {
        Array.Fill(_groundChars, ' ');
        Array.Fill(_groundBri, 0);

        for (int x = 0; x < Width; x++)
        {
            float surfaceY = GetSurfaceY(x);
            int startY = (int)surfaceY;
            float groundHeight = Math.Max(1f, Height - surfaceY);

            for (int y = startY; y < Height; y++)
            {
                int idx = y * Width + x;
                float depthT = (y - surfaceY) / groundHeight; // 0.0 (кромка холма) .. 1.0 (низ)
                float z = 1.0f / (depthT + 0.12f);

                float nx = (x - Width * 0.5f) / Width;
                float wave1 = MathF.Sin(nx * z * 18.0f + z * 2.2f);
                float wave2 = MathF.Cos(x * 0.14f - y * 0.45f);
                float ridge = wave1 * 0.6f + wave2 * 0.4f;

                char c;
                double r = rand.NextDouble();

                // Кромка округлого холма (гребень сугроба)
                if (y == startY)
                {
                    c = r < 0.45 ? '-' : (r < 0.75 ? '~' : (r < 0.90 ? '.' : '+'));
                }
                else if (depthT < 0.20f)
                {
                    c = r < 0.60 ? '.' : (r < 0.85 ? '-' : ' ');
                }
                else
                {
                    float density = ridge + depthT * 0.55f;
                    if (density > 0.72f)
                        c = r < 0.45 ? '=' : '-';
                    else if (density > 0.18f)
                        c = r < 0.70 ? '-' : '.';
                    else
                        c = r < 0.80 ? '.' : ' ';

                    if (r > 0.982) c = '+';
                    else if (r > 0.995) c = '*';
                }

                int baseBright = (int)(55 + depthT * 80 + Math.Max(0f, ridge) * 35);
                if (c == '+' || c == '*') baseBright += 55;

                _groundChars[idx] = c;
                _groundBri[idx] = c == ' ' ? 0 : Math.Clamp(baseBright, 40, 220);
            }
        }
    }

    public void AddSnowDeposit(int x, int y, float amount, Random rand)
    {
        if ((uint)x >= (uint)Width || (uint)y >= (uint)Height) return;
        int idx = y * Width + x;

        _settledSnow[idx] = Math.Min(1.0f, _settledSnow[idx] + amount);

        if (_groundBri[idx] < 170 && rand.NextDouble() < 0.35)
        {
            _groundBri[idx] += 8;
            if (_groundChars[idx] == ' ' || _groundChars[idx] == '.')
                _groundChars[idx] = '-';
            else if (_groundChars[idx] == '-' && rand.NextDouble() < 0.25)
                _groundChars[idx] = '=';
        }
    }

    public void RenderGround(ScreenBuffer buffer, float dt)
    {
        for (int x = 0; x < Width; x++)
        {
            int startY = (int)GetSurfaceY(x);

            for (int y = startY; y < Height; y++)
            {
                int idx = y * Width + x;
                int bri = _groundBri[idx];
                char c = _groundChars[idx];

                if (_settledSnow[idx] > 0.01f)
                {
                    _settledSnow[idx] = Math.Max(0f, _settledSnow[idx] - dt * 0.035f);
                    float intensity = _settledSnow[idx];

                    char snowGlyph = intensity > 0.75f ? '*' : (intensity > 0.35f ? '+' : '.');
                    int snowBri = Math.Clamp((int)(95 + intensity * 145), 95, 240);

                    if (snowBri >= bri)
                    {
                        c = snowGlyph;
                        bri = snowBri;
                    }
                }

                buffer.Chars[idx] = c;
                buffer.Colors[idx] = c == ' ' ? 0 : ColorRgb.PackSnow(bri);
                buffer.BgColors[idx] = 0;
                buffer.Depths[idx] = -10f; // Земля всегда на заднем плане относительно ёлки
            }
        }
    }
}
