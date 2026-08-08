namespace JunhyunHelper.Core.Profiles;

public readonly record struct TraderProgress(int? LoyaltyLevel, decimal? Standing)
{
    public TraderProgress Normalize() => new(
        LoyaltyLevel is null ? null : Math.Max(0, LoyaltyLevel.Value),
        Standing);
}
