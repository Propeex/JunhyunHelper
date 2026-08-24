using System.Net;
using System.Text;
using System.Text.Json;
using JunhyunHelper.Core.Profiles;
using JunhyunHelper.Infrastructure.Scanner;
using Xunit;

namespace JunhyunHelper.Tests.Scanner;

public sealed class ScannerCatalogNetworkResilienceTests
{
    [Fact]
    public async Task RefreshAsync_RetriesTransientRequiredEndpointAndRecovers()
    {
        var root = CreateTemporaryDirectory();
        try
        {
            var handler = new ResilienceHandler(failFirstBaseRequest: true);
            using var httpClient = new HttpClient(handler);
            using var service = new ScannerCatalogService(httpClient, root);

            var success = await service.RefreshAsync(GameMode.Regular, TestContext.Current.CancellationToken);

            Assert.True(success);
            Assert.Equal("success", service.LastDiagnostics.Outcome);
            Assert.Equal(2, handler.BaseItemRequests);
            Assert.Equal(4000, service.Count);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task RefreshAsync_OptionalLocalizationAndTraderFailuresDoNotDisableKoreanIdentity()
    {
        var root = CreateTemporaryDirectory();
        try
        {
            var handler = new ResilienceHandler(failOptionalEndpoints: true);
            using var httpClient = new HttpClient(handler);
            using var service = new ScannerCatalogService(httpClient, root);

            var success = await service.RefreshAsync(GameMode.Regular, TestContext.Current.CancellationToken);

            Assert.True(success);
            Assert.Equal("success", service.LastDiagnostics.Outcome);
            Assert.Equal(4000, service.Count);
            Assert.True(service.TryGetItem("item-0", out var item));
            Assert.Equal("공식 아이템 0", item.OfficialName);
            Assert.Equal(1250, item.BestTraderSellPrice);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task RefreshAsync_MalformedOptionalEnglishJsonFailsSoft()
    {
        var root = CreateTemporaryDirectory();
        try
        {
            var handler = new ResilienceHandler(malformedEnglish: true);
            using var httpClient = new HttpClient(handler);
            using var service = new ScannerCatalogService(httpClient, root);

            var success = await service.RefreshAsync(GameMode.Regular, TestContext.Current.CancellationToken);

            Assert.True(success);
            Assert.Equal("success", service.LastDiagnostics.Outcome);
            Assert.True(service.TryGetItem("item-17", out var item));
            Assert.Equal("공식 아이템 17", item.OfficialName);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task RefreshAsync_NonRetryableRequired404FailsWithoutRepeatedRequests()
    {
        var root = CreateTemporaryDirectory();
        try
        {
            var handler = new ResilienceHandler(baseStatus: HttpStatusCode.NotFound);
            using var httpClient = new HttpClient(handler);
            using var service = new ScannerCatalogService(httpClient, root);

            var success = await service.RefreshAsync(GameMode.Regular, TestContext.Current.CancellationToken);

            Assert.False(success);
            Assert.Equal("http-failure", service.LastDiagnostics.Outcome);
            Assert.Equal(1, handler.BaseItemRequests);
            Assert.Equal(0, service.Count);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static string CreateTemporaryDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "JunhyunHelper-ScannerNetworkTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private sealed class ResilienceHandler : HttpMessageHandler
    {
        private readonly string _items;
        private readonly string _korean;
        private readonly string _english;
        private readonly bool _failFirstBaseRequest;
        private readonly bool _failOptionalEndpoints;
        private readonly bool _malformedEnglish;
        private readonly HttpStatusCode? _baseStatus;
        private int _baseItemRequests;

        public ResilienceHandler(
            bool failFirstBaseRequest = false,
            bool failOptionalEndpoints = false,
            bool malformedEnglish = false,
            HttpStatusCode? baseStatus = null)
        {
            _failFirstBaseRequest = failFirstBaseRequest;
            _failOptionalEndpoints = failOptionalEndpoints;
            _malformedEnglish = malformedEnglish;
            _baseStatus = baseStatus;

            var records = Enumerable.Range(0, 4000)
                .Select(index => new
                {
                    id = $"item-{index}",
                    name = $"name-{index}",
                    shortName = $"short-{index}",
                    avg24hPrice = 2000 + index,
                    width = 2,
                    height = 2,
                    sellFor = new[]
                    {
                        new { priceRUB = 1250 + index, source = "Mechanic" },
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

        public int BaseItemRequests => Volatile.Read(ref _baseItemRequests);

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath ?? string.Empty;
            if (path.EndsWith("/items", StringComparison.Ordinal))
            {
                var count = Interlocked.Increment(ref _baseItemRequests);
                if (_baseStatus is { } status)
                    return Task.FromResult(Response(status, "{}"));
                if (_failFirstBaseRequest && count == 1)
                    return Task.FromResult(Response(HttpStatusCode.ServiceUnavailable, "{}"));
                return Task.FromResult(Response(HttpStatusCode.OK, _items));
            }

            if (path.EndsWith("/items_ko", StringComparison.Ordinal))
                return Task.FromResult(Response(HttpStatusCode.OK, _korean));

            if (path.EndsWith("/items_en", StringComparison.Ordinal))
            {
                if (_failOptionalEndpoints)
                    return Task.FromResult(Response(HttpStatusCode.ServiceUnavailable, "{}"));
                return Task.FromResult(Response(
                    HttpStatusCode.OK,
                    _malformedEnglish ? "{ definitely-not-json" : _english));
            }

            if (path.Contains("/traders", StringComparison.Ordinal))
            {
                if (_failOptionalEndpoints)
                    return Task.FromResult(Response(HttpStatusCode.ServiceUnavailable, "{}"));
                return Task.FromResult(Response(HttpStatusCode.OK, "{\"data\":{}}"));
            }

            return Task.FromResult(Response(HttpStatusCode.NotFound, "{}"));
        }

        private static HttpResponseMessage Response(HttpStatusCode status, string body) => new(status)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };
    }
}
