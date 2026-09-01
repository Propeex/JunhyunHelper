using JunhyunHelper.Core.FarmingGuide;
using JunhyunHelper.Core.Items;
using JunhyunHelper.Desktop.Scanner;

namespace JunhyunHelper.Desktop.FarmingGuide;

public partial class FarmingGuidePage
{
    private void VerifyDedicatedNestedRaidPrioritySmoke()
    {
        const string secureId = "__junhyun_smoke_priority_secure";
        const string caseId = "__junhyun_smoke_priority_case";
        const string keyId = "__junhyun_smoke_priority_key";
        const string keyCategoryId = "__junhyun_smoke_priority_key_category";
        const string parentInstanceId = "__junhyun_smoke_priority_parent";

        var ids = new[] { secureId, caseId, keyId };
        var previousItems = ids.ToDictionary(
            id => id,
            id => _itemsById.TryGetValue(id, out var item) ? item : null,
            StringComparer.Ordinal);
        var previousSecure = _secureContainer;

        var secure = SmokeItem(secureId) with
        {
            FarmingGuideData = new FarmingGuideItemLayout(
                "ItemPropertiesContainer",
                [new FarmingGuideStorageGridDefinition(2, 2, FarmingGuideItemFilter.Empty)],
                [],
                [],
                [],
                [],
                false,
                false),
        };
        var specializedCase = SmokeItem(caseId) with
        {
            FarmingGuideData = new FarmingGuideItemLayout(
                "ItemPropertiesContainer",
                [
                    new FarmingGuideStorageGridDefinition(
                        1,
                        2,
                        new FarmingGuideItemFilter([keyCategoryId], [], [], [])),
                ],
                [],
                [],
                [],
                [],
                false,
                false),
        };
        var key = SmokeItem(keyId) with
        {
            CategoryIds = [keyCategoryId],
        };

        _itemsById[secureId] = secure;
        _itemsById[caseId] = specializedCase;
        _itemsById[keyId] = key;
        _secureContainer = FarmingGuideItemState.Create(secureId);
        _storedItems.Add(new FarmingGuideStoredItemState(
            parentInstanceId,
            FarmingGuideItemState.Create(caseId),
            FarmingGuideStorageKind.SecureContainer,
            0,
            0,
            0,
            false));

        try
        {
            var scanned = new ScannerItemSnapshot(
                keyId,
                keyId,
                null,
                TraderSellPrice: 0,
                FleaAveragePrice: 0,
                TraderPricePerSlot: 0,
                FleaPricePerSlot: 0,
                Slots: 1,
                CurrentNeeded: 0);

            var recommendation = PlanScannedItem(scanned, key);
            if (recommendation.Action != FarmingGuideInstructionAction.Store)
            {
                throw new InvalidOperationException(
                    $"Dedicated nested storage priority returned unexpected action {recommendation.Action}.");
            }

            var added = recommendation.ProposedSnapshot.StoredItems
                .FirstOrDefault(item =>
                    string.Equals(item.Item.ItemId, keyId, StringComparison.Ordinal) &&
                    string.Equals(item.ParentInstanceId, parentInstanceId, StringComparison.Ordinal));
            if (added is null)
            {
                throw new InvalidOperationException(
                    "Raid advisor consumed general storage before the compatible dedicated nested container.");
            }
        }
        finally
        {
            _storedItems.RemoveAll(item =>
                string.Equals(item.InstanceId, parentInstanceId, StringComparison.Ordinal) ||
                string.Equals(item.ParentInstanceId, parentInstanceId, StringComparison.Ordinal));
            _secureContainer = previousSecure;

            foreach (var id in ids)
            {
                if (previousItems[id] is { } original)
                    _itemsById[id] = original;
                else
                    _itemsById.Remove(id);
            }
        }
    }
}
