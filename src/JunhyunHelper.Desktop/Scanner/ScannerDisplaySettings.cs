using System.Text.Json.Serialization;

namespace JunhyunHelper.Desktop.Scanner;

public sealed class ScannerDisplaySettings
{
    public const int CurrentSchemaVersion = 4;

    public int SchemaVersion { get; set; }
    public bool Enabled { get; set; }
    public bool ShowItemName { get; set; } = true;
    public bool ShowItemIcon { get; set; } = true;
    public bool ShowTraderSellPrice { get; set; } = true;
    public bool ShowFleaAveragePrice { get; set; } = true;
    public bool ShowTraderPricePerSlot { get; set; } = true;
    public bool ShowFleaPricePerSlot { get; set; }
    public bool ShowCurrentNeeded { get; set; } = true;
    public double? PositionX { get; set; }
    public double? PositionY { get; set; }
    public double FontSize { get; set; } = 18;
    public string OneShotTarkovHotkey { get; set; } = ScannerHotkeyGesture.DefaultOneShotTarkov.ToString();
    public string OneShotTestHotkey { get; set; } = ScannerHotkeyGesture.DefaultOneShotTest.ToString();
    public string ScannerToggleHotkey { get; set; } = ScannerHotkeyGesture.DefaultScannerToggle.ToString();

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? OneShotHotkey { get; set; }

    public ScannerDisplaySettings Clone() => new()
    {
        SchemaVersion = SchemaVersion,
        Enabled = Enabled,
        ShowItemName = ShowItemName,
        ShowItemIcon = ShowItemIcon,
        ShowTraderSellPrice = ShowTraderSellPrice,
        ShowFleaAveragePrice = ShowFleaAveragePrice,
        ShowTraderPricePerSlot = ShowTraderPricePerSlot,
        ShowFleaPricePerSlot = ShowFleaPricePerSlot,
        ShowCurrentNeeded = ShowCurrentNeeded,
        PositionX = PositionX,
        PositionY = PositionY,
        FontSize = FontSize,
        OneShotTarkovHotkey = OneShotTarkovHotkey,
        OneShotTestHotkey = OneShotTestHotkey,
        ScannerToggleHotkey = ScannerToggleHotkey,
        OneShotHotkey = OneShotHotkey,
    };

    public void Normalize()
    {
        if (SchemaVersion < 2)
        {
            ShowItemIcon = true;
            ShowTraderSellPrice = true;
            ShowTraderPricePerSlot = true;
        }

        if (SchemaVersion < 4)
        {
            if (SchemaVersion >= 3)
            {
                OneShotTarkovHotkey = string.IsNullOrWhiteSpace(OneShotHotkey)
                    ? string.Empty
                    : ScannerHotkeyGesture.TryParse(OneShotHotkey, out var migrated)
                        ? migrated.ToString()
                        : ScannerHotkeyGesture.DefaultOneShotTarkov.ToString();
            }
            else
            {
                OneShotTarkovHotkey = ScannerHotkeyGesture.DefaultOneShotTarkov.ToString();
            }

            OneShotTestHotkey = ScannerHotkeyGesture.DefaultOneShotTest.ToString();
            ScannerToggleHotkey = ScannerHotkeyGesture.DefaultScannerToggle.ToString();
            OneShotHotkey = null;
        }

        OneShotTarkovHotkey = NormalizeHotkey(
            OneShotTarkovHotkey,
            ScannerHotkeyGesture.DefaultOneShotTarkov);
        OneShotTestHotkey = NormalizeHotkey(
            OneShotTestHotkey,
            ScannerHotkeyGesture.DefaultOneShotTest);
        ScannerToggleHotkey = NormalizeHotkey(
            ScannerToggleHotkey,
            ScannerHotkeyGesture.DefaultScannerToggle);
        SchemaVersion = CurrentSchemaVersion;

        if (PositionX is { } x && !double.IsFinite(x))
            PositionX = null;
        if (PositionY is { } y && !double.IsFinite(y))
            PositionY = null;
        if (PositionX.HasValue != PositionY.HasValue)
        {
            PositionX = null;
            PositionY = null;
        }
        FontSize = double.IsFinite(FontSize) ? Math.Clamp(FontSize, 12, 32) : 18;
    }

    private static string NormalizeHotkey(string? value, ScannerHotkeyGesture fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;
        return ScannerHotkeyGesture.TryParse(value, out var gesture)
            ? gesture.ToString()
            : fallback.ToString();
    }
}
