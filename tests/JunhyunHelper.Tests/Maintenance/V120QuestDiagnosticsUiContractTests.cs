using System.Runtime.CompilerServices;
using Xunit;

namespace JunhyunHelper.Tests.Maintenance;

public sealed class V120QuestDiagnosticsUiContractTests
{
    [Fact]
    public void Shared_search_clear_tracks_the_textbox_vertical_margin()
    {
        var root = FindRepositoryRoot();
        var behavior = Read(root, "src", "JunhyunHelper.Desktop", "Controls", "ProductSearchClearButtonBehavior.cs");

        Assert.Contains("searchBox.Margin.Top", behavior, StringComparison.Ordinal);
        Assert.Contains("searchBox.Margin.Bottom", behavior, StringComparison.Ordinal);
        Assert.Contains("Math.Max(4, searchBox.Margin.Right + 4)", behavior, StringComparison.Ordinal);
    }

    [Fact]
    public void Header_avatar_is_the_explicit_kim_taeyoung_diagnostic_entry_point()
    {
        var root = FindRepositoryRoot();
        var xaml = Read(root, "src", "JunhyunHelper.Desktop", "MainWindow.xaml");
        var workflow = Read(root, "src", "JunhyunHelper.Desktop", "MainWindow.KimTaeyoungDiagnostic.cs");

        Assert.Contains("ToolTip=\"김태영 PC 진단\"", xaml, StringComparison.Ordinal);
        Assert.Contains("MouseLeftButtonUp=\"AppIcon_MouseLeftButtonUp\"", xaml, StringComparison.Ordinal);
        Assert.Contains("김태영 본인이 맞습니까?", workflow, StringComparison.Ordinal);
        Assert.Contains("KimTaeyoungPcDiagnosticExporter.ExportAsync", workflow, StringComparison.Ordinal);
        Assert.Contains("hyune4784@naver.com", workflow, StringComparison.Ordinal);
    }

    [Fact]
    public void Kim_taeyoung_bundle_is_local_broad_and_privacy_bounded()
    {
        var root = FindRepositoryRoot();
        var exporter = Read(root, "src", "JunhyunHelper.Desktop", "Scanner", "KimTaeyoungPcDiagnosticExporter.cs");

        Assert.Contains("DesktopDirectory", exporter, StringComparison.Ordinal);
        Assert.Contains("scanner-support.zip", exporter, StringComparison.Ordinal);
        Assert.Contains("display-gpu-powershell.json", exporter, StringComparison.Ordinal);
        Assert.Contains("dxdiag-display.txt", exporter, StringComparison.Ordinal);
        Assert.Contains("CaptureDisplayEvidence", exporter, StringComparison.Ordinal);
        Assert.Contains("CaptureTarkovEvidence", exporter, StringComparison.Ordinal);
        Assert.Contains("AnalyzeBitmap", exporter, StringComparison.Ordinal);
        Assert.Contains("HDR Support:", exporter, StringComparison.Ordinal);
        Assert.Contains("Discord", exporter, StringComparison.Ordinal);
        Assert.Contains("obs64", exporter, StringComparison.Ordinal);

        Assert.DoesNotContain("Environment.UserName", exporter, StringComparison.Ordinal);
        Assert.DoesNotContain("Environment.MachineName", exporter, StringComparison.Ordinal);
        Assert.DoesNotContain("GetEnvironmentVariables", exporter, StringComparison.Ordinal);
        Assert.DoesNotContain("GetProcesses()", exporter, StringComparison.Ordinal);
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
