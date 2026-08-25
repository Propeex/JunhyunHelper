using JunhyunHelper.Core.Profiles;
using JunhyunHelper.Infrastructure.Content;
using JunhyunHelper.Infrastructure.EditionData;
using JunhyunHelper.Infrastructure.TarkovJson;
using JunhyunHelper.Infrastructure.Validation;
using Xunit;

namespace JunhyunHelper.Tests.Live;

public sealed class LiveV171DataUpdateProbeTests
{
    [Fact]
    public async Task CurrentRegularAndPveSourcesHaveZeroFatalValidationIssues()
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

        var failures = new List<string>();
        foreach (var mode in new[] { GameMode.Regular, GameMode.Pve })
        {
            var build = await builder.BuildAsync(mode, TestContext.Current.CancellationToken);
            var fatals = build.Validation.Issues
                .Where(static issue => issue.Severity == ContentValidationSeverity.Fatal)
                .ToArray();
            var canonicalItemIds = build.Content.Items
                .Select(static item => item.Id)
                .ToHashSet(StringComparer.Ordinal);
            var noncanonicalQuestItems = build.Content.QuestObjectives
                .Where(objective =>
                    !string.IsNullOrWhiteSpace(objective.QuestItemId) &&
                    !canonicalItemIds.Contains(objective.QuestItemId))
                .ToArray();

            Console.WriteLine(
                $"LIVE_UPDATE_PROBE {mode}: items={build.Content.Items.Count} quests={build.Content.Quests.Count} " +
                $"objectives={build.Content.QuestObjectives.Count} questItems={build.Content.QuestItemRequirements.Count} " +
                $"hideout={build.Content.HideoutStations.Count} ammo={build.Content.Ammunition.Count} fatal={fatals.Length} " +
                $"noncanonicalQuestItem={noncanonicalQuestItems.Length}");

            foreach (var group in noncanonicalQuestItems
                         .GroupBy(static objective => (objective.Type, objective.ItemKind))
                         .OrderBy(static group => group.Key.Type, StringComparer.Ordinal))
            {
                Console.WriteLine(
                    $"QUEST_ITEM_SEMANTICS {mode}: type={group.Key.Type} kind={group.Key.ItemKind} count={group.Count()}");
            }

            foreach (var objective in noncanonicalQuestItems.Take(12))
            {
                Console.WriteLine(
                    $"QUEST_ITEM_SAMPLE {mode}: quest={objective.QuestId} objective={objective.ObjectiveId} " +
                    $"type={objective.Type} kind={objective.ItemKind} questItem={objective.QuestItemId}");
            }

            foreach (var fatal in fatals)
                failures.Add($"{mode}: {fatal.Code}: {fatal.Message}");
        }

        Assert.Fail(string.Join(Environment.NewLine, failures));
    }
}
