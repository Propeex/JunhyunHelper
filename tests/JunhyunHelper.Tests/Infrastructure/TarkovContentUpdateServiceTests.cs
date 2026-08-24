using JunhyunHelper.Core.Ammo;
using JunhyunHelper.Core.Content;
using JunhyunHelper.Core.Editions;
using JunhyunHelper.Core.Hideout;
using JunhyunHelper.Core.Items;
using JunhyunHelper.Core.Profiles;
using JunhyunHelper.Core.Quests;
using JunhyunHelper.Core.Reference;
using JunhyunHelper.Infrastructure.Content;
using JunhyunHelper.Infrastructure.Storage;
using JunhyunHelper.Infrastructure.Validation;
using Xunit;

namespace JunhyunHelper.Tests.Infrastructure;

public sealed class TarkovContentUpdateServiceTests
{
    [Fact]
    public async Task HealthyCandidateIsPersistedVerifiedAndActivated()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var root = CreateTemporaryDirectory();
        try
        {
            var candidate = CreateCatalog(8, "candidate");
            var builder = new FixedBuildService(candidate);
            var store = new ContentSnapshotStore();
            var activation = new ContentActivationService(root, store);
            var service = new TarkovContentUpdateService(builder, activation, store);

            var result = await service.UpdateAsync(GameMode.Regular, cancellationToken);

            Assert.True(result.Applied);
            Assert.True(result.Validation.IsValid);
            var active = await activation.ReadActiveOrRecoverAsync(GameMode.Regular, cancellationToken);
            Assert.Equal(8, active.Content.Items.Count);
            Assert.StartsWith("candidate-", active.Content.Items[0].Id, StringComparison.Ordinal);
            Assert.False(File.Exists(activation.GetPaths(GameMode.Regular).CandidatePath));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task SuspiciousPartialCandidateDoesNotReplaceLastKnownGoodActiveSnapshot()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var root = CreateTemporaryDirectory();
        try
        {
            var store = new ContentSnapshotStore();
            var activation = new ContentActivationService(root, store);
            var paths = activation.GetPaths(GameMode.Regular);
            Directory.CreateDirectory(paths.Directory);
            await store.WriteNewAsync(
                paths.ActivePath,
                GameMode.Regular,
                CreateCatalog(10, "baseline"),
                cancellationToken: cancellationToken);

            var builder = new FixedBuildService(CreateCatalog(4, "candidate"));
            var service = new TarkovContentUpdateService(builder, activation, store);

            var result = await service.UpdateAsync(GameMode.Regular, cancellationToken);

            Assert.False(result.Applied);
            Assert.Contains(result.Validation.Issues, issue => issue.Code == "update.items.suspicious-shrink");
            var active = await activation.ReadActiveOrRecoverAsync(GameMode.Regular, cancellationToken);
            Assert.Equal(10, active.Content.Items.Count);
            Assert.StartsWith("baseline-", active.Content.Items[0].Id, StringComparison.Ordinal);
            Assert.False(File.Exists(paths.CandidatePath));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task ConcurrentUpdateRequestsAreSerializedAcrossBuildAndActivationBoundary()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var root = CreateTemporaryDirectory();
        try
        {
            var builder = new ConcurrencyBuildService(CreateCatalog(8, "candidate"));
            var store = new ContentSnapshotStore();
            var activation = new ContentActivationService(root, store);
            var service = new TarkovContentUpdateService(builder, activation, store);

            var first = service.UpdateAsync(GameMode.Regular, cancellationToken);
            await builder.FirstBuildEntered.Task.WaitAsync(cancellationToken);
            var second = service.UpdateAsync(GameMode.Regular, cancellationToken);

            await Task.Delay(50, cancellationToken);
            Assert.Equal(1, builder.MaximumConcurrentBuilds);

            builder.ReleaseBuilds.TrySetResult();
            var results = await Task.WhenAll(first, second);

            Assert.All(results, result => Assert.True(result.Applied));
            Assert.Equal(1, builder.MaximumConcurrentBuilds);
            Assert.Equal(2, builder.BuildCount);
            Assert.False(File.Exists(activation.GetPaths(GameMode.Regular).CandidatePath));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static GameContentCatalog CreateCatalog(int itemCount, string prefix)
    {
        var items = Enumerable.Range(0, Math.Max(1, itemCount))
            .Select(index => new GameItem(
                $"{prefix}-item-{index}",
                $"{prefix} 아이템 {index}",
                $"{prefix} item {index}",
                null,
                null,
                $"https://example.test/{prefix}/{index}.png",
                $"https://example.test/{prefix}/{index}",
                Array.Empty<string>()))
            .ToArray();
        var firstItemId = items[0].Id;

        return new GameContentCatalog(
            items,
            [new TraderDefinition($"{prefix}-trader", $"{prefix} 상인", $"{prefix} trader")],
            [new MapReference($"{prefix}-map", $"{prefix} 맵", $"{prefix} map", $"{prefix}-map")],
            [
                new QuestDefinition(
                    $"{prefix}-quest",
                    $"{prefix} 퀘스트",
                    $"{prefix} quest",
                    $"{prefix}-trader",
                    $"{prefix}-map",
                    $"https://example.test/{prefix}/quest",
                    100,
                    false,
                    false,
                    false,
                    1,
                    null,
                    null,
                    Array.Empty<QuestTaskRequirement>(),
                    Array.Empty<QuestTraderStandingRequirement>(),
                    Array.Empty<QuestTraderLoyaltyRequirement>()),
            ],
            [
                new QuestObjective(
                    $"{prefix}-quest",
                    $"{prefix}-objective",
                    "giveItem",
                    $"{prefix} 제출",
                    $"{prefix} submit",
                    false,
                    1,
                    true,
                    [$"{prefix}-map"],
                    [firstItemId],
                    null,
                    QuestItemObjectiveKind.Submit),
            ],
            [new QuestItemRequirement($"{prefix}-quest", $"{prefix}-objective", [firstItemId], 1, true)],
            [
                new HideoutStation(
                    $"{prefix}-station",
                    $"{prefix} 시설",
                    $"{prefix} station",
                    $"https://example.test/{prefix}/station.png",
                    [
                        new HideoutLevel(
                            $"{prefix}-station",
                            1,
                            10,
                            [new HideoutItemRequirement($"{prefix}-station", 1, firstItemId, 1, false)]),
                    ]),
            ],
            Ammo:
            [
                new AmmoDefinition(
                    firstItemId,
                    "CaliberTest",
                    "bullet",
                    1,
                    50,
                    20,
                    30,
                    0.1m,
                    0.1m,
                    0,
                    0,
                    300,
                    0,
                    0,
                    false,
                    null,
                    Array.Empty<AmmoAcquisition>()),
            ],
            EditionData:
            [
                new EditionDefinition(
                    $"{prefix}-edition",
                    $"{prefix} edition",
                    new HashSet<string>(StringComparer.Ordinal),
                    new HashSet<string>(StringComparer.Ordinal)),
            ]);
    }

    private static string CreateTemporaryDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "JunhyunHelper-ContentUpdateTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private sealed class FixedBuildService(GameContentCatalog content) : ITarkovContentBuildService
    {
        public Task<TarkovContentBuildResult> BuildAsync(
            GameMode gameMode,
            CancellationToken cancellationToken = default,
            IProgress<ContentUpdateProgress>? progress = null)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var validation = new GameContentIntegrityValidator().Validate(content);
            return Task.FromResult(new TarkovContentBuildResult(content, validation, []));
        }
    }

    private sealed class ConcurrencyBuildService(GameContentCatalog content) : ITarkovContentBuildService
    {
        private int _activeBuilds;
        private int _maximumConcurrentBuilds;
        private int _buildCount;

        public TaskCompletionSource FirstBuildEntered { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource ReleaseBuilds { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int MaximumConcurrentBuilds => Volatile.Read(ref _maximumConcurrentBuilds);
        public int BuildCount => Volatile.Read(ref _buildCount);

        public async Task<TarkovContentBuildResult> BuildAsync(
            GameMode gameMode,
            CancellationToken cancellationToken = default,
            IProgress<ContentUpdateProgress>? progress = null)
        {
            var current = Interlocked.Increment(ref _activeBuilds);
            Interlocked.Increment(ref _buildCount);
            UpdateMaximum(current);
            FirstBuildEntered.TrySetResult();
            try
            {
                await ReleaseBuilds.Task.WaitAsync(cancellationToken);
                var validation = new GameContentIntegrityValidator().Validate(content);
                return new TarkovContentBuildResult(content, validation, []);
            }
            finally
            {
                Interlocked.Decrement(ref _activeBuilds);
            }
        }

        private void UpdateMaximum(int current)
        {
            while (true)
            {
                var observed = Volatile.Read(ref _maximumConcurrentBuilds);
                if (current <= observed ||
                    Interlocked.CompareExchange(ref _maximumConcurrentBuilds, current, observed) == observed)
                {
                    return;
                }
            }
        }
    }
}
