using System.Runtime.CompilerServices;
using Xunit;

namespace JunhyunHelper.Tests.Maintenance;

public sealed class V1174MiniScannerNeededCountContractTests
{
    [Fact]
    public void MiniScanner_needed_count_preserves_fir_and_unrestricted_components()
    {
        var root = FindRepositoryRoot();
        var window = Read(root, "src", "JunhyunHelper.Desktop", "Scanner", "MiniScannerWindow.xaml.cs");
        var presentation = Read(root, "src", "JunhyunHelper.Desktop", "Scanner", "ScannerItemPresentationService.cs");
        var smoke = Read(root, "src", "JunhyunHelper.Desktop", "Scanner", "ScannerPage.MiniScannerSmoke.cs");
        var product = Read(root, "docs", "PRODUCT.md");

        Assert.Contains("FormatCurrentNeeded(snapshot)", window, StringComparison.Ordinal);
        Assert.Contains("var fir = Math.Max(0, snapshot.CurrentNeededFir);", window, StringComparison.Ordinal);
        Assert.Contains("var nonFir = Math.Max(0, snapshot.CurrentNeeded - fir);", window, StringComparison.Ordinal);
        Assert.Contains("(인레이드) +", window, StringComparison.Ordinal);
        Assert.DoesNotContain(
            "$\"필요  {snapshot.CurrentNeeded.ToString",
            window,
            StringComparison.Ordinal);

        Assert.Contains(
            "CurrentNeededFir = Math.Max(0, needed?.RemainingFir ?? 0)",
            presentation,
            StringComparison.Ordinal);

        Assert.Contains("필요  3(인레이드) + 4개", smoke, StringComparison.Ordinal);
        Assert.Contains("필요  0(인레이드) + 4개", smoke, StringComparison.Ordinal);
        Assert.Contains("필요  4(인레이드) + 0개", smoke, StringComparison.Ordinal);

        Assert.Contains(
            "<RemainingFir>(인레이드) + <RemainingUnrestricted>개",
            product,
            StringComparison.Ordinal);
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
