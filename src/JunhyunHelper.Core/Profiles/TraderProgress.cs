namespace JunhyunHelper.Core.Profiles;

public readonly record struct TraderProgress(int LoyaltyLevel, decimal Standing)
{
    public TraderProgress Normalize() => new(
        Math.Max(0, LoyaltyLevel),
        Standing);
}
