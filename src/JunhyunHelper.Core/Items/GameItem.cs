using System.Text.Json.Serialization;

namespace JunhyunHelper.Core.Items;

public sealed record GameItem(
    string Id,
    string? NameKo,
    string? NameEn,
    string? ShortNameKo,
    string? ShortNameEn,
    string? IconUrl,
    string? WikiUrl,
    IReadOnlyList<string> CategoryIds,
    IReadOnlyList<string>? CategoryKeys = null)
{
    [JsonIgnore]
    public IReadOnlyList<string> Categories => CategoryKeys ?? Array.Empty<string>();
}
