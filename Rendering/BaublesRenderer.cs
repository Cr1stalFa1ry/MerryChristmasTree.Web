using MerryChristmasTree.Core;

namespace MerryChristmasTree.Rendering;

public static class BaublesRenderer
{
    private struct BaublePoint
    {
        public float X;
        public float Y;
        public float Z;
        public float Nx;
        public float Ny;
        public float Nz;
        public char Glyph;
    }

    private sealed class VoxelBauble
    {
        public float NormalizedH;  // Высота крепления на ёлке (0.12 .. 0.80)
        public float BranchRadius; // Вылет ветви (с учетом степени втопленности)
        public float BaseAngle;    // Угол расположения на ветви
        public float DepthOffset;  // Смещение по Z (для эффекта нависания хвои)
        public float TwinklePhase; // Индивидуальная фаза мерцания блика
        public List<BaublePoint> Points = new();
    }

    private static readonly List<VoxelBauble> _baubles = new();
    private static bool _initialized = false;

    public static void Initialize()
    {
        _baubles.Clear();
        var rand = new Random(54321);

        // 20 объемных круглых шаров по 4 ярусам:
        // Часть втоплена вглубь под ветви, остальные - полностью или почти полностью снаружи
        AddTier(rand, count: 6, hMin: 0.12f, hMax: 0.25f);
        AddTier(rand, count: 6, hMin: 0.28f, hMax: 0.44f);
        AddTier(rand, count: 5, hMin: 0.48f, hMax: 0.64f);
        AddTier(rand, count: 3, hMin: 0.68f, hMax: 0.78f);

        _initialized = true;
    }

    private static void AddTier(Random rand, int count, float hMin, float hMax)
    {
        float angleStep = MathF.PI * 2f / count;
        float angleOffset = (float)rand.NextDouble() * MathF.PI;

        for (int i = 0; i < count; i++)
        {
            float h = hMin + (hMax - hMin) * ((float)i / count + (float)rand.NextDouble() * 0.15f);
            float cone = 1.0f - (h - 0.08f) / 0.92f;
            float tierWave = 0.74f + 0.26f * MathF.Pow(MathF.Sin(h * MathF.PI * 6.5f), 2);

            // Разделение по глубине:
            // ~35% втоплены вглубь под елочные ветви (хвоя нависает сверху)
            // ~30% полуутоплены в ветвях
            // ~35% полностью снаружи на кончиках ветвей
            int depthCategory = i % 3;

            float rFactor;
            float depthOffset;

            switch (depthCategory)
            {
                case 0: // Втоплены вглубь под елочные ветви
                    rFactor = 0.72f + (float)rand.NextDouble() * 0.08f;
                    depthOffset = 0.01f + (float)rand.NextDouble() * 0.03f;
                    break;
                case 1: // Полуутоплены в ветвях
                    rFactor = 0.84f + (float)rand.NextDouble() * 0.07f;
                    depthOffset = 0.07f + (float)rand.NextDouble() * 0.04f;
                    break;
                default: // Полностью или почти полностью снаружи
                    rFactor = 0.95f + (float)rand.NextDouble() * 0.06f;
                    depthOffset = 0.16f + (float)rand.NextDouble() * 0.05f;
                    break;
            }

            float branchR = cone * tierWave * rFactor;
            float baseAngle = angleOffset + i * angleStep + ((float)rand.NextDouble() - 0.5f) * 0.25f;

            var bauble = CreateRoundSphereBauble();
            bauble.NormalizedH = h;
            bauble.BranchRadius = branchR;
            bauble.BaseAngle = baseAngle;
            bauble.DepthOffset = depthOffset;
            bauble.TwinklePhase = (float)(rand.NextDouble() * Math.PI * 2);

            _baubles.Add(bauble);
        }
    }

    // Создание идеально круглого 3D-шара (без верхних петелек и креплений)
    private static VoxelBauble CreateRoundSphereBauble()
    {
        var bauble = new VoxelBauble();

        // Пропорции строго 2:1 по символам (2.70 : 1.35) -> ровно 1:1 в терминальных пикселях (идеальный круг)
        float rx = 2.70f;
        float ry = 1.35f;
        float rz = 2.20f;

        int thetaSteps = 8;
        int phiSteps = 18;

        for (int t = 1; t < thetaSteps; t++)
        {
            float theta = MathF.PI * t / thetaSteps;
            float sinT = MathF.Sin(theta);
            float cosT = MathF.Cos(theta);

            for (int p = 0; p < phiSteps; p++)
            {
                float phi = MathF.PI * 2f * p / phiSteps;
                float sinP = MathF.Sin(phi);
                float cosP = MathF.Cos(phi);

                float nx = sinT * cosP;
                float ny = cosT;
                float nz = sinT * sinP;

                char glyph;
                if (MathF.Abs(nx) > 0.78f)
                {
                    glyph = nx > 0 ? ')' : '(';
                }
                else if (MathF.Abs(ny) > 0.82f)
                {
                    glyph = '-';
                }
                else
                {
                    glyph = t switch
                    {
                        1 or 2 => '●',
                        3 or 4 => '@',
                        _ => 'O'
                    };
                }

                bauble.Points.Add(new BaublePoint
                {
                    X = rx * nx,
                    Y = ry * ny,
                    Z = rz * nz,
                    Nx = nx,
                    Ny = ny,
                    Nz = nz,
                    Glyph = glyph
                });
            }
        }

        return bauble;
    }

    public static void RenderBaubles(
        ScreenBuffer buffer,
        int centerX,
        int baseY,
        float maxTreeRadiusX,
        float maxTreeHeight,
        float currentRot,
        float baubleFade,
        float globalTime)
    {
        if (baubleFade <= 0.001f) return;
        if (!_initialized) Initialize();

        // 3D-источник света: сверху, справа и спереди (согласован с подарками)
        float lx = 0.35f;
        float ly = 0.78f;
        float lz = 0.52f;
        float lLen = MathF.Sqrt(lx * lx + ly * ly + lz * lz);
        lx /= lLen; ly /= lLen; lz /= lLen;

        foreach (var bauble in _baubles)
        {
            // Плавное каскадное появление игрушек снизу вверх
            float normH = Math.Clamp((bauble.NormalizedH - 0.12f) / 0.66f, 0f, 1f);
            float bStart = normH * 0.55f;
            float bProgress = Math.Clamp((baubleFade - bStart) / 0.45f, 0f, 1f);
            if (bProgress <= 0.001f) continue;
            float bScale = bProgress * bProgress * (3f - 2f * bProgress);

            float angle = bauble.BaseAngle + currentRot;
            float sinA = MathF.Sin(angle);
            float cosA = MathF.Cos(angle);

            float r = bauble.BranchRadius;

            // Координаты центра шара на ветви
            float centerWorldX = sinA * r * maxTreeRadiusX;
            float centerWorldY = baseY - (bauble.NormalizedH * maxTreeHeight + cosA * r * 1.35f);
            float centerWorldZ = cosA * r;

            // Вращение шара в 3D (согласованное с направлением вращения дерева и подарков)
            float rotAngle = angle;
            float sinRot = MathF.Sin(rotAngle);
            float cosRot = MathF.Cos(rotAngle);

            float shine = MathF.Sin(globalTime * 3.0f + bauble.TwinklePhase) * 0.5f + 0.5f;

            foreach (var pt in bauble.Points)
            {
                // Поворот нормали
                float wnx = pt.Nx * cosRot + pt.Nz * sinRot;
                float wnz = -pt.Nx * sinRot + pt.Nz * cosRot;
                float wny = pt.Ny;

                // Back-face Culling: задние грани шара невидимы
                if (wnz < -0.15f) continue;

                // Поворот локальной точки с учетом плавного масштабирования при появлении
                float wx = (pt.X * cosRot + pt.Z * sinRot) * bScale;
                float wz = (-pt.X * sinRot + pt.Z * cosRot) * bScale;
                float wy = pt.Y * bScale;

                // Изометрическая проекция на экран
                int sx = centerX + (int)MathF.Round(centerWorldX + wx);
                int sy = (int)MathF.Round(centerWorldY - wy + wz * 0.22f);
                float sz = centerWorldZ + (wz / maxTreeRadiusX) * 0.25f + bauble.DepthOffset;

                if (sx < 0 || sx >= buffer.Width || sy < 0 || sy >= buffer.Height) continue;

                int idx = sy * buffer.Width + sx;
                if (sz < buffer.Depths[idx]) continue;

                // Динамическое 3D-освещение по Ламберту
                float dot = MathF.Max(0f, wnx * lx + wny * ly + wnz * lz);
                float shade = 0.44f + 0.56f * dot;

                // Ослепительный глянцевый блик при взгляде на свет
                float specDot = MathF.Max(0f, 2f * dot * wnz - lz);
                float spec = MathF.Pow(specDot, 3.8f);

                // Сочный глубокий рубиновый цвет стекла
                int rCol = Math.Clamp((int)(245 * shade + spec * 80 + shine * 15), 90, 255);
                int gCol = Math.Clamp((int)(15 * shade + spec * 180), 0, 220);
                int bCol = Math.Clamp((int)(25 * shade + spec * 180), 0, 220);
                int fgColor = ColorRgb.Pack(rCol, gCol, bCol);

                // Фоновое свечение для создания монолитного объемного тела круглого шара
                int bgColor = 0;
                if (wnz > 0.05f)
                {
                    int bgR = Math.Clamp((int)(65 * shade), 15, 80);
                    int bgG = Math.Clamp((int)(4 * shade), 1, 10);
                    int bgB = Math.Clamp((int)(10 * shade), 2, 16);
                    bgColor = ColorRgb.Pack(bgR, bgG, bgB);
                }

                char glyph = pt.Glyph;
                if (spec > 0.60f)
                {
                    glyph = '✦';
                }
                else if (spec > 0.30f)
                {
                    glyph = '*';
                }

                buffer.Chars[idx] = glyph;
                buffer.Colors[idx] = fgColor;
                buffer.BgColors[idx] = bgColor;
                buffer.Depths[idx] = sz;
            }
        }
    }
}
