namespace JunhyunHelper.Core.Items;

public enum ItemRequirementSourceKind
{
    Quest,
    Hideout,
}

public sealed record ItemRequirementSource(
    ItemRequirementSourceKind Kind,
    string SourceId,
    string? DetailId = null);

public sealed record ItemRequirement(
    string ItemId,
    int RequiredTotal,
    int RequiredFir,
    ItemRequirementSource Source)
{
    public ItemRequirement Normalize()
    {
        var total = Math.Max(0, Math.Max(RequiredTotal, RequiredFir));
        var fir = Math.Clamp(RequiredFir, 0, total);

        return this with
        {
            RequiredTotal = total,
            RequiredFir = fir,
        };
    }
}
