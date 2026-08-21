namespace JunhyunHelper.Desktop.Scanner;

public sealed class ScannerDisplaySettings
{
    public const int CurrentSchemaVersion = 3;

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
    public string OneShotHotkey { get; set; } = ScannerHotkeyGesture.Default.ToString();

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
        OneShotHotkey = OneShotHotkey,
    };

    public void Normalize()
    {
        // v1.1.4 persisted the icon and trader-per-slot switches as false by default.
        // v1.1.5 makes the complete matched-item presentation the product default. The
        // one-time migration deliberately turns these fields on so existing installs do
        // not keep the old accidental defaults forever.
        if (SchemaVersion < 2)
        {
            ShowItemIcon = true;
            ShowTraderSellPrice = true;
            ShowTraderPricePerSlot = true;
        }

        // v1.2.0 adds an optional global one-shot high-precision scan hotkey. Existing
        // settings files have no field, so migrate them to the safe modified-key default.
        if (SchemaVersion < 3 && string.IsNullOrWhiteSpace(OneShotHotkey))
            OneShotHotkey = ScannerHotkeyGesture.Default.ToString();
        SchemaVersion = CurrentSchemaVersion;

        if (!string.IsNullOrWhiteSpace(OneShotHotkey) &&
            !ScannerHotkeyGesture.TryParse(OneShotHotkey, out _))
        {
            OneShotHotkey = ScannerHotkeyGesture.Default.ToString();
        }

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
}
