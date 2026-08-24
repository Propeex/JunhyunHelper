using JunhyunHelper.Core.Items;
using JunhyunHelper.Core.Scanner;
using JunhyunHelper.Infrastructure.Scanner;

namespace JunhyunHelper.Desktop.Scanner;

/// <summary>
/// Primitive presentation values resolved from one Scanner-confirmed Tarkov item ID.
/// Keeping this identity join explicit makes it possible for the release smoke to prove
/// that market, canonical metadata and needed-item state cannot be mixed across IDs.
/// </summary>
internal sealed record ScannerItemPresentationMapping(
    string ItemId,
    string OfficialName,
    string? IconUrl,
    string? WikiUrl,
    int? TraderSellPrice,
    int? FleaAveragePrice,
    int? TraderPricePerSlot,
    int? FleaPricePerSlot,
    int Slots,
    int CurrentNeeded,
    string? BestTraderName);

/// <summary>
/// Bridges a Scanner-confirmed Tarkov item ID to current JunhyunHelper derived state.
/// It never reimplements Quest/Hideout requirement logic and never subtracts ownership
/// from the displayed current-needed value.
/// </summary>
public sealed class ScannerItemPresentationService
{
    private readonly ScannerCatalogService _catalog;
    private readonly ScannerLocalIconService _icons;
    private readonly Func<ScannerDataContext?> _contextProvider;

    public ScannerItemPresentationService(
        ScannerCatalogService catalog,
        ScannerLocalIconService icons,
        Func<ScannerDataContext?> contextProvider)
    {
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        _icons = icons ?? throw new ArgumentNullException(nameof(icons));
        _contextProvider = contextProvider ?? throw new ArgumentNullException(nameof(contextProvider));
    }

    public ScannerItemSnapshot? CreateSnapshot(string itemId)
    {
        using var timing = ScannerLatencyTelemetry.Measure(ScannerLatencyTelemetry.Presentation);

        if (string.IsNullOrWhiteSpace(itemId) || !_catalog.TryGetItem(itemId.Trim(), out var catalogItem))
            return null;

        var context = _contextProvider();
        if (context is null || _catalog.LoadedMode != context.GameMode)
            return null;

        var mapping = ResolveMapping(
            catalogItem,
            context.Content.Items,
            context.ItemsWorkspace.Plan.NeededItems
                .Select(static item => (item.ItemId, item.RequiredTotal)));
        var icon = _icons.Load($"item-{mapping.ItemId}", mapping.IconUrl);

        return new ScannerItemSnapshot(
            mapping.ItemId,
            mapping.OfficialName,
            icon,
            mapping.TraderSellPrice,
            mapping.FleaAveragePrice,
            mapping.TraderPricePerSlot,
            mapping.FleaPricePerSlot,
            mapping.Slots,
            mapping.CurrentNeeded,
            mapping.BestTraderName);
    }

    internal static ScannerItemPresentationMapping ResolveMapping(
        ScannerCatalogItem catalogItem,
        IEnumerable<GameItem> canonicalItems,
        IEnumerable<(string ItemId, int RequiredTotal)> neededItems)
    {
        ArgumentNullException.ThrowIfNull(catalogItem);
        ArgumentNullException.ThrowIfNull(canonicalItems);
        ArgumentNullException.ThrowIfNull(neededItems);

        // The Scanner catalog item is the identity authority after recognition. Every
        // secondary data source is joined only on that exact Tarkov item ID.
        var canonicalItem = canonicalItems.FirstOrDefault(item =>
            string.Equals(item.Id, catalogItem.Id, StringComparison.Ordinal));
        var needed = neededItems.FirstOrDefault(item =>
            string.Equals(item.ItemId, catalogItem.Id, StringComparison.Ordinal));

        return new ScannerItemPresentationMapping(
            catalogItem.Id,
            catalogItem.OfficialName,
            canonicalItem?.IconUrl ?? catalogItem.IconUrl,
            canonicalItem?.WikiUrl,
            catalogItem.BestTraderSellPrice,
            catalogItem.FleaAveragePrice,
            catalogItem.TraderPricePerSlot,
            catalogItem.FleaPricePerSlot,
            catalogItem.Slots,
            needed.RequiredTotal,
            catalogItem.BestTraderName);
    }

    public ScannerItemSnapshot? CreateDefaultPreviewSnapshot()
    {
        var context = _contextProvider();
        if (context is null || _catalog.LoadedMode != context.GameMode)
            return null;

        foreach (var needed in context.ItemsWorkspace.Plan.NeededItems
                     .OrderByDescending(item => item.RequiredTotal))
        {
            var snapshot = CreateSnapshot(needed.ItemId);
            if (snapshot is not null)
                return snapshot;
        }

        var fallback = _catalog.GetItemsSnapshot().FirstOrDefault();
        return fallback is null ? null : CreateSnapshot(fallback.Id);
    }
}
