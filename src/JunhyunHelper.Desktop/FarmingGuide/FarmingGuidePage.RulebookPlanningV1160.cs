using JunhyunHelper.Core.FarmingGuide;
using JunhyunHelper.Core.Items;
using JunhyunHelper.Desktop.Scanner;

namespace JunhyunHelper.Desktop.FarmingGuide;

public partial class FarmingGuidePage
{
    /// <summary>
    /// v1.16 manual boundary: empty compatible slots may still be used, but an occupied
    /// equipment/carrier target is replaceable only by the explicit representative
    /// superiority rules. Market value never upgrades worn equipment.
    /// </summary>
    private RaidRecommendation PlanScannedItemRulebookV1160(
        FarmingGuideLoadoutSnapshot current,
        ScannerItemSnapshot scanned,
        GameItem incoming)
    {
        if (TryBuildProtectiveUpgrade(current, incoming, out var protective))
            return protective;
        if (TryBuildHeadsetUpgrade(current, incoming, out var headset))
            return headset;
        if (TryBuildCarrierUpgradeRulebookV1160(current, incoming, out var carrier, out var carrierHandled))
            return carrier;

        var addedEquipmentLocks = new List<FarmingGuideEquipmentSlot>();
        var addedCarrierLocks = new List<FarmingGuideStorageKind>();
        try
        {
            // Every occupied compatible equipment slot is temporarily protected from the
            // generic economic replacement planner. This is an internal planning guard,
            // not a user-visible lock and is removed immediately after this call.
            foreach (var pair in current.Equipment)
            {
                if (_lockedEquipmentSlots.Contains(pair.Key) ||
                    !FarmingGuideCompatibility.IsEquipmentSlotCompatible(pair.Key, incoming))
                {
                    continue;
                }

                _lockedEquipmentSlots.Add(pair.Key);
                addedEquipmentLocks.Add(pair.Key);
            }

            foreach (var pair in new[]
                     {
                         (FarmingGuideStorageKind.Rig, current.Rig),
                         (FarmingGuideStorageKind.Backpack, current.Backpack),
                         (FarmingGuideStorageKind.SecureContainer, current.SecureContainer),
                     })
            {
                if (pair.Item2 is null || _lockedCarriers.Contains(pair.Item1) ||
                    !FarmingGuideCompatibility.IsStorageCarrierCompatible(pair.Item1, incoming))
                {
                    continue;
                }

                _lockedCarriers.Add(pair.Item1);
                addedCarrierLocks.Add(pair.Item1);
            }

            // If an explicit carrier upgrade was recognized but could not satisfy protected
            // contents/reservations, the temporary carrier lock above prevents any fallback
            // path from bypassing that failure. The item may still be stored as ordinary loot.
            _ = carrierHandled;
            return PlanScannedItemHardened(scanned, incoming);
        }
        finally
        {
            foreach (var slot in addedEquipmentLocks)
                _lockedEquipmentSlots.Remove(slot);
            foreach (var kind in addedCarrierLocks)
                _lockedCarriers.Remove(kind);
        }
    }
}
