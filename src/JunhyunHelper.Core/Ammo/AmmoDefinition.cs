namespace JunhyunHelper.Core.Ammo;

public enum AmmoAcquisitionKind
{
    TraderPurchase,
    TraderBarter,
    HideoutCraft,
}

public sealed record AmmoAcquisitionRequirement(
    string ItemId,
    decimal Count,
    bool IsTool = false);

public sealed record AmmoAcquisition(
    AmmoAcquisitionKind Kind,
    string? ReferenceId,
    string? TraderId,
    string? StationId,
    int RequiredLevel,
    string? TaskUnlockQuestId,
    decimal OutputCount,
    decimal? Price,
    string? CurrencyItemId,
    string? CurrencyCode,
    int? DurationSeconds,
    int? BuyLimit,
    IReadOnlyList<AmmoAcquisitionRequirement> Requirements);

public sealed record AmmoDefinition(
    string ItemId,
    string Caliber,
    string? AmmoType,
    int ProjectileCount,
    int Damage,
    int ArmorDamage,
    int PenetrationPower,
    decimal FragmentationChance,
    decimal RicochetChance,
    decimal AccuracyModifier,
    decimal RecoilModifier,
    decimal InitialSpeed,
    decimal HeavyBleedModifier,
    decimal LightBleedModifier,
    bool Tracer,
    string? TracerColor,
    IReadOnlyList<AmmoAcquisition> Acquisitions);
