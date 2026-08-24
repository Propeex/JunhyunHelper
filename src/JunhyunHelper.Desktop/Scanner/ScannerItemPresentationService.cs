using JunhyunHelper.Infrastructure.Scanner;

namespace JunhyunHelper.Desktop.Scanner;

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
        if (string.IsNullOrWhiteSpace(itemId) || !_catalog.TryGetItem(itemId.Trim(), out var catalogItem))
            return null;

        var context = _contextProvider();
        if (context is null || _catalog.LoadedMode != context.GameMode)
            return null;

        var canonicalItem = context.Content.Items.FirstOrDefault(item =>
            string.Equals(item.Id, catalogItem.Id, StringComparison.Ordinal));
        var needed = context.ItemsWorkspace.Plan.NeededItems.FirstOrDefault(item =>
            string.Equals(item.ItemId, catalogItem.Id, StringComparison.Ordinal));

        var iconUrl = canonicalItem?.IconUrl ?? catalogItem.IconUrl;
        var icon = _icons.Load($"item-{catalogItem.Id}", iconUrl);

        return new ScannerItemSnapshot(
            catalogItem.Id,
            catalogItem.OfficialName,
            icon,
            catalogItem.BestTraderSellPrice,
            catalogItem.FleaAveragePrice,
            catalogItem.TraderPricePerSlot,
            catalogItem.FleaPricePerSlot,
            catalogItem.Slots,
            needed?.RequiredTotal ?? 0,
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