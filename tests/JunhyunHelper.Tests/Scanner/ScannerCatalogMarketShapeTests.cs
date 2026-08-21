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
    public async Task RefreshAsync_RawJsonTraderPricesPopulateTraderAndPerSlotValues()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var root = CreateTemporaryDirectory();
        try
        {
            using var httpClient = new HttpClient(new RawCatalogHandler(includeTraderPrices: true));
            using var service = new ScannerCatalogService(httpClient, root);

            Assert.True(await service.RefreshAsync(GameMode.Regular, cancellationToken));
            Assert.True(service.TryGetItem("raw-item-17", out var item));
            Assert.Equal(1517, item.BestTraderSellPrice);
            Assert.Equal(379, item.TraderPricePerSlot);
            Assert.Equal(3017, item.FleaAveragePrice);
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
    public async Task RefreshAsync_TraderlessLargeCatalogRemainsUsableForIdentityAndFailsClosedPerTraderField()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var root = CreateTemporaryDirectory();
        try
        {
            using var httpClient = new HttpClient(new RawCatalogHandler(includeTraderPrices: false));
            using var service = new ScannerCatalogService(httpClient, root);

            Assert.True(await service.RefreshAsync(GameMode.Regular, cancellationToken));
            Assert.Equal(4000, service.Count);
            Assert.True(service.HasHealthyCatalog);
            Assert.True(service.TryGetItem("raw-item-17", out var item));
            Assert.Equal("원본 아이템 17", item.OfficialName);
            Assert.Null(item.BestTraderSellPrice);
            Assert.Null(item.TraderPricePerSlot);
            Assert.Equal(3017, item.FleaAveragePrice);
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
            using var httpClient = new HttpClient(new RawCatalogHandler(includeTraderPrices: true, itemCount: 3999));
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

    private sealed class RawCatalogHandler : HttpMessageHandler
    {
        private readonly string _items;
        private readonly string _korean;
        private readonly string _english;

        public RawCatalogHandler(bool includeTraderPrices, int itemCount = 4000)
        {
            object[] records = Enumerable.Range(0, itemCount)
                .Select(index => includeTraderPrices
                    ? (object)new
                    {
                        id = $"raw-item-{index}",
                        name = $"raw-name-{index}",
                        shortName = $"raw-short-{index}",
                        iconLink = $"https://example.test/icons/raw-{index}.png",
                        avg24hPrice = 3000 + index,
                        width = 2,
                        height = 2,
                        traderPrices = new[]
                        {
                            new { priceRUB = 1200 + index, price = 1200 + index, currency = "RUB", source = "Therapist" },
                            new { priceRUB = 1500 + index, price = 1500 + index, currency = "RUB", source = "Mechanic" },
                        },
                    }
                    : new
                    {
                        id = $"raw-item-{index}",
                        name = $"raw-name-{index}",
                        shortName = $"raw-short-{index}",
                        iconLink = $"https://example.test/icons/raw-{index}.png",
                        avg24hPrice = 3000 + index,
                        width = 2,
                        height = 2,
                    })
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
                    : _items;

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            });
        }
    }
}