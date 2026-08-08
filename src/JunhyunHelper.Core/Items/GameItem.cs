namespace JunhyunHelper.Core.Items;

public sealed record GameItem(
    string Id,
    string? NameKo,
    string? NameEn,
    string? ShortNameKo,
    string? ShortNameEn,
    string? IconUrl,
    string? WikiUrl,
    IReadOnlyList<string> CategoryIds);
