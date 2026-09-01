using System.Runtime.CompilerServices;
using Xunit;

namespace JunhyunHelper.Tests.Maintenance;

public sealed class V1111ScannerSearchFeedbackContractTests
{
    [Fact]
    public void Ammo_pickup_is_a_configurable_ordered_mini_scanner_field()
    {
        var root = FindRepositoryRoot();
        var settings = Read(root, "src", "JunhyunHelper.Desktop", "Scanner", "ScannerDisplaySettings.cs");
        var settingsWindow = Read(root, "src", "JunhyunHelper.Desktop", "Scanner", "ScannerSettingsWindow.xaml.cs");
        var miniScanner = Read(root, "src", "JunhyunHelper.Desktop", "Scanner", "MiniScannerWindow.xaml.cs");

        Assert.Contains("public const int CurrentSchemaVersion = 10;", settings, StringComparison.Ordinal);
        Assert.Contains("public const string AmmoPickupField = \"ammo_pickup\";", settings, StringComparison.Ordinal);
        Assert.Contains("public bool ShowAmmoPickup { get; set; } = true;", settings, StringComparison.Ordinal);
        Assert.Contains("if (SchemaVersion < 9)", settings, StringComparison.Ordinal);
        Assert.Contains("ShowAmmoPickup = true;", settings, StringComparison.Ordinal);
        Assert.Contains("AmmoPickupField => ShowAmmoPickup", settings, StringComparison.Ordinal);

        Assert.Contains("ScannerDisplaySettings.AmmoPickupField => \"탄약 줍기 판단\"", settingsWindow, StringComparison.Ordinal);
        Assert.Contains("settings.ShowAmmoPickup && snapshot.AmmoShouldPickUp.HasValue", miniScanner, StringComparison.Ordinal);
        Assert.Contains("[ScannerDisplaySettings.AmmoPickupField] = AmmoPickupText", miniScanner, StringComparison.Ordinal);
        Assert.DoesNotContain("InfoStackPanel.Children.Add(AmmoPickupText);", miniScanner, StringComparison.Ordinal);
    }

    [Fact]
    public void Items_and_hideout_share_the_quest_conditional_clear_button_contract()
    {
        var root = FindRepositoryRoot();
        var behavior = Read(root, "src", "JunhyunHelper.Desktop", "Controls", "ProductSearchClearButtonBehavior.cs");

        Assert.Contains("typeof(QuestPage)", behavior, StringComparison.Ordinal);
        Assert.Contains("typeof(HideoutPage)", behavior, StringComparison.Ordinal);
        Assert.Contains("typeof(ItemsPage)", behavior, StringComparison.Ordinal);
        Assert.Contains("Content = \"×\"", behavior, StringComparison.Ordinal);
        Assert.Contains("Visibility = string.IsNullOrEmpty(searchBox.Text) ? Visibility.Collapsed : Visibility.Visible", behavior, StringComparison.Ordinal);
        Assert.Contains("clearButton.Visibility = string.IsNullOrEmpty(searchBox.Text)", behavior, StringComparison.Ordinal);
        Assert.Contains("searchBox.Clear();", behavior, StringComparison.Ordinal);
        Assert.Contains("searchBox.Focus();", behavior, StringComparison.Ordinal);

        Assert.False(File.Exists(Path.Combine(root, "src", "JunhyunHelper.Desktop", "Controls", "SearchClearButtonInstaller.cs")));
        Assert.False(File.Exists(Path.Combine(root, "src", "JunhyunHelper.Desktop", "Items", "ItemsPage.SearchClear.cs")));
        Assert.False(File.Exists(Path.Combine(root, "src", "JunhyunHelper.Desktop", "Hideout", "HideoutPage.SearchClear.cs")));
    }

    [Fact]
    public void Correction_hotkey_success_is_confirmed_in_mini_scanner_without_changing_evidence_policy()
    {
        var root = FindRepositoryRoot();
        var capture = Read(root, "src", "JunhyunHelper.Desktop", "Scanner", "ScannerCoordinator.CorrectionCapture.cs");
        var policy = Read(root, "src", "JunhyunHelper.Desktop", "Scanner", "ScannerCorrectionCapturePolicy.cs");
        var overlay = Read(root, "src", "JunhyunHelper.Desktop", "Scanner", "MiniScannerOverlayService.cs");
        var windowXaml = Read(root, "src", "JunhyunHelper.Desktop", "Scanner", "MiniScannerWindow.xaml");
        var windowCode = Read(root, "src", "JunhyunHelper.Desktop", "Scanner", "MiniScannerWindow.xaml.cs");

        Assert.Contains("CorrectionSaveCompletedStatus = \"저장 완료\"", capture, StringComparison.Ordinal);
        Assert.Contains("_overlay.ShowTransientStatus(CorrectionSaveCompletedStatus);", capture, StringComparison.Ordinal);
        Assert.Contains("public void ShowTransientStatus(string message)", overlay, StringComparison.Ordinal);
        Assert.Contains("_snapshot is not null", overlay, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"TransientStatusBadge\"", windowXaml, StringComparison.Ordinal);
        Assert.Contains("TimeSpan.FromSeconds(2)", windowCode, StringComparison.Ordinal);

        Assert.DoesNotContain("ScannerDiagnosticCasesWindow", capture, StringComparison.Ordinal);
        Assert.DoesNotContain("ShowDialog()", capture, StringComparison.Ordinal);
        Assert.DoesNotContain("FocusScannerSectionAfterCorrectionCapture", capture, StringComparison.Ordinal);

        Assert.Contains("internal const string NoEvidenceStatus = \"저장할 스캔 결과가 없습니다.\";", policy, StringComparison.Ordinal);
        Assert.Contains("GroundTruthItemName: null", policy, StringComparison.Ordinal);
        Assert.Contains("UserConfirmed: false", policy, StringComparison.Ordinal);
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
