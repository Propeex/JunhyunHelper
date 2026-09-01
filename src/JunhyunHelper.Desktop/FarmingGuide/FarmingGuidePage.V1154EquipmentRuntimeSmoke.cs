using System.IO;
using System.Windows.Media;
using System.Windows.Threading;
using JunhyunHelper.Core.FarmingGuide;
using JunhyunHelper.Core.Items;
using JunhyunHelper.Desktop.Scanner;

namespace JunhyunHelper.Desktop.FarmingGuide;

public partial class FarmingGuidePage
{
    private bool _v1154EquipmentSmokeScheduled;
    private bool _v1154EquipmentSmokeCompleted;

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);
        if (_v1154EquipmentSmokeScheduled ||
            _v1154EquipmentSmokeCompleted ||
            !string.Equals(
                Environment.GetEnvironmentVariable("JUNHYUNHELPER_MAP_SMOKE"),
                "1",
                StringComparison.Ordinal))
        {
            return;
        }

        ScheduleV1154EquipmentRuntimeSmoke();
    }

    private void ScheduleV1154EquipmentRuntimeSmoke()
    {
        if (_v1154EquipmentSmokeScheduled || _v1154EquipmentSmokeCompleted)
            return;
        _v1154EquipmentSmokeScheduled = true;
        _ = Dispatcher.BeginInvoke(
            DispatcherPriority.ContextIdle,
            new Action(RunV1154EquipmentRuntimeSmokeWhenReady));
    }

    private void RunV1154EquipmentRuntimeSmokeWhenReady()
    {
        if (_v1154EquipmentSmokeCompleted)
            return;

        if (!_productSmokeCompleted)
        {
            _ = Dispatcher.BeginInvoke(
                DispatcherPriority.ContextIdle,
                new Action(RunV1154EquipmentRuntimeSmokeWhenReady));
            return;
        }

        try
        {
            VerifyArmorAndRigToArmoredRigUpgradeSmoke();
            var marker = Path.Combine(
                Path.GetTempPath(),
                "junhyun-farming-guide-v1154-equipment-smoke-success.txt");
            File.WriteAllLines(marker,
            [
                "armor-class-upgrade=ok",
                "body-armor-plus-rig-to-armored-rig=ok",
                "carrier-contents-preserved=ok",
                "carrier-contents-repacked=ok",
                "armored-rig-reverse-not-invented=ok",
            ]);
            _v1154EquipmentSmokeCompleted = true;
        }
        catch (Exception exception)
        {
            var diagnostic = Path.Combine(Path.GetTempPath(), "junhyun-map-smoke-error.txt");
            File.WriteAllText(
                diagnostic,
                "Farming Guide v1.15.4 equipment-upgrade smoke failed." + Environment.NewLine + exception);
            throw;
        }
    }

    private void VerifyArmorAndRigToArmoredRigUpgradeSmoke()
    {
        const string bodyArmorId = "__junhyun_smoke_upgrade_body_armor";
        const string oldRigId = "__junhyun_smoke_upgrade_old_rig";
        const string armoredRigId = "__junhyun_smoke_upgrade_armored_rig";
        const string reverseBodyArmorId = "__junhyun_smoke_upgrade_reverse_body_armor";
        const string itemAId = "__junhyun_smoke_upgrade_item_a";
        const string itemBId = "__junhyun_smoke_upgrade_item_b";
        const string itemCId = "__junhyun_smoke_upgrade_item_c";
        const string instanceA = "__junhyun_smoke_upgrade_instance_a";
        const string instanceB = "__junhyun_smoke_upgrade_instance_b";
        const string instanceC = "__junhyun_smoke_upgrade_instance_c";

        var ids = new[]
        {
            bodyArmorId,
            oldRigId,
            armoredRigId,
            reverseBodyArmorId,
            itemAId,
            itemBId,
            itemCId,
        };
        var previousItems = ids.ToDictionary(
            id => id,
            id => _itemsById.TryGetValue(id, out var item) ? item : null,
            StringComparer.Ordinal);
        var previousSnapshot = BuildSnapshot();
        var previousLocks = BuildLockState();

        static GameItem Item(string id, int width = 1, int height = 1) =>
            new(
                id,
                id,
                id,
                id,
                id,
                null,
                null,
                [],
                [],
                [],
                width,
                height);

        static FarmingGuideItemLayout Layout(
            string propertiesType,
            int? armorClass,
            bool armoredRig,
            params FarmingGuideStorageGridDefinition[] grids) =>
            new(
                propertiesType,
                grids,
                [],
                [],
                [],
                [],
                false,
                armoredRig)
            {
                ArmorClass = armorClass,
            };

        var bodyArmor = Item(bodyArmorId) with
        {
            FarmingGuideData = Layout("ItemPropertiesArmor", 4, false),
        };
        var oldRig = Item(oldRigId) with
        {
            FarmingGuideData = Layout(
                "ItemPropertiesChestRig",
                null,
                false,
                new FarmingGuideStorageGridDefinition(3, 2, FarmingGuideItemFilter.Empty)),
        };
        var armoredRig = Item(armoredRigId) with
        {
            FarmingGuideData = Layout(
                "ItemPropertiesChestRig",
                5,
                true,
                new FarmingGuideStorageGridDefinition(2, 2, FarmingGuideItemFilter.Empty)),
        };
        var reverseBodyArmor = Item(reverseBodyArmorId) with
        {
            FarmingGuideData = Layout("ItemPropertiesArmor", 6, false),
        };
        var itemA = Item(itemAId);
        var itemB = Item(itemBId);
        var itemC = Item(itemCId);

        _itemsById[bodyArmorId] = bodyArmor;
        _itemsById[oldRigId] = oldRig;
        _itemsById[armoredRigId] = armoredRig;
        _itemsById[reverseBodyArmorId] = reverseBodyArmor;
        _itemsById[itemAId] = itemA;
        _itemsById[itemBId] = itemB;
        _itemsById[itemCId] = itemC;

        try
        {
            Equipment.Clear();
            Equipment[FarmingGuideEquipmentSlot.BodyArmor] = FarmingGuideItemState.Create(bodyArmorId);
            SetCarrier(FarmingGuideStorageKind.Rig, FarmingGuideItemState.Create(oldRigId));
            SetCarrier(FarmingGuideStorageKind.Backpack, null);
            SetCarrier(FarmingGuideStorageKind.SecureContainer, null);
            StoredItems.Clear();
            StoredItems.AddRange(
            [
                new FarmingGuideStoredItemState(
                    instanceA,
                    FarmingGuideItemState.Create(itemAId),
                    FarmingGuideStorageKind.Rig,
                    0,
                    0,
                    0,
                    false),
                new FarmingGuideStoredItemState(
                    instanceB,
                    FarmingGuideItemState.Create(itemBId),
                    FarmingGuideStorageKind.Rig,
                    0,
                    1,
                    0,
                    false),
                new FarmingGuideStoredItemState(
                    instanceC,
                    FarmingGuideItemState.Create(itemCId),
                    FarmingGuideStorageKind.Rig,
                    0,
                    2,
                    0,
                    false),
            ]);
            ApplyLockState(FarmingGuideLockState.Empty);

            var scanned = new ScannerItemSnapshot(
                armoredRigId,
                armoredRigId,
                null,
                TraderSellPrice: 1,
                FleaAveragePrice: 1,
                TraderPricePerSlot: 0,
                FleaPricePerSlot: 0,
                Slots: 4,
                CurrentNeeded: 0);
            var recommendation = PlanScannedItemEquipmentAware(scanned, armoredRig);

            if (recommendation.Action != FarmingGuideInstructionAction.ReplaceEquip)
            {
                throw new InvalidOperationException(
                    $"Armor + rig did not transition to superior armored rig: {recommendation.Action}.");
            }
            if (recommendation.ProposedSnapshot.Equipment.ContainsKey(FarmingGuideEquipmentSlot.BodyArmor))
                throw new InvalidOperationException("Body armor remained equipped after armored-rig transition.");
            if (!string.Equals(recommendation.ProposedSnapshot.Rig?.ItemId, armoredRigId, StringComparison.Ordinal))
                throw new InvalidOperationException("Incoming armored rig was not equipped.");

            var preservedIds = recommendation.ProposedSnapshot.StoredItems
                .Where(value => value.ParentInstanceId is null && value.Storage == FarmingGuideStorageKind.Rig)
                .Select(value => value.InstanceId)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
            var expectedIds = new[] { instanceA, instanceB, instanceC }
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
            if (!preservedIds.SequenceEqual(expectedIds, StringComparer.Ordinal))
                throw new InvalidOperationException("Rig contents were not preserved exactly during armored-rig transition.");

            var movedC = recommendation.ProposedSnapshot.StoredItems.Single(value =>
                string.Equals(value.InstanceId, instanceC, StringComparison.Ordinal));
            if (movedC.X == 2)
                throw new InvalidOperationException("Carrier migration did not repack the item that no longer fits its old X coordinate.");

            var sanitized = FarmingGuideLoadoutPolicy.SanitizeSnapshot(
                recommendation.ProposedSnapshot,
                _itemsById,
                _pocketGrids);
            if (sanitized.StoredItems.Count != recommendation.ProposedSnapshot.StoredItems.Count ||
                !string.Equals(sanitized.Rig?.ItemId, armoredRigId, StringComparison.Ordinal) ||
                sanitized.Equipment.ContainsKey(FarmingGuideEquipmentSlot.BodyArmor))
            {
                throw new InvalidOperationException("Proposed armored-rig transition does not survive canonical loadout validation.");
            }

            // Reverse direction cannot materialize a missing ordinary rig. Scanning body
            // armor while an armored rig is worn may be stored if legal, but it must never
            // be proposed as an equipped BodyArmor state while the armored rig remains.
            Equipment.Clear();
            SetCarrier(FarmingGuideStorageKind.Rig, FarmingGuideItemState.Create(armoredRigId));
            StoredItems.Clear();
            var reverseScan = new ScannerItemSnapshot(
                reverseBodyArmorId,
                reverseBodyArmorId,
                null,
                TraderSellPrice: 1,
                FleaAveragePrice: 1,
                TraderPricePerSlot: 0,
                FleaPricePerSlot: 0,
                Slots: 1,
                CurrentNeeded: 0);
            var reverseRecommendation = PlanScannedItemEquipmentAware(reverseScan, reverseBodyArmor);
            if (reverseRecommendation.ProposedSnapshot.Equipment.ContainsKey(FarmingGuideEquipmentSlot.BodyArmor) &&
                reverseRecommendation.ProposedSnapshot.Rig?.ItemId == armoredRigId)
            {
                throw new InvalidOperationException("Reverse armored-rig transition illegally equipped body armor without an ordinary rig.");
            }
        }
        finally
        {
            ApplySnapshot(previousSnapshot);
            ApplyLockState(previousLocks);
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
