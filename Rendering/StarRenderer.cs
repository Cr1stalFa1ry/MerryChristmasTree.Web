using MerryChristmasTree.Core;

namespace MerryChristmasTree.Rendering;

public static class StarRenderer
{
    private static readonly string[] StarPattern =
    {
        "  ###  ",
        "##@@@##",
        "##@@@##",
        " #@@@# ",
        " ## ## "
    };

    public static void Render(
        ScreenBuffer buffer,
        int cx,
        int cy,
        float time,
        float starFade = 1.0f)
    {
        if (starFade <= 0.001f) return;

        int w = buffer.Width;
        int h = buffer.Height;

        float sf = Math.Clamp(starFade, 0f, 1f);
        float pulse = (0.88f + 0.12f * MathF.Sin(time * 4.2f)) * sf;
        float breath = (0.5f + 0.5f * MathF.Sin(time * 2.8f)) * sf;

        // 1. Широкое круговое поле золотых искр и ореола (плавно разрастается от центра)
        int haloRx = Math.Max(2, (int)(15 * sf));
        int haloRy = Math.Max(1, (int)(7 * sf));
        for (int dy = -haloRy; dy <= haloRy; dy++)
        {
            int py = cy + dy;
            if (py < 0 || py >= h) continue;

            for (int dx = -haloRx; dx <= haloRx; dx++)
            {
                int px = cx + dx;
                if (px < 0 || px >= w) continue;

                float normDist = MathF.Sqrt((dx / (float)haloRx) * (dx / (float)haloRx) + (dy / (float)haloRy) * (dy / (float)haloRy));
                if (normDist >= 1.0f) continue;

                float falloff = MathF.Pow(1.0f - normDist, 1.45f) * pulse;
                if (falloff < 0.06f) continue;

                int yellowR = Math.Clamp((int)(255 * falloff), 45, 255);
                int yellowG = Math.Clamp((int)(240 * MathF.Pow(falloff, 1.10f)), 30, 255);
                int yellowB = Math.Clamp((int)(135 * MathF.Pow(falloff, 2.0f)), 5, 230);

                int idx = py * w + px;
                if (buffer.Chars[idx] != '\0' && buffer.Chars[idx] != ' ' && buffer.Depths[idx] > -5f)
                {
                    buffer.Colors[idx] = ColorRgb.AddGlow(buffer.Colors[idx], yellowR, yellowG, yellowB, falloff * 0.95f);
                }
                else
                {
                    char haloChar = falloff switch
                    {
                        > 0.70f => '✦',
                        > 0.52f => '*',
                        > 0.36f => '+',
                        > 0.22f => ':',
                        > 0.12f => '.',
                        _ => '·'
                    };

                    buffer.TrySetCell(px, py, haloChar, ColorRgb.Pack(yellowR, yellowG, yellowB), 4.1f);
                }
            }
        }

        // 2. Длинные горизонтальные лучи "══════──────····" (растут наружу)
        int maxHoriz = 16 + (int)(breath * 5f);
        int horizRayLength = Math.Max(3, (int)(maxHoriz * sf));
        for (int dir = -1; dir <= 1; dir += 2)
        {
            for (int step = 4; step <= horizRayLength; step++)
            {
                int px = cx + dir * step;
                if (px < 0 || px >= w || cy < 0 || cy >= h) continue;

                float t = 1.0f - (step - 3f) / (horizRayLength - 2f);
                float fade = MathF.Pow(Math.Clamp(t, 0f, 1f), 1.25f) * pulse;

                char rayChar = t > 0.65f ? '═' : (t > 0.35f ? '─' : (t > 0.15f ? '-' : '·'));
                int rayColor = ColorRgb.GetFadingYellow(fade);

                buffer.TrySetCell(px, cy, rayChar, rayColor, 4.8f);
            }
        }

        // 3. Вертикальные лучи вверх и вниз ('║', '│', '|', ':')
        int maxVert = 6 + (int)(breath * 2.5f);
        int vertRayLength = Math.Max(2, (int)(maxVert * sf));
        for (int dir = -1; dir <= 1; dir += 2)
        {
            for (int step = 3; step <= vertRayLength; step++)
            {
                int py = cy + dir * step;
                if (py < 0 || py >= h || cx < 0 || cx >= w) continue;

                float t = 1.0f - (step - 2f) / (vertRayLength - 1f);
                float fade = MathF.Pow(Math.Clamp(t, 0f, 1f), 1.25f) * pulse;

                char rayChar = t > 0.65f ? '║' : (t > 0.35f ? '│' : (t > 0.18f ? '|' : '·'));
                int rayColor = ColorRgb.GetFadingYellow(fade);

                buffer.TrySetCell(cx, py, rayChar, rayColor, 4.8f);
            }
        }

        // 4. Диагональные лучи ('\', '/', '·')
        int maxDiag = 4 + (int)(breath * 2f);
        int diagRayLength = Math.Max(1, (int)(maxDiag * sf));
        for (int step = 2; step <= diagRayLength; step++)
        {
            float t = 1.0f - (step - 1.5f) / (diagRayLength - 0.5f);
            float fade = MathF.Pow(Math.Clamp(t, 0f, 1f), 1.35f) * pulse;
            int diagColor = ColorRgb.GetFadingYellow(fade);

            char cMain = step < diagRayLength ? '\\' : '·';
            char cAnti = step < diagRayLength ? '/' : '·';

            buffer.TrySetCell(cx - step * 2, cy - step, cMain, diagColor, 4.7f);
            buffer.TrySetCell(cx + step * 2, cy - step, cAnti, diagColor, 4.7f);
            buffer.TrySetCell(cx - step * 2, cy + step, cAnti, diagColor, 4.7f);
            buffer.TrySetCell(cx + step * 2, cy + step, cMain, diagColor, 4.7f);
        }

        // 5. Ослепительно яркая пятистрочная звезда заданной формы
        //   ###
        // ##@@@##
        // ##@@@##
        //  #@@@#
        //  ## ##
        int coreFg = ColorRgb.Pack((int)(255 * sf), (int)(255 * sf), (int)(240 * sf));
        int coreBg = ColorRgb.Pack((int)(185 * sf), (int)(140 * sf), (int)(25 * sf));
        int armFg = ColorRgb.Pack((int)(255 * sf), (int)(225 * sf), (int)(35 * sf));
        int armBg = ColorRgb.Pack((int)(85 * sf), (int)(55 * sf), (int)(8 * sf));

        int patternH = StarPattern.Length;
        int patternW = StarPattern[0].Length;
        int centerRow = 2; // Центр в строке 2 (от 0 до 4)
        int centerCol = 3; // Центр в столбце 3 (от 0 до 6)

        for (int r = 0; r < patternH; r++)
        {
            string line = StarPattern[r];
            int py = cy + (r - centerRow);
            if (py < 0 || py >= h) continue;

            for (int c = 0; c < patternW; c++)
            {
                char ch = line[c];
                if (ch == ' ') continue;

                if (ch == '#' && sf < 0.25f) continue;

                int px = cx + (c - centerCol);
                if (px < 0 || px >= w) continue;

                int fg = ch == '@' ? coreFg : armFg;
                int bg = ch == '@' ? coreBg : armBg;
                float depth = ch == '@' ? 5.5f : 5.3f;

                buffer.TrySetCell(px, py, ch, fg, depth, bg);
            }
        }
    }
}
