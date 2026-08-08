using JunhyunHelper.Core.Profiles;
using JunhyunHelper.Infrastructure.Content;
using JunhyunHelper.Infrastructure.EditionData;
using JunhyunHelper.Infrastructure.Storage;
using JunhyunHelper.Infrastructure.TarkovJson;
using Xunit;

namespace JunhyunHelper.Tests.Live;

public sealed class LiveEditionAllModesUpdateProbeTests
{
    [Fact]
    public async Task CurrentPrimaryAndEditionSourcesBuildActivateAndReadForEveryMode()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var root = Path.Combine(Path.GetTempPath(), $"JunhyunHelper-LiveEdition-{Guid.NewGuid():N}");

        try
        {
            using var httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromMinutes(3),
            };
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("JunhyunHelper-live-edition-probe/1.0");

            var builder = new TarkovContentBuildService(
                new TarkovEndpointSourceLoader(new TarkovJsonClient(httpClient)),
                new TarkovEditionCatalogClient(httpClient));
            var activation = new ContentActivationService(root);
            var updater = new TarkovContentUpdateService(builder, activation);

            foreach (var gameMode in Enum.GetValues<GameMode>())
            {
                var update = await updater.UpdateAsync(gameMode, cancellationToken);
                Assert.True(update.Applied);
                Assert.True(update.Validation.IsValid);

                var active = await activation.ReadActiveOrRecoverAsync(gameMode, cancellationToken);
                Assert.Equal(gameMode, active.GameMode);
                Assert.NotEmpty(active.Content.Items);
                Assert.NotEmpty(active.Content.Quests);
                Assert.NotEmpty(active.Content.HideoutStations);
                Assert.NotEmpty(active.Content.Ammunition);
                Assert.NotEmpty(active.Content.Editions);

                var eod = Assert.Single(
                    active.Content.Editions,
                    edition => edition.Id == "edge_of_darkness");
                Assert.NotEmpty(eod.ExclusiveQuestIds);

                Console.WriteLine(
                    $"{gameMode}: Items={active.Content.Items.Count}, Quests={active.Content.Quests.Count}, " +
                    $"Hideout={active.Content.HideoutStations.Count}, Ammo={active.Content.Ammunition.Count}, " +
                    $"Editions={active.Content.Editions.Count}, Warnings={update.Validation.Issues.Count}");
            }
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }
}
