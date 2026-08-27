using System.Runtime.CompilerServices;
using Xunit;

namespace JunhyunHelper.Tests.Maintenance;

public sealed class V1714UiConsistencyContractTests
{
    [Fact]
    public void AmmoPopupLaunchers_CloseAlreadyOpenPopupBeforeClickReopensIt()
    {
        var root = FindRepositoryRoot();
        var source = Read(root, "src", "JunhyunHelper.Desktop", "Ammo", "AmmoPage.PopupToggleFixes.cs");

        Assert.Contains("OnPreviewMouseDown", source, StringComparison.Ordinal);
        Assert.Contains("FavoriteCaliberPopup.IsOpen = false;", source, StringComparison.Ordinal);
        Assert.Contains("ColumnMenuPopup.IsOpen = false;", source, StringComparison.Ordinal);
        Assert.Contains("e.Handled = true;", source, StringComparison.Ordinal);
    }

    [Fact]
    public void MainWindowOverlay_IsSharedOwnerForWindowAndElementSurfaces()
    {
        var root = FindRepositoryRoot();
        var source = Read(root, "src", "JunhyunHelper.Desktop", "MainWindow.InAppOverlay.cs");

        Assert.Contains("ToggleInAppWindowAsync", source, StringComparison.Ordinal);
        Assert.Contains("ShowInAppElementAsync", source, StringComparison.Ordinal);
        Assert.Contains("DismissInAppOverlay", source, StringComparison.Ordinal);
        Assert.Contains("backdrop.MouseLeftButtonDown", source, StringComparison.Ordinal);
        Assert.Contains("RequestActiveInAppOverlayDismiss", source, StringComparison.Ordinal);
    }

    [Fact]
    public void ScannerSettings_OwnDisplayAndHotkeyConfiguration()
    {
        var root = FindRepositoryRoot();
        var xaml = Read(root, "src", "JunhyunHelper.Desktop", "Scanner", "ScannerSettingsWindow.xaml");
        var code = Read(root, "src", "JunhyunHelper.Desktop", "Scanner", "ScannerSettingsWindow.xaml.cs");
        var page = Read(root, "src", "JunhyunHelper.Desktop", "Scanner", "ScannerPage.ProductUsability.cs");
        var scannerDirectory = Path.Combine(root, "src", "JunhyunHelper.Desktop", "Scanner");

        Assert.Contains("Scanner 단축키", xaml, StringComparison.Ordinal);
        Assert.Contains("OneShotTarkovText", xaml, StringComparison.Ordinal);
        Assert.Contains("OneShotTestText", xaml, StringComparison.Ordinal);
        Assert.Contains("ScannerToggleText", xaml, StringComparison.Ordinal);
        Assert.Contains("SetOneShotTarkovHotkey", code, StringComparison.Ordinal);
        Assert.Contains("SetOneShotTestHotkey", code, StringComparison.Ordinal);
        Assert.Contains("SetScannerToggleHotkey", code, StringComparison.Ordinal);
        Assert.Contains("ToggleInAppWindowAsync(\"scanner-settings\"", page, StringComparison.Ordinal);
        Assert.False(File.Exists(Path.Combine(scannerDirectory, "ScannerHotkeySettingsWindow.xaml")));
        Assert.False(File.Exists(Path.Combine(scannerDirectory, "ScannerHotkeySettingsWindow.xaml.cs")));
    }

    [Fact]
    public void ScannerAdvanced_UsesSharedOverlayWithoutContentCloseButton()
    {
        var root = FindRepositoryRoot();
        var xaml = Read(root, "src", "JunhyunHelper.Desktop", "Scanner", "ScannerAdvancedWindow.xaml");
        var code = Read(root, "src", "JunhyunHelper.Desktop", "Scanner", "ScannerAdvancedWindow.xaml.cs");
        var page = Read(root, "src", "JunhyunHelper.Desktop", "Scanner", "ScannerPage.ProductUsability.cs");

        Assert.DoesNotContain("AdvancedCloseButton", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Content=\"닫기\"", xaml, StringComparison.Ordinal);
        Assert.Contains("IInAppOverlayDialog", code, StringComparison.Ordinal);
        Assert.Contains("TryDismissInAppOverlay", code, StringComparison.Ordinal);
        Assert.Contains("ToggleInAppWindowAsync(\"scanner-advanced\"", page, StringComparison.Ordinal);
    }

    [Fact]
    public void MapLaunchers_UseCompactButtonChromeAndSharedSettingsOverlay()
    {
        var root = FindRepositoryRoot();
        var source = Read(root, "src", "JunhyunHelper.Desktop", "Map", "MapPage.JunhyunUiSimplification.cs");

        Assert.Contains("NormalizeMiniMapLauncherChrome", source, StringComparison.Ordinal);
        Assert.Contains("BtnMinimapHelp.Visibility = Visibility.Collapsed", source, StringComparison.Ordinal);
        Assert.Contains("container.Padding = new Thickness(0)", source, StringComparison.Ordinal);
        Assert.Contains("ApplyMapMarkerPanelChrome", source, StringComparison.Ordinal);
        Assert.Contains("MapMarkersOverlay.MinWidth = 0", source, StringComparison.Ordinal);
        Assert.Contains("MapMarkersOverlay.MinWidth = 220", source, StringComparison.Ordinal);
        Assert.Contains("AvailableMarkerPanelHeight", source, StringComparison.Ordinal);
        Assert.Contains("ShowInAppElementAsync", source, StringComparison.Ordinal);
        Assert.Contains("\"map-settings\"", source, StringComparison.Ordinal);
    }

    [Fact]
    public void ProfileEditor_UsesScannerStyleContentCardInsideSharedOverlay()
    {
        var root = FindRepositoryRoot();
        var source = Read(root, "src", "JunhyunHelper.Desktop", "Profiles", "ProfileEditorWindow.ProductOverlayStyle.cs");

        Assert.Contains("ApplyProductOverlayStyle", source, StringComparison.Ordinal);
        Assert.Contains("BackgroundMediumBrush", source, StringComparison.Ordinal);
        Assert.Contains("BorderBrush", source, StringComparison.Ordinal);
        Assert.Contains("CornerRadius = new CornerRadius(8)", source, StringComparison.Ordinal);
    }

    [Fact]
    public void ProductSearchBars_UseInFieldClearBehaviorAcrossPrimarySearchSurfaces()
    {
        var root = FindRepositoryRoot();
        var behavior = Read(root, "src", "JunhyunHelper.Desktop", "Controls", "ProductSearchClearButtonBehavior.cs");
        var ammo = Read(root, "src", "JunhyunHelper.Desktop", "Ammo", "AmmoPage.ProductSearchAndDetails.cs");
        var scanner = Read(root, "src", "JunhyunHelper.Desktop", "Scanner", "ScannerPage.ProductUsability.cs");

        Assert.Contains("typeof(QuestPage)", behavior, StringComparison.Ordinal);
        Assert.Contains("typeof(HideoutPage)", behavior, StringComparison.Ordinal);
        Assert.Contains("typeof(ItemsPage)", behavior, StringComparison.Ordinal);
        Assert.Contains("Content = \"×\"", behavior, StringComparison.Ordinal);
        Assert.Contains("HorizontalAlignment = HorizontalAlignment.Right", behavior, StringComparison.Ordinal);
        Assert.Contains("searchBox.Clear()", behavior, StringComparison.Ordinal);
        Assert.Contains("ProductSearchClearButtonBehavior.Attach(_productSearchBox)", ammo, StringComparison.Ordinal);
        Assert.Contains("ProductSearchClearButtonBehavior.Attach(ItemSearchBox)", scanner, StringComparison.Ordinal);
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
