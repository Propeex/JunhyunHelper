using System.Net;
using System.Text;
using System.Text.Json;
using JunhyunHelper.Core.Profiles;
using JunhyunHelper.Infrastructure.Scanner;
using Xunit;

namespace JunhyunHelper.Tests.Scanner;

public sealed class ScannerCatalogServiceTests
{
    [Fact]
    public async Task RefreshAsync_LoadsFullKoreanCatalogAndMarketFields()
    {
        var root = CreateTemporaryDirectory();
        try
        {
            using var httpClient = new HttpClient(new CatalogHandler());
            using var service = new ScannerCatalogService(httpClient, root);

            var success = await service.RefreshAsync(GameMode.Regular);

            Assert.True(success);
            Assert.Equal(4000, service.Count);
            Assert.Equal(GameMode.Regular, service.LoadedMode);
            Assert.True(service.TryGetItem("item-0", out var item));
            Assert.Equal("공식 아이템 0", item.OfficialName);
            Assert.Equal(2000, item.FleaAveragePrice);
            Assert.Equal(1000, item.BestTraderSellPrice);
            Assert.Equal(4, item.Slots);
            Assert.Equal(250, item.TraderPricePerSlot);
            Assert.Equal(500, item.FleaPricePerSlot);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task LoadCacheAsync_MissingDifferentMode_ClearsPreviousModeInsteadOfLeakingIdentity()
    {
        var root = CreateTemporaryDirectory();
        try
        {
            using var httpClient = new HttpClient(new CatalogHandler());
            using var service = new ScannerCatalogService(httpClient, root);
            Assert.True(await service.RefreshAsync(GameMode.Regular));
            Assert.True(service.TryGetItem("item-0", out _));

            var loaded = await service.LoadCacheAsync(GameMode.Pve);

            Assert.False(loaded);
            Assert.Equal(GameMode.Pve, service.LoadedMode);
            Assert.Equal(0, service.Count);
            Assert.False(service.TryGetItem("item-0", out _));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task RefreshAsync_UsesCurrentProfileGameModePathIncludingSeason()
    {
        var root = CreateTemporaryDirectory();
        try
        {
            var handler = new CatalogHandler();
            using var httpClient = new HttpClient(handler);
            using var service = new ScannerCatalogService(httpClient, root);

            Assert.True(await service.RefreshAsync(GameMode.PvpSeason));

            Assert.Contains(handler.RequestedPaths, path => path.StartsWith("/pvp-season/", StringComparison.Ordinal));
            Assert.Equal(GameMode.PvpSeason, service.LoadedMode);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static string CreateTemporaryDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "JunhyunHelper-ScannerTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private sealed class CatalogHandler : HttpMessageHandler
    {
        private readonly string _items;
        private readonly string _korean;
        private readonly string _english;
        private readonly object _requestGate = new();
        private readonly List<string> _requestedPaths = [];

        public CatalogHandler()
        {
            var records = Enumerable.Range(0, 4000)
                .Select(index => new
                {
                    id = $"item-{index}",
                    name = $"name-{index}",
                    shortName = $"short-{index}",
                    iconLink = $"https://example.test/icons/{index}.png",
                    avg24hPrice = 2000 + index,
                    width = 2,
                    height = 2,
                    sellFor = new[]
                    {
                        new { priceRUB = 1000 + index, source = "Therapist" },
                    },
                })
                .ToArray();
            _items = JsonSerializer.Serialize(new { data = new { items = records } });

            var korean = new Dictionary<string, string>(StringComparer.Ordinal);
            var english = new Dictionary<string, string>(StringComparer.Ordinal);
            for (var index = 0; index < 4000; index++)
            {
                korean[$"name-{index}"] = $"공식 아이템 {index}";
                korean[$"short-{index}"] = $"아이템 {index}";
                english[$"name-{index}"] = $"Official item {index}";
                english[$"short-{index}"] = $"Item {index}";
            }
            _korean = JsonSerializer.Serialize(new { data = korean });
            _english = JsonSerializer.Serialize(new { data = english });
        }

        public IReadOnlyList<string> RequestedPaths
        {
            get
            {
                lock (_requestGate)
                    return _requestedPaths.ToArray();
            }
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath ?? string.Empty;
            lock (_requestGate)
                _requestedPaths.Add(path);

            var body = path.EndsWith("/items_ko", StringComparison.Ordinal)
                ? _korean
                : path.EndsWith("/items_en", StringComparison.Ordinal)
                    ? _english
                    : _items;
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            };
            return Task.FromResult(response);
        }
    }
}
