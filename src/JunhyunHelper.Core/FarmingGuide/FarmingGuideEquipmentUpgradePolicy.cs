using JunhyunHelper.Core.Items;

namespace JunhyunHelper.Core.FarmingGuide;

/// <summary>
/// Conservative source-backed superiority rules for live-raid equipment changes.
/// Market price is intentionally not an equipment-performance metric. A replacement is
/// called an upgrade only when current canonical Tarkov facts prove a strict improvement.
/// Multi-dimensional equipment uses Pareto dominance rather than a guessed scalar tier.
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
        var incomingDistortion = incoming.FarmingGuideData?.HeadsetDistortion;
        var existingDistortion = existing.FarmingGuideData?.HeadsetDistortion;
        if (incomingDistance is null ||
            existingDistance is null ||
            incomingDistortion is null ||
            existingDistortion is null)
        {
            return false;
        }

        // Higher distanceModifier extends useful hearing distance, while lower distortion
        // keeps the amplified signal cleaner. Trade-offs are not an objective upgrade.
        return incomingDistance.Value >= existingDistance.Value &&
               incomingDistortion.Value <= existingDistortion.Value &&
               (incomingDistance.Value > existingDistance.Value ||
                incomingDistortion.Value < existingDistortion.Value);
    }

    public static bool IsCarrierUpgrade(
        FarmingGuideStorageKind kind,
        GameItem incoming,
        GameItem existing)
    {
        ArgumentNullException.ThrowIfNull(incoming);
        ArgumentNullException.ThrowIfNull(existing);

        if (kind is not (FarmingGuideStorageKind.Rig or FarmingGuideStorageKind.Backpack))
            return false;

        var incomingCapacity = StorageCapacity(incoming);
        var existingCapacity = StorageCapacity(existing);
        if (incomingCapacity <= 0 || existingCapacity <= 0)
            return false;

        if (kind == FarmingGuideStorageKind.Backpack)
            return incomingCapacity > existingCapacity;

        var incomingArmored = incoming.FarmingGuideData?.IsArmoredRig == true;
        var existingArmored = existing.FarmingGuideData?.IsArmoredRig == true;
        if (existingArmored && !incomingArmored)
            return false;

        if (!existingArmored && !incomingArmored)
            return incomingCapacity > existingCapacity;

        var incomingClass = ArmorClass(incoming) ?? 0;
        var existingClass = ArmorClass(existing) ?? 0;

        // Ordinary rig -> armored rig is an automatic upgrade only when adding protection
        // does not also reduce raw storage capacity. The body-armor + rig -> armored-rig
        // transition has its own rule because the user explicitly permits a smaller carrier
        // when all actual contents still fit.
        if (!existingArmored && incomingArmored)
            return incomingClass > 0 && incomingCapacity >= existingCapacity;

        // Armored rig -> armored rig uses Pareto dominance: no objective regression in
        // either protection class or capacity, and at least one strict improvement.
        return incomingClass >= existingClass &&
               incomingCapacity >= existingCapacity &&
               (incomingClass > existingClass || incomingCapacity > existingCapacity);
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
