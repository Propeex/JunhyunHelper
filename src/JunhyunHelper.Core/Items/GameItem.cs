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
    IReadOnlyList<string>? CategoryKeys = null,
    IReadOnlyList<string>? TypeKeys = null,
    int? Width = null,
    int? Height = null,
    decimal? WeightKg = null,
    int? BasePrice = null,
    bool? FleaTradable = null)
{
    [JsonIgnore]
    public IReadOnlyList<string> Categories => CategoryKeys ?? Array.Empty<string>();

    [JsonIgnore]
    public IReadOnlyList<string> Types => TypeKeys ?? Array.Empty<string>();
}
