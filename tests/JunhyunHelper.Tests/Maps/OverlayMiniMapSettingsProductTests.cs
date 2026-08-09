using TarkovHelper.Models.Map;

namespace JunhyunHelper.Tests.Maps;

public sealed class OverlayMiniMapSettingsProductTests
{
    [Fact]
    public void SetHotkey_ReassignsDuplicateKeyToLatestAction()
    {
        var settings = new OverlayMiniMapSettings();

        settings.SetHotkey(OverlayMiniMapHotkeyAction.ToggleOverlay, 0x70);
        settings.SetHotkey(OverlayMiniMapHotkeyAction.SizeIncrease, 0x70);

        Assert.Equal(0, settings.GetHotkey(OverlayMiniMapHotkeyAction.ToggleOverlay));
        Assert.Equal(0x70, settings.GetHotkey(OverlayMiniMapHotkeyAction.SizeIncrease));
        Assert.Equal(
            OverlayMiniMapHotkeyAction.SizeIncrease,
            settings.GetActionForHotkey(0x70));
    }

    [Fact]
    public void OpacityCommands_KeepProductOpacityAtOneHundredPercent()
    {
        var settings = new OverlayMiniMapSettings { Opacity = 0.25 };

        settings.DecreaseOpacity();
        Assert.Equal(1.0, settings.Opacity);

        settings.Opacity = 0.4;
        settings.IncreaseOpacity();
        Assert.Equal(1.0, settings.Opacity);
    }

    [Fact]
    public void ResetToDefaults_ContainsRequiredProductHotkeys()
    {
        var settings = new OverlayMiniMapSettings
        {
            ToggleOverlayKey = 0x70,
            SizeIncreaseKey = 0x71,
            SizeDecreaseKey = 0x72,
            Opacity = 0.3,
        };

        settings.ResetToDefaults();

        Assert.Equal(1.0, settings.Opacity);
        Assert.Equal(0, settings.ToggleOverlayKey);
        Assert.Equal(0, settings.SizeIncreaseKey);
        Assert.Equal(0, settings.SizeDecreaseKey);
        Assert.Equal(0x6B, settings.ZoomInKey);
        Assert.Equal(0x6D, settings.ZoomOutKey);
        Assert.Equal(0x21, settings.FloorUpKey);
        Assert.Equal(0x22, settings.FloorDownKey);
    }
}
