using System.Runtime.CompilerServices;
using Xunit;

namespace JunhyunHelper.Tests.Maintenance;

public sealed class V1173StabilityOptimizationContractTests
{
    [Fact]
    public void Hideout_ReusesContentItemIndexAndFlushesPreviousStationBeforeSelectionSwitch()
    {
        var root = FindRepositoryRoot();
        var source = Read(root, "src", "JunhyunHelper.Desktop", "Hideout", "HideoutPage.xaml.cs");

        Assert.Contains("EnsureContentIndex(content);", source, StringComparison.Ordinal);
        Assert.Equal(1, Count(source, "content.Items.ToDictionary(item => item.Id, StringComparer.Ordinal)"));
        Assert.Contains("_itemsById.TryGetValue(requirement.ItemId, out var item)", source, StringComparison.Ordinal);
        Assert.Contains("_pendingLevelChange is { } pending", source, StringComparison.Ordinal);
        Assert.Contains("FlushPendingLevelChange();", source, StringComparison.Ordinal);
        Assert.Contains("if (!IsEnabled)", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Quest_ReusesContentIndexesAndQuestScopedRequirementGroups()
    {
        var root = FindRepositoryRoot();
        var source = Read(root, "src", "JunhyunHelper.Desktop", "Quests", "QuestPage.xaml.cs");
        var navigation = Read(root, "src", "JunhyunHelper.Desktop", "Quests", "QuestPage.Navigation.cs");

        Assert.Contains("EnsureContentIndexes(content);", source, StringComparison.Ordinal);
        Assert.Equal(1, Count(source, "content.Items.ToDictionary(item => item.Id, StringComparer.Ordinal)"));
        Assert.Equal(1, Count(source, "content.Quests.ToDictionary(quest => quest.Id, StringComparer.Ordinal)"));
        Assert.Contains("QuestObjectivesFor(quest.Id)", source, StringComparison.Ordinal);
        Assert.Contains("QuestItemRequirementsFor(quest.Id)", source, StringComparison.Ordinal);
        Assert.DoesNotContain("_content.QuestObjectives\n            .Where(objective => objective.QuestId == quest.Id)", source, StringComparison.Ordinal);
        Assert.DoesNotContain("_content.Items.ToDictionary", navigation, StringComparison.Ordinal);
        Assert.DoesNotContain("_content.Quests.ToDictionary", navigation, StringComparison.Ordinal);
        Assert.Contains("QuestItemRequirementsFor(quest.Id)", navigation, StringComparison.Ordinal);
    }

    [Fact]
    public void Items_ReusesStableContentAndRowIndexesAcrossInventoryRefreshes()
    {
        var root = FindRepositoryRoot();
        var source = Read(root, "src", "JunhyunHelper.Desktop", "Items", "ItemsPage.xaml.cs");
        var inventoryRefresh = Read(root, "src", "JunhyunHelper.Desktop", "Items", "ItemsPage.InventoryRefresh.cs");

        Assert.Contains("EnsureContentIndexes(content);", source, StringComparison.Ordinal);
        Assert.Equal(1, Count(source, "content.Items.ToDictionary(item => item.Id, StringComparer.Ordinal)"));
        Assert.Contains("_questsById.TryGetValue(source.SourceId, out var quest)", source, StringComparison.Ordinal);
        Assert.Contains("_stationsById.TryGetValue(source.SourceId, out var station)", source, StringComparison.Ordinal);
        Assert.Contains("_rowsById.TryGetValue(itemId, out var target)", source, StringComparison.Ordinal);
        Assert.Contains("EnsureContentIndexes(content);", inventoryRefresh, StringComparison.Ordinal);
        Assert.Contains("_rowsById = _allRows.ToDictionary", inventoryRefresh, StringComparison.Ordinal);
    }

    [Fact]
    public void MutationFailures_RebuildAuthoritativePresentationBeforeReturningControl()
    {
        var root = FindRepositoryRoot();
        var source = Read(root, "src", "JunhyunHelper.Desktop", "MainWindow.Mutations.cs");

        Assert.Contains("RecoverMutationPresentationAsync", source, StringComparison.Ordinal);
        Assert.Equal(3, Count(source, "await RecoverMutationPresentationAsync("));
        Assert.Contains("await RefreshActiveWorkspacesAsync(detectCleanupChanges: false);", source, StringComparison.Ordinal);
        Assert.Contains("App.WriteDiagnostic(diagnosticContext, recoveryException);", source, StringComparison.Ordinal);
    }

    [Fact]
    public void ContentUpdates_ShareOneUiOperationGateAndRecheckSchemaAfterWaiting()
    {
        var root = FindRepositoryRoot();
        var schemaRefresh = Read(root, "src", "JunhyunHelper.Desktop", "MainWindow.ContentSchemaRefresh.cs");
        var manualUpdate = Read(root, "src", "JunhyunHelper.Desktop", "MainWindow.DataUpdate.cs");

        Assert.Contains("private readonly SemaphoreSlim _contentOperationGate = new(1, 1);", schemaRefresh, StringComparison.Ordinal);
        Assert.Contains("await _contentOperationGate.WaitAsync();", schemaRefresh, StringComparison.Ordinal);
        Assert.Contains("snapshot = await _services.Content.ReadActiveOrRecoverAsync(gameMode);", schemaRefresh, StringComparison.Ordinal);
        Assert.Contains("if (!ContentSnapshotStore.RequiresCurrentSchemaRefresh(snapshot))", schemaRefresh, StringComparison.Ordinal);
        Assert.Contains("_contentOperationGate.Release();", schemaRefresh, StringComparison.Ordinal);

        Assert.Contains("await _contentOperationGate.WaitAsync();", manualUpdate, StringComparison.Ordinal);
        Assert.Contains("_contentOperationGate.Release();", manualUpdate, StringComparison.Ordinal);
        Assert.Contains("ownsBusyState", manualUpdate, StringComparison.Ordinal);
    }

    private static int Count(string source, string value)
    {
        var count = 0;
        var offset = 0;
        while ((offset = source.IndexOf(value, offset, StringComparison.Ordinal)) >= 0)
        {
            count++;
            offset += value.Length;
        }

        return count;
    }

    private static string Read(string root, params string[] path) =>
        File.ReadAllText(Path.Combine([root, .. path]));

    private static string FindRepositoryRoot([CallerFilePath] string sourcePath = "")
    {
        var directory = new DirectoryInfo(Path.GetDirectoryName(sourcePath)
            ?? throw new InvalidOperationException("Test source path is unavailable."));

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "AGENTS.md")) &&
                Directory.Exists(Path.Combine(directory.FullName, "src")) &&
                Directory.Exists(Path.Combine(directory.FullName, "tests")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the JunhyunHelper repository root.");
    }
}
