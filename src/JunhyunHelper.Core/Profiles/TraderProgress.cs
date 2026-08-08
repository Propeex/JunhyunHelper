namespace JunhyunHelper.Core.Profiles;

/// <summary>
/// User-observed trader facts. Loyalty level and standing are independent because a user may know
/// one without knowing the other. Null always means "not entered/unknown" and is never coerced to 0.
/// </summary>
public readonly record struct TraderProgress(int? LoyaltyLevel, decimal? Standing)
{
    public TraderProgress Normalize() => new(
        LoyaltyLevel is null ? null : Math.Max(0, LoyaltyLevel.Value),
        Standing);

    public bool HasAnyValue => LoyaltyLevel is not null || Standing is not null;
}
