using JunhyunHelper.Core.Items;

namespace JunhyunHelper.Core.FarmingGuide;

/// <summary>
/// v1.16 deterministic equipment-superiority manual. Each equipment family has one
/// representative criterion; armored rigs are the deliberate two-step exception because
/// they are both protection and storage equipment. Unknown instance facts such as
/// durability or live weapon assembly never participate.
/// </summary>
public static class FarmingGuideEquipmentUpgradePolicy
{
    public static bool IsProtectiveUpgrade(GameItem incoming, GameItem existing)
    {
        ArgumentNullException.ThrowIfNull(incoming);
        ArgumentNullException.ThrowIfNull(existing);

        var incomingClass = ArmorClass(incoming);
        var existingClass = ArmorClass(existing);
        return incomingClass is > 0 &&
               existingClass is > 0 &&
               incomingClass.Value > existingClass.Value;
    }

    public static bool IsHeadsetUpgrade(GameItem incoming, GameItem existing)
    {
        ArgumentNullException.ThrowIfNull(incoming);
        ArgumentNullException.ThrowIfNull(existing);

        var incomingDistance = incoming.FarmingGuideData?.HeadsetDistanceModifier;
        var existingDistance = existing.FarmingGuideData?.HeadsetDistanceModifier;
        return incomingDistance is not null &&
               existingDistance is not null &&
               incomingDistance.Value > existingDistance.Value;
    }

    public static bool IsCarrierUpgrade(
        FarmingGuideStorageKind kind,
        GameItem incoming,
        GameItem existing)
    {
        ArgumentNullException.ThrowIfNull(incoming);
        ArgumentNullException.ThrowIfNull(existing);

        if (kind is not (FarmingGuideStorageKind.Rig or FarmingGuideStorageKind.Backpack or FarmingGuideStorageKind.SecureContainer))
            return false;

        var incomingCapacity = StorageCapacity(incoming);
        var existingCapacity = StorageCapacity(existing);
        if (incomingCapacity <= 0 || existingCapacity <= 0)
            return false;

        if (kind is FarmingGuideStorageKind.Backpack or FarmingGuideStorageKind.SecureContainer)
            return incomingCapacity > existingCapacity;

        var incomingArmored = incoming.FarmingGuideData?.IsArmoredRig == true;
        var existingArmored = existing.FarmingGuideData?.IsArmoredRig == true;

        // Ordinary rigs are storage equipment: capacity is the sole superiority fact.
        if (!incomingArmored && !existingArmored)
            return incomingCapacity > existingCapacity;

        // An ordinary rig never automatically replaces an armored rig because that would
        // discard the rig's primary protective role.
        if (!incomingArmored && existingArmored)
            return false;

        // Ordinary -> armored adds a protection role. A positive armor class proves the
        // equipment-class upgrade; actual content migration is validated separately by the
        // transition planner and lock/reservation inheritance rules.
        if (incomingArmored && !existingArmored)
            return ArmorClass(incoming) is > 0;

        // Armored rig: armor class first. Only an equal class falls through to capacity.
        var incomingClass = ArmorClass(incoming);
        var existingClass = ArmorClass(existing);
        if (incomingClass is null || existingClass is null)
            return false;
        if (incomingClass.Value != existingClass.Value)
            return incomingClass.Value > existingClass.Value;
        return incomingCapacity > existingCapacity;
    }

    public static bool IsBodyArmorToArmoredRigUpgrade(GameItem incomingRig, GameItem bodyArmor)
    {
        ArgumentNullException.ThrowIfNull(incomingRig);
        ArgumentNullException.ThrowIfNull(bodyArmor);

        if (incomingRig.FarmingGuideData?.IsArmoredRig != true)
            return false;

        var incomingClass = ArmorClass(incomingRig);
        var existingClass = ArmorClass(bodyArmor);
        return incomingClass is > 0 &&
               existingClass is > 0 &&
               incomingClass.Value > existingClass.Value;
    }

    public static int? ArmorClass(GameItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return item.FarmingGuideData?.ArmorClass is > 0 ? item.FarmingGuideData.ArmorClass : null;
    }

    public static int StorageCapacity(GameItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return item.FarmingGuideData?.StorageCapacity ?? 0;
    }
}
