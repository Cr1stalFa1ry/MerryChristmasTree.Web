using MerryChristmasTree.Rendering;

namespace MerryChristmasTree;

public sealed class AppState
{
    public bool IsPaused { get; set; } = false;
    public bool ShowBanner { get; set; } = true;
    public bool ShowHud { get; set; } = true;
    public int PaletteIndex { get; private set; } = 0;

    public float GrowthTime { get; set; } = 0f;
    public float GlobalTime { get; set; } = 0f;

    public GarlandPalette CurrentPalette => Palettes.All[PaletteIndex];

    public void NextPalette()
    {
        PaletteIndex = (PaletteIndex + 1) % Palettes.All.Length;
    }

    public void RestartGrowth()
    {
        GrowthTime = 0f;
    }

    public void TogglePause()
    {
        IsPaused = !IsPaused;
    }

    public void ToggleBanner()
    {
        ShowBanner = !ShowBanner;
    }

    public void ToggleHud()
    {
        ShowHud = !ShowHud;
    }
}
