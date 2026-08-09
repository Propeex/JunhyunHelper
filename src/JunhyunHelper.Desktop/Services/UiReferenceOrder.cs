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

    public static string MapFilterKey(MapReference map)
    {
        if (IsGroundZero(map))
            return "group:groundzero";
        if (IsFactory(map))
            return "group:factory";
        return $"map:{map.Id}";
    }

    public static bool IsGroundZero(MapReference? map)
    {
        if (map is null)
            return false;

        var normalizedKey = Normalize(map.NormalizedKey);
        var englishName = Normalize(map.NameEn);
        return normalizedKey is "groundzero" or "groundzero21" or "sandbox" or "sandboxhigh" ||
               englishName is "groundzero" or "groundzero21";
    }

    public static bool IsFactory(MapReference? map)
    {
        if (map is null)
            return false;

        var normalizedKey = Normalize(map.NormalizedKey);
        var englishName = Normalize(map.NameEn);
        return normalizedKey is "factory" or "factoryday" or "factorynight" or "factory4day" or "factory4night" ||
               englishName is "factory" or "factoryday" or "factorynight";
    }

    public static bool IsSecondaryMapVariant(MapReference? map)
    {
        if (map is null)
            return false;

        var normalizedKey = Normalize(map.NormalizedKey);
        var englishName = Normalize(map.NameEn);
        if (normalizedKey is "groundzero21" or "sandboxhigh" || englishName == "groundzero21")
            return true;

        return normalizedKey is "factorynight" or "factory4night" || englishName == "factorynight";
    }

    // Compatibility name used by the existing Quest filter projection. It now means
    // "prefer the primary daytime/base variant as the group label", not only Ground Zero.
    public static bool IsGroundZeroHighVariant(MapReference? map) => IsSecondaryMapVariant(map);

    private static string CanonicalMapName(MapReference? map)
    {
        if (map is null)
            return string.Empty;
        if (IsGroundZero(map))
            return "groundzero";
        if (IsFactory(map))
            return "factory";

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
