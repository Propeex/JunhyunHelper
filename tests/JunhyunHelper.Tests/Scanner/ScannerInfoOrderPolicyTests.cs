using JunhyunHelper.Core.Scanner;
using Xunit;

namespace JunhyunHelper.Tests.Scanner;

public sealed class ScannerInfoOrderPolicyTests
{
    [Fact]
    public void NormalizePreservesExistingOrderAndAppendsNewFieldExactlyOnce()
    {
        string[] known =
        [
            "trader_sell_price",
            "flea_average_price",
            "flea_minimum_price",
            "trader_price_per_slot",
            "flea_price_per_slot",
            "current_needed",
        ];
        string[] schemaV6Order =
        [
            "current_needed",
            "flea_average_price",
            "unknown-old-field",
            "trader_sell_price",
            "current_needed",
            "trader_price_per_slot",
            "flea_price_per_slot",
        ];

        var migrated = ScannerInfoOrderPolicy.Normalize(schemaV6Order, known);

        Assert.Equal(
        [
            "current_needed",
            "flea_average_price",
            "trader_sell_price",
            "trader_price_per_slot",
            "flea_price_per_slot",
            "flea_minimum_price",
        ], migrated);
        Assert.Single(migrated.Where(value => value == "flea_minimum_price"));
    }

    [Fact]
    public void NormalizeUsesCanonicalOrderForFreshOrEmptySettings()
    {
        string[] known = ["a", "b", "c"];

        Assert.Equal(known, ScannerInfoOrderPolicy.Normalize(null, known));
        Assert.Equal(known, ScannerInfoOrderPolicy.Normalize([], known));
    }
}
