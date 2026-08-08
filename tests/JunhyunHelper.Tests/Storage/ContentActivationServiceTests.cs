using JunhyunHelper.Core.Content;
using JunhyunHelper.Core.Items;
using JunhyunHelper.Core.Profiles;
using JunhyunHelper.Infrastructure.Storage;
using Xunit;

namespace JunhyunHelper.Tests.Storage;

public sealed class ContentActivationServiceTests
{
    [Fact]
    public async Task ValidCandidateReplacesActiveAndPreservesPrevious()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var root = TempRoot();
        try
        {
            var store = new ContentSnapshotStore();
            var activation = new ContentActivationService(root, store);
            var paths = activation.GetPaths(GameMode.Regular);

            await store.WriteNewAsync(
                paths.ActivePath,
                GameMode.Regular,
                Catalog("old-item"),
                cancellationToken: cancellationToken);
            await store.WriteNewAsync(
                paths.CandidatePath,
                GameMode.Regular,
                Catalog("new-item"),
                cancellationToken: cancellationToken);

            await activation.ActivateCandidateAsync(GameMode.Regular, cancellationToken);

            Assert.False(File.Exists(paths.CandidatePath));
            Assert.Equal(
                "new-item",
                Assert.Single((await store.ReadAsync(paths.ActivePath, cancellationToken)).Content.Items).Id);
            Assert.Equal(
                "old-item",
                Assert.Single((await store.ReadAsync(paths.PreviousPath, cancellationToken)).Content.Items).Id);
        }
        finally
        {
            DeleteRoot(root);
        }
    }

    [Fact]
    public async Task InvalidCandidateNeverTouchesActiveContent()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var root = TempRoot();
        try
        {
            var store = new ContentSnapshotStore();
            var activation = new ContentActivationService(root, store);
            var paths = activation.GetPaths(GameMode.Regular);

            await store.WriteNewAsync(
                paths.ActivePath,
                GameMode.Regular,
                Catalog("old-item"),
                cancellationToken: cancellationToken);
            Directory.CreateDirectory(paths.Directory);
            await File.WriteAllTextAsync(
                paths.CandidatePath,
                "not a sqlite database",
                cancellationToken);

            await Assert.ThrowsAnyAsync<Exception>(
                () => activation.ActivateCandidateAsync(GameMode.Regular, cancellationToken));

            Assert.Equal(
                "old-item",
                Assert.Single((await store.ReadAsync(paths.ActivePath, cancellationToken)).Content.Items).Id);
        }
        finally
        {
            DeleteRoot(root);
        }
    }

    [Fact]
    public async Task DifferentGameModesKeepIndependentActiveDatabases()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var root = TempRoot();
        try
        {
            var store = new ContentSnapshotStore();
            var activation = new ContentActivationService(root, store);
            var regular = activation.GetPaths(GameMode.Regular);
            var pve = activation.GetPaths(GameMode.Pve);

            await store.WriteNewAsync(
                regular.CandidatePath,
                GameMode.Regular,
                Catalog("regular-item"),
                cancellationToken: cancellationToken);
            await activation.ActivateCandidateAsync(GameMode.Regular, cancellationToken);

            await store.WriteNewAsync(
                pve.CandidatePath,
                GameMode.Pve,
                Catalog("pve-item"),
                cancellationToken: cancellationToken);
            await activation.ActivateCandidateAsync(GameMode.Pve, cancellationToken);

            Assert.NotEqual(regular.ActivePath, pve.ActivePath);
            Assert.Equal(
                "regular-item",
                Assert.Single((await activation.ReadActiveOrRecoverAsync(GameMode.Regular, cancellationToken)).Content.Items).Id);
            Assert.Equal(
                "pve-item",
                Assert.Single((await activation.ReadActiveOrRecoverAsync(GameMode.Pve, cancellationToken)).Content.Items).Id);
        }
        finally
        {
            DeleteRoot(root);
        }
    }

    [Fact]
    public async Task CandidateFromWrongGameModeIsRejected()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var root = TempRoot();
        try
        {
            var store = new ContentSnapshotStore();
            var activation = new ContentActivationService(root, store);
            var regular = activation.GetPaths(GameMode.Regular);

            await store.WriteNewAsync(
                regular.CandidatePath,
                GameMode.Pve,
                Catalog("wrong-mode-item"),
                cancellationToken: cancellationToken);

            await Assert.ThrowsAsync<InvalidDataException>(
                () => activation.ActivateCandidateAsync(GameMode.Regular, cancellationToken));

            Assert.False(File.Exists(regular.ActivePath));
        }
        finally
        {
            DeleteRoot(root);
        }
    }

    private static GameContentCatalog Catalog(string itemId) => new(
        new[]
        {
            new GameItem(
                itemId,
                itemId,
                itemId,
                null,
                null,
                null,
                null,
                Array.Empty<string>()),
        },
        Array.Empty<JunhyunHelper.Core.Reference.TraderDefinition>(),
        Array.Empty<JunhyunHelper.Core.Reference.MapReference>(),
        Array.Empty<JunhyunHelper.Core.Quests.QuestDefinition>(),
        Array.Empty<JunhyunHelper.Core.Quests.QuestObjective>(),
        Array.Empty<JunhyunHelper.Core.Quests.QuestItemRequirement>(),
        Array.Empty<JunhyunHelper.Core.Hideout.HideoutStation>());

    private static string TempRoot() => Path.Combine(
        Path.GetTempPath(),
        "JunhyunHelper.Tests",
        Guid.NewGuid().ToString("N"));

    private static void DeleteRoot(string root)
    {
        if (Directory.Exists(root))
            Directory.Delete(root, recursive: true);
    }
}
