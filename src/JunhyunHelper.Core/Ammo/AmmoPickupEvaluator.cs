using JunhyunHelper.Core.Profiles;

namespace JunhyunHelper.Core.Ammo;

/// <summary>
/// Profile-aware Scanner decision for one canonical ammunition item.
/// The purchasable band is defined only by direct trader purchases that the current
/// profile can actually use. Barters, crafts, flea availability and unknown quest/trader
/// state never make ammunition directly purchasable.
/// </summary>
public sealed record AmmoPickupDecision(
    string AmmoItemId,
    string Caliber,
    int PenetrationPower,
    int Rank,
    bool IsDirectlyPurchasable,
    bool ShouldPickUp,
    int? PurchasableMinPenetration,
    int? PurchasableMaxPenetration);

public static class AmmoPickupEvaluator
{
    public static AmmoPickupDecision? Evaluate(
        string ammoItemId,
        IReadOnlyList<AmmoDefinition> ammunition,
        GameProfileSnapshot profile)
    {
        ArgumentNullException.ThrowIfNull(ammunition);
        ArgumentNullException.ThrowIfNull(profile);

        if (string.IsNullOrWhiteSpace(ammoItemId))
            return null;

        var normalizedId = ammoItemId.Trim();
        var target = ammunition.FirstOrDefault(ammo =>
            string.Equals(ammo.ItemId, normalizedId, StringComparison.Ordinal));
        if (target is null || string.IsNullOrWhiteSpace(target.Caliber))
            return null;

        var sameCaliber = ammunition
            .Where(ammo => string.Equals(ammo.Caliber, target.Caliber, StringComparison.Ordinal))
            .OrderByDescending(static ammo => ammo.PenetrationPower)
            .ThenBy(static ammo => ammo.ItemId, StringComparer.Ordinal)
            .ToArray();
        if (sameCaliber.Length == 0)
            return null;

        var rank = Array.FindIndex(
            sameCaliber,
            ammo => string.Equals(ammo.ItemId, target.ItemId, StringComparison.Ordinal)) + 1;
        if (rank <= 0)
            return null;

        var targetPurchasable = IsDirectlyPurchasable(target, profile);
        var purchasable = sameCaliber
            .Where(ammo => IsDirectlyPurchasable(ammo, profile))
            .ToArray();

        if (targetPurchasable)
        {
            return new AmmoPickupDecision(
                target.ItemId,
                target.Caliber,
                target.PenetrationPower,
                rank,
                IsDirectlyPurchasable: true,
                ShouldPickUp: false,
                PurchasableMinPenetration: purchasable.Min(static ammo => ammo.PenetrationPower),
                PurchasableMaxPenetration: purchasable.Max(static ammo => ammo.PenetrationPower));
        }

        if (purchasable.Length == 0)
        {
            return new AmmoPickupDecision(
                target.ItemId,
                target.Caliber,
                target.PenetrationPower,
                rank,
                IsDirectlyPurchasable: false,
                ShouldPickUp: true,
                PurchasableMinPenetration: null,
                PurchasableMaxPenetration: null);
        }

        var minPenetration = purchasable.Min(static ammo => ammo.PenetrationPower);
        var maxPenetration = purchasable.Max(static ammo => ammo.PenetrationPower);
        var outsidePurchasableBand =
            target.PenetrationPower < minPenetration ||
            target.PenetrationPower > maxPenetration;

        return new AmmoPickupDecision(
            target.ItemId,
            target.Caliber,
            target.PenetrationPower,
            rank,
            IsDirectlyPurchasable: false,
            ShouldPickUp: outsidePurchasableBand,
            PurchasableMinPenetration: minPenetration,
            PurchasableMaxPenetration: maxPenetration);
    }

    public static bool IsDirectlyPurchasable(AmmoDefinition ammunition, GameProfileSnapshot profile)
    {
        ArgumentNullException.ThrowIfNull(ammunition);
        ArgumentNullException.ThrowIfNull(profile);

        return ammunition.Acquisitions.Any(acquisition =>
        {
            if (acquisition.Kind != AmmoAcquisitionKind.TraderPurchase ||
                string.IsNullOrWhiteSpace(acquisition.TraderId))
            {
                return false;
            }

            if (!profile.Traders.TryGetValue(acquisition.TraderId, out var progress) ||
                progress.LoyaltyLevel is not { } loyaltyLevel ||
                loyaltyLevel < acquisition.RequiredLevel)
            {
                return false;
            }

            return string.IsNullOrWhiteSpace(acquisition.TaskUnlockQuestId) ||
                   profile.CompletedQuestIds.Contains(acquisition.TaskUnlockQuestId);
        });
    }
}
