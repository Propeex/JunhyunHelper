using System.Runtime.CompilerServices;
using Xunit;

namespace JunhyunHelper.Tests.Maintenance;

public sealed class V1170GlobalFarmingGuideContractTests
{
    [Fact]
    public void LiveRaidRouteUsesGlobalStateDiffPresentationNotLegacyLocalPresentation()
    {
        var directory = FarmingGuideDirectory();
        var raid = File.ReadAllText(Path.Combine(directory, "FarmingGuidePage.Raid.cs"));
        var presentation = File.ReadAllText(Path.Combine(directory, "FarmingGuidePage.RaidInstructionV1170.cs"));

        Assert.Contains("ApplyRaidInstructionPresentationV1170", raid, StringComparison.Ordinal);
        Assert.DoesNotContain("ApplyRaidInstructionPresentationV1155(current, weightChecked", raid, StringComparison.Ordinal);
        Assert.Contains("TryBuildCurrentOwnedRootsV1170", presentation, StringComparison.Ordinal);
        Assert.Contains("FindProposedRootLocationV1170", presentation, StringComparison.Ordinal);
        Assert.Contains("ReferenceEquals", presentation, StringComparison.Ordinal);
    }

    [Fact]
    public void CurrencyUsesCanonicalRoubleDenominationWithoutGeneralBasePriceFallback()
    {
        var directory = FarmingGuideDirectory();
        var economics = File.ReadAllText(Path.Combine(directory, "FarmingGuidePage.EconomicValueV1170.cs"));
        var global = File.ReadAllText(Path.Combine(directory, "FarmingGuidePage.GlobalOptimizationV1170.cs"));
        var raid = File.ReadAllText(Path.Combine(directory, "FarmingGuidePage.Raid.cs"));

        Assert.Contains("FarmingGuideStackQuantityPolicy.IsCurrency(item)", economics, StringComparison.Ordinal);
        Assert.Contains("item.BasePrice is > 0", economics, StringComparison.Ordinal);
        Assert.Contains("ResolveUnitEconomicValueV1170", global, StringComparison.Ordinal);
        Assert.Contains("RememberCanonicalCurrencyValuesV1170", raid, StringComparison.Ordinal);
        Assert.Contains("ResolveUnitEconomicValueV1170", raid, StringComparison.Ordinal);
    }

    [Fact]
    public void FirDecisionUsesObservedProvenanceAndFailsClosedOnlyWhenNeededStatusIsUnknown()
    {
        var directory = FarmingGuideDirectory();
        var global = File.ReadAllText(Path.Combine(directory, "FarmingGuidePage.GlobalOptimizationV1170.cs"));
        var quantities = File.ReadAllText(Path.Combine(directory, "FarmingGuidePage.UnifiedQuantityOptimizationV1170.cs"));

        Assert.Contains("HasProvableFirDecisionFactsV1170", global, StringComparison.Ordinal);
        Assert.Contains("scanned.CurrentNeededFir", global, StringComparison.Ordinal);
        Assert.Contains("scanned.FirStatus == FarmingGuideFirStatus.Unknown", global, StringComparison.Ordinal);
        Assert.Contains("state.FirStatus == FarmingGuideFirStatus.Unknown", global, StringComparison.Ordinal);
        Assert.Contains("firStatus: scanned.FirStatus", quantities, StringComparison.Ordinal);
        Assert.Contains("root.State.IsFirQualified", quantities, StringComparison.Ordinal);
        Assert.Contains("currentScan.CurrentNeededFir", quantities, StringComparison.Ordinal);
        Assert.DoesNotContain("TryFindBestUnifiedRaidStateV1170(", quantities, StringComparison.Ordinal);
        Assert.DoesNotContain("Math.Max(0, currentScan.CurrentNeeded)", global, StringComparison.Ordinal);
        Assert.DoesNotContain("Math.Max(0, currentScan.CurrentNeeded)", quantities, StringComparison.Ordinal);
    }

    [Fact]
    public void ScannerFirObservationIsPositiveOnlyAndRestrictedToCurrentLiveTarkovItem()
    {
        var scanner = ScannerDirectory();
        var models = File.ReadAllText(Path.Combine(scanner, "ScannerModels.cs"));
        var coordinator = File.ReadAllText(Path.Combine(scanner, "ScannerCoordinator.FarmingGuide.cs"));
        var runtime = File.ReadAllText(Path.Combine(scanner, "ScannerRuntimeService.FirObservationV1170.cs"));
        var vision = File.ReadAllText(Path.Combine(scanner, "ScannerLab38WindowsVision.cs"));

        Assert.Contains("FarmingGuideFirStatus FirStatus", models, StringComparison.Ordinal);
        Assert.Contains("HasFoundInRaidMarkerEvidence", models, StringComparison.Ordinal);
        Assert.Contains("Runtime.CreateLiveFarmingGuideSnapshot", coordinator, StringComparison.Ordinal);
        Assert.Contains("Presentation.CreateSnapshot", coordinator, StringComparison.Ordinal);
        Assert.Contains("Status.CaptureMode != ScannerCaptureMode.TarkovWindow", runtime, StringComparison.Ordinal);
        Assert.Contains("FarmingGuideFirStatus.FoundInRaid", runtime, StringComparison.Ordinal);
        Assert.Contains("FarmingGuideFirStatus.Unknown", runtime, StringComparison.Ordinal);
        Assert.DoesNotContain("FarmingGuideFirStatus.NotFoundInRaid", runtime, StringComparison.Ordinal);
        Assert.Contains("ScannerFirMarkerDetector.HasFoundInRaidMarker", vision, StringComparison.Ordinal);
    }

    private static string FarmingGuideDirectory([CallerFilePath] string sourcePath = "") =>
        Path.Combine(RepositoryRoot(sourcePath), "src", "JunhyunHelper.Desktop", "FarmingGuide");

    private static string ScannerDirectory([CallerFilePath] string sourcePath = "") =>
        Path.Combine(RepositoryRoot(sourcePath), "src", "JunhyunHelper.Desktop", "Scanner");

    private static string RepositoryRoot(string sourcePath)
    {
        var directory = new DirectoryInfo(Path.GetDirectoryName(sourcePath)
            ?? throw new InvalidOperationException("Test source path is unavailable."));
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
                return directory.FullName;
            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Repository root was not found.");
    }
}
