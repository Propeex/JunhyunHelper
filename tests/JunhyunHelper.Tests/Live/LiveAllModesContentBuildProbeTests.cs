using JunhyunHelper.Core.Profiles;
using JunhyunHelper.Infrastructure.Content;
using JunhyunHelper.Infrastructure.TarkovJson;
using Xunit;

namespace JunhyunHelper.Tests.Live;

public sealed class LiveAllModesContentBuildProbeTests
{
    [Fact]
    public async Task CurrentSourcesBuildForEverySupportedGameMode()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(3),
        };
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("JunhyunHelper-live-probe/1.0");

        var builder = new TarkovContentBuildService(
            new TarkovEndpointSourceLoader(new TarkovJsonClient(httpClient)));

        foreach (var gameMode in Enum.GetValues<GameMode>())
        {
            var result = await builder.BuildAsync(gameMode, cancellationToken);

            foreach (var warning in result.Warnings)
                Console.WriteLine($"{gameMode} WARNING: {warning}");
            foreach (var issue in result.Validation.Issues)
                Console.WriteLine($"{gameMode} {issue.Severity}: {issue.Code} - {issue.Message}");

            Console.WriteLine(
                $"{gameMode}: Items={result.Content.Items.Count}, Quests={result.Content.Quests.Count}, " +
                $"Hideout={result.Content.HideoutStations.Count}, Ammo={result.Content.Ammunition.Count}");

            Assert.True(result.IsValid);
            Assert.NotEmpty(result.Content.Items);
            Assert.NotEmpty(result.Content.Quests);
            Assert.NotEmpty(result.Content.HideoutStations);
            Assert.NotEmpty(result.Content.Ammunition);
        }
    }
}
