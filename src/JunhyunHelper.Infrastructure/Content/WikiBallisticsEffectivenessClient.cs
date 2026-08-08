using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using JunhyunHelper.Core.Ammo;
using JunhyunHelper.Core.Content;

namespace JunhyunHelper.Infrastructure.Content;

public sealed record WikiArmorEffectivenessRow(
    IReadOnlyList<string> IdentityCells,
    AmmoArmorEffectiveness Effectiveness);

public sealed record WikiArmorEffectivenessSource(
    bool Available,
    IReadOnlyList<WikiArmorEffectivenessRow> Rows,
    IReadOnlyList<string> Warnings);

public sealed record WikiArmorEffectivenessEnrichmentResult(
    GameContentCatalog Content,
    int MatchedAmmoCount,
    int SourceRowCount,
    IReadOnlyList<string> Warnings);

/// <summary>
/// Reads the rendered Ballistics table from the Escape from Tarkov Wiki through
/// MediaWiki's Action API. The Wiki is an optional enrichment source only: failure
/// to retrieve or confidently match ratings must never invent replacement values.
/// </summary>
public sealed class WikiBallisticsEffectivenessClient
{
    internal const string SourceName = "Wiki 방탄 효율";
    internal const string ApiUrl =
        "https://escapefromtarkov.fandom.com/api.php?action=parse&page=Ballistics&prop=text&format=json&formatversion=2&redirects=1";

    private static readonly Regex RowRegex = new(
        "<tr\\b[^>]*>(?<body>.*?)</tr>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant);

    private static readonly Regex CellRegex = new(
        "<t[dh]\\b[^>]*>(?<body>.*?)</t[dh]>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant);

    private static readonly Regex SupRegex = new(
        "<sup\\b[^>]*>.*?</sup>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant);

    private static readonly Regex TagRegex = new(
        "<[^>]+>",
        RegexOptions.Singleline | RegexOptions.CultureInvariant);

    private static readonly Regex WhitespaceRegex = new(
        "\\s+",
        RegexOptions.CultureInvariant);

    private readonly HttpClient _httpClient;

    public WikiBallisticsEffectivenessClient(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public async Task<WikiArmorEffectivenessSource> LoadAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await _httpClient.GetAsync(ApiUrl, cancellationToken);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            return ParseApiResponse(json);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception) when (
            exception is HttpRequestException or
            JsonException or
            InvalidDataException)
        {
            return new WikiArmorEffectivenessSource(
                Available: false,
                Rows: Array.Empty<WikiArmorEffectivenessRow>(),
                Warnings:
                [
                    $"Tarkov Wiki 방탄 효율 데이터를 가져오지 못했습니다: {exception.Message} " +
                    "기본 탄약 데이터는 계속 업데이트하며 효율값은 추정하지 않습니다.",
                ]);
        }
    }

    internal static WikiArmorEffectivenessSource ParseApiResponse(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);

        using var document = JsonDocument.Parse(json);
        if (!document.RootElement.TryGetProperty("parse", out var parse) ||
            !parse.TryGetProperty("text", out var textElement) ||
            textElement.ValueKind != JsonValueKind.String)
        {
            throw new InvalidDataException(
                "Tarkov Wiki MediaWiki 응답에 Ballistics parser output이 없습니다.");
        }

        var html = textElement.GetString();
        if (string.IsNullOrWhiteSpace(html))
            throw new InvalidDataException("Tarkov Wiki Ballistics parser output이 비어 있습니다.");

        var rows = ParseRows(html);
        if (rows.Count == 0)
        {
            throw new InvalidDataException(
                "Tarkov Wiki Ballistics 표에서 Class 1~6 효율 행을 찾지 못했습니다.");
        }

        return new WikiArmorEffectivenessSource(
            Available: true,
            Rows: rows,
            Warnings: Array.Empty<string>());
    }

    internal static IReadOnlyList<WikiArmorEffectivenessRow> ParseRows(string html)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(html);
        var rows = new List<WikiArmorEffectivenessRow>();

        foreach (Match rowMatch in RowRegex.Matches(html))
        {
            var cells = CellRegex.Matches(rowMatch.Groups["body"].Value)
                .Select(match => VisibleText(match.Groups["body"].Value))
                .ToArray();

            if (cells.Length < 12)
                continue;

            var ratingTexts = cells[^6..];
            var ratings = new int[6];
            var valid = true;
            for (var index = 0; index < ratings.Length; index++)
            {
                if (!int.TryParse(ratingTexts[index], out var value) || value is < 0 or > 6)
                {
                    valid = false;
                    break;
                }
                ratings[index] = value;
            }

            if (!valid)
                continue;

            var identityCells = cells[..^6]
                .Where(cell => !string.IsNullOrWhiteSpace(cell))
                .ToArray();
            if (identityCells.Length == 0)
                continue;

            rows.Add(new WikiArmorEffectivenessRow(
                identityCells,
                new AmmoArmorEffectiveness(
                    ratings[0],
                    ratings[1],
                    ratings[2],
                    ratings[3],
                    ratings[4],
                    ratings[5])));
        }

        return rows;
    }

    public static WikiArmorEffectivenessEnrichmentResult Enrich(
        GameContentCatalog content,
        WikiArmorEffectivenessSource source)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(source);

        var warnings = new List<string>(source.Warnings);
        if (!source.Available || source.Rows.Count == 0 || content.Ammunition.Count == 0)
        {
            return new WikiArmorEffectivenessEnrichmentResult(
                content,
                0,
                source.Rows.Count,
                warnings);
        }

        var itemsById = content.Items.ToDictionary(item => item.Id, StringComparer.Ordinal);
        var canonicalNameGroups = content.Ammunition
            .Select(ammo =>
            {
                itemsById.TryGetValue(ammo.ItemId, out var item);
                return new
                {
                    Ammo = ammo,
                    Name = NormalizeIdentity(item?.NameEn),
                };
            })
            .Where(entry => entry.Name.Length > 0)
            .GroupBy(entry => entry.Name, StringComparer.Ordinal)
            .ToArray();

        var canonicalByName = canonicalNameGroups
            .Where(group => group.Count() == 1)
            .ToDictionary(
                group => group.Key,
                group => group.Single().Ammo,
                StringComparer.Ordinal);

        var ambiguousCanonicalNames = canonicalNameGroups
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToHashSet(StringComparer.Ordinal);

        if (ambiguousCanonicalNames.Count > 0)
        {
            warnings.Add(
                $"동일한 영문 탄약명이 {ambiguousCanonicalNames.Count}개 있어 해당 이름은 Wiki 효율 매칭에서 제외했습니다.");
        }

        var matchesByItemId = new Dictionary<string, List<AmmoArmorEffectiveness>>(StringComparer.Ordinal);
        foreach (var row in source.Rows)
        {
            var rowMatches = row.IdentityCells
                .Select(NormalizeIdentity)
                .Where(value => value.Length > 0)
                .Distinct(StringComparer.Ordinal)
                .Where(canonicalByName.ContainsKey)
                .Select(value => canonicalByName[value])
                .DistinctBy(ammo => ammo.ItemId, StringComparer.Ordinal)
                .ToArray();

            if (rowMatches.Length != 1)
                continue;

            var itemId = rowMatches[0].ItemId;
            if (!matchesByItemId.TryGetValue(itemId, out var ratings))
            {
                ratings = [];
                matchesByItemId[itemId] = ratings;
            }
            ratings.Add(row.Effectiveness);
        }

        var resolved = new Dictionary<string, AmmoArmorEffectiveness>(StringComparer.Ordinal);
        var conflicting = 0;
        foreach (var (itemId, ratings) in matchesByItemId)
        {
            var distinct = ratings.Distinct().ToArray();
            if (distinct.Length == 1 && distinct[0].IsValid)
                resolved[itemId] = distinct[0];
            else
                conflicting++;
        }

        if (conflicting > 0)
        {
            warnings.Add(
                $"Tarkov Wiki에서 같은 탄약에 서로 다른 Class 효율값이 연결된 {conflicting}건을 제외했습니다.");
        }

        var minimumHealthyMatches = Math.Min(
            content.Ammunition.Count,
            Math.Max(20, content.Ammunition.Count / 2));
        if (resolved.Count < minimumHealthyMatches)
        {
            warnings.Add(
                $"Tarkov Wiki 방탄 효율 매칭이 비정상적으로 적습니다 " +
                $"({resolved.Count}/{content.Ammunition.Count}). Wiki 구조 변경 가능성이 있어 이번 효율값은 적용하지 않습니다.");

            return new WikiArmorEffectivenessEnrichmentResult(
                content,
                0,
                source.Rows.Count,
                warnings);
        }

        var enrichedAmmo = content.Ammunition
            .Select(ammo => resolved.TryGetValue(ammo.ItemId, out var effectiveness)
                ? ammo with { ArmorEffectiveness = effectiveness }
                : ammo with { ArmorEffectiveness = null })
            .ToArray();

        if (resolved.Count < content.Ammunition.Count)
        {
            warnings.Add(
                $"Tarkov Wiki 방탄 효율 {resolved.Count}/{content.Ammunition.Count}종을 안전하게 매칭했습니다. " +
                "매칭되지 않은 탄약은 효율값을 추정하지 않습니다.");
        }

        return new WikiArmorEffectivenessEnrichmentResult(
            content with { Ammo = enrichedAmmo },
            resolved.Count,
            source.Rows.Count,
            warnings);
    }

    internal static string NormalizeIdentity(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var decoded = WebUtility.HtmlDecode(value)
            .Replace('×', 'x')
            .ToLowerInvariant();

        return new string(decoded
            .Where(char.IsLetterOrDigit)
            .ToArray());
    }

    private static string VisibleText(string htmlCell)
    {
        var withoutSuperscript = SupRegex.Replace(htmlCell, string.Empty);
        var withoutTags = TagRegex.Replace(withoutSuperscript, " ");
        var decoded = WebUtility.HtmlDecode(withoutTags);
        return WhitespaceRegex.Replace(decoded, " ").Trim();
    }
}
