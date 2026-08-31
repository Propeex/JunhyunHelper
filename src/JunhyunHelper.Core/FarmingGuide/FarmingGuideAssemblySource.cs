namespace JunhyunHelper.Core.FarmingGuide;

/// <summary>
/// Optional source-backed assembly metadata retained in canonical Game Content.
/// It is evidence only: missing fields never cause the desktop client to invent a
/// preset or a composed Tarkov image.
/// </summary>
public sealed record FarmingGuideAssemblySource(
    string? GridImageUrl,
    string? Image512Url,
    string? DefaultPresetItemId,
    IReadOnlyList<string> ContainedItemIds)
{
    public static FarmingGuideAssemblySource Empty { get; } = new(null, null, null, []);
}
