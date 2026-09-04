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
    public void RelationshipImporter_DeduplicatesOnlyExactTraderPurchaseRecords()
    {
        var items = Parse("""
            {"data":{"items":{
              "target":{"id":"target","types":[],"buyFromTrader":[
                {"trader":"trader","currencyItem":"rub","currency":"RUB","price":5371,"minTraderLevel":1,"taskUnlock":null,"buyLimit":5},
                {"trader":"trader","currencyItem":"rub","currency":"RUB","price":5371,"minTraderLevel":1,"taskUnlock":null,"buyLimit":5},
                {"trader":"trader","currencyItem":"rub","currency":"RUB","price":5371,"minTraderLevel":1,"taskUnlock":null,"buyLimit":7}
              ]},
              "rub":{"id":"rub","types":[]}
            }}}
            """);
        var barters = Parse("""{"data":[]}""");
        var crafts = Parse("""{"data":[]}""");

        var result = new TarkovItemRelationshipImporter().Import(items, barters, crafts);

        Assert.Equal(2, result.TraderPurchases.Count);
        Assert.Contains(result.TraderPurchases, purchase => purchase.BuyLimit == 5);
        Assert.Contains(result.TraderPurchases, purchase => purchase.BuyLimit == 7);
    }

    [Fact]
    public void RelationshipImporter_ExcludesAuditedPassiveBitcoinFarmProduction()
    {
        var items = Parse("""{"data":{"items":[]}}""");
        var barters = Parse("""{"data":[]}""");
        var crafts = Parse("""
            {"data":[{
              "id":"5d5c205bd582a50d042a3c0e",
              "requiredItems":[],
              "requiredQuestItems":[],
              "station":"5d494a445b56502f18c98a10",
              "duration":300000,
              "level":1,
              "productItem":{"item":"59faff1d86f7746c51718c9c","count":1,"attributes":{}}
            }]}
            """);

        var result = new TarkovItemRelationshipImporter().Import(items, barters, crafts);

        Assert.Empty(result.Crafts);
    }

    [Fact]
    public void RelationshipImporter_StillRejectsUnknownZeroInputCraft()
    {
        var items = Parse("""{"data":{"items":[]}}""");
        var barters = Parse("""{"data":[]}""");
        var crafts = Parse("""
            {"data":[{
              "id":"unexpected-zero-input-craft",
              "requiredItems":[],
              "requiredQuestItems":[],
              "station":"workbench",
              "duration":60,
              "level":1,
              "productItem":{"item":"product","count":1}
            }]}
            """);

        var error = Assert.Throws<InvalidDataException>(() =>
            new TarkovItemRelationshipImporter().Import(items, barters, crafts));

        Assert.Contains("has no required items", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ScannerItemDatabase_UiAndStorageContractsRemainLocalAndComplete()
    {
        var root = FindRepositoryRoot();
        var item = Read(root, "src", "JunhyunHelper.Core", "Items", "GameItem.cs");
        var build = Read(root, "src", "JunhyunHelper.Infrastructure", "Content", "TarkovContentBuildService.cs");
        var page = Read(root, "src", "JunhyunHelper.Desktop", "Scanner", "ScannerPage.ItemRelationships.cs");
        var scannerXaml = Read(root, "src", "JunhyunHelper.Desktop", "Scanner", "ScannerPage.xaml");
        var usability = Read(root, "src", "JunhyunHelper.Desktop", "Scanner", "ScannerPage.ProductUsability.cs");
        var search = Read(root, "src", "JunhyunHelper.Desktop", "Scanner", "ScannerCoordinator.Search.cs");

        // Storage keeps the complete canonical item facts even when the v1.8.4 Scanner
        // presentation intentionally exposes only the four user-approved basic fields.
        Assert.Contains("TypeKeys", item, StringComparison.Ordinal);
        Assert.Contains("WeightKg", item, StringComparison.Ordinal);
        Assert.Contains("BasePrice", item, StringComparison.Ordinal);
        Assert.Contains("FleaTradable", item, StringComparison.Ordinal);
        Assert.Contains("TarkovItemRelationshipImporter", build, StringComparison.Ordinal);
        Assert.DoesNotContain("HttpClient", search, StringComparison.Ordinal);

        Assert.Contains("기본 정보", page, StringComparison.Ordinal);
        Assert.Contains("퀘스트 사용처", page, StringComparison.Ordinal);
        Assert.Contains("은신처 업그레이드 사용처", page, StringComparison.Ordinal);
        Assert.Contains("제작 재료 사용처", page, StringComparison.Ordinal);
        Assert.DoesNotContain("교환 재료 사용처", page, StringComparison.Ordinal);
        Assert.Contains("수급처", page, StringComparison.Ordinal);
        Assert.Contains("AddAcquisitionSubsection(\"제작\")", page, StringComparison.Ordinal);
        Assert.Contains("AddAcquisitionSubsection(\"교환\")", page, StringComparison.Ordinal);
        Assert.Contains("AddAcquisitionSubsection(\"구매\")", page, StringComparison.Ordinal);
        Assert.Contains("AddAcquisitionSubsection(\"레이드 획득\")", page, StringComparison.Ordinal);
        Assert.Contains("레이드 획득 가능", page, StringComparison.Ordinal);
        Assert.Contains("레이드에서만 획득 가능", page, StringComparison.Ordinal);
        Assert.Contains("WrapPanel", page, StringComparison.Ordinal);
        Assert.Contains("RelationshipItemButton_Click", page, StringComparison.Ordinal);
        Assert.Contains("SelectSearchItemById", page, StringComparison.Ordinal);
        Assert.Contains(" ₽", page, StringComparison.Ordinal);
        Assert.Contains("<ScrollViewer VerticalScrollBarVisibility=\"Auto\"", scannerXaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"SelectedItemPanel\" Visibility=\"Collapsed\"", scannerXaml, StringComparison.Ordinal);
        Assert.DoesNotContain("new ScrollViewer", usability, StringComparison.Ordinal);
        Assert.DoesNotContain("Children.Remove(SelectedItemPanel)", usability, StringComparison.Ordinal);
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
