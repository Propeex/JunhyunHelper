using JunhyunHelper.Core.Profiles;
using JunhyunHelper.Infrastructure.Content;
using JunhyunHelper.Infrastructure.EditionData;
using JunhyunHelper.Infrastructure.TarkovJson;
using JunhyunHelper.Infrastructure.Validation;
using Xunit;

namespace JunhyunHelper.Tests.Live;

/// <summary>
/// Opt-in external contract probe for the current Tarkov data sources.
/// This intentionally stays outside hermetic PR/main CI because upstream/network
/// availability is not a product-build invariant.
/// </summary>
public sealed class LiveDataUpdateProbeTests
{
    [Fact]
    public async Task CurrentRegularAndPveSourcesHaveZeroFatalValidationIssues()
    {
        if (!string.Equals(
                Environment.GetEnvironmentVariable("JUNHYUNHELPER_LIVE_DATA_PROBE"),
                "1",
                StringComparison.Ordinal))
        {
            return;
        }

        using var httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(3) };
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("JunhyunHelper-live-data-probe/1.0");

        var builder = new TarkovContentBuildService(
            new TarkovEndpointSourceLoader(new TarkovJsonClient(httpClient)),
            new TarkovEditionCatalogClient(httpClient));

        var failures = new List<string>();
        foreach (var mode in new[] { GameMode.Regular, GameMode.Pve })
        {
            var build = await builder.BuildAsync(mode, TestContext.Current.CancellationToken);
            var fatals = build.Validation.Issues
                .Where(static issue => issue.Severity == ContentValidationSeverity.Fatal)
                .ToArray();

            Console.WriteLine(
                $"LIVE_DATA_PROBE {mode}: items={build.Content.Items.Count} quests={build.Content.Quests.Count} " +
                $"objectives={build.Content.QuestObjectives.Count} questItems={build.Content.QuestItemRequirements.Count} " +
                $"hideout={build.Content.HideoutStations.Count} ammo={build.Content.Ammunition.Count} " +
                $"sourceWarnings={build.Warnings.Count} validationIssues={build.Validation.Issues.Count} fatal={fatals.Length}");

            foreach (var warning in build.Warnings.Take(20))
                Console.WriteLine($"LIVE_DATA_PROBE_WARNING {mode}: {warning}");

            if (build.Warnings.Count > 20)
                Console.WriteLine($"LIVE_DATA_PROBE_WARNING {mode}: {build.Warnings.Count - 20} additional warning(s) omitted.");

            foreach (var fatal in fatals)
                failures.Add($"{mode}: {fatal.Code}: {fatal.Message}");
        }

        Assert.Empty(failures);
    }
}
