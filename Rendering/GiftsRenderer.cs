using MerryChristmasTree.Core;

namespace MerryChristmasTree.Rendering;

public static class GiftsRenderer
{
    private struct BoxPoint
    {
        public float X;
        public float Y;
        public float Z;
        public float Nx;
        public float Ny;
        public float Nz;
        public bool IsRibbon;
        public bool IsBow;
        public char Glyph;
    }

    private sealed class VoxelGift
    {
        public float OrbitAngle;    // Угол орбиты вокруг ствола
        public float OrbitRadius;   // Радиус орбиты
        public int BoxColor;        // Насыщенный цвет коробки
        public int RibbonColor;     // Насыщенный цвет ленты
        public List<BoxPoint> Points = new();
    }

    private static readonly List<VoxelGift> _gifts = new();
    private static float _lastRadius = -1f;

    private static void EnsureInitialized(float maxTreeRadiusX)
    {
        if (MathF.Abs(_lastRadius - maxTreeRadiusX) < 1f && _gifts.Count > 0) return;
        _lastRadius = maxTreeRadiusX;

        _gifts.Clear();

        // 4 больших подарка равномерно расставлены вокруг ёлки (шаг 90 градусов)
        // 1. Большая ярко-красная коробка с золотой лентой (0 градусов)
        float r1 = Math.Clamp(maxTreeRadiusX * 0.16f, 3.4f, 5.8f);
        float h1 = Math.Clamp(maxTreeRadiusX * 0.13f, 2.8f, 4.4f);
        _gifts.Add(CreateBox(
            orbitAngle: 0.0f,
            orbitRadius: 0.82f,
            color: ColorRgb.Pack(255, 30, 50),       // Светящийся яркий рубин
            ribbonColor: ColorRgb.Pack(255, 235, 25), // Ослепительное золото
            halfW: r1,
            height: h1,
            halfD: r1 * 0.90f,
            ribbonW: 1.1f
        ));

        // 2. Большая солнечно-желтая (золотая) коробка с рубиновой лентой (90 градусов)
        float r2 = Math.Clamp(maxTreeRadiusX * 0.18f, 3.8f, 6.4f);
        float h2 = Math.Clamp(maxTreeRadiusX * 0.11f, 2.5f, 3.8f);
        _gifts.Add(CreateBox(
            orbitAngle: MathF.PI * 0.5f, // 90 градусов
            orbitRadius: 0.86f,
            color: ColorRgb.Pack(255, 225, 15),      // Чистое яркое золото
            ribbonColor: ColorRgb.Pack(255, 25, 50), // Сочный рубин
            halfW: r2,
            height: h2,
            halfD: r2 * 0.90f,
            ribbonW: 1.2f
        ));

        // 3. Большая царственно-фиолетовая коробка с золотой лентой (180 градусов)
        float r3 = Math.Clamp(maxTreeRadiusX * 0.15f, 3.2f, 5.2f);
        float h3 = Math.Clamp(maxTreeRadiusX * 0.15f, 3.2f, 4.8f);
        _gifts.Add(CreateBox(
            orbitAngle: MathF.PI, // 180 градусов
            orbitRadius: 0.84f,
            color: ColorRgb.Pack(170, 35, 245),       // Насыщенный королевский фиолетовый
            ribbonColor: ColorRgb.Pack(255, 230, 50), // Искрящееся яркое золото
            halfW: r3,
            height: h3,
            halfD: r3 * 0.90f,
            ribbonW: 1.1f
        ));

        // 4. Большая сапфирово-синяя коробка с серебристо-золотой лентой (270 градусов)
        float r4 = Math.Clamp(maxTreeRadiusX * 0.17f, 3.6f, 6.0f);
        float h4 = Math.Clamp(maxTreeRadiusX * 0.14f, 3.0f, 4.5f);
        _gifts.Add(CreateBox(
            orbitAngle: MathF.PI * 1.5f, // 270 градусов
            orbitRadius: 0.87f,
            color: ColorRgb.Pack(25, 125, 255),       // Насыщенный сапфирово-синий
            ribbonColor: ColorRgb.Pack(255, 240, 180), // Светло-золотая лента
            halfW: r4,
            height: h4,
            halfD: r4 * 0.90f,
            ribbonW: 1.15f
        ));
    }

    private static VoxelGift CreateBox(
        float orbitAngle,
        float orbitRadius,
        int color,
        int ribbonColor,
        float halfW,
        float height,
        float halfD,
        float ribbonW)
    {
        var gift = new VoxelGift
        {
            OrbitAngle = orbitAngle,
            OrbitRadius = orbitRadius,
            BoxColor = color,
            RibbonColor = ribbonColor
        };

        float stepX = 0.75f;
        float stepY = 0.75f;
        float stepZ = 0.75f;

        // 1. Верхняя крышка коробки (Y = height)
        for (float x = -halfW; x <= halfW; x += stepX)
        {
            for (float z = -halfD; z <= halfD; z += stepZ)
            {
                bool isRib = MathF.Abs(x) <= ribbonW || MathF.Abs(z) <= ribbonW;
                char glyph = isRib ? ((Math.Abs((int)(x * 3f + z * 3f)) % 2 == 0) ? '@' : 'x') : '#';

                gift.Points.Add(new BoxPoint
                {
                    X = x,
                    Y = height,
                    Z = z,
                    Nx = 0,
                    Ny = 1f,
                    Nz = 0,
                    IsRibbon = isRib,
                    IsBow = false,
                    Glyph = glyph
                });
            }
        }

        // 2. Передняя грань (Z = +halfD)
        for (float x = -halfW; x <= halfW; x += stepX)
        {
            for (float y = 0; y <= height; y += stepY)
            {
                bool isRib = MathF.Abs(x) <= ribbonW || MathF.Abs(y - height * 0.5f) <= ribbonW * 0.75f;
                char glyph = isRib ? ((Math.Abs((int)(x * 2f + y * 2f)) % 2 == 0) ? 'x' : '@') : '#';

                gift.Points.Add(new BoxPoint
                {
                    X = x,
                    Y = y,
                    Z = halfD,
                    Nx = 0,
                    Ny = 0,
                    Nz = 1f,
                    IsRibbon = isRib,
                    IsBow = false,
                    Glyph = glyph
                });
            }
        }

        // 3. Задняя грань (Z = -halfD)
        for (float x = -halfW; x <= halfW; x += stepX)
        {
            for (float y = 0; y <= height; y += stepY)
            {
                bool isRib = MathF.Abs(x) <= ribbonW || MathF.Abs(y - height * 0.5f) <= ribbonW * 0.75f;
                char glyph = isRib ? 'x' : '#';

                gift.Points.Add(new BoxPoint
                {
                    X = x,
                    Y = y,
                    Z = -halfD,
                    Nx = 0,
                    Ny = 0,
                    Nz = -1f,
                    IsRibbon = isRib,
                    IsBow = false,
                    Glyph = glyph
                });
            }
        }

        // 4. Правая грань (X = +halfW)
        for (float z = -halfD; z <= halfD; z += stepZ)
        {
            for (float y = 0; y <= height; y += stepY)
            {
                bool isRib = MathF.Abs(z) <= ribbonW || MathF.Abs(y - height * 0.5f) <= ribbonW * 0.75f;
                char glyph = isRib ? 'x' : '&';

                gift.Points.Add(new BoxPoint
                {
                    X = halfW,
                    Y = y,
                    Z = z,
                    Nx = 1f,
                    Ny = 0,
                    Nz = 0,
                    IsRibbon = isRib,
                    IsBow = false,
                    Glyph = glyph
                });
            }
        }

        // 5. Левая грань (X = -halfW)
        for (float z = -halfD; z <= halfD; z += stepZ)
        {
            for (float y = 0; y <= height; y += stepY)
            {
                bool isRib = MathF.Abs(z) <= ribbonW || MathF.Abs(y - height * 0.5f) <= ribbonW * 0.75f;
                char glyph = isRib ? 'x' : '&';

                gift.Points.Add(new BoxPoint
                {
                    X = -halfW,
                    Y = y,
                    Z = z,
                    Nx = -1f,
                    Ny = 0,
                    Nz = 0,
                    IsRibbon = isRib,
                    IsBow = false,
                    Glyph = glyph
                });
            }
        }

        // 6. Объемный праздничный бант на крышке (узел, 4 лепестка и хвостики)
        gift.Points.Add(new BoxPoint { X = 0, Y = height + 0.6f, Z = 0, Nx = 0, Ny = 1f, Nz = 0, IsRibbon = true, IsBow = true, Glyph = '*' });
        gift.Points.Add(new BoxPoint { X = 0.45f, Y = height + 0.65f, Z = 0, Nx = 0, Ny = 1f, Nz = 0, IsRibbon = true, IsBow = true, Glyph = '@' });
        gift.Points.Add(new BoxPoint { X = -0.45f, Y = height + 0.65f, Z = 0, Nx = 0, Ny = 1f, Nz = 0, IsRibbon = true, IsBow = true, Glyph = '@' });

        float loopW = ribbonW * 1.5f;
        gift.Points.Add(new BoxPoint { X = -loopW, Y = height + 0.95f, Z = -0.3f, Nx = -0.5f, Ny = 0.8f, Nz = 0, IsRibbon = true, IsBow = true, Glyph = '@' });
        gift.Points.Add(new BoxPoint { X = -loopW * 0.7f, Y = height + 1.25f, Z = 0, Nx = 0, Ny = 1f, Nz = 0, IsRibbon = true, IsBow = true, Glyph = '*' });
        gift.Points.Add(new BoxPoint { X = -loopW * 0.3f, Y = height + 0.95f, Z = 0.3f, Nx = 0.5f, Ny = 0.8f, Nz = 0, IsRibbon = true, IsBow = true, Glyph = 'x' });

        gift.Points.Add(new BoxPoint { X = loopW, Y = height + 0.95f, Z = -0.3f, Nx = 0.5f, Ny = 0.8f, Nz = 0, IsRibbon = true, IsBow = true, Glyph = '@' });
        gift.Points.Add(new BoxPoint { X = loopW * 0.7f, Y = height + 1.25f, Z = 0, Nx = 0, Ny = 1f, Nz = 0, IsRibbon = true, IsBow = true, Glyph = '*' });
        gift.Points.Add(new BoxPoint { X = loopW * 0.3f, Y = height + 0.95f, Z = 0.3f, Nx = -0.5f, Ny = 0.8f, Nz = 0, IsRibbon = true, IsBow = true, Glyph = 'x' });

        gift.Points.Add(new BoxPoint { X = 0, Y = height + 1.15f, Z = loopW * 0.7f, Nx = 0, Ny = 1f, Nz = 0.5f, IsRibbon = true, IsBow = true, Glyph = '*' });
        gift.Points.Add(new BoxPoint { X = 0, Y = height + 1.15f, Z = -loopW * 0.7f, Nx = 0, Ny = 1f, Nz = -0.5f, IsRibbon = true, IsBow = true, Glyph = '*' });

        gift.Points.Add(new BoxPoint { X = -loopW * 1.2f, Y = height + 0.4f, Z = loopW * 0.6f, Nx = 0, Ny = 0.5f, Nz = 0.8f, IsRibbon = true, IsBow = true, Glyph = '+' });
        gift.Points.Add(new BoxPoint { X = -loopW * 1.5f, Y = height + 0.15f, Z = loopW * 0.8f, Nx = 0, Ny = 0.5f, Nz = 0.8f, IsRibbon = true, IsBow = true, Glyph = 'x' });
        gift.Points.Add(new BoxPoint { X = loopW * 1.2f, Y = height + 0.4f, Z = loopW * 0.6f, Nx = 0, Ny = 0.5f, Nz = 0.8f, IsRibbon = true, IsBow = true, Glyph = '+' });
        gift.Points.Add(new BoxPoint { X = loopW * 1.5f, Y = height + 0.15f, Z = loopW * 0.8f, Nx = 0, Ny = 0.5f, Nz = 0.8f, IsRibbon = true, IsBow = true, Glyph = 'x' });

        return gift;
    }

    public static void RenderGifts(
        ScreenBuffer buffer,
        int centerX,
        int baseY,
        float maxTreeRadiusX,
        float currentRot,
        float giftsFade)
    {
        if (giftsFade <= 0.001f) return;
        float gf = Math.Clamp(giftsFade, 0f, 1f);
        float scale = gf * gf * (3f - 2f * gf);

        EnsureInitialized(maxTreeRadiusX);

        // Направление света: сверху, спереди и справа
        float lx = 0.35f;
        float ly = 0.78f;
        float lz = 0.52f;
        float lLen = MathF.Sqrt(lx * lx + ly * ly + lz * lz);
        lx /= lLen; ly /= lLen; lz /= lLen;

        foreach (var gift in _gifts)
        {
            float orbitAngle = gift.OrbitAngle + currentRot;
            float sinOrbit = MathF.Sin(orbitAngle);
            float cosOrbit = MathF.Cos(orbitAngle);

            float orbitDistX = maxTreeRadiusX * gift.OrbitRadius;
            // Мягкая перспектива земли: когда подарок впереди, он держится ближе к основанию ёлки,
            // чтобы не вылезать за нижнюю кромку экрана
            float forwardLimit = Math.Max(1.0f, (buffer.Height - baseY - 2.5f));
            float orbitDistY = Math.Clamp(orbitDistX * 0.11f, 1.0f, forwardLimit);

            float centerWorldX = sinOrbit * orbitDistX;
            float centerWorldZ = cosOrbit * (gift.OrbitRadius * 0.85f);

            // Когда подарок спереди (cosOrbit > 0), смещение вниз умеренное (cosOrbit * orbitDistY * 0.70f)
            float verticalOffset = cosOrbit > 0 ? (cosOrbit * orbitDistY * 0.70f) : (cosOrbit * orbitDistY);
            float centerWorldY = baseY + 0.2f + verticalOffset;

            // Гарантируем, что коробка никогда не выйдет за нижний край экрана
            if (centerWorldY > buffer.Height - 2.2f)
            {
                centerWorldY = buffer.Height - 2.2f;
            }

            float rotAngle = orbitAngle;
            float sinRot = MathF.Sin(rotAngle);
            float cosRot = MathF.Cos(rotAngle);

            foreach (var pt in gift.Points)
            {
                // Поворот 3D-нормали (согласованный с направлением обхода сцены)
                float wnx = pt.Nx * cosRot + pt.Nz * sinRot;
                float wnz = -pt.Nx * sinRot + pt.Nz * cosRot;
                float wny = pt.Ny;

                // Back-face Culling: скрытые задние грани отсекаются
                if (wnz < -0.15f && wny <= 0.05f) continue;

                // Поворот локальной точки с учетом плавного масштабирования при появлении
                float wx = (pt.X * cosRot + pt.Z * sinRot) * scale;
                float wz = (-pt.X * sinRot + pt.Z * cosRot) * scale;
                float wy = pt.Y * scale;

                // Изометрический наклон камеры сверху вниз (+ wz * 0.22f),
                // благодаря чему верхняя крышка коробки отчетливо видна и коробка не опускается слишком низко
                int sx = centerX + (int)MathF.Round(centerWorldX + wx);
                int sy = (int)MathF.Round(centerWorldY - wy + wz * 0.22f);
                float sz = centerWorldZ + (wz / maxTreeRadiusX) * 0.25f;

                if (sx < 0 || sx >= buffer.Width || sy < 0 || sy >= buffer.Height) continue;

                int idx = sy * buffer.Width + sx;
                if (sz < buffer.Depths[idx]) continue;

                // 3D-освещение по Ламберту:
                // Верхняя крышка освещена ярче всего (~0.95..1.0),
                // лицевая грань яркая (0.9..1.0), а отвернутая грань четко затенена (~0.60)
                float dot = MathF.Max(0f, wnx * lx + wny * ly + wnz * lz);
                float shade = wny > 0.6f ? 0.96f : (0.58f + 0.42f * dot);

                int fgColor;

                if (pt.IsBow)
                {
                    // Искрящийся светящийся бант
                    fgColor = ColorRgb.Pack(255, 255, 235);
                }
                else if (pt.IsRibbon)
                {
                    // Сочная яркая лента коробки
                    ColorRgb.Unpack(gift.RibbonColor, out int rr, out int rg, out int rb);
                    fgColor = ColorRgb.Pack(
                        Math.Clamp((int)(rr * shade), 40, 255),
                        Math.Clamp((int)(rg * shade), 40, 255),
                        Math.Clamp((int)(rb * shade), 40, 255)
                    );
                }
                else
                {
                    // Очень насыщенный цвет коробки с четким переходом света и тени на гранях
                    ColorRgb.Unpack(gift.BoxColor, out int br, out int bg, out int bb);
                    fgColor = ColorRgb.Pack(
                        Math.Clamp((int)(br * shade), 30, 255),
                        Math.Clamp((int)(bg * shade), 30, 255),
                        Math.Clamp((int)(bb * shade), 30, 255)
                    );
                }

                buffer.Chars[idx] = pt.Glyph;
                buffer.Colors[idx] = fgColor;
                buffer.BgColors[idx] = 0; // Чистый черный фон без рамок ячеек
                buffer.Depths[idx] = sz;
            }
        }
    }
}
