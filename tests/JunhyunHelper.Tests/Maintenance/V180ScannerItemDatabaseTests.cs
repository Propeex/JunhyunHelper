using System.Runtime.CompilerServices;
using System.Text.Json;
using JunhyunHelper.Core.Items;
using JunhyunHelper.Infrastructure.TarkovJson;
using JunhyunHelper.Infrastructure.TarkovJson.Items;
using Xunit;

namespace JunhyunHelper.Tests.Maintenance;

public sealed class V180ScannerItemDatabaseTests
{
    [Fact]
    public void RelationshipQuery_ResolvesForwardReverseAndAcquisitionRelations()
    {
        var catalog = new ItemRelationshipCatalog(
            [new ItemTraderPurchase("target", "trader", 2, Price: 12000, CurrencyItemId: "rub", CurrencyCode: "RUB", BuyLimit: 4)],
            [new ItemBarter("barter", "trader", 3, "target", 2, [new ItemIngredient("material", 5)], BuyLimit: 1)],
            [new ItemCraft("craft", "workbench", 2, "crafted", 3, [new ItemIngredient("target", 2), new ItemIngredient("tool", 1, true)], DurationSeconds: 3600)],
            ["target"]);

        var target = ItemRelationshipQuery.ForItem(catalog, "target");

        Assert.Single(target.TraderPurchasesForItem);
        Assert.Single(target.BartersForItem);
        Assert.Single(target.CraftsUsingItem);
        Assert.True(target.FleaMarketAvailable);
        Assert.Equal(12000m, target.TraderPurchasesForItem[0].Price);
        Assert.Equal(4, target.TraderPurchasesForItem[0].BuyLimit);
        Assert.Equal(3600, target.CraftsUsingItem[0].DurationSeconds);
        Assert.Equal(2m, target.CraftsUsingItem[0].RequiredItems.Single(item => item.ItemId == "target").Count);
    }

    [Fact]
    public void RelationshipImporter_PreservesPricesLimitsCraftDurationAndTools()
    {
        var items = Parse("""
            {"data":{"items":[
              {"id":"target","types":[],"lastLowPrice":50000,"buyFromTrader":[{"trader":{"id":"trader"},"currencyItem":{"id":"rub"},"currency":"RUB","price":12345,"minTraderLevel":2,"buyLimit":7}]},
              {"id":"material","types":[]},
              {"id":"rub","types":[]},
              {"id":"crafted","types":[]},
              {"id":"tool","types":[]}
            ]}}
            """);
        var barters = Parse("""
            {"data":[{"id":"b1","trader":{"id":"trader"},"minTraderLevel":3,"buyLimit":2,
              "offeredItem":{"item":{"id":"target"},"count":2},
              "requiredItems":[{"item":{"id":"material"},"count":5}]}]}
            """);
        var crafts = Parse("""
            {"data":[{"id":"c1","station":{"id":"workbench"},"level":2,"duration":3660,
              "productItem":{"item":{"id":"crafted"},"count":3},
              "requiredItems":[{"item":{"id":"target"},"count":2},{"item":{"id":"tool"},"count":1,"attributes":{"tool":true}}]}]}
            """);

        var result = new TarkovItemRelationshipImporter().Import(items, barters, crafts);

        var purchase = Assert.Single(result.TraderPurchases);
        Assert.Equal(12345m, purchase.Price);
        Assert.Equal("RUB", purchase.CurrencyCode);
        Assert.Equal("rub", purchase.CurrencyItemId);
        Assert.Equal(7, purchase.BuyLimit);
        var barter = Assert.Single(result.Barters);
        Assert.Equal(2, barter.BuyLimit);
        Assert.Equal(5m, Assert.Single(barter.RequiredItems).Count);
        var craft = Assert.Single(result.Crafts);
        Assert.Equal(3660, craft.DurationSeconds);
        Assert.True(craft.RequiredItems.Single(item => item.ItemId == "tool").IsTool);
        Assert.Contains("target", result.FleaMarketItemIds);
    }

    [Fact]
    public void ScannerItemDatabase_UiAndStorageContractsRemainLocalAndComplete()
    {
        var root = FindRepositoryRoot();
        var item = Read(root, "src", "JunhyunHelper.Core", "Items", "GameItem.cs");
        var build = Read(root, "src", "JunhyunHelper.Infrastructure", "Content", "TarkovContentBuildService.cs");
        var page = Read(root, "src", "JunhyunHelper.Desktop", "Scanner", "ScannerPage.ItemRelationships.cs");
        var usability = Read(root, "src", "JunhyunHelper.Desktop", "Scanner", "ScannerPage.ProductUsability.cs");
        var search = Read(root, "src", "JunhyunHelper.Desktop", "Scanner", "ScannerCoordinator.Search.cs");

        Assert.Contains("TypeKeys", item, StringComparison.Ordinal);
        Assert.Contains("WeightKg", item, StringComparison.Ordinal);
        Assert.Contains("BasePrice", item, StringComparison.Ordinal);
        Assert.Contains("FleaTradable", item, StringComparison.Ordinal);
        Assert.Contains("TarkovItemRelationshipImporter", build, StringComparison.Ordinal);
        Assert.DoesNotContain("HttpClient", search, StringComparison.Ordinal);
        Assert.Contains("퀘스트 사용처", page, StringComparison.Ordinal);
        Assert.Contains("은신처 업그레이드 사용처", page, StringComparison.Ordinal);
        Assert.Contains("제작 재료 사용처", page, StringComparison.Ordinal);
        Assert.Contains("교환 재료 사용처", page, StringComparison.Ordinal);
        Assert.Contains("수급처", page, StringComparison.Ordinal);
        Assert.Contains("SelectSearchItemById", page, StringComparison.Ordinal);
        Assert.Contains("VerticalScrollBarVisibility = ScrollBarVisibility.Auto", usability, StringComparison.Ordinal);
    }

    private static TarkovJsonDocument Parse(string json)
    {
        using var document = JsonDocument.Parse(json);
        return TarkovJsonDocument.Parse(document.RootElement);
    }

    private static string Read(string root, params string[] path) => File.ReadAllText(Path.Combine([root, .. path]));

    private static string FindRepositoryRoot([CallerFilePath] string sourcePath = "")
    {
        var directory = new DirectoryInfo(Path.GetDirectoryName(sourcePath) ?? throw new InvalidOperationException("Test source path is unavailable."));
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "AGENTS.md")) && Directory.Exists(Path.Combine(directory.FullName, "src")) && Directory.Exists(Path.Combine(directory.FullName, "tests")))
                return directory.FullName;
            directory = directory.Parent;
        }
        throw new DirectoryNotFoundException("Could not locate the JunhyunHelper repository root.");
    }
}
