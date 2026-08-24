using JunhyunHelper.Core.Content;
using JunhyunHelper.Core.Hideout;
using JunhyunHelper.Core.Items;
using JunhyunHelper.Infrastructure.Validation;
using Xunit;

namespace JunhyunHelper.Tests.Validation;

public sealed class ContentUpdateCompletenessGuardTests
{
    [Fact]
    public void NestedHideoutRequirementCollapseIsRejectedEvenWhenStationAndLevelRemain()
    {
        var baseline = EmptyCatalog() with
        {
            HideoutStations =
            [
                CreateStation(4),
            ],
        };
        var candidate = baseline with
        {
            HideoutStations =
            [
                CreateStation(1),
            ],
        };

        var result = new ContentUpdateCompletenessGuard().Validate(candidate, baseline);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, issue => issue.Code == "update.hideout-items.suspicious-shrink");
    }

    [Fact]
    public void BulkItemIconCoverageLossIsRejected()
    {
        var baseline = EmptyCatalog() with { Items = CreateItems(iconCount: 20, koreanCount: 20) };
        var candidate = EmptyCatalog() with { Items = CreateItems(iconCount: 9, koreanCount: 20) };

        var result = new ContentUpdateCompletenessGuard().Validate(candidate, baseline);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, issue => issue.Code == "update.item-icons.suspicious-shrink");
    }

    [Fact]
    public void BulkKoreanLocalizationLossIsRejected()
    {
        var baseline = EmptyCatalog() with { Items = CreateItems(iconCount: 20, koreanCount: 20) };
        var candidate = EmptyCatalog() with { Items = CreateItems(iconCount: 20, koreanCount: 9) };

        var result = new ContentUpdateCompletenessGuard().Validate(candidate, baseline);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, issue => issue.Code == "update.item-korean.suspicious-shrink");
    }

    private static GameContentCatalog EmptyCatalog() => new(
        Array.Empty<GameItem>(),
        [],
        [],
        [],
        [],
        [],
        []);

    private static HideoutStation CreateStation(int requirementCount) =>
        new(
            "station-a",
            "시설 A",
            "Station A",
            "https://example.test/station.png",
            [
                new HideoutLevel(
                    "station-a",
                    1,
                    10,
                    Enumerable.Range(0, requirementCount)
                        .Select(index => new HideoutItemRequirement(
                            "station-a",
                            1,
                            $"item-{index}",
                            1,
                            false))
                        .ToArray()),
            ]);

    private static IReadOnlyList<GameItem> CreateItems(int iconCount, int koreanCount) =>
        Enumerable.Range(0, 20)
            .Select(index => new GameItem(
                $"item-{index}",
                index < koreanCount ? $"아이템 {index}" : null,
                $"Item {index}",
                null,
                null,
                index < iconCount ? $"https://example.test/{index}.png" : null,
                "https://example.test/wiki",
                Array.Empty<string>()))
            .ToArray();
}
