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

public sealed record AmmoArmorEffectiveness(
    int Class1,
    int Class2,
    int Class3,
    int Class4,
    int Class5,
    int Class6)
{
    public IReadOnlyList<int> Values =>
        [Class1, Class2, Class3, Class4, Class5, Class6];

    public bool IsValid => Values.All(value => value is >= 0 and <= 6);
}

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
    IReadOnlyList<AmmoAcquisition> Acquisitions)
{
    /// <summary>
    /// Optional Class 1-6 comparison ratings copied from the verified Tarkov Wiki
    /// Ballistics table. Null means the current source could not provide a confident
    /// match; callers must not synthesize replacement values.
    /// </summary>
    public AmmoArmorEffectiveness? ArmorEffectiveness { get; init; }
}
