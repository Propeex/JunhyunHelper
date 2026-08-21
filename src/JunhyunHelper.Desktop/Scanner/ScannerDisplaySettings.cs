namespace JunhyunHelper.Desktop.Scanner;

public sealed class ScannerDisplaySettings
{
    public bool Enabled { get; set; }
    public bool ShowItemName { get; set; } = true;
    public bool ShowItemIcon { get; set; }
    public bool ShowTraderSellPrice { get; set; } = true;
    public bool ShowFleaAveragePrice { get; set; } = true;
    public bool ShowTraderPricePerSlot { get; set; }
    public bool ShowFleaPricePerSlot { get; set; }
    public bool ShowCurrentNeeded { get; set; } = true;
    public double? PositionX { get; set; }
    public double? PositionY { get; set; }
    public double FontSize { get; set; } = 18;

    public ScannerDisplaySettings Clone() => new()
    {
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
    };

    public void Normalize()
    {
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
