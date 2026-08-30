using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using JunhyunHelper.Core.Ammo;
using JunhyunHelper.Core.Items;

namespace JunhyunHelper.Infrastructure.TarkovJson.Ammo;

/// <summary>
/// Builds the package-to-ammunition relationship used by Scanner pickup evaluation.
/// json.tarkov.dev's containsItems relationship is authoritative. Name matching is a
/// deliberately narrow compatibility fallback for explicit English "ammo pack" names
/// only, and is used only when no authoritative relationship was supplied for that item.
/// </summary>
public sealed partial class TarkovAmmoPackImporter
{
    public IReadOnlyList<AmmoPackDefinition> Import(
        TarkovJsonDocument itemsDocument,
        IReadOnlyList<GameItem> items,
        IReadOnlyList<AmmoDefinition> ammunition)
    {
        ArgumentNullException.ThrowIfNull(itemsDocument);
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(ammunition);

        var ammoIds = ammunition
            .Select(static ammo => ammo.ItemId)
            .ToHashSet(StringComparer.Ordinal);
        if (ammoIds.Count == 0)
            return [];

        var itemsWithContainedRelationship = new HashSet<string>(StringComparer.Ordinal);
        var authoritative = new Dictionary<string, AmmoPackDefinition>(StringComparer.Ordinal);
        foreach (var rawItem in TarkovJsonReader.ReadCollection(itemsDocument.Data, "items"))
        {
            if (rawItem.ValueKind != JsonValueKind.Object ||
                !rawItem.TryGetProperty("containsItems", out var rawContains) ||
                rawContains.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            {
                continue;
            }

            var packItemId = TarkovJsonReader.RequiredString(rawItem, "id", "Ammo pack item");
            itemsWithContainedRelationship.Add(packItemId);

            IReadOnlyList<JsonElement> entries;
            try
            {
                entries = TarkovJsonReader.ReadCollectionValue(rawContains, $"item {packItemId} containsItems");
            }
            catch (InvalidDataException)
            {
                // containsItems is optional Scanner enrichment. A future unrelated shape
                // must not make the entire Game Content update unusable, but the presence
                // of that authoritative field still blocks name-based reinterpretation.
                continue;
            }

            var contained = entries
                .Select(TryReadContainedItem)
                .Where(static value => value is not null)
                .Select(static value => value!.Value)
                .ToArray();
            var distinctContainedIds = contained
                .Select(static value => value.ItemId)
                .Distinct(StringComparer.Ordinal)
                .ToArray();

            // Do not reinterpret mixed containers/kits as ammunition packages. An ammo
            // pack has one canonical contained item, although the count can be > 1.
            if (distinctContainedIds.Length != 1 || !ammoIds.Contains(distinctContainedIds[0]))
                continue;

            var count = contained
                .Where(value => string.Equals(value.ItemId, distinctContainedIds[0], StringComparison.Ordinal))
                .Sum(static value => value.Count);
            authoritative[packItemId] = new AmmoPackDefinition(
                packItemId,
                distinctContainedIds[0],
                count > 0 ? count : null,
                IsNameFallback: false);
        }

        var itemById = items.ToDictionary(static item => item.Id, StringComparer.Ordinal);
        var ammoNameCandidates = ammunition
            .Select(ammo =>
            {
                itemById.TryGetValue(ammo.ItemId, out var item);
                return (Ammo: ammo, Stem: NormalizeAmmoStem(item?.NameEn));
            })
            .Where(static value => value.Stem.Length > 0)
            .GroupBy(static value => value.Stem, StringComparer.Ordinal)
            .Where(static group => group.Count() == 1)
            .ToDictionary(
                static group => group.Key,
                static group => group.Single().Ammo.ItemId,
                StringComparer.Ordinal);

        var result = new Dictionary<string, AmmoPackDefinition>(authoritative, StringComparer.Ordinal);
        foreach (var item in items)
        {
            if (result.ContainsKey(item.Id) ||
                itemsWithContainedRelationship.Contains(item.Id) ||
                !TryGetPackStem(item.NameEn, out var packStem))
            {
                continue;
            }

            if (!ammoNameCandidates.TryGetValue(packStem, out var ammoItemId))
                continue;

            result[item.Id] = new AmmoPackDefinition(
                item.Id,
                ammoItemId,
                TryReadPackCount(item.NameEn),
                IsNameFallback: true);
        }

        return result.Values
            .OrderBy(static value => value.PackItemId, StringComparer.Ordinal)
            .ToArray();
    }

    private static (string ItemId, decimal Count)? TryReadContainedItem(JsonElement entry)
    {
        if (entry.ValueKind != JsonValueKind.Object ||
            !entry.TryGetProperty("item", out var rawItem))
        {
            return null;
        }

        var itemId = TarkovJsonReader.ReferenceId(rawItem);
        if (string.IsNullOrWhiteSpace(itemId))
            return null;

        var count = 1m;
        if (entry.TryGetProperty("count", out var rawCount) &&
            rawCount.ValueKind == JsonValueKind.Number &&
            rawCount.TryGetDecimal(out var parsedCount) &&
            parsedCount > 0)
        {
            count = parsedCount;
        }

        return (itemId, count);
    }

    private static bool TryGetPackStem(string? value, out string stem)
    {
        stem = string.Empty;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var name = value.Trim();
        var markerIndex = name.IndexOf(" ammo pack", StringComparison.OrdinalIgnoreCase);
        if (markerIndex < 0)
            markerIndex = name.IndexOf(" ammunition pack", StringComparison.OrdinalIgnoreCase);
        if (markerIndex <= 0)
            return false;

        stem = NormalizeKey(name[..markerIndex]);
        return stem.Length > 0;
    }

    private static string NormalizeAmmoStem(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var name = value.Trim();
        if (name.EndsWith(" ammo", StringComparison.OrdinalIgnoreCase))
            name = name[..^5];
        else if (name.EndsWith(" ammunition", StringComparison.OrdinalIgnoreCase))
            name = name[..^11];

        return NormalizeKey(name);
    }

    private static string NormalizeKey(string value) =>
        new(value
            .Where(char.IsLetterOrDigit)
            .Select(char.ToLowerInvariant)
            .ToArray());

    private static decimal? TryReadPackCount(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var match = PackCountRegex().Match(value);
        if (!match.Success ||
            !decimal.TryParse(match.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var count) ||
            count <= 0)
        {
            return null;
        }

        return count;
    }

    [GeneratedRegex(@"\((\d+)\s*(?:pcs?|rounds?)?\)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex PackCountRegex();
}
