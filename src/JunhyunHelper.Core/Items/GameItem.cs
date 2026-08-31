using System.Text.Json.Serialization;
using JunhyunHelper.Core.FarmingGuide;

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
    /// <summary>
    /// Optional Tarkov equipment/storage structure used by Farming Guide. Content
    /// snapshots predating v1.13 legitimately deserialize with no layout data.
    /// </summary>
    public FarmingGuideItemLayout? FarmingGuideData { get; init; }

    /// <summary>
    /// Optional source-backed preset/composed-image metadata used by the recursive
    /// Farming Guide assembly editor. Content snapshots predating v1.14 legitimately
    /// deserialize with no assembly source data.
    /// </summary>
    public FarmingGuideAssemblySource? FarmingGuideAssembly { get; init; }

    [JsonIgnore]
    public IReadOnlyList<string> Categories => CategoryKeys ?? Array.Empty<string>();

    [JsonIgnore]
    public IReadOnlyList<string> Types => TypeKeys ?? Array.Empty<string>();
}
