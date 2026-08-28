using JunhyunHelper.Core.Content;
using JunhyunHelper.Core.Items;
using JunhyunHelper.Infrastructure.Validation;
using Xunit;

namespace JunhyunHelper.Tests.Validation;

public sealed class V181ItemRelationshipCompletenessTests
{
    [Fact]
    public void RelationshipCollectionShrinkIsRejectedAgainstV8Baseline()
    {
        var baseline = Catalog(CreateRelationships(count: 8, requirementsPerRelation: 2));
        var candidate = Catalog(CreateRelationships(count: 3, requirementsPerRelation: 2));

        var result = new ContentUpdateCompletenessGuard().Validate(candidate, baseline);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, issue => issue.Code == "update.item-trader-purchases.suspicious-shrink");
        Assert.Contains(result.Issues, issue => issue.Code == "update.item-barters.suspicious-shrink");
        Assert.Contains(result.Issues, issue => issue.Code == "update.item-crafts.suspicious-shrink");
        Assert.Contains(result.Issues, issue => issue.Code == "update.item-flea-acquisitions.suspicious-shrink");
    }

    [Fact]
    public void NestedRelationshipMaterialShrinkIsRejectedEvenWhenRelationCountsRemain()
    {
        var baseline = Catalog(CreateRelationships(count: 8, requirementsPerRelation: 4));
        var candidate = Catalog(CreateRelationships(count: 8, requirementsPerRelation: 1));

        var result = new ContentUpdateCompletenessGuard().Validate(candidate, baseline);

        Assert.False(result.IsValid);
        Assert.DoesNotContain(result.Issues, issue => issue.Code == "update.item-barters.suspicious-shrink");
        Assert.DoesNotContain(result.Issues, issue => issue.Code == "update.item-crafts.suspicious-shrink");
        Assert.Contains(result.Issues, issue => issue.Code == "update.item-barter-requirements.suspicious-shrink");
        Assert.Contains(result.Issues, issue => issue.Code == "update.item-craft-requirements.suspicious-shrink");
    }

    [Fact]
    public void LegacyBaselineWithoutRelationshipGraphDoesNotInventRelativeCoverageFailure()
    {
        var baseline = Catalog(relationships: null);
        var candidate = Catalog(CreateRelationships(count: 1, requirementsPerRelation: 1));

        var result = new ContentUpdateCompletenessGuard().Validate(candidate, baseline);

        Assert.True(result.IsValid);
        Assert.DoesNotContain(result.Issues, issue => issue.Code.StartsWith("update.item-", StringComparison.Ordinal));
    }

    [Fact]
    public void PresentButEmptyV8RelationshipGraphIsRejected()
    {
        var content = Catalog(ItemRelationshipCatalog.Empty);

        var result = new ItemRelationshipIntegrityValidator().Validate(content);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, issue => issue.Code == "item-relationship.purchases.empty");
        Assert.Contains(result.Issues, issue => issue.Code == "item-relationship.barters.empty");
        Assert.Contains(result.Issues, issue => issue.Code == "item-relationship.crafts.empty");
        Assert.Contains(result.Issues, issue => issue.Code == "item-relationship.flea.empty");
    }

    [Fact]
    public void LegacySnapshotWithoutRelationshipGraphRemainsReadableByRelationshipValidator()
    {
        var content = Catalog(relationships: null);

        var result = new ItemRelationshipIntegrityValidator().Validate(content);

        Assert.True(result.IsValid);
        Assert.Empty(result.Issues);
    }

    private static GameContentCatalog Catalog(ItemRelationshipCatalog? relationships) => new(
        [],
        [],
        [],
        [],
        [],
        [],
        [],
        ItemRelationshipData: relationships);

    private static ItemRelationshipCatalog CreateRelationships(int count, int requirementsPerRelation)
    {
        var purchases = Enumerable.Range(0, count)
            .Select(index => new ItemTraderPurchase(
                $"item-{index}",
                "trader",
                1,
                Price: 100,
                CurrencyItemId: "currency",
                CurrencyCode: "RUB"))
            .ToArray();

        var barters = Enumerable.Range(0, count)
            .Select(index => new ItemBarter(
                $"barter-{index}",
                "trader",
                1,
                $"barter-product-{index}",
                1,
                CreateIngredients(index, requirementsPerRelation)))
            .ToArray();

        var crafts = Enumerable.Range(0, count)
            .Select(index => new ItemCraft(
                $"craft-{index}",
                "station",
                1,
                $"craft-product-{index}",
                1,
                CreateIngredients(index, requirementsPerRelation),
                DurationSeconds: 60))
            .ToArray();

        var flea = Enumerable.Range(0, count)
            .Select(index => $"flea-{index}")
            .ToArray();

        return new ItemRelationshipCatalog(purchases, barters, crafts, flea);
    }

    private static IReadOnlyList<ItemIngredient> CreateIngredients(int relationIndex, int count) =>
        Enumerable.Range(0, count)
            .Select(index => new ItemIngredient($"material-{relationIndex}-{index}", 1))
            .ToArray();
}
