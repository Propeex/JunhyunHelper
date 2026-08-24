using System.Net;
using System.Text;
using System.Text.Json;
using JunhyunHelper.Core.Profiles;
using JunhyunHelper.Infrastructure.Scanner;
using Xunit;

namespace JunhyunHelper.Tests.Scanner;

public sealed class ScannerCatalogBaselineProtectionTests
{
    [Fact]
    public async Task RefreshAsync_SevereTraderCoverageRegressionKeepsDiskBaseline()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var root = CreateTemporaryDirectory();
        try
        {
            WriteHealthyBaseline(root, traderPriceBase: 5000);
            using var httpClient = new HttpClient(new CatalogHandler(includeTraderPrices: false));
            using var service = new ScannerCatalogService(httpClient, root);

            var success = await service.RefreshAsync(GameMode.Regular, cancellationToken);

            Assert.True(success);
            Assert.Equal("market-regression", service.LastDiagnostics.Outcome);
            Assert.True(service.LastDiagnostics.UsedExistingCatalog);
            Assert.Equal(4000, service.Count);
            Assert.True(service.TryGetItem("baseline-item-17", out var baselineItem));
            Assert.Equal(5017, baselineItem.BestTraderSellPrice);
            Assert.False(service.TryGetItem("candidate-item-17", out _));

            using var reloadClient = new HttpClient(new CatalogHandler(includeTraderPrices: true));
            using var reloaded = new ScannerCatalogService(reloadClient, root);
            Assert.True(await reloaded.LoadCacheAsync(GameMode.Regular, cancellationToken));
            Assert.True(reloaded.TryGetItem("baseline-item-17", out var persistedItem));
            Assert.Equal(5017, persistedItem.BestTraderSellPrice);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task RefreshAsync_HttpFailureRestoresHealthyDiskBaselineWhenScannerWasNotLoaded()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var root = CreateTemporaryDirectory();
        try
        {
            WriteHealthyBaseline(root, traderPriceBase: 7000);
            using var httpClient = new HttpClient(new FailureHandler());
            using var service = new ScannerCatalogService(httpClient, root);

            var success = await service.RefreshAsync(GameMode.Regular, cancellationToken);

            Assert.True(success);
            Assert.Equal("http-failure", service.LastDiagnostics.Outcome);
            Assert.True(service.LastDiagnostics.UsedExistingCatalog);
            Assert.Equal(GameMode.Regular, service.LoadedMode);
            Assert.True(service.TryGetItem("baseline-item-23", out var item));
            Assert.Equal(7023, item.BestTraderSellPrice);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static void WriteHealthyBaseline(string root, int traderPriceBase)
    {
        var directory = Path.Combine(root, "scanner", "catalog");
        Directory.CreateDirectory(directory);
        var items = Enumerable.Range(0, 4000)
            .Select(index => new
            {
                Id = $"baseline-item-{index}",
                OfficialName = $"기준 아이템 {index}",
                ShortName = $"기준 {index}",
                IconUrl = $"https://example.test/baseline/{index}.png",
                FleaAveragePrice = 9000 + index,
                BestTraderSellPrice = traderPriceBase + index,
                Width = 2,
                Height = 2,
                BestTraderId = "mechanic-id",
                BestTraderName = "메카닉",
            })
            .ToArray();
        var cache = new
        {
            SchemaVersion = 3,
            Source = "https://json.tarkov.dev",
            Language = "ko",
            GameMode = "regular",
            GeneratedAtUtc = DateTimeOffset.UtcNow - TimeSpan.FromDays(1),
            Items = items,
        };
        File.WriteAllText(
            Path.Combine(directory, "items-regular-ko.json"),
            JsonSerializer.Serialize(cache),
            new UTF8Encoding(false));
    }

    private static string CreateTemporaryDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "JunhyunHelper-ScannerBaselineTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private sealed class CatalogHandler : HttpMessageHandler
    {
        private readonly string _items;
        private readonly string _korean;
        private readonly string _english;

        public CatalogHandler(bool includeTraderPrices)
        {
            var records = Enumerable.Range(0, 4000)
                .Select(index => new
                {
                    id = $"candidate-item-{index}",
                    name = $"candidate-name-{index}",
                    shortName = $"candidate-short-{index}",
                    iconLink = $"https://example.test/candidate/{index}.png",
                    avg24hPrice = 10000 + index,
                    width = 2,
                    height = 2,
                    sellToTrader = includeTraderPrices
                        ? new[] { new { trader = "mechanic-id", priceRUB = 6000 + index } }
                        : Array.Empty<object>(),
                })
                .ToArray();
            _items = JsonSerializer.Serialize(new { data = new { items = records } });

            var korean = new Dictionary<string, string>(StringComparer.Ordinal);
            var english = new Dictionary<string, string>(StringComparer.Ordinal);
            for (var index = 0; index < 4000; index++)
            {
                korean[$"candidate-name-{index}"] = $"후보 아이템 {index}";
                korean[$"candidate-short-{index}"] = $"후보 {index}";
                english[$"candidate-name-{index}"] = $"Candidate item {index}";
                english[$"candidate-short-{index}"] = $"Candidate {index}";
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
                    : path.EndsWith("/traders", StringComparison.Ordinal) ||
                      path.EndsWith("/traders_ko", StringComparison.Ordinal) ||
                      path.EndsWith("/traders_en", StringComparison.Ordinal)
                        ? "{\"data\":{}}"
                        : _items;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            });
        }
    }

    private sealed class FailureHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent("missing", Encoding.UTF8, "text/plain"),
            });
    }
}
