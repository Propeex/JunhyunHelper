using JunhyunHelper.Core.FarmingGuide;
using JunhyunHelper.Core.Items;

namespace JunhyunHelper.Desktop.FarmingGuide;

public partial class FarmingGuidePage
{
    /// <summary>
    /// Weight is the one user farming constraint, so automatic advice cannot silently treat
    /// a missing source weight as 0 kg. Presentation may remain tolerant of legacy data, but
    /// the optimizer requires every weight that can affect admissibility to be proven.
    /// </summary>
    private bool HasProvableRaidWeightDomainV1170(
        FarmingGuideLoadoutSnapshot current,
        GameItem incoming)
    {
        EnsureWeightSettingsLoadedV1160();
        if (incoming.WeightKg is null)
            return false;

        foreach (var pair in current.Equipment)
        {
            if (!FarmingGuideWeightPolicy.EquipmentCountsTowardWeight(pair.Key, _weightSettingsV1160))
                continue;
            if (ResolveItem(pair.Value)?.WeightKg is null)
                return false;
        }

        foreach (var state in new[] { current.Rig, current.Backpack, current.SecureContainer })
        {
            if (state is not null && ResolveItem(state)?.WeightKg is null)
                return false;
        }

        return current.StoredItems.All(stored => ResolveItem(stored.Item)?.WeightKg is not null);
    }
}
