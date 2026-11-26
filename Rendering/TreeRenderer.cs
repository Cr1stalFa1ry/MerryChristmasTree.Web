using MerryChristmasTree.Core;

namespace MerryChristmasTree.Rendering;

public sealed class TreeRenderer
{
    private readonly List<TreePoint> _treePoints = new();
    private readonly List<GlowEmitter> _emitters = new(256);
    private readonly Random _rand = new();

    public void Initialize()
    {
        _treePoints.Clear();

        // 1. Ствол в основании из естественных текстурных символов (без монолитных блоков)
        char[] trunkGlyphs = { '#', 'H', 'M', '%', '&' };
        for (int i = 0; i < 260; i++)
        {
            _treePoints.Add(new TreePoint
            {
                NormalizedH = (float)_rand.NextDouble() * 0.12f,
                Radius = (float)_rand.NextDouble() * 0.11f,
                BaseAngle = (float)(_rand.NextDouble() * Math.PI * 2),
                Type = 0,
                Glyph = trunkGlyphs[_rand.Next(trunkGlyphs.Length)]
            });
        }

        // 2. Густая непрозрачная хвоя из заполняющих буквенно-знаковых символов (W, M, @, #, &, *)
        char[] innerFoliage = { 'W', 'M', '@', '#', '&' };
        char[] outerFoliage = { 'W', 'M', '@', '#', '*', '&' };
        int needleCount = 6000;

        for (int i = 0; i < needleCount; i++)
        {
            float h = 0.08f + (float)_rand.NextDouble() * 0.92f;
            float rawCone = MathF.Max(0f, 1.0f - (h - 0.08f) / 0.92f);
            // Пышная расширенная верхушка (0.16 + 0.84 * rawCone), чтобы звезда опиралась на густую хвою, а не на тонкую палку
            float cone = 0.16f + 0.84f * MathF.Pow(rawCone, 0.85f);
            float tierWave = 0.70f + 0.30f * MathF.Pow(MathF.Sin(h * MathF.PI * 6.5f), 2);
            float maxR = cone * tierWave;

            float rFrac = MathF.Pow((float)_rand.NextDouble(), 0.65f);
            float r = maxR * rFrac;

            char g = rFrac < 0.50f
                ? innerFoliage[_rand.Next(innerFoliage.Length)]
                : outerFoliage[_rand.Next(outerFoliage.Length)];

            _treePoints.Add(new TreePoint
            {
                NormalizedH = h,
                Radius = r,
                BaseAngle = (float)(_rand.NextDouble() * Math.PI * 2),
                Type = 1,
                TwinklePhase = (float)(_rand.NextDouble() * Math.PI * 2),
                Glyph = g
            });
        }

        // 3. Светящаяся широкая золотая спиральная лента из изящных символов (~, =, ≈, ≡, *)
        char[] ribbonGlyphs = { '~', '=', '≈', '≡', '*', '=' };
        int ribbonSteps = 700;
        int ribbonTurns = 5;

        for (int i = 0; i < ribbonSteps; i++)
        {
            float t = (float)i / ribbonSteps;
            float h = 0.08f + t * 0.89f;
            float cone = 0.16f + 0.84f * MathF.Pow(MathF.Max(0f, 1.0f - (h - 0.08f) / 0.92f), 0.85f);
            float tierWave = 0.72f + 0.28f * MathF.Pow(MathF.Sin(h * MathF.PI * 6.5f), 2);

            float spiralAngle = t * MathF.PI * 2f * ribbonTurns;
            float r = cone * tierWave * 1.05f;

            for (int band = -1; band <= 1; band++)
            {
                float bandH = h + band * 0.006f;
                if (bandH < 0.08f || bandH > 0.97f) continue;

                _treePoints.Add(new TreePoint
                {
                    NormalizedH = bandH,
                    Radius = r,
                    BaseAngle = spiralAngle + band * 0.030f,
                    Type = 3, // Спиральная лента
                    LightColorIdx = band == 0 ? 1 : 0, // 1 = центр, 0 = кант
                    TwinklePhase = t * 12f,
                    Glyph = ribbonGlyphs[(i + band + 10) % ribbonGlyphs.Length]
                });
            }
        }

        // 4. Огоньки гирлянды
        char[] garlandGlyphs = { '@', 'O', '●', '◈' };
        int garlandCount = 380;

        for (int i = 0; i < garlandCount; i++)
        {
            float t = (float)i / garlandCount;
            float h = 0.09f + t * 0.88f;
            float cone = 0.16f + 0.84f * MathF.Pow(MathF.Max(0f, 1.0f - (h - 0.08f) / 0.92f), 0.85f);
            float tierWave = 0.74f + 0.26f * MathF.Pow(MathF.Sin(h * MathF.PI * 6.5f), 2);

            float spiralAngle = (i % 6 == 0)
                ? (float)(_rand.NextDouble() * Math.PI * 2)
                : t * MathF.PI * 2f * 7.5f + (i % 2 == 0 ? 0.8f : 1.2f);

            float r = cone * tierWave * 1.04f;

            _treePoints.Add(new TreePoint
            {
                NormalizedH = h,
                Radius = r,
                BaseAngle = spiralAngle,
                Type = 2,
                LightColorIdx = i,
                TwinklePhase = i * 0.45f,
                Glyph = garlandGlyphs[i % garlandGlyphs.Length]
            });
        }

        // 5. Крупные объемные 3D-игрушки на ветвях
        BaublesRenderer.Initialize();
    }

    private static readonly (int R, int G, int B)[] HolidayLights =
    {
        (255, 220, 40),   // Яркое теплое золото
        (255, 248, 210),  // Искристый шампань / теплый белый
        (255, 175, 30),   // Янтарно-медовый огонек
        (255, 235, 120),  // Золотистый свет
        (255, 200, 50)    // Солнечный золотой
    };

    public void RenderTree(
        ScreenBuffer buffer,
        float growthTime,
        float globalTime)
    {
        // 1. Сначала полностью вырастает ёлка (ствол и хвоя) с естественным плавным замедлением
        float treeGrowDuration = 4.2f;
        float rawGrowth = Math.Clamp((growthTime - 0.15f) / treeGrowDuration, 0f, 1f);
        float growth = rawGrowth * rawGrowth * (3f - 2f * rawGrowth); // Плавный SmoothStep
        if (growth <= 0.001f) return;

        // 2. Декорации появляются сразу после завершения роста кроны (без мертвого зависания)
        float decorTime = Math.Max(0f, growthTime - (0.15f + treeGrowDuration));

        // Плавные, несуетливые анимации для всех деталей:
        // А. Золотая лента и гирлянды накручиваются снизу вверх за 4.6 сек
        float rawRibbon = Math.Clamp(decorTime / 4.6f, 0f, 1f);
        float ribbonProgress = rawRibbon * rawRibbon * (3f - 2f * rawRibbon);

        // Б. Подарки под ёлкой плавно материализуются за 3.8 сек
        float rawGifts = Math.Clamp((decorTime - 0.2f) / 3.8f, 0f, 1f);
        float giftsFade = rawGifts * rawGifts * (3f - 2f * rawGifts);

        // В. Крупные 3D-шары каскадно расцветают на ветвях за 4.8 сек
        float rawBaubles = Math.Clamp((decorTime - 0.5f) / 4.8f, 0f, 1f);
        float baublesFade = rawBaubles * rawBaubles * (3f - 2f * rawBaubles);

        // Г. Звезда на вершине плавно зажигается и разворачивает лучи за 3.2 сек
        float rawStar = Math.Clamp((decorTime - 3.4f) / 3.2f, 0f, 1f);
        float starFade = rawStar * rawStar * (3f - 2f * rawStar);

        int width = buffer.Width;
        int height = buffer.Height;

        int centerX = width / 2;
        int baseY = (int)(height * 0.85f) + 2;
        float maxTreeHeight = height * 0.75f;
        float maxTreeRadiusX = Math.Min(width * 0.36f, maxTreeHeight * 0.92f);

        float spinBoost = (1f - growth) * 1.2f;
        float currentRot = globalTime * 0.22f + spinBoost;

        // Теплый золотистый отсвет на снегу у подножия
        int glowRx = (int)(maxTreeRadiusX * 0.95f * growth);
        int glowRy = Math.Max(2, glowRx / 4);

        for (int dy = -glowRy; dy <= glowRy; dy++)
        {
            int py = baseY + dy;
            if (py < 0 || py >= height) continue;

            for (int dx = -glowRx; dx <= glowRx; dx++)
            {
                int px = centerX + dx;
                if (px < 0 || px >= width) continue;

                float dist = (dx * dx) / (float)Math.Max(1, glowRx * glowRx) + (dy * dy) / (float)Math.Max(1, glowRy * glowRy);
                int idx = py * width + px;

                if (dist < 1f && buffer.Chars[idx] != ' ' && buffer.Chars[idx] != '\0')
                {
                    float glow = (1f - dist) * 0.65f * growth;
                    buffer.Colors[idx] = ColorRgb.BlendTreeGlow(buffer.Colors[idx], glow, globalTime + dx * 0.1f);
                }
            }
        }

        _emitters.Clear();

        // 2. Отрисовка геометрии дерева
        foreach (var pt in _treePoints)
        {
            if (pt.NormalizedH > growth) continue;

            // Декорации (гирлянда и лента) накручиваются только ПОСЛЕ полной отрисовки ёлки
            if ((pt.Type == 2 || pt.Type == 3) && (decorTime <= 0.001f || pt.NormalizedH > ribbonProgress)) continue;

            float tipScale = Math.Clamp((growth - pt.NormalizedH) / 0.12f, 0.20f, 1.0f);
            float angle = pt.BaseAngle + currentRot + pt.NormalizedH * (1f - growth) * 1.6f;
            float sinA = MathF.Sin(angle);
            float cosA = MathF.Cos(angle);

            float r = pt.Radius * tipScale;
            int sx = centerX + (int)Math.Round(sinA * r * maxTreeRadiusX);
            int sy = baseY - (int)Math.Round(pt.NormalizedH * maxTreeHeight + cosA * r * 1.35f);

            if (sx < 0 || sx >= width || sy < 0 || sy >= height) continue;

            float zOffset = pt.Type switch
            {
                2 => 0.20f,
                3 => 0.24f,
                _ => 0.0f
            };
            float zDepth = cosA * r + zOffset;

            int idx = sy * width + sx;
            if (zDepth < buffer.Depths[idx]) continue;

            float depthShade = 0.45f + 0.55f * ((cosA + 1f) * 0.5f);
            bool isGrowingEdge = (growth < 0.99f) && (growth - pt.NormalizedH < 0.045f);

            int fgColor;
            int bgColor = 0; // По умолчанию черный фон
            char glyph = pt.Glyph;

            if (isGrowingEdge)
            {
                fgColor = ColorRgb.Pack(130, 255, 160);
                glyph = '#';
            }
            else if (pt.Type == 0) // Ствол дерева
            {
                int rCol = (int)(135 * depthShade);
                int gCol = (int)(70 * depthShade);
                int bCol = (int)(25 * depthShade);
                fgColor = ColorRgb.Pack(rCol, gCol, bCol);
            }
            else if (pt.Type == 1) // Хвоя: глубокий изумрудный цвет
            {
                float vitality = MathF.Sin(globalTime * 2.0f + pt.TwinklePhase) * 0.5f + 0.5f;
                int rCol = Math.Clamp((int)((8 + vitality * 16) * depthShade), 2, 45);
                int gCol = Math.Clamp((int)((180 + vitality * 75) * depthShade), 75, 255);
                int bCol = Math.Clamp((int)((25 + vitality * 25) * depthShade), 10, 65);
                fgColor = ColorRgb.Pack(rCol, gCol, bCol);
            }
            else if (pt.Type == 3) // Светящаяся широкая ярко-желтая спиральная лента
            {
                float ribbonShade = 0.50f + 0.50f * ((cosA + 1f) * 0.5f);
                float specular = MathF.Pow(MathF.Max(0f, cosA), 2.8f);

                // Ослепительно яркий насыщенный солнечный желтый цвет
                int rCol = Math.Clamp((int)(255 * ribbonShade + specular * 30), 185, 255);
                int gCol = Math.Clamp((int)(255 * ribbonShade + specular * 30), 185, 255);
                int bCol = Math.Clamp((int)(15 * ribbonShade + specular * 220), 0, 220);
                fgColor = ColorRgb.Pack(rCol, gCol, bCol);

                if (cosA > -0.1f)
                {
                    // Ярко-желтое фоновое свечение
                    int bgR = Math.Clamp((int)(75 * ribbonShade), 25, 95);
                    int bgG = Math.Clamp((int)(70 * ribbonShade), 20, 90);
                    bgColor = ColorRgb.Pack(bgR, bgG, 0);
                }
            }
            else // Огоньки гирлянды: теплое золотистое праздничное сияние
            {
                var baseLight = HolidayLights[pt.LightColorIdx % HolidayLights.Length];
                float wave = MathF.Sin(globalTime * 4.2f - pt.NormalizedH * 9f + pt.TwinklePhase);
                float pulse = 0.72f + 0.28f * wave;

                // Белое горячее ядро при пике вспышки
                int coreBoost = wave > 0.50f ? (int)((wave - 0.50f) * 220f) : 0;
                int rCol = Math.Clamp((int)(baseLight.R * pulse * depthShade) + coreBoost, 40, 255);
                int gCol = Math.Clamp((int)(baseLight.G * pulse * depthShade) + coreBoost, 40, 255);
                int bCol = Math.Clamp((int)(baseLight.B * pulse * depthShade) + coreBoost, 40, 255);

                fgColor = ColorRgb.Pack(rCol, gCol, bCol);
                if (wave > 0.70f) glyph = '@';

                // Эффект светящегося фона для лампочек на пике яркости
                if (cosA > -0.10f && wave > 0.35f)
                {
                    float bgStrength = (wave - 0.35f) / 0.65f * depthShade;
                    int bgR = Math.Clamp((int)(baseLight.R * 0.22f * bgStrength), 0, 60);
                    int bgG = Math.Clamp((int)(baseLight.G * 0.22f * bgStrength), 0, 60);
                    int bgB = Math.Clamp((int)(baseLight.B * 0.22f * bgStrength), 0, 60);
                    bgColor = ColorRgb.Pack(bgR, bgG, bgB);

                    float glowIntensity = (0.40f + 0.60f * ((wave + 1f) * 0.5f)) * depthShade;
                    _emitters.Add(new GlowEmitter
                    {
                        X = sx,
                        Y = sy,
                        R = baseLight.R,
                        G = baseLight.G,
                        B = baseLight.B,
                        Intensity = glowIntensity
                    });
                }
            }

            buffer.Chars[idx] = glyph;
            buffer.Colors[idx] = fgColor;
            buffer.BgColors[idx] = bgColor;
            buffer.Depths[idx] = zDepth;

            // Золотая лента делается широкой: ровно 2 символа по горизонтали
            if (pt.Type == 3 && sx + 1 < width)
            {
                int idx2 = sy * width + (sx + 1);
                if (zDepth >= buffer.Depths[idx2])
                {
                    char glyph2 = glyph == '=' ? '~' : (glyph == '~' ? '≈' : (glyph == '≈' ? '≡' : glyph));
                    buffer.Chars[idx2] = glyph2;
                    buffer.Colors[idx2] = fgColor;
                    buffer.BgColors[idx2] = bgColor;
                    buffer.Depths[idx2] = zDepth;
                }
            }
        }

        // 3. Крупные объемные 3D-шары на ветвях ёлки (появляются плавно после роста ёлки)
        BaublesRenderer.RenderBaubles(buffer, centerX, baseY, maxTreeRadiusX, maxTreeHeight, currentRot, baublesFade, globalTime);

        // 4. Рассеянный свет от огоньков гирлянд на хвою
        foreach (var em in _emitters)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                int ny = em.Y + dy;
                if (ny < 0 || ny >= height) continue;

                for (int dx = -3; dx <= 3; dx++)
                {
                    if (dx == 0 && dy == 0) continue;
                    int nx = em.X + dx;
                    if (nx < 0 || nx >= width) continue;

                    float dist = MathF.Sqrt((dx * 0.55f) * (dx * 0.55f) + dy * dy);
                    if (dist > 1.85f) continue;

                    float falloff = (1.0f - dist / 1.95f) * em.Intensity;
                    if (falloff <= 0.06f) continue;

                    int glowR = (em.R + 255) / 2;
                    int glowG = (em.G + 230) / 2;
                    int glowB = (em.B + 90) / 2;

                    int nIdx = ny * width + nx;

                    if (buffer.Chars[nIdx] != '\0' && buffer.Chars[nIdx] != ' ' && buffer.Depths[nIdx] > -5f)
                    {
                        buffer.Colors[nIdx] = ColorRgb.AddGlow(buffer.Colors[nIdx], glowR, glowG, glowB, falloff * 0.58f);
                    }
                    else if (falloff > 0.22f && buffer.Depths[nIdx] < 0f)
                    {
                        char glowChar;
                        if (dy == 0 && Math.Abs(dx) <= 2 && em.Intensity > 0.72f)
                            glowChar = '-';
                        else if (falloff > 0.55f)
                            glowChar = '+';
                        else if (falloff > 0.35f)
                            glowChar = '.';
                        else
                            glowChar = '·';

                        int col = ColorRgb.Pack(
                            Math.Clamp((int)(glowR * falloff * 0.85f), 35, 255),
                            Math.Clamp((int)(glowG * falloff * 0.85f), 30, 255),
                            Math.Clamp((int)(glowB * falloff * 0.75f), 15, 255)
                        );

                        buffer.Chars[nIdx] = glowChar;
                        buffer.Colors[nIdx] = col;
                        buffer.Depths[nIdx] = -0.05f;
                    }
                }
            }
        }

        // 5. Подарки под ёлкой (появляются плавно после роста ёлки)
        GiftsRenderer.RenderGifts(buffer, centerX, baseY, maxTreeRadiusX, currentRot, giftsFade);

        // 6. Звезда на макушке (зажигается на вершине после украшения ёлки)
        if (starFade > 0.001f)
        {
            int topY = baseY - (int)Math.Round(maxTreeHeight) - 1;
            StarRenderer.Render(buffer, centerX, topY, globalTime, starFade);
        }
    }
}
