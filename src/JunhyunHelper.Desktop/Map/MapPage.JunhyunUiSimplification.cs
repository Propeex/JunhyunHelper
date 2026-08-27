using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace TarkovHelper.Pages.Map;

public partial class MapPage
{
    private static readonly bool JunhyunUiSimplificationHandlerRegistered = RegisterJunhyunUiSimplificationHandler();
    private bool _junhyunUiSimplificationApplied;

    private static bool RegisterJunhyunUiSimplificationHandler()
    {
        EventManager.RegisterClassHandler(
            typeof(MapPage),
            FrameworkElement.LoadedEvent,
            new RoutedEventHandler(OnJunhyunUiSimplificationLoaded));
        return true;
    }

    private static void OnJunhyunUiSimplificationLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is not MapPage page)
            return;

        page.Dispatcher.BeginInvoke(page.ApplyJunhyunUiSimplification, DispatcherPriority.Loaded);
    }

    private void ApplyJunhyunUiSimplification()
    {
        if (_junhyunUiSimplificationApplied)
            return;
        _junhyunUiSimplificationApplied = true;

        // Route/trail is not part of the JunhyunHelper product surface anymore.
        BtnClearTrail.Visibility = Visibility.Collapsed;
        BtnClearTrail.IsEnabled = false;
        TrailPath.Points.Clear();
        TrailPath.Visibility = Visibility.Collapsed;
        TrailPath.IsHitTestVisible = false;

        // The marker selector itself is the toggle. Do not expose a separate arrow.
        TxtMapMarkersTitle.Visibility = Visibility.Collapsed;
        BtnToggleMapMarkersPanel.Click -= BtnToggleMapMarkersPanel_Click;
        BtnToggleMapMarkersPanel.Click += JunhyunMapMarkersButton_Click;
        BtnToggleMapMarkersPanel.Content = "지도 마커";
        BtnToggleMapMarkersPanel.FontSize = 12;
        BtnToggleMapMarkersPanel.FontWeight = FontWeights.SemiBold;
        BtnToggleMapMarkersPanel.Padding = new Thickness(10, 5, 10, 5);
        BtnToggleMapMarkersPanel.Margin = new Thickness(0);
        BtnToggleMapMarkersPanel.HorizontalContentAlignment = HorizontalAlignment.Center;

        _isMapMarkersPanelCollapsed = true;
        MapMarkersContent.Visibility = Visibility.Collapsed;
        MapMarkersOverlay.MinWidth = 178;
        MapMarkersOverlay.MinHeight = 0;
        MapMarkersOverlay.MaxHeight = double.PositiveInfinity;
        MapMarkersContent.MaxHeight = double.PositiveInfinity;

        // Settings is also a true toggle: pressing the launcher again closes it.
        BtnSettings.Click -= BtnSettings_Click;
        BtnSettings.Click += JunhyunSettingsButton_Click;

        // Product hotkey controls explain themselves; remove the long instructional copy.
        foreach (var text in EnumerateJunhyunDescendants<TextBlock>(SettingsPanel))
        {
            if (text.Text?.StartsWith("일반 키를 단독으로 사용하거나", StringComparison.Ordinal) == true)
            {
                text.Visibility = Visibility.Collapsed;
                text.Margin = new Thickness(0);
            }
        }
    }

    private void JunhyunMapMarkersButton_Click(object sender, RoutedEventArgs e)
    {
        var expand = MapMarkersContent.Visibility != Visibility.Visible;
        _isMapMarkersPanelCollapsed = !expand;
        MapMarkersContent.Visibility = expand ? Visibility.Visible : Visibility.Collapsed;
    }

    private void JunhyunSettingsButton_Click(object sender, RoutedEventArgs e)
    {
        ToggleSettingsPanel(SettingsPanel.Visibility != Visibility.Visible);
    }

    private static IEnumerable<T> EnumerateJunhyunDescendants<T>(DependencyObject root)
        where T : DependencyObject
    {
        foreach (var child in LogicalTreeHelper.GetChildren(root))
        {
            if (child is T typed)
                yield return typed;
            if (child is DependencyObject dependencyObject)
            {
                foreach (var descendant in EnumerateJunhyunDescendants<T>(dependencyObject))
                    yield return descendant;
            }
        }
    }
}
