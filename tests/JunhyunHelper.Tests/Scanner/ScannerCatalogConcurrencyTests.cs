using System.Net;
using System.Text;
using System.Text.Json;
using JunhyunHelper.Core.Profiles;
using JunhyunHelper.Infrastructure.Scanner;
using Xunit;

namespace JunhyunHelper.Tests.Scanner;

public sealed class ScannerCatalogConcurrencyTests
{
    [Fact]
    public async Task LoadCacheAsync_WaitsForInFlightRefreshAndKeepsNewestMode()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var root = CreateTemporaryDirectory();
        try
        {
            var handler = new BlockingCatalogHandler();
            using var httpClient = new HttpClient(handler);
            using var service = new ScannerCatalogService(httpClient, root);

            // Seed a valid PvE disk cache, then start an older Regular network refresh.
            Assert.True(await service.RefreshAsync(GameMode.Pve, cancellationToken));
            Assert.Equal(GameMode.Pve, service.LoadedMode);

            handler.BlockRegularRequests = true;
            var regularRefresh = service.RefreshAsync(GameMode.Regular, cancellationToken);
            await handler.RegularRequestStarted.Task.WaitAsync(cancellationToken);

            // This represents the newer profile transition. Before the fix LoadCacheAsync
            // could replace the in-memory catalog immediately while the Regular refresh
            // was still running; that older refresh would then overwrite PvE on completion.
            var pveLoad = service.LoadCacheAsync(GameMode.Pve, cancellationToken);
            try
            {
                await Task.Delay(100, cancellationToken);
                Assert.False(pveLoad.IsCompleted);
            }
            finally
            {
                handler.ReleaseRegularRequests();
            }

            Assert.True(await regularRefresh);
            Assert.True(await pveLoad);
            Assert.Equal(GameMode.Pve, service.LoadedMode);
            Assert.True(service.HasHealthyCatalog);
            Assert.True(service.TryGetItem("item-0", out _));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static string CreateTemporaryDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "JunhyunHelper-ScannerConcurrencyTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private sealed class BlockingCatalogHandler : HttpMessageHandler
    {
        private readonly string _items;
        private readonly string _korean;
        private readonly string _english;
        private readonly TaskCompletionSource _regularRequestStarted =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _releaseRegular =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public BlockingCatalogHandler()
        {
            var records = Enumerable.Range(0, ScannerCatalogService.MinimumHealthyItemCount)
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
                        new { priceRUB = 1250 + index, source = "Mechanic" },
                    },
                })
                .ToArray();
            _items = JsonSerializer.Serialize(new { data = new { items = records } });

            var korean = new Dictionary<string, string>(StringComparer.Ordinal);
            var english = new Dictionary<string, string>(StringComparer.Ordinal);
            for (var index = 0; index < ScannerCatalogService.MinimumHealthyItemCount; index++)
            {
                korean[$"name-{index}"] = $"공식 아이템 {index}";
                korean[$"short-{index}"] = $"아이템 {index}";
                english[$"name-{index}"] = $"Official item {index}";
                english[$"short-{index}"] = $"Item {index}";
            }
            _korean = JsonSerializer.Serialize(new { data = korean });
            _english = JsonSerializer.Serialize(new { data = english });
        }

        public bool BlockRegularRequests { get; set; }

        public TaskCompletionSource RegularRequestStarted => _regularRequestStarted;

        public void ReleaseRegularRequests() => _releaseRegular.TrySetResult();

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath ?? string.Empty;
            if (BlockRegularRequests && path.StartsWith("/regular/", StringComparison.Ordinal))
            {
                _regularRequestStarted.TrySetResult();
                await _releaseRegular.Task.WaitAsync(cancellationToken);
            }

            var body = path.EndsWith("/items_ko", StringComparison.Ordinal)
                ? _korean
                : path.EndsWith("/items_en", StringComparison.Ordinal)
                    ? _english
                    : _items;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            };
        }
    }
}
