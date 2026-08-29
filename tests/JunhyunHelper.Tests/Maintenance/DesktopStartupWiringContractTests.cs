using System.Runtime.CompilerServices;
using Xunit;

namespace JunhyunHelper.Tests.Maintenance;

public sealed class DesktopStartupWiringContractTests
{
    [Fact]
    public void PageInfrastructure_IsOwnedExplicitly_NotByIncidentalPageLoadedHandlers()
    {
        var root = FindRepositoryRoot();
        var lifecycle = File.ReadAllText(Path.Combine(
            root,
            "src",
            "JunhyunHelper.Desktop",
            "MainWindow.ProductLifecycle.cs"));
        var images = File.ReadAllText(Path.Combine(
            root,
            "src",
            "JunhyunHelper.Desktop",
            "MainWindow.Images.cs"));
        var xaml = File.ReadAllText(Path.Combine(
            root,
            "src",
            "JunhyunHelper.Desktop",
            "MainWindow.xaml"));
        var ammoInitialization = File.ReadAllText(Path.Combine(
            root,
            "src",
            "JunhyunHelper.Desktop",
            "Ammo",
            "AmmoPage.ProductGridFixes.cs"));
        var ammoPresentation = File.ReadAllText(Path.Combine(
            root,
            "src",
            "JunhyunHelper.Desktop",
            "Ammo",
            "AmmoPage.ProductSearchAndDetails.cs"));
        var headerPresentation = File.ReadAllText(Path.Combine(
            root,
            "src",
            "JunhyunHelper.Desktop",
            "MainWindow.HeaderStatusPolish.cs"));

        Assert.Contains("QuestPage.SetImageCache(_services.Images);", lifecycle, StringComparison.Ordinal);
        Assert.Contains("HideoutPage.SetImageCache(_services.Images);", lifecycle, StringComparison.Ordinal);
        Assert.Contains("ItemsPage.SetImageCache(_services.Images);", lifecycle, StringComparison.Ordinal);
        Assert.Contains("AmmoPage.SetImageCache(_services.Images);", lifecycle, StringComparison.Ordinal);
        Assert.Contains("AmmoPage.SetFavoriteStore(_services.AmmoFavorites);", lifecycle, StringComparison.Ordinal);
        Assert.Contains("AttachContentNavigation();", lifecycle, StringComparison.Ordinal);
        Assert.Contains("ScheduleHeaderStatusPolish();", lifecycle, StringComparison.Ordinal);
        Assert.Contains("DetachHeaderStatusPolish();", lifecycle, StringComparison.Ordinal);

        Assert.DoesNotContain("Loaded=\"ItemsPage_Loaded\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Loaded=\"HideoutPage_Loaded\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Loaded=\"AmmoPage_Loaded\"", xaml, StringComparison.Ordinal);

        Assert.DoesNotContain("ItemsPage_Loaded", images, StringComparison.Ordinal);
        Assert.DoesNotContain("HideoutPage_Loaded", images, StringComparison.Ordinal);
        Assert.DoesNotContain("AmmoPage_Loaded", images, StringComparison.Ordinal);

        Assert.Contains(
            "Dispatcher.BeginInvoke(InitializeProductSearchAndDetails, DispatcherPriority.Loaded);",
            ammoInitialization,
            StringComparison.Ordinal);
        Assert.DoesNotContain("RegisterClassHandler", ammoPresentation, StringComparison.Ordinal);
        Assert.DoesNotContain("ProductLoaded", ammoPresentation, StringComparison.Ordinal);

        Assert.Contains(
            "Dispatcher.BeginInvoke(ApplyHeaderStatusPolish, DispatcherPriority.Loaded);",
            headerPresentation,
            StringComparison.Ordinal);
        Assert.Contains(
            "_statusTextDescriptor.RemoveValueChanged(StatusText, StatusText_ValueChanged);",
            headerPresentation,
            StringComparison.Ordinal);
        Assert.DoesNotContain("RegisterClassHandler", headerPresentation, StringComparison.Ordinal);
        Assert.DoesNotContain("HeaderStatusPolishHandlerRegistered", headerPresentation, StringComparison.Ordinal);
    }

    [Fact]
    public void ProductLifetime_DisposesOwnedLongLivedServices()
    {
        var root = FindRepositoryRoot();
        var app = File.ReadAllText(Path.Combine(
            root,
            "src",
            "JunhyunHelper.Desktop",
            "App.xaml.cs"));
        var lifecycle = File.ReadAllText(Path.Combine(
            root,
            "src",
            "JunhyunHelper.Desktop",
            "MainWindow.ProductLifecycle.cs"));
        var mainWindow = File.ReadAllText(Path.Combine(
            root,
            "src",
            "JunhyunHelper.Desktop",
            "MainWindow.xaml.cs"));
        var xaml = File.ReadAllText(Path.Combine(
            root,
            "src",
            "JunhyunHelper.Desktop",
            "MainWindow.xaml"));
        var desktopServices = File.ReadAllText(Path.Combine(
            root,
            "src",
            "JunhyunHelper.Desktop",
            "Services",
            "DesktopServices.cs"));
        var scannerCoordinator = File.ReadAllText(Path.Combine(
            root,
            "src",
            "JunhyunHelper.Desktop",
            "Scanner",
            "ScannerCoordinator.cs"));
        var retentionService = File.ReadAllText(Path.Combine(
            root,
            "src",
            "JunhyunHelper.Desktop",
            "Scanner",
            "ScannerDiagnosticRetentionService.cs"));

        // MainWindow owns DesktopServices for exactly the product-window lifetime.
        Assert.Contains("Closed=\"Window_Closed\"", xaml, StringComparison.Ordinal);
        Assert.Contains("_services.Dispose();", mainWindow, StringComparison.Ordinal);
        Assert.Contains("base.OnClosed(e);", lifecycle, StringComparison.Ordinal);

        // DesktopServices owns Scanner and the shared network client.
        Assert.Contains("Scanner.Dispose();", desktopServices, StringComparison.Ordinal);
        Assert.Contains("_httpClient.Dispose();", desktopServices, StringComparison.Ordinal);

        // Scanner owns its monitor/hotkey/runtime/OCR/overlay/catalog resources.
        Assert.Contains("StopContextMonitor();", scannerCoordinator, StringComparison.Ordinal);
        Assert.Contains("_hotkeyService.Dispose();", scannerCoordinator, StringComparison.Ordinal);
        Assert.Contains("_runtime.StatusChanged -= OnRuntimeStatusChanged;", scannerCoordinator, StringComparison.Ordinal);
        Assert.Contains("_runtime.Dispose();", scannerCoordinator, StringComparison.Ordinal);
        Assert.Contains("disposableOcr.Dispose();", scannerCoordinator, StringComparison.Ordinal);
        Assert.Contains("_overlay.Dispose();", scannerCoordinator, StringComparison.Ordinal);
        Assert.Contains("_catalog.Dispose();", scannerCoordinator, StringComparison.Ordinal);

        // App-level services outlive MainWindow but are still released during application exit.
        Assert.Contains("_scannerDiagnosticRetentionService?.Dispose();", app, StringComparison.Ordinal);
        Assert.Contains("_programUpdateCoordinator?.Dispose();", app, StringComparison.Ordinal);
        Assert.Contains("_timer.Dispose();", retentionService, StringComparison.Ordinal);
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
