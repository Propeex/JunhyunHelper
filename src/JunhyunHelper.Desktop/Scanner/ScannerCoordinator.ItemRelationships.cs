using System.Windows.Media;
using JunhyunHelper.Core.Items;

namespace JunhyunHelper.Desktop.Scanner;

public sealed record ScannerItemLink(string ItemId, string OfficialName, ImageSource? Icon);
public sealed record ScannerItemMaterialRow(ScannerItemLink Item, decimal Count, bool IsTool);

public sealed record ScannerItemUsageRow(
    string SourceName,
    int RequiredLevel,
    ScannerItemLink Product,
    decimal ProductCount,
    IReadOnlyList<ScannerItemMaterialRow> Materials);

public enum ScannerItemRequirementUsageKind { Quest, Hideout }

public sealed record ScannerItemRequirementUsageRow(
    ScannerItemRequirementUsageKind Kind,
    string TargetId,
    string SourceName,
    int Count,
    bool FoundInRaid,
    int? TargetLevel = null);

public enum ScannerItemAcquisitionKind { TraderPurchase, TraderBarter, HideoutCraft, FleaMarket, Raid }

public sealed record ScannerItemAcquisitionRow(
    ScannerItemAcquisitionKind Kind,
    string SourceName,
    int? RequiredLevel,
    IReadOnlyList<ScannerItemMaterialRow> Materials,
    decimal ProductCount = 1,
    decimal? Price = null,
    string? CurrencyCode = null,
    int? BuyLimit = null,
    string? RefreshTime = null,
    int? DurationSeconds = null,
    int? FleaAveragePrice = null);

public sealed record ScannerItemRelationshipDetails(
    IReadOnlyList<ScannerItemRequirementUsageRow> QuestUsages,
    IReadOnlyList<ScannerItemRequirementUsageRow> HideoutUsages,
    IReadOnlyList<ScannerItemUsageRow> CraftUsages,
    IReadOnlyList<ScannerItemUsageRow> BarterUsages,
    IReadOnlyList<ScannerItemAcquisitionRow> Acquisitions);

public sealed partial class ScannerCoordinator
{
    private ScannerItemRelationshipDetails? BuildItemRelationshipDetails(ScannerDataContext context, string itemId)
    {
        if (context.Content.ItemRelationshipData is null)
            return null;

        var relation = ItemRelationshipQuery.ForItem(context.Content.ItemRelationshipData, itemId);
        var contentIndex = GetContentPresentationIndex(context.Content);
        var questRequirements = contentIndex.QuestRequirementsByItemId.TryGetValue(itemId, out var indexedQuestRequirements)
            ? indexedQuestRequirements
            : Array.Empty<JunhyunHelper.Core.Quests.QuestItemRequirement>();
        var questUsages = questRequirements
            .Select(requirement =>
            {
                contentIndex.QuestsById.TryGetValue(requirement.QuestId, out var quest);
                return new ScannerItemRequirementUsageRow(
                    ScannerItemRequirementUsageKind.Quest,
                    requirement.QuestId,
                    FirstNonBlank(quest?.NameKo, quest?.NameEn, requirement.QuestId),
                    requirement.Count,
                    requirement.FoundInRaid);
            })
            .OrderBy(row => row.SourceName, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(row => row.TargetId, StringComparer.Ordinal)
            .ToArray();

        var hideoutRequirements = contentIndex.HideoutRequirementsByItemId.TryGetValue(itemId, out var indexedHideoutRequirements)
            ? indexedHideoutRequirements
            : Array.Empty<ScannerHideoutRequirementReference>();
        var hideoutUsages = hideoutRequirements
            .Select(entry => new ScannerItemRequirementUsageRow(
                ScannerItemRequirementUsageKind.Hideout,
                entry.Station.Id,
                FirstNonBlank(entry.Station.NameKo, entry.Station.NameEn, entry.Station.Id),
                entry.Requirement.Count,
                entry.Requirement.FoundInRaid,
                entry.Requirement.TargetLevel))
            .OrderBy(row => row.SourceName, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(row => row.TargetLevel ?? 0)
            .ToArray();

        var craftUsages = relation.CraftsUsingItem.Select(craft => new ScannerItemUsageRow(
                ResolveStationName(contentIndex, craft.StationId), craft.RequiredLevel,
                ResolveItemLink(contentIndex, craft.ProductItemId), craft.ProductCount,
                ResolveMaterials(contentIndex, craft.RequiredItems)))
            .OrderBy(row => row.SourceName, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(row => row.RequiredLevel)
            .ThenBy(row => row.Product.OfficialName, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(row => row.Product.ItemId, StringComparer.Ordinal).ToArray();

        var barterUsages = relation.BartersUsingItem.Select(barter => new ScannerItemUsageRow(
                ResolveTraderName(contentIndex, barter.TraderId), barter.RequiredLevel,
                ResolveItemLink(contentIndex, barter.ProductItemId), barter.ProductCount,
                ResolveMaterials(contentIndex, barter.RequiredItems)))
            .OrderBy(row => row.SourceName, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(row => row.RequiredLevel)
            .ThenBy(row => row.Product.OfficialName, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(row => row.Product.ItemId, StringComparer.Ordinal).ToArray();

        var acquisitions = new List<ScannerItemAcquisitionRow>();
        acquisitions.AddRange(relation.TraderPurchasesForItem.Select(purchase =>
        {
            contentIndex.TradersById.TryGetValue(purchase.TraderId, out var trader);
            return new ScannerItemAcquisitionRow(
                ScannerItemAcquisitionKind.TraderPurchase,
                ResolveTraderName(contentIndex, purchase.TraderId),
                purchase.RequiredLevel,
                [],
                Price: purchase.Price,
                CurrencyCode: purchase.CurrencyCode,
                BuyLimit: purchase.BuyLimit,
                RefreshTime: trader?.ResetTime);
        }));
        acquisitions.AddRange(relation.BartersForItem.Select(barter =>
            new ScannerItemAcquisitionRow(
                ScannerItemAcquisitionKind.TraderBarter,
                ResolveTraderName(contentIndex, barter.TraderId),
                barter.RequiredLevel,
                ResolveMaterials(contentIndex, barter.RequiredItems),
                barter.ProductCount,
                BuyLimit: barter.BuyLimit,
                RefreshTime: contentIndex.TradersById.TryGetValue(barter.TraderId, out var trader)
                    ? trader.ResetTime
                    : null)));
        acquisitions.AddRange(relation.CraftsForItem.Select(craft =>
            new ScannerItemAcquisitionRow(
                ScannerItemAcquisitionKind.HideoutCraft,
                ResolveStationName(contentIndex, craft.StationId),
                craft.RequiredLevel,
                ResolveMaterials(contentIndex, craft.RequiredItems),
                craft.ProductCount,
                DurationSeconds: craft.DurationSeconds)));
        if (relation.FleaMarketAvailable)
        {
            _catalog.TryGetItem(itemId, out var catalogItem);
            acquisitions.Add(new ScannerItemAcquisitionRow(
                ScannerItemAcquisitionKind.FleaMarket, "플리마켓", null, [],
                FleaAveragePrice: catalogItem?.FleaAveragePrice));
        }

        // The current relationship schema has no authoritative per-item "cannot spawn in raid"
        // field. Preserve the established product meaning: raid acquisition is the fallback
        // source when no canonical purchase/barter/craft/flea source exists. Presentation can
        // therefore distinguish "raid only" from "raid available alongside other sources"
        // without inventing a false negative.
        acquisitions.Add(new ScannerItemAcquisitionRow(ScannerItemAcquisitionKind.Raid, "레이드", null, []));

        return new ScannerItemRelationshipDetails(
            questUsages,
            hideoutUsages,
            craftUsages,
            barterUsages,
            acquisitions.OrderBy(row => AcquisitionRank(row.Kind))
                .ThenBy(row => row.SourceName, StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(row => row.RequiredLevel ?? 0).ToArray());
    }

    private ScannerItemLink ResolveItemLink(
        ScannerContentPresentationIndex contentIndex,
        string itemId)
    {
        _catalog.TryGetItem(itemId, out var scannerItem);
        contentIndex.ItemsById.TryGetValue(itemId, out var canonical);
        var name = !string.IsNullOrWhiteSpace(scannerItem?.OfficialName)
            ? scannerItem.OfficialName
            : FirstNonBlank(canonical?.NameKo, canonical?.NameEn, itemId);
        var iconUrl = canonical?.IconUrl ?? scannerItem?.IconUrl;
        return new ScannerItemLink(itemId, name, _icons.Load($"item-{itemId}", iconUrl));
    }

    private IReadOnlyList<ScannerItemMaterialRow> ResolveMaterials(
        ScannerContentPresentationIndex contentIndex,
        IReadOnlyList<ItemIngredient> materials) =>
        materials.Select(material => new ScannerItemMaterialRow(
                ResolveItemLink(contentIndex, material.ItemId), material.Count, material.IsTool))
            .OrderBy(row => row.Item.OfficialName, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(row => row.Item.ItemId, StringComparer.Ordinal).ToArray();

    private static string ResolveTraderName(ScannerContentPresentationIndex contentIndex, string traderId)
    {
        contentIndex.TradersById.TryGetValue(traderId, out var trader);
        return FirstNonBlank(trader?.NameKo, trader?.NameEn, traderId);
    }

    private static string ResolveStationName(ScannerContentPresentationIndex contentIndex, string stationId)
    {
        contentIndex.StationsById.TryGetValue(stationId, out var station);
        return FirstNonBlank(station?.NameKo, station?.NameEn, stationId);
    }

    private static string FirstNonBlank(string? primary, string? secondary, string fallback) =>
        !string.IsNullOrWhiteSpace(primary) ? primary.Trim() : !string.IsNullOrWhiteSpace(secondary) ? secondary.Trim() : fallback;

    private static int AcquisitionRank(ScannerItemAcquisitionKind kind) => kind switch
    {
        ScannerItemAcquisitionKind.HideoutCraft => 0,
        ScannerItemAcquisitionKind.TraderBarter => 1,
        ScannerItemAcquisitionKind.TraderPurchase => 2,
        ScannerItemAcquisitionKind.FleaMarket => 3,
        ScannerItemAcquisitionKind.Raid => 4,
        _ => 9,
    };
}
