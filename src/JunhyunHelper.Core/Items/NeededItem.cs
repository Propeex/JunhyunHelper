namespace JunhyunHelper.Core.Items;

public sealed record NeededItem(
    string ItemId,
    int RequiredTotal,
    int RequiredFir,
    int OwnedFir,
    int OwnedNonFir,
    int RemainingTotal,
    int RemainingFir,
    IReadOnlyList<ItemRequirementSource> Sources)
{
    public int OwnedTotal => OwnedFir + OwnedNonFir;

    public bool IsFulfilled => RemainingTotal == 0 && RemainingFir == 0;
}
