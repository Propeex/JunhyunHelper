using System.Windows;
using TarkovHelper.Services;

namespace JunhyunHelper.Desktop;

public partial class MainWindow
{
    protected override void OnInitialized(EventArgs e)
    {
        base.OnInitialized(e);

        // Product rule: closing the main JunhyunHelper window closes the process even
        // when the always-on-top MiniMap window exists.
        if (System.Windows.Application.Current is not null)
            System.Windows.Application.Current.ShutdownMode = ShutdownMode.OnMainWindowClose;

        // Page-level infrastructure belongs to the product window lifetime, not to
        // whichever tab happens to raise Loaded first. Bind every page once during
        // initialization so cold-start behavior is independent of visibility/order.
        QuestPage.SetImageCache(_services.Images);
        HideoutPage.SetImageCache(_services.Images);
        ItemsPage.SetImageCache(_services.Images);
        AmmoPage.SetImageCache(_services.Images);
        AmmoPage.SetFavoriteStore(_services.AmmoFavorites);
        AttachContentNavigation();

        // Scanner global commands belong to the product window lifetime, not the
        // Scanner tab lifetime. WindowInteropHandle may not exist yet here; the hotkey
        // service listens for SourceInitialized and registers as soon as the HWND is
        // available. This makes the shortcuts usable from any product section.
        ScannerCoordinator.AttachContextProvider(GetScannerDataContext);
        ScannerCoordinator.AttachHotkeyHost(this);

        // Replace the original full-refresh mutation handlers with dependency-aware
        // product handlers. This keeps the existing UI events while avoiding duplicate
        // DB reads/workspace rebuilds after each Quest/Hideout change.
        EnableFastMutationHandlers();
    }

    protected override void OnClosed(EventArgs e)
    {
        // Dispose product-owned hooks/timers before WPF tears down remaining windows.
        try { _legacyMapProductRuntime?.Dispose(); } catch { }
        try { _legacyAdditionalMapMarkers?.Dispose(); } catch { }
        try { _legacyMapQuestV2?.Dispose(); } catch { }
        try { (_legacyMapProductAdapter as IDisposable)?.Dispose(); } catch { }

        try { OverlayMiniMapService.Instance.HideOverlay(); } catch { }
        try
        {
            GlobalKeyboardHookService.Instance.IsEnabled = false;
            GlobalKeyboardHookService.Instance.Dispose();
        }
        catch
        {
        }

        base.OnClosed(e);

        // ShutdownMode handles the normal case. Explicit Shutdown is a final WPF-level
        // guarantee against a hidden auxiliary window keeping the message pump alive.
        if (System.Windows.Application.Current is { } app && !app.Dispatcher.HasShutdownStarted)
            app.Shutdown();
    }
}
