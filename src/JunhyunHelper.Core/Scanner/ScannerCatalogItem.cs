namespace JunhyunHelper.Core.Scanner;

/// <summary>
/// Scanner-owned full Tarkov item identity/market entry. The identity catalog is
/// deliberately separate from JunhyunHelper's needed-item subset and is keyed by the
/// same Tarkov item ID used by canonical content.
/// </summary>
public sealed record ScannerCatalogItem(
    string Id,
    string OfficialName,
    string ShortName,
    string? IconUrl,
    int? FleaAveragePrice,
    int? BestTraderSellPrice,
    int Width,
    int Height)
{
    /// <summary>
    /// Optional source identity for the highest explicit trader sell offer. These are
    /// additive cache fields so v1/v2 Scanner catalogs can still be read offline.
    /// </summary>
    public string? BestTraderId { get; init; }

    public string? BestTraderName { get; init; }

    public int Slots => Width > 0 && Height > 0 ? Width * Height : 0;

    public int? TraderPricePerSlot => BestTraderSellPrice is { } value && Slots > 0
        ? value / Slots
        : null;

    public int? FleaPricePerSlot => FleaAveragePrice is { } value && Slots > 0
        ? value / Slots
        : null;
}