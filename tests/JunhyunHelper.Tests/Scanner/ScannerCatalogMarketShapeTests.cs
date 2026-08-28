using System.Net;
using System.Text;
using System.Text.Json;
using JunhyunHelper.Core.Profiles;
using JunhyunHelper.Infrastructure.Scanner;
using Xunit;

namespace JunhyunHelper.Tests.Scanner;

public sealed class ScannerCatalogMarketShapeTests
{
    [Fact]
    public async Task RefreshAsync_CurrentStaticSellToTraderPopulatesTraderIdentityAndPerSlotValues()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var root = CreateTemporaryDirectory();
        try
        {
            using var httpClient = new HttpClient(new RawCatalogHandler(MarketShape.StaticSellToTrader));
            using var service = new ScannerCatalogService(httpClient, root);

            Assert.True(await service.RefreshAsync(GameMode.Regular, cancellationToken));
            Assert.True(service.TryGetItem("raw-item-17", out var item));
            Assert.Equal(1517, item.BestTraderSellPrice);
            Assert.Equal("mechanic-id", item.BestTraderId);
            Assert.Equal("메카닉", item.BestTraderName);
            Assert.Equal(379, item.TraderPricePerSlot);
            Assert.Equal(3017, item.FleaAveragePrice);
            Assert.Equal(2517, item.FleaMinimumPrice);
            Assert.Equal(754, item.FleaPricePerSlot);
            Assert.Equal("success", service.LastDiagnostics.Outcome);
            Assert.Equal(4000, service.LastDiagnostics.ItemCount);
            Assert.Equal(4000, service.LastDiagnostics.TraderPriceCount);
            Assert.Equal(4000, service.LastDiagnostics.FleaPriceCount);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task RefreshAsync_LegacyRawTraderPricesRemainCompatible()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var root = CreateTemporaryDirectory();
        try
        {
            using var httpClient = new HttpClient(new RawCatalogHandler(MarketShape.LegacyTraderPrices));
            using var service = new ScannerCatalogService(httpClient, root);

            Assert.True(await service.RefreshAsync(GameMode.Regular, cancellationToken));
            Assert.True(service.TryGetItem("raw-item-17", out var item));
            Assert.Equal(1517, item.BestTraderSellPrice);
            Assert.Equal("mechanic-id", item.BestTraderId);
            Assert.Equal("메카닉", item.BestTraderName);
            Assert.Equal(379, item.TraderPricePerSlot);
            Assert.Equal(2517, item.FleaMinimumPrice);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task RefreshAsync_TraderlessLargeCatalogRemainsUsableForIdentityAndFailsClosedPerTraderField()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var root = CreateTemporaryDirectory();
        try
        {
            using var httpClient = new HttpClient(new RawCatalogHandler(MarketShape.None));
            using var service = new ScannerCatalogService(httpClient, root);

            Assert.True(await service.RefreshAsync(GameMode.Regular, cancellationToken));
            Assert.Equal(4000, service.Count);
            Assert.True(service.HasHealthyCatalog);
            Assert.True(service.TryGetItem("raw-item-17", out var item));
            Assert.Equal("원본 아이템 17", item.OfficialName);
            Assert.Null(item.BestTraderSellPrice);
            Assert.Null(item.BestTraderId);
            Assert.Null(item.BestTraderName);
            Assert.Null(item.TraderPricePerSlot);
            Assert.Equal(3017, item.FleaAveragePrice);
            Assert.Equal(2517, item.FleaMinimumPrice);
            Assert.Equal(754, item.FleaPricePerSlot);

            Assert.Equal("success", service.LastDiagnostics.Outcome);
            Assert.Equal(4000, service.LastDiagnostics.ItemCount);
            Assert.Equal(0, service.LastDiagnostics.TraderPriceCount);
            Assert.Equal(4000, service.LastDiagnostics.FleaPriceCount);
            Assert.False(service.LastDiagnostics.UsedExistingCatalog);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task RefreshAsync_TooSmallCatalogStillFailsClosedForIdentity()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var root = CreateTemporaryDirectory();
        try
        {
            using var httpClient = new HttpClient(new RawCatalogHandler(MarketShape.StaticSellToTrader, itemCount: 3999));
            using var service = new ScannerCatalogService(httpClient, root);

            Assert.False(await service.RefreshAsync(GameMode.Regular, cancellationToken));
            Assert.Equal(0, service.Count);
            Assert.False(service.HasHealthyCatalog);
            Assert.Equal("identity-invalid", service.LastDiagnostics.Outcome);
            Assert.Equal(3999, service.LastDiagnostics.ItemCount);
            Assert.Equal(3999, service.LastDiagnostics.TraderPriceCount);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static string CreateTemporaryDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "JunhyunHelper-ScannerMarketTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private enum MarketShape
    {
        None,
        StaticSellToTrader,
        LegacyTraderPrices,
    }

    private sealed class RawCatalogHandler : HttpMessageHandler
    {
        private readonly string _items;
        private readonly string _korean;
        private readonly string _english;
        private readonly string _traders;
        private readonly string _tradersKorean;
        private readonly string _tradersEnglish;

        public RawCatalogHandler(MarketShape marketShape, int itemCount = 4000)
        {
            object[] records = Enumerable.Range(0, itemCount)
                .Select(index => CreateItem(index, marketShape))
                .ToArray();

            _items = JsonSerializer.Serialize(new { data = new { items = records } });

            var korean = new Dictionary<string, string>(StringComparer.Ordinal);
            var english = new Dictionary<string, string>(StringComparer.Ordinal);
            for (var index = 0; index < itemCount; index++)
            {
                korean[$"raw-name-{index}"] = $"원본 아이템 {index}";
                korean[$"raw-short-{index}"] = $"원본 {index}";
                english[$"raw-name-{index}"] = $"Raw item {index}";
                english[$"raw-short-{index}"] = $"Raw {index}";
            }

            _korean = JsonSerializer.Serialize(new { data = korean });
            _english = JsonSerializer.Serialize(new { data = english });

            _traders = JsonSerializer.Serialize(new
            {
                data = new Dictionary<string, object>(StringComparer.Ordinal)
                {
                    ["therapist-id"] = new { id = "therapist-id", name = "trader-therapist" },
                    ["mechanic-id"] = new { id = "mechanic-id", name = "trader-mechanic" },
                },
            });
            _tradersKorean = JsonSerializer.Serialize(new
            {
                data = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["trader-therapist"] = "테라피스트",
                    ["trader-mechanic"] = "메카닉",
                },
            });
            _tradersEnglish = JsonSerializer.Serialize(new
            {
                data = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["trader-therapist"] = "Therapist",
                    ["trader-mechanic"] = "Mechanic",
                },
            });
        }

        private static object CreateItem(int index, MarketShape marketShape)
        {
            var common = new
            {
                id = $"raw-item-{index}",
                name = $"raw-name-{index}",
                shortName = $"raw-short-{index}",
                iconLink = $"https://example.test/icons/raw-{index}.png",
                avg24hPrice = 3000 + index,
                lastLowPrice = 2500 + index,
                width = 2,
                height = 2,
            };

            return marketShape switch
            {
                MarketShape.StaticSellToTrader => new
                {
                    common.id,
                    common.name,
                    common.shortName,
                    common.iconLink,
                    common.avg24hPrice,
                    common.lastLowPrice,
                    common.width,
                    common.height,
                    sellToTrader = new[]
                    {
                        new { trader = "therapist-id", priceRUB = 1200 + index, price = 1200 + index, currency = "RUB" },
                        new { trader = "mechanic-id", priceRUB = 1500 + index, price = 1500 + index, currency = "RUB" },
                    },
                },
                MarketShape.LegacyTraderPrices => new
                {
                    common.id,
                    common.name,
                    common.shortName,
                    common.iconLink,
                    common.avg24hPrice,
                    common.lastLowPrice,
                    common.width,
                    common.height,
                    traderPrices = new[]
                    {
                        new { trader = "therapist-id", priceRUB = 1200 + index, price = 1200 + index, currency = "RUB", source = "Therapist" },
                        new { trader = "mechanic-id", priceRUB = 1500 + index, price = 1500 + index, currency = "RUB", source = "Mechanic" },
                    },
                },
                _ => common,
            };
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath ?? string.Empty;
            var body = path.EndsWith("/items_ko", StringComparison.Ordinal)
                ? _korean
                : path.EndsWith("/items_en", StringComparison.Ordinal)
                    ? _english
                    : path.EndsWith("/traders_ko", StringComparison.Ordinal)
                        ? _tradersKorean
                        : path.EndsWith("/traders_en", StringComparison.Ordinal)
                            ? _tradersEnglish
                            : path.EndsWith("/traders", StringComparison.Ordinal)
                                ? _traders
                                : _items;

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            });
        }
    }
}
