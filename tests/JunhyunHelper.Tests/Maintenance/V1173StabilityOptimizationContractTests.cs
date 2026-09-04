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

    [Fact]
    public void ScannerSearch_ReusesCatalogSnapshotAndCanonicalContentIndexes()
    {
        var root = FindRepositoryRoot();
        var catalog = Read(root, "src", "JunhyunHelper.Infrastructure", "Scanner", "ScannerCatalogService.cs");
        var index = Read(root, "src", "JunhyunHelper.Desktop", "Scanner", "ScannerCoordinator.ContentPresentationIndex.cs");
        var search = Read(root, "src", "JunhyunHelper.Desktop", "Scanner", "ScannerCoordinator.Search.cs");
        var relationships = Read(root, "src", "JunhyunHelper.Desktop", "Scanner", "ScannerCoordinator.ItemRelationships.cs");

        Assert.Contains("_itemsSnapshot = Array.Empty<ScannerCatalogItem>()", catalog, StringComparison.Ordinal);
        Assert.Contains("return _itemsSnapshot;", catalog, StringComparison.Ordinal);
        Assert.Contains("_itemsSnapshot = snapshot;", catalog, StringComparison.Ordinal);

        Assert.Contains("GetContentPresentationIndex", index, StringComparison.Ordinal);
        Assert.Contains("ReferenceEquals(_indexedPresentationContent, content)", index, StringComparison.Ordinal);
        Assert.Contains("GetContentPresentationIndex(context.Content)", search, StringComparison.Ordinal);
        Assert.Contains("_catalog.TryGetItem(normalizedItemId, out var catalogItem)", search, StringComparison.Ordinal);
        Assert.DoesNotContain("CreateContentById", search, StringComparison.Ordinal);
        Assert.DoesNotContain("GetItemsSnapshot().FirstOrDefault", search, StringComparison.Ordinal);

        Assert.Contains("var contentIndex = GetContentPresentationIndex(context.Content);", relationships, StringComparison.Ordinal);
        Assert.DoesNotContain("context.Content.Items.FirstOrDefault", relationships, StringComparison.Ordinal);
        Assert.DoesNotContain("context.Content.Traders.FirstOrDefault", relationships, StringComparison.Ordinal);
        Assert.DoesNotContain("context.Content.HideoutStations.FirstOrDefault", relationships, StringComparison.Ordinal);
    }

    [Fact]
    public void SharedImageCache_DeduplicatesConcurrentDownloadsAndDecodedImages()
    {
        var root = FindRepositoryRoot();
        var source = Read(root, "src", "JunhyunHelper.Desktop", "Services", "ImageCacheService.cs");

        Assert.Contains("ConcurrentDictionary<string, SemaphoreSlim> _cachePathGates", source, StringComparison.Ordinal);
        Assert.Contains("ConcurrentDictionary<string, WeakReference<ImageSource>> _decodedImages", source, StringComparison.Ordinal);
        Assert.Contains("TryGetDecodedImage(path, out var memoryCached)", source, StringComparison.Ordinal);
        Assert.Contains("_cachePathGates.GetOrAdd(", source, StringComparison.Ordinal);
        Assert.Contains("await pathGate.WaitAsync(cancellationToken);", source, StringComparison.Ordinal);
        Assert.Contains("_cachePathGates.TryRemove(path, out _);", source, StringComparison.Ordinal);
        Assert.Contains("RememberDecodedImage(path, cached)", source, StringComparison.Ordinal);
    }

    [Fact]
    public void MapQuestV2_UsesChangeDrivenScaleAndContentIndexesInsteadOfPolling()
    {
        var root = FindRepositoryRoot();
        var source = Read(root, "src", "JunhyunHelper.Desktop", "Map", "LegacyMapQuestV2.cs");

        Assert.DoesNotContain("DispatcherTimer _scaleTimer", source, StringComparison.Ordinal);
        Assert.DoesNotContain("TimeSpan.FromMilliseconds(120)", source, StringComparison.Ordinal);
        Assert.Contains("_mapScale.Changed += MapScale_Changed;", source, StringComparison.Ordinal);
        Assert.Contains("_mapScale.Changed -= MapScale_Changed;", source, StringComparison.Ordinal);
        Assert.Contains("private readonly ScaleTransform _inverseMarkerScale", source, StringComparison.Ordinal);
        Assert.Contains("visual.RenderTransform = _inverseMarkerScale;", source, StringComparison.Ordinal);
        Assert.Contains("EnsureContentIndexes(content);", source, StringComparison.Ordinal);
        Assert.Equal(1, Count(source, "content.Maps.ToDictionary(map => map.Id, StringComparer.Ordinal)"));
        Assert.Equal(1, Count(source, "content.QuestObjectives"));
    }

    [Fact]
    public void MapSchemaRefresh_SharesTheContentOperationGate()
    {
        var root = FindRepositoryRoot();
        var source = Read(root, "src", "JunhyunHelper.Desktop", "MainWindow.LegacyMapHost.cs");

        Assert.Contains("await _contentOperationGate.WaitAsync();", source, StringComparison.Ordinal);
        Assert.Contains("snapshot = await _services.Content.ReadActiveOrRecoverAsync", source, StringComparison.Ordinal);
        Assert.Contains("_contentOperationGate.Release();", source, StringComparison.Ordinal);
        Assert.Contains("TargetIsStillCurrent()", source, StringComparison.Ordinal);
        Assert.Contains("App.WriteDiagnostic(\"Map-triggered content schema refresh failed\"", source, StringComparison.Ordinal);
    }

    [Fact]
    public void SharedTheme_ShowsKeyboardFocusOnButtons()
    {
        var root = FindRepositoryRoot();
        var source = Read(root, "src", "JunhyunHelper.Desktop", "Themes", "DarkControls.xaml");

        Assert.Contains("<Trigger Property=\"IsKeyboardFocused\" Value=\"True\">", source, StringComparison.Ordinal);
        Assert.Contains("<Setter TargetName=\"ButtonBorder\" Property=\"BorderBrush\" Value=\"{StaticResource AccentBrush}\" />", source, StringComparison.Ordinal);
    }

    [Fact]
    public void ProgramUpdater_CancelsStartupAndPreparationWorkOnApplicationShutdown()
    {
        var root = FindRepositoryRoot();
        var source = Read(root, "src", "JunhyunHelper.Desktop", "Updates", "ProgramUpdateCoordinator.cs");

        Assert.Contains("private readonly CancellationTokenSource _lifetimeCts = new();", source, StringComparison.Ordinal);
        Assert.Contains("GetCurrentProductVersion(),", source, StringComparison.Ordinal);
        Assert.Contains("_lifetimeCts.Token);", source, StringComparison.Ordinal);
        Assert.Contains("catch (OperationCanceledException) when (_lifetimeCts.IsCancellationRequested)", source, StringComparison.Ordinal);
        Assert.Contains("progress,", source, StringComparison.Ordinal);
        Assert.Contains("_lifetimeCts.Token.ThrowIfCancellationRequested();", source, StringComparison.Ordinal);
        Assert.Contains("_lifetimeCts.Cancel();", source, StringComparison.Ordinal);
        Assert.Contains("_client.Dispose();", source, StringComparison.Ordinal);
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
