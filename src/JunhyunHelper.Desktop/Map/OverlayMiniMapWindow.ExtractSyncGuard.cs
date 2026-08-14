using System.Windows;

namespace TarkovHelper.Windows;

/// <summary>
/// Detects the legacy MiniMap marker loader clearing the extract container after the
/// JunhyunHelper product renderer has already synchronized it. The product pulse owns
/// the actual re-render; this guard only invalidates its cached signature on a real
/// child-count transition, so an empty legacy refresh cannot be mistaken for a valid
/// synchronized empty state.
/// </summary>
public partial class OverlayMiniMapWindow
{
    private static readonly bool JunhyunExtractSyncGuardRegistered = RegisterJunhyunExtractSyncGuard();

    private bool _junhyunExtractSyncGuardInitialized;
    private int _junhyunObservedExtractChildCount = -1;

    private static bool RegisterJunhyunExtractSyncGuard()
    {
        EventManager.RegisterClassHandler(
            typeof(OverlayMiniMapWindow),
            FrameworkElement.LoadedEvent,
            new RoutedEventHandler(OnJunhyunExtractSyncGuardLoaded));
        return true;
    }

    private static void OnJunhyunExtractSyncGuardLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is OverlayMiniMapWindow window)
            window.InitializeJunhyunExtractSyncGuard();
    }

    private void InitializeJunhyunExtractSyncGuard()
    {
        if (_junhyunExtractSyncGuardInitialized)
            return;

        _junhyunExtractSyncGuardInitialized = true;
        _junhyunObservedExtractChildCount = ExtractMarkersContainer.Children.Count;
        ExtractMarkersContainer.LayoutUpdated += JunhyunExtractContainer_LayoutUpdated;
        Closed += JunhyunExtractSyncGuard_Closed;
    }

    private void JunhyunExtractContainer_LayoutUpdated(object? sender, EventArgs e)
    {
        var count = ExtractMarkersContainer.Children.Count;
        if (count == _junhyunObservedExtractChildCount)
            return;

        _junhyunObservedExtractChildCount = count;
        _junhyunLastExtractSignature = -1;
    }

    private void JunhyunExtractSyncGuard_Closed(object? sender, EventArgs e)
    {
        Closed -= JunhyunExtractSyncGuard_Closed;
        ExtractMarkersContainer.LayoutUpdated -= JunhyunExtractContainer_LayoutUpdated;
        _junhyunExtractSyncGuardInitialized = false;
        _junhyunObservedExtractChildCount = -1;
    }
}
