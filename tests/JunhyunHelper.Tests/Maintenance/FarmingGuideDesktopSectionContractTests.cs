using System.Runtime.CompilerServices;
using Xunit;

namespace JunhyunHelper.Tests.Maintenance;

public sealed class FarmingGuideDesktopSectionContractTests
{
    [Fact]
    public void FarmingGuide_IsOwnedByMainSectionNavigationAndBusyLifecycle()
    {
        var root = FindRepositoryRoot();
        var mainWindow = File.ReadAllText(Path.Combine(
            root,
            "src",
            "JunhyunHelper.Desktop",
            "MainWindow.xaml.cs"));
        var farmingGuide = File.ReadAllText(Path.Combine(
            root,
            "src",
            "JunhyunHelper.Desktop",
            "MainWindow.FarmingGuide.cs"));
        var xaml = File.ReadAllText(Path.Combine(
            root,
            "src",
            "JunhyunHelper.Desktop",
            "MainWindow.xaml"));

        Assert.Contains("FarmingGuide,", mainWindow, StringComparison.Ordinal);
        Assert.Contains(
            "FarmingGuidePage.Visibility = _activeSection == DesktopSection.FarmingGuide ? Visibility.Visible : Visibility.Collapsed;",
            mainWindow,
            StringComparison.Ordinal);
        Assert.Contains("FarmingGuidePage.SetBusy(busy);", mainWindow, StringComparison.Ordinal);
        Assert.Contains(
            "FarmingGuideTabButton.IsEnabled = !busy && _activeProfile is not null && _activeSection != DesktopSection.FarmingGuide;",
            mainWindow,
            StringComparison.Ordinal);
        Assert.Contains(
            "_activeSection = DesktopSection.FarmingGuide;",
            farmingGuide,
            StringComparison.Ordinal);
        Assert.Contains(
            "<farming:FarmingGuidePage x:Name=\"FarmingGuidePage\" Visibility=\"Collapsed\" />",
            xaml,
            StringComparison.Ordinal);

        Assert.DoesNotContain("QuestTabButton.Click +=", farmingGuide, StringComparison.Ordinal);
        Assert.DoesNotContain("ProfileComboBox.SelectionChanged +=", farmingGuide, StringComparison.Ordinal);
    }

    [Fact]
    public void FarmingGuide_DoubleClickExposesSourceBackedNestedStorageWithoutRestoringEquipmentInternals()
    {
        var root = FindRepositoryRoot();
        var directory = Path.Combine(
            root,
            "src",
            "JunhyunHelper.Desktop",
            "FarmingGuide");
        var xaml = File.ReadAllText(Path.Combine(directory, "FarmingGuidePage.xaml"));
        var workbench = File.ReadAllText(Path.Combine(directory, "FarmingGuidePage.Workbench.cs"));
        var drag = File.ReadAllText(Path.Combine(directory, "FarmingGuidePage.Drag.cs"));
        var page = File.ReadAllText(Path.Combine(directory, "FarmingGuidePage.xaml.cs"));
        var images = File.ReadAllText(Path.Combine(directory, "FarmingGuidePage.Images.cs"));
        var locks = File.ReadAllText(Path.Combine(directory, "FarmingGuidePage.Locks.cs"));
        var rendering = File.ReadAllText(Path.Combine(directory, "FarmingGuidePage.Rendering.cs"));

        Assert.Contains("x:Name=\"WorkbenchHost\"", xaml, StringComparison.Ordinal);
        Assert.Contains("FarmingGuideCompleteEquipmentPolicy.SupportsNestedStorage(item)", workbench, StringComparison.Ordinal);
        Assert.Contains("OpenStoredWorkbench", workbench, StringComparison.Ordinal);
        Assert.Contains("OpenEquipmentWorkbench", workbench, StringComparison.Ordinal);
        Assert.Contains("opaque complete item", workbench, StringComparison.Ordinal);
        Assert.Contains("Root rig/backpack/secure-container storage is already visible", workbench, StringComparison.Ordinal);
        Assert.Contains("SizeWorkbenchToGrid", workbench, StringComparison.Ordinal);
        Assert.DoesNotContain("StoragePanel.Visibility = Visibility.Collapsed", workbench, StringComparison.Ordinal);
        Assert.DoesNotContain("CreateWorkbenchSlot(", workbench, StringComparison.Ordinal);
        Assert.DoesNotContain("CreateCompatiblePicker(", workbench, StringComparison.Ordinal);
        Assert.Contains("ParentInstanceId", drag, StringComparison.Ordinal);
        Assert.Contains("FarmingGuideSearchPolicy.IsDraggableInventoryItem", page, StringComparison.Ordinal);
        Assert.Contains("FarmingGuideCompleteEquipmentPolicy.ToRuntimeItem", page, StringComparison.Ordinal);
        Assert.DoesNotContain("EnumerateStates(state)", images, StringComparison.Ordinal);
        Assert.Contains("ApplyUnlockedBorder(border)", locks, StringComparison.Ordinal);
        Assert.Contains("PublishSimulatedScanAsync(row.Item.Id)", locks, StringComparison.Ordinal);
        Assert.DoesNotContain(
            "BorderBrush = (Brush)FindResource(\"AccentBrush\"),\r\n                Background = (Brush)FindResource(\"BackgroundMediumBrush\"),\r\n                Cursor = Cursors.Hand,\r\n                Tag = new PlacedItemSource",
            rendering,
            StringComparison.Ordinal);
        Assert.False(File.Exists(Path.Combine(directory, "FarmingGuideItemConfigurationWindow.cs")));
    }

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
