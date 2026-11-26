using MerryChristmasTree.Core;

namespace MerryChristmasTree.Rendering;

public readonly record struct HudHitbox(string ActionId, int StartX, int EndX, int Y);

public static class BannerRenderer
{
    private static readonly List<HudHitbox> _hitboxes = new();
    
    public static string? HitTest(int col, int row)
    {
        foreach (var box in _hitboxes)
        {
            if (row == box.Y && col >= box.StartX && col <= box.EndX)
            {
                return box.ActionId;
            }
        }
        return null;
    }

    private readonly record struct Segment(string Text, string? ActionId, bool IsActive = false, bool IsGap = false);

    public static void RenderBottomHud(
        ScreenBuffer buffer,
        float windOffset,
        float snowDensity,
        bool isPaused,
        bool isMuted,
        bool hasAudio,
        bool isCrtActive,
        bool showHud,
        int hoverCol,
        int hoverRow)
    {
        _hitboxes.Clear();
        if (buffer.Height < 10) return;

        int y = buffer.Height - 1;

        if (!showHud)
        {
            // Minimal clickable restore button in bottom-right corner when HUD is hidden
            const string restoreLabel = "[H]UD";
            int rx = Math.Max(0, buffer.Width - restoreLabel.Length - 1);
            bool hovered = hoverRow == y && hoverCol >= rx && hoverCol < rx + restoreLabel.Length;
            DrawSegment(buffer, rx, y, restoreLabel, hovered, false, "hud");
            return;
        }

        var segments = new List<Segment>();

        if (isPaused)
        {
            segments.Add(new Segment("[PAUSED]", "pause", IsActive: true));
            segments.Add(new Segment(" ", null, IsGap: true));
        }

        if (MathF.Abs(windOffset) > 0.1f)
        {
            string arrow = windOffset > 0 ? "→" : "←";
            segments.Add(new Segment($"Wind:{arrow}{MathF.Abs(windOffset):0.0}", null));
            segments.Add(new Segment(" ", null, IsGap: true));
        }

        if (hasAudio)
        {
            segments.Add(new Segment(isMuted ? "[M]ute:OFF" : "[M]usic:♫", "music", IsActive: !isMuted));
            segments.Add(new Segment(" ", null, IsGap: true));
        }

        segments.Add(new Segment(isPaused ? "[Space]Play" : "[Space]Pause", "pause", IsActive: isPaused));
        segments.Add(new Segment(" ", null, IsGap: true));
        segments.Add(new Segment("[R]eset", "reset"));
        segments.Add(new Segment(" ", null, IsGap: true));
        segments.Add(new Segment("[←]", "wind_left"));
        segments.Add(new Segment("[→]Wind", "wind_right"));
        segments.Add(new Segment(" ", null, IsGap: true));
        segments.Add(new Segment("[+]", "snow_plus"));
        segments.Add(new Segment("[-]Snow", "snow_minus"));
        segments.Add(new Segment(" ", null, IsGap: true));
        segments.Add(new Segment("[B]anner", "banner"));
        segments.Add(new Segment(" ", null, IsGap: true));
        segments.Add(new Segment("[C]RT", "crt", IsActive: isCrtActive));
        segments.Add(new Segment(" ", null, IsGap: true));
        segments.Add(new Segment("[F]ull", "fullscreen"));
        segments.Add(new Segment(" ", null, IsGap: true));
        segments.Add(new Segment("[H]ide", "hud"));

        int totalLen = 0;
        foreach (var seg in segments) totalLen += seg.Text.Length;

        // Compact fallback for narrow screens (e.g. mobile portrait)
        if (totalLen > buffer.Width)
        {
            segments.Clear();
            if (hasAudio) segments.Add(new Segment(isMuted ? "[M]ute" : "[M]♫", "music", IsActive: !isMuted));
            segments.Add(new Segment(" ", null, IsGap: true));
            segments.Add(new Segment(isPaused ? "[▶]" : "[⏸]", "pause", IsActive: isPaused));
            segments.Add(new Segment(" ", null, IsGap: true));
            segments.Add(new Segment("[R]", "reset"));
            segments.Add(new Segment(" ", null, IsGap: true));
            segments.Add(new Segment("[←]", "wind_left"));
            segments.Add(new Segment("[→]", "wind_right"));
            segments.Add(new Segment(" ", null, IsGap: true));
            segments.Add(new Segment("[+]", "snow_plus"));
            segments.Add(new Segment("[-]", "snow_minus"));
            segments.Add(new Segment(" ", null, IsGap: true));
            segments.Add(new Segment("[C]", "crt", IsActive: isCrtActive));
            segments.Add(new Segment(" ", null, IsGap: true));
            segments.Add(new Segment("[F]", "fullscreen"));
            segments.Add(new Segment(" ", null, IsGap: true));
            segments.Add(new Segment("[H]", "hud"));

            totalLen = 0;
            foreach (var seg in segments) totalLen += seg.Text.Length;
        }

        int curX = Math.Max(0, (buffer.Width - totalLen) / 2);

        foreach (var seg in segments)
        {
            if (curX >= buffer.Width) break;

            if (seg.IsGap || seg.ActionId == null)
            {
                DrawSegment(buffer, curX, y, seg.Text, false, seg.IsActive, null);
            }
            else
            {
                int endX = Math.Min(buffer.Width - 1, curX + seg.Text.Length - 1);
                bool isHovered = hoverRow == y && hoverCol >= curX && hoverCol <= endX;
                DrawSegment(buffer, curX, y, seg.Text, isHovered, seg.IsActive, seg.ActionId);
            }

            curX += seg.Text.Length;
        }
    }

    private static void DrawSegment(
        ScreenBuffer buffer,
        int startX,
        int y,
        string text,
        bool isHovered,
        bool isActive,
        string? actionId)
    {
        if (actionId != null)
        {
            int endX = Math.Min(buffer.Width - 1, startX + text.Length - 1);
            _hitboxes.Add(new HudHitbox(actionId, startX, endX, y));
        }

        int dimGray = ColorRgb.Pack(135, 155, 175);
        int brightCyan = ColorRgb.Pack(100, 220, 255);
        int musicNoteColor = ColorRgb.Pack(255, 215, 0);
        int activeColor = ColorRgb.Pack(120, 255, 160);
        int pauseColor = ColorRgb.Pack(255, 95, 95);
        int hoverFg = ColorRgb.Pack(255, 255, 255);
        int hoverBg = isHovered ? ColorRgb.Pack(35, 65, 110) : 0;

        for (int i = 0; i < text.Length; i++)
        {
            if (startX + i >= buffer.Width) break;
            char c = text[i];

            int col;
            if (isHovered)
            {
                col = c == '♫' ? musicNoteColor : hoverFg;
            }
            else if (actionId == "pause" && isActive)
            {
                col = pauseColor;
            }
            else if (c == '♫')
            {
                col = musicNoteColor;
            }
            else if (isActive && c is '[' or ']')
            {
                col = activeColor;
            }
            else if (c is '[' or ']')
            {
                col = brightCyan;
            }
            else
            {
                col = isActive ? ColorRgb.Pack(210, 240, 220) : dimGray;
            }

            buffer.TrySetCell(startX + i, y, c, col, 10.0f, hoverBg);
        }
    }
}
