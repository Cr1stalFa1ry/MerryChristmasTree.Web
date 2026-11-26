using MerryChristmasTree.Core;
using MerryChristmasTree.Rendering;
using MerryChristmasTree.Simulation;

namespace MerryChristmasTree.Web.Services;

public sealed class TreeEngine
{
    public const int DefaultWidth = 100;
    public const int DefaultHeight = 45;

    public int Width { get; private set; } = DefaultWidth;
    public int Height { get; private set; } = DefaultHeight;

    public AppState State { get; } = new();
    public GroundField Ground { get; } = new();
    public SnowSimulation Snow { get; } = new();
    public TreeRenderer Tree { get; } = new();
    public ScreenBuffer Buffer { get; private set; }

    public bool IsAudioMuted { get; set; } = false;
    public bool HasAudio { get; set; } = true;
    public bool IsCrtActive { get; set; } = false;

    public int HoverCol { get; set; } = -1;
    public int HoverRow { get; set; } = -1;

    private readonly Random _rand = new();
    private byte[] _packedFrame = Array.Empty<byte>();

    public TreeEngine()
    {
        Buffer = new ScreenBuffer(Width, Height);
        AllocatePackedFrame();
    }

    public void Initialize(int width = DefaultWidth, int height = DefaultHeight)
    {
        Width = Math.Max(40, width);
        Height = Math.Max(25, height);

        Buffer.Resize(Width, Height);
        AllocatePackedFrame();

        Ground.Resize(Width, Height, _rand);
        Snow.Resize(Ground, Width, Height);
        Tree.Initialize();
        State.RestartGrowth();
    }

    private void AllocatePackedFrame()
    {
        int total = Width * Height;
        int required = total * 8;
        if (_packedFrame.Length != required)
        {
            _packedFrame = new byte[required];
        }
    }

    public void Resize(int width, int height)
    {
        if (width == Width && height == Height) return;

        Width = Math.Max(40, width);
        Height = Math.Max(25, height);

        Buffer.Resize(Width, Height);
        AllocatePackedFrame();

        Ground.Resize(Width, Height, _rand);
        Snow.Resize(Ground, Width, Height);
    }

    public byte[] Tick(float dt, int width = 0, int height = 0)
    {
        if (width > 0 && height > 0 && (width != Width || height != Height))
        {
            Resize(width, height);
        }

        if (dt > 0.1f) dt = 0.1f;

        if (!State.IsPaused)
        {
            State.GlobalTime += dt;
            State.GrowthTime += dt;
        }

        // 1. Очистка буфера под новый кадр
        Buffer.PrepareFrame();

        // 2. Земля и сугробы
        Ground.RenderGround(Buffer, dt);

        // 3. Ёлка, ленты, игрушки, подарки, звезда
        Tree.RenderTree(Buffer, State.GrowthTime, State.GlobalTime);

        // 4. Падающий снег
        Snow.UpdateAndRender(Buffer, Ground, State.GlobalTime, dt);
        
        // 6. Нижний HUD (всегда вызывается, при ShowHud == false рисует кнопку восстановления)
        BannerRenderer.RenderBottomHud(
            Buffer,
            Snow.UserWindOffset,
            Snow.SnowDensityFactor,
            State.IsPaused,
            IsAudioMuted,
            HasAudio,
            IsCrtActive,
            State.ShowHud,
            HoverCol,
            HoverRow);

        // 7. Упаковка буфера в байтовый массив (8 байт на ячейку: char_lo, char_hi, fg_r, fg_g, fg_b, bg_r, bg_g, bg_b)
        int total = Width * Height;
        var chars = Buffer.Chars;
        var colors = Buffer.Colors;
        var bgColors = Buffer.BgColors;
        var frame = _packedFrame;

        for (int i = 0; i < total; i++)
        {
            char c = chars[i];
            int fg = colors[i];
            int bg = bgColors[i];

            int offset = i * 8;
            frame[offset] = (byte)(c & 0xFF);
            frame[offset + 1] = (byte)((c >> 8) & 0xFF);
            frame[offset + 2] = (byte)((fg >> 16) & 0xFF);
            frame[offset + 3] = (byte)((fg >> 8) & 0xFF);
            frame[offset + 4] = (byte)(fg & 0xFF);
            frame[offset + 5] = (byte)((bg >> 16) & 0xFF);
            frame[offset + 6] = (byte)((bg >> 8) & 0xFF);
            frame[offset + 7] = (byte)(bg & 0xFF);
        }

        return frame;
    }

    public void TogglePause() => State.TogglePause();
    public void RestartGrowth() => State.RestartGrowth();
    public void ToggleBanner() => State.ToggleBanner();
    public void ToggleHud() => State.ToggleHud();

    public void AdjustWind(float delta)
    {
        Snow.UserWindOffset = Math.Clamp(Snow.UserWindOffset + delta, -25f, 25f);
    }

    public void AdjustSnowDensity(float delta)
    {
        Snow.SnowDensityFactor = Math.Clamp(Snow.SnowDensityFactor + delta, 0.25f, 3.0f);
        Snow.Resize(Ground, Width, Height);
    }

    public bool UpdateHover(int col, int row)
    {
        HoverCol = col;
        HoverRow = row;
        return BannerRenderer.HitTest(col, row) != null;
    }

    public string? HandleClick(int col, int row)
    {
        string? action = BannerRenderer.HitTest(col, row);
        if (action == null) return null;

        switch (action)
        {
            case "pause":
                TogglePause();
                break;
            case "reset":
                RestartGrowth();
                break;
            case "wind_left":
                AdjustWind(-3.5f);
                break;
            case "wind_right":
                AdjustWind(3.5f);
                break;
            case "snow_plus":
                AdjustSnowDensity(0.25f);
                break;
            case "snow_minus":
                AdjustSnowDensity(-0.25f);
                break;
            case "banner":
                ToggleBanner();
                break;
            case "hud":
                ToggleHud();
                break;
            case "music":
            case "crt":
            case "fullscreen":
                return action;
        }

        return "handled";
    }
}
