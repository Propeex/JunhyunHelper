using JunhyunHelper.Core.Reference;

namespace JunhyunHelper.Desktop.Services;

internal static class UiReferenceOrder
{
    private static readonly IReadOnlyDictionary<string, int> TraderRanks = BuildRanks(
        "prapor",
        "therapist",
        "fence",
        "skier",
        "peacekeeper",
        "mechanic",
        "ragman",
        "jaeger",
        "ref",
        "lightkeeper",
        "btrdriver");

    // This is the established in-game filter/location order used by the helper.
    // Unknown future locations remain visible after these entries and fall back to display-name order.
    private static readonly IReadOnlyDictionary<string, int> MapRanks = BuildRanks(
        "customs",
        "shoreline",
        "thelabyrinth",
        "icebreaker",
        "factory",
        "woods",
        "interchange",
        "thelab",
        "reserve",
        "lighthouse",
        "streetsoftarkov",
        "groundzero",
        "terminal");

    public static int TraderRank(TraderDefinition? trader)
    {
        var key = Normalize(trader?.NameEn);
        return TraderRanks.TryGetValue(key, out var rank) ? rank : int.MaxValue;
    }

    public static int MapRank(MapReference? map)
    {
        var key = CanonicalMapName(map);
        return MapRanks.TryGetValue(key, out var rank) ? rank : int.MaxValue;
    }

    public static string MapFilterKey(MapReference map) =>
        IsGroundZero(map) ? "group:groundzero" : $"map:{map.Id}";

    public static bool IsGroundZero(MapReference? map)
    {
        if (map is null)
            return false;

        var normalizedKey = Normalize(map.NormalizedKey);
        var englishName = Normalize(map.NameEn);

        return normalizedKey is "groundzero" or "groundzero21" or "sandbox" or "sandboxhigh" ||
               englishName is "groundzero" or "groundzero21";
    }

    public static bool IsGroundZeroHighVariant(MapReference? map)
    {
        if (map is null)
            return false;

        var normalizedKey = Normalize(map.NormalizedKey);
        var englishName = Normalize(map.NameEn);
        return normalizedKey is "groundzero21" or "sandboxhigh" || englishName == "groundzero21";
    }

    private static string CanonicalMapName(MapReference? map)
    {
        if (map is null)
            return string.Empty;
        if (IsGroundZero(map))
            return "groundzero";

        var key = Normalize(map.NameEn);
        return key switch
        {
            "streets" or "streetsoftarkov" => "streetsoftarkov",
            "thelabs" or "labs" or "lab" or "thelab" => "thelab",
            "labyrinth" or "thelabyrinth" => "thelabyrinth",
            "icebreakerterminal" => "icebreaker",
            _ => key,
        };
    }

    private static IReadOnlyDictionary<string, int> BuildRanks(params string[] values) =>
        values.Select((value, index) => (value, index))
            .ToDictionary(
                pair => Normalize(pair.value),
                pair => pair.index,
                StringComparer.OrdinalIgnoreCase);

    private static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        return new string(value
            .Trim()
            .ToLowerInvariant()
            .Where(char.IsLetterOrDigit)
            .ToArray());
    }
}
