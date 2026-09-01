using JunhyunHelper.Core.Ammo;
using JunhyunHelper.Core.Scanner;
using JunhyunHelper.Infrastructure.Scanner;

namespace JunhyunHelper.Desktop.Scanner;

/// <summary>
/// Bridges a Scanner-confirmed Tarkov item ID to current JunhyunHelper derived state.
/// It never reimplements Quest/Hideout requirement or inventory accounting logic.
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

        var mapping = ScannerPresentationJoin.Resolve(
            catalogItem,
            context.Content.Items,
            context.ItemsWorkspace.Plan.NeededItems
                .Select(static item => (item.ItemId, item.RemainingTotal)));
        var needed = context.ItemsWorkspace.Plan.NeededItems.FirstOrDefault(item =>
            string.Equals(item.ItemId, mapping.ItemId, StringComparison.Ordinal));
        var icon = _icons.Load($"item-{mapping.ItemId}", mapping.IconUrl);

        var evaluationItemId = mapping.ItemId;
        var pack = context.Content.AmmoPacks.FirstOrDefault(candidate =>
            string.Equals(candidate.PackItemId, mapping.ItemId, StringComparison.Ordinal));
        if (pack is not null)
            evaluationItemId = pack.AmmoItemId;

        var pickupDecision = context.Profile is null
            ? null
            : AmmoPickupEvaluator.Evaluate(
                evaluationItemId,
                context.Content.Ammunition,
                context.Profile);
        var evaluatedAmmoName = pickupDecision is null
            ? null
            : DisplayName(context, pickupDecision.AmmoItemId);

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
            mapping.BestTraderName)
        {
            CurrentNeededFir = Math.Max(0, needed?.RemainingFir ?? 0),
            FleaMinimumPrice = mapping.FleaMinimumPrice,
            AmmoShouldPickUp = pickupDecision?.ShouldPickUp,
            EvaluatedAmmoName = pack is null ? null : evaluatedAmmoName,
        };
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

    private static string? DisplayName(ScannerDataContext context, string itemId)
    {
        var item = context.Content.Items.FirstOrDefault(candidate =>
            string.Equals(candidate.Id, itemId, StringComparison.Ordinal));
        return item is null
            ? null
            : !string.IsNullOrWhiteSpace(item.NameKo)
                ? item.NameKo
                : item.NameEn;
    }
}
