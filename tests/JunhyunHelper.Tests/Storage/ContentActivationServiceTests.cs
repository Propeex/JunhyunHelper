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
        var root = TempRoot();
        try
        {
            var store = new ContentSnapshotStore();
            var activation = new ContentActivationService(root, store);

            await store.WriteNewAsync(
                activation.ActivePath,
                GameMode.Regular,
                Catalog("old-item"));
            await store.WriteNewAsync(
                activation.CandidatePath,
                GameMode.Regular,
                Catalog("new-item"));

            await activation.ActivateCandidateAsync();

            Assert.False(File.Exists(activation.CandidatePath));
            Assert.Equal(
                "new-item",
                Assert.Single((await store.ReadAsync(activation.ActivePath)).Content.Items).Id);
            Assert.Equal(
                "old-item",
                Assert.Single((await store.ReadAsync(activation.PreviousPath)).Content.Items).Id);
        }
        finally
        {
            DeleteRoot(root);
        }
    }

    [Fact]
    public async Task InvalidCandidateNeverTouchesActiveContent()
    {
        var root = TempRoot();
        try
        {
            var store = new ContentSnapshotStore();
            var activation = new ContentActivationService(root, store);

            await store.WriteNewAsync(
                activation.ActivePath,
                GameMode.Regular,
                Catalog("old-item"));
            Directory.CreateDirectory(root);
            await File.WriteAllTextAsync(activation.CandidatePath, "not a sqlite database");

            await Assert.ThrowsAnyAsync<Exception>(
                () => activation.ActivateCandidateAsync());

            Assert.Equal(
                "old-item",
                Assert.Single((await store.ReadAsync(activation.ActivePath)).Content.Items).Id);
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
