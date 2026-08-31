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
