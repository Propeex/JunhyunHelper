using JunhyunHelper.Core.FarmingGuide;
using JunhyunHelper.Core.Items;
using Xunit;

namespace JunhyunHelper.Tests.Core;

public sealed class FarmingGuideStackQuantityPolicyV1170Tests
{
    [Theory]
    [InlineData("5449016a4bdc2d6f028b456f")]
    [InlineData("5696686a4bdc2da3298b456a")]
    [InlineData("569668774bdc2da2298b4568")]
    public void CanonicalTarkovCurrenciesAreExplicitlyClassified(string itemId)
    {
        var item = Item(itemId);

        Assert.True(FarmingGuideStackQuantityPolicy.IsCurrency(item));
        Assert.True(FarmingGuideStackQuantityPolicy.RequiresQuantity(item));
    }

    [Fact]
    public void OrdinaryAndAmmoItemsAreNotMisclassifiedAsCurrency()
    {
        var ordinary = Item("ordinary");
        var ammo = Item("ammo", ["ammo"]);

        Assert.False(FarmingGuideStackQuantityPolicy.IsCurrency(ordinary));
        Assert.False(FarmingGuideStackQuantityPolicy.IsCurrency(ammo));
        Assert.False(FarmingGuideStackQuantityPolicy.RequiresQuantity(ordinary));
        Assert.True(FarmingGuideStackQuantityPolicy.RequiresQuantity(ammo));
    }

    private static GameItem Item(string id, IReadOnlyList<string>? types = null) =>
        new(
            id,
            id,
            id,
            id,
            id,
            null,
            null,
            [],
            [],
            types ?? [],
            1,
            1);
}
