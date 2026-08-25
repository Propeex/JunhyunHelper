using JunhyunHelper.Core.Profiles;
using JunhyunHelper.Infrastructure.Content;
using JunhyunHelper.Infrastructure.EditionData;
using JunhyunHelper.Infrastructure.TarkovJson;
using Xunit;

namespace JunhyunHelper.Tests.Live;

public sealed class LiveV171DataUpdateProbeTests
{
    [Fact]
    public async Task CurrentRegularAndPveQuestItemSemanticsAreKnown()
    {
        if (!string.Equals(
                Environment.GetEnvironmentVariable("JUNHYUNHELPER_V171_LIVE_PROBE"),
                "1",
                StringComparison.Ordinal))
        {
            return;
        }

        using var httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(3) };
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("JunhyunHelper-v1.7.1-release-probe/1.0");

        var builder = new TarkovContentBuildService(
            new TarkovEndpointSourceLoader(new TarkovJsonClient(httpClient)),
            new TarkovEditionCatalogClient(httpClient));

        var report = new List<string>();
        foreach (var mode in new[] { GameMode.Regular, GameMode.Pve })
        {
            var build = await builder.BuildAsync(mode, TestContext.Current.CancellationToken);
            var canonicalItemIds = build.Content.Items
                .Select(static item => item.Id)
                .ToHashSet(StringComparer.Ordinal);
            var noncanonicalQuestItems = build.Content.QuestObjectives
                .Where(objective =>
                    !string.IsNullOrWhiteSpace(objective.QuestItemId) &&
                    !canonicalItemIds.Contains(objective.QuestItemId))
                .ToArray();

            report.Add($"{mode}: noncanonicalQuestItem={noncanonicalQuestItems.Length}");
            foreach (var group in noncanonicalQuestItems
                         .GroupBy(static objective => (objective.Type, objective.ItemKind))
                         .OrderBy(static group => group.Key.Type, StringComparer.Ordinal))
            {
                report.Add($"{mode}: type={group.Key.Type} kind={group.Key.ItemKind} count={group.Count()}");
            }

            foreach (var objective in noncanonicalQuestItems.Take(8))
            {
                report.Add(
                    $"{mode}: sample quest={objective.QuestId} objective={objective.ObjectiveId} " +
                    $"type={objective.Type} kind={objective.ItemKind} questItem={objective.QuestItemId}");
            }
        }

        Assert.Fail(string.Join(Environment.NewLine, report));
    }
}
