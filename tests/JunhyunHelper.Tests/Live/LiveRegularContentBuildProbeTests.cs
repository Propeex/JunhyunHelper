using JunhyunHelper.Core.Profiles;
using JunhyunHelper.Infrastructure.Content;
using JunhyunHelper.Infrastructure.TarkovJson;
using Xunit;

namespace JunhyunHelper.Tests.Live;

public sealed class LiveRegularContentBuildProbeTests
{
    [Fact]
    public async Task CurrentRegularSourceBuildsCanonicalContent()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(3),
        };
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("JunhyunHelper-live-probe/1.0");

        var client = new TarkovJsonClient(httpClient);
        var loader = new TarkovEndpointSourceLoader(client);
        var builder = new TarkovContentBuildService(loader);

        var result = await builder.BuildAsync(GameMode.Regular, cancellationToken);

        foreach (var warning in result.Warnings)
            Console.WriteLine($"WARNING: {warning}");
        foreach (var issue in result.Validation.Issues)
            Console.WriteLine($"{issue.Severity}: {issue.Code} - {issue.Message}");

        Console.WriteLine($"Items={result.Content.Items.Count}");
        Console.WriteLine($"Quests={result.Content.Quests.Count}");
        Console.WriteLine($"Hideout={result.Content.HideoutStations.Count}");
        Console.WriteLine($"Ammo={result.Content.Ammunition.Count}");

        Assert.True(result.IsValid);
        Assert.NotEmpty(result.Content.Items);
        Assert.NotEmpty(result.Content.Quests);
        Assert.NotEmpty(result.Content.HideoutStations);
        Assert.NotEmpty(result.Content.Ammunition);
    }
}
