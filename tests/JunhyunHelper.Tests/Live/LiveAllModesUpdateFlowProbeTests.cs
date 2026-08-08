using JunhyunHelper.Core.Profiles;
using JunhyunHelper.Infrastructure.Content;
using JunhyunHelper.Infrastructure.Storage;
using JunhyunHelper.Infrastructure.TarkovJson;
using Xunit;

namespace JunhyunHelper.Tests.Live;

public sealed class LiveAllModesUpdateFlowProbeTests
{
    [Fact]
    public async Task CurrentSourcesBuildActivateAndReadForEverySupportedMode()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var root = Path.Combine(Path.GetTempPath(), $"JunhyunHelper-Live-{Guid.NewGuid():N}");

        try
        {
            using var httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromMinutes(3),
            };
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("JunhyunHelper-live-update-probe/1.0");

            var builder = new TarkovContentBuildService(
                new TarkovEndpointSourceLoader(new TarkovJsonClient(httpClient)));
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

                var paths = activation.GetPaths(gameMode);
                Assert.True(File.Exists(paths.ActivePath));
                Assert.False(File.Exists(paths.CandidatePath));

                Console.WriteLine(
                    $"{gameMode}: Items={active.Content.Items.Count}, Quests={active.Content.Quests.Count}, " +
                    $"Hideout={active.Content.HideoutStations.Count}, Ammo={active.Content.Ammunition.Count}");
            }

            Assert.Equal(3, Enum.GetValues<GameMode>()
                .Count(mode => File.Exists(activation.GetPaths(mode).ActivePath)));
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }
}
