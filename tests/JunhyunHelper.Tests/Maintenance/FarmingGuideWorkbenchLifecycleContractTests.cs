using System.Runtime.CompilerServices;
using Xunit;

namespace JunhyunHelper.Tests.Maintenance;

public sealed class FarmingGuideWorkbenchLifecycleContractTests
{
    [Fact]
    public void EquipmentRearrangementClosesOpenWorkbenchBeforeDragBegins()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root,
            "src",
            "JunhyunHelper.Desktop",
            "FarmingGuide",
            "FarmingGuidePage.WorkbenchLifecycle.cs"));

        Assert.Contains("OnPreviewMouseLeftButtonDown", source, StringComparison.Ordinal);
        Assert.Contains("IsWorkbenchOpen", source, StringComparison.Ordinal);
        Assert.Contains("Tag: EquipmentDropTarget", source, StringComparison.Ordinal);
        Assert.Contains("CloseWorkbench();", source, StringComparison.Ordinal);
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
