namespace MerryChristmasTree.Core;

public static class ColorRgb
{
    public static int Pack(int r, int g, int b) => (Math.Clamp(r, 0, 255) << 16) | (Math.Clamp(g, 0, 255) << 8) | Math.Clamp(b, 0, 255);

    public static void Unpack(int rgb, out int r, out int g, out int b)
    {
        r = (rgb >> 16) & 0xFF;
        g = (rgb >> 8) & 0xFF;
        b = rgb & 0xFF;
    }

    public static int AddGlow(int baseRgb, int glowR, int glowG, int glowB, float strength)
    {
        if (strength <= 0f) return baseRgb;
        Unpack(baseRgb, out int r, out int g, out int b);

        r = Math.Clamp(r + (int)(glowR * strength), 0, 255);
        g = Math.Clamp(g + (int)(glowG * strength), 0, 255);
        b = Math.Clamp(b + (int)(glowB * strength), 0, 255);

        return Pack(r, g, b);
    }

    public static int Blend(int c1, int c2, float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        Unpack(c1, out int r1, out int g1, out int b1);
        Unpack(c2, out int r2, out int g2, out int b2);

        int r = (int)(r1 + (r2 - r1) * t);
        int g = (int)(g1 + (g2 - g1) * t);
        int b = (int)(b1 + (b2 - b1) * t);

        return Pack(r, g, b);
    }

    public static int PackSnow(int brightness)
    {
        float t = brightness / 255f;
        int r = (int)(brightness * (0.76f + 0.22f * t));
        int g = (int)(brightness * (0.84f + 0.14f * t));
        int b = brightness;
        return Pack(r, g, b);
    }

    public static int BlendTreeGlow(int baseRgb, float glowStrength, float phase)
    {
        Unpack(baseRgb, out int r, out int g, out int b);
        float shimmer = 0.85f + 0.15f * MathF.Sin(phase * 3f);

        r = Math.Clamp(r + (int)(85 * glowStrength * shimmer), 0, 255);
        g = Math.Clamp(g + (int)(115 * glowStrength * shimmer), 0, 255);
        b = Math.Clamp(b + (int)(35 * glowStrength * shimmer), 0, 255);

        return Pack(r, g, b);
    }

    public static int GetFadingYellow(float fade)
    {
        fade = Math.Clamp(fade, 0f, 1f);
        int r = Math.Clamp((int)(70 + 185 * MathF.Pow(fade, 0.70f)), 50, 255);
        int g = Math.Clamp((int)(45 + 205 * MathF.Pow(fade, 1.05f)), 35, 250);
        int b = Math.Clamp((int)(180 * MathF.Pow(fade, 2.40f)), 5, 200);
        return Pack(r, g, b);
    }
}
