using System.Runtime.CompilerServices;
using Xunit;

namespace JunhyunHelper.Tests.Maintenance;

public sealed class V1162FarmingGuideRegressionContractTests
{
    [Fact]
    public void RaidValueSummary_UsesNetAcquiredAverageFleaValueAndRefreshesWithRaidUi()
    {
        var directory = FarmingGuideDirectory();
        var raid = File.ReadAllText(Path.Combine(directory, "FarmingGuidePage.Raid.cs"));

        Assert.Contains("FarmingGuideRaidValuePolicy.CalculateAcquiredFleaValue", raid, StringComparison.Ordinal);
        Assert.Contains("_raidSession.BaselineSnapshot", raid, StringComparison.Ordinal);
        Assert.Contains("BuildSnapshot()", raid, StringComparison.Ordinal);
        Assert.Contains("FleaAveragePrice", raid, StringComparison.Ordinal);
        Assert.Contains("ValueSummaryText.Text = active ? FormatRaidValue() : \"—\";", raid, StringComparison.Ordinal);
        Assert.DoesNotContain("BasePrice", raid, StringComparison.Ordinal);
    }

    [Fact]
    public void ReservedCellMarker_IsBehindPlacedItemAndCoveredByPublishedProductSmoke()
    {
        var directory = FarmingGuideDirectory();
        var locks = File.ReadAllText(Path.Combine(directory, "FarmingGuidePage.Locks.cs"));
        var smoke = File.ReadAllText(Path.Combine(directory, "FarmingGuidePage.V1162RegressionSmoke.cs"));
        var storageSmoke = File.ReadAllText(Path.Combine(directory, "FarmingGuidePage.StorageLayoutSmoke.cs"));

        Assert.Contains("ReservedCellOverlayZIndex = -1", locks, StringComparison.Ordinal);
        Assert.Contains("Panel.SetZIndex(overlay, ReservedCellOverlayZIndex);", locks, StringComparison.Ordinal);
        Assert.Contains("Panel.GetZIndex(overlay) >= Panel.GetZIndex(card)", smoke, StringComparison.Ordinal);
        Assert.Contains("VerifyV1162RaidValueAndReservedCellSmoke();", storageSmoke, StringComparison.Ordinal);
    }

    private static string FarmingGuideDirectory([CallerFilePath] string sourcePath = "")
    {
        var directory = new DirectoryInfo(Path.GetDirectoryName(sourcePath)
            ?? throw new InvalidOperationException("Test source path is unavailable."));
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
            {
                return Path.Combine(
                    directory.FullName,
                    "src",
                    "JunhyunHelper.Desktop",
                    "FarmingGuide");
            }
            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Repository root was not found.");
    }
}
