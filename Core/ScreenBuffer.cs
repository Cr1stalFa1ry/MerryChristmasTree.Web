namespace MerryChristmasTree.Core;

public sealed class ScreenBuffer
{
    public int Width { get; private set; }
    public int Height { get; private set; }

    public char[] Chars { get; private set; } = Array.Empty<char>();
    public int[] Colors { get; private set; } = Array.Empty<int>();
    public int[] BgColors { get; private set; } = Array.Empty<int>();
    public float[] Depths { get; private set; } = Array.Empty<float>();

    private char[] _writeBuffer = Array.Empty<char>();

    public ScreenBuffer(int width, int height)
    {
        Resize(width, height);
    }

    public void Resize(int width, int height)
    {
        Width = Math.Max(10, width);
        Height = Math.Max(5, height);
        int total = Width * Height;

        Chars = new char[total];
        Colors = new int[total];
        BgColors = new int[total];
        Depths = new float[total];

        // 32 chars max per cell: \x1b[38;2;...m\x1b[48;2;...mX + \r\n + header
        _writeBuffer = new char[total * 32 + 256];
    }

    public void ClearDepths(float depth = -1000f)
    {
        Array.Fill(Depths, depth);
    }

    public void PrepareFrame()
    {
        Array.Fill(Depths, -1000f);
        Array.Fill(Chars, ' ');
        Array.Fill(Colors, 0);
        Array.Fill(BgColors, 0);
    }

    public void ClearBuffer()
    {
        Array.Fill(Chars, ' ');
        Array.Fill(Colors, 0);
        Array.Fill(BgColors, 0);
        Array.Fill(Depths, -1000f);
    }

    public bool TrySetCell(int x, int y, char c, int color, float depth, int bgColor = 0)
    {
        if ((uint)x >= (uint)Width || (uint)y >= (uint)Height) return false;
        int idx = y * Width + x;
        if (depth < Depths[idx]) return false;

        Chars[idx] = c;
        Colors[idx] = color;
        BgColors[idx] = bgColor;
        Depths[idx] = depth;
        return true;
    }

    public void RenderToTerminal(TextWriter writer)
    {
        Span<char> buf = _writeBuffer;
        int pos = 0;

        // Move cursor to home (1,1) and initialize black background
        "\x1b[H\x1b[48;2;0;0;0m".CopyTo(buf[pos..]);
        pos += 16;

        int lastFgRgb = -1;
        int lastBgRgb = 0; // Solid black initialized above

        for (int y = 0; y < Height; y++)
        {
            int rowOffset = y * Width;
            for (int x = 0; x < Width; x++)
            {
                int idx = rowOffset + x;
                char c = Chars[idx];
                int fgRgb = Colors[idx];
                int bgRgb = BgColors[idx];

                // Обновление цвета фона (Glow / Свечение фона клетки)
                if (bgRgb != lastBgRgb)
                {
                    ColorRgb.Unpack(bgRgb, out int bgr, out int bgg, out int bgb);
                    "\x1b[48;2;".CopyTo(buf[pos..]);
                    pos += 7;

                    bgr.TryFormat(buf[pos..], out int wbr); pos += wbr; buf[pos++] = ';';
                    bgg.TryFormat(buf[pos..], out int wbg); pos += wbg; buf[pos++] = ';';
                    bgb.TryFormat(buf[pos..], out int wbb); pos += wbb; buf[pos++] = 'm';

                    lastBgRgb = bgRgb;
                }

                if (c == '\0' || (c == ' ' && bgRgb == 0))
                {
                    buf[pos++] = ' ';
                }
                else
                {
                    if (fgRgb != lastFgRgb)
                    {
                        ColorRgb.Unpack(fgRgb, out int fgr, out int fgg, out int fgb);
                        "\x1b[38;2;".CopyTo(buf[pos..]);
                        pos += 7;

                        fgr.TryFormat(buf[pos..], out int wr); pos += wr; buf[pos++] = ';';
                        fgg.TryFormat(buf[pos..], out int wg); pos += wg; buf[pos++] = ';';
                        fgb.TryFormat(buf[pos..], out int wb); pos += wb; buf[pos++] = 'm';

                        lastFgRgb = fgRgb;
                    }
                    buf[pos++] = c;
                }
            }

            if (y < Height - 1)
            {
                buf[pos++] = '\r';
                buf[pos++] = '\n';
            }
        }

        writer.Write(_writeBuffer, 0, pos);
    }
}
