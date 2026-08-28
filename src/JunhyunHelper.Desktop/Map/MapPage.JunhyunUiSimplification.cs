using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace TarkovHelper.Pages.Map;

public partial class MapPage
{
    private static readonly bool JunhyunUiSimplificationHandlerRegistered = RegisterJunhyunUiSimplificationHandler();
    private bool _junhyunUiSimplificationApplied;
    private Panel? _junhyunSettingsOriginalParent;
    private int _junhyunSettingsOriginalIndex = -1;

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

        NormalizeMiniMapLauncherChrome();
        PrepareMapMarkerLauncher();
        PrepareSettingsOverlaySurface();

        // Settings is a real product overlay. Re-clicking the launcher and clicking the
        // backdrop are both handled by MainWindow's shared overlay owner.
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

    private void NormalizeMiniMapLauncherChrome()
    {
        BtnMinimapHelp.Visibility = Visibility.Collapsed;
        BtnMinimapHelp.IsEnabled = false;
        BtnMinimap.Margin = new Thickness(0);

        // The donor wraps the MiniMap button in a padded dark Border that also made
        // room for the now-hidden help button. Once help is removed that parent chrome
        // becomes a visible blank strip, so let the actual themed button own all chrome.
        if (BtnMinimap.Parent is StackPanel controls && controls.Parent is Border container)
        {
            container.Padding = new Thickness(0);
            container.Background = Brushes.Transparent;
            container.BorderThickness = new Thickness(0);
            container.CornerRadius = new CornerRadius(0);
        }
    }

    private void PrepareMapMarkerLauncher()
    {
        TxtMapMarkersTitle.Visibility = Visibility.Collapsed;
        BtnToggleMapMarkersPanel.Click -= BtnToggleMapMarkersPanel_Click;
        BtnToggleMapMarkersPanel.Click += JunhyunMapMarkersButton_Click;
        BtnToggleMapMarkersPanel.Content = "지도 마커";
        BtnToggleMapMarkersPanel.FontSize = 12;
        BtnToggleMapMarkersPanel.FontWeight = FontWeights.SemiBold;
        BtnToggleMapMarkersPanel.Padding = new Thickness(12, 7, 12, 7);
        BtnToggleMapMarkersPanel.Margin = new Thickness(0);
        BtnToggleMapMarkersPanel.MinWidth = 96;
        BtnToggleMapMarkersPanel.MinHeight = 34;
        BtnToggleMapMarkersPanel.HorizontalAlignment = HorizontalAlignment.Left;
        BtnToggleMapMarkersPanel.HorizontalContentAlignment = HorizontalAlignment.Center;

        // Remove donor's transparent-arrow local values so the product button uses the
        // normal JunhyunHelper Button style instead of looking like text in a wide panel.
        BtnToggleMapMarkersPanel.ClearValue(Button.BackgroundProperty);
        BtnToggleMapMarkersPanel.ClearValue(Button.BorderBrushProperty);
        BtnToggleMapMarkersPanel.ClearValue(Button.BorderThicknessProperty);
        BtnToggleMapMarkersPanel.ClearValue(Button.ForegroundProperty);

        // v1.9.0 restores the donor's actual extract controls to the product marker panel.
        // This happens in the same real Loaded lifecycle as the launcher itself so runtime
        // verification cannot repair a missing activation path.
        RestoreJunhyunExtractMarkerFiltersToMarkerPanel();

        _isMapMarkersPanelCollapsed = true;
        MapMarkersContent.Visibility = Visibility.Collapsed;
        MapMarkersOverlay.MaxHeight = double.PositiveInfinity;
        MapMarkersContent.MaxHeight = double.PositiveInfinity;
        ApplyMapMarkerPanelChrome(expanded: false);
    }

    private void JunhyunMapMarkersButton_Click(object sender, RoutedEventArgs e)
    {
        var expand = MapMarkersContent.Visibility != Visibility.Visible;
        _isMapMarkersPanelCollapsed = !expand;
        MapMarkersContent.Visibility = expand ? Visibility.Visible : Visibility.Collapsed;
        ApplyMapMarkerPanelChrome(expand);
    }

    private void ApplyMapMarkerPanelChrome(bool expanded)
    {
        if (BtnToggleMapMarkersPanel.Parent is StackPanel header)
            header.Margin = expanded ? new Thickness(0, 0, 0, 8) : new Thickness(0);

        if (!expanded)
        {
            MapMarkersOverlay.MinWidth = 0;
            MapMarkersOverlay.MinHeight = 0;
            MapMarkersOverlay.Padding = new Thickness(0);
            MapMarkersOverlay.Background = Brushes.Transparent;
            MapMarkersOverlay.BorderThickness = new Thickness(0);
            return;
        }

        MapMarkersOverlay.MinWidth = 220;
        MapMarkersOverlay.MinHeight = AvailableMarkerPanelHeight();
        MapMarkersOverlay.Padding = new Thickness(10);
        MapMarkersOverlay.Background = new SolidColorBrush(Color.FromArgb(224, 32, 32, 32));
        MapMarkersOverlay.BorderBrush = TryFindResource("BorderBrush") as Brush ?? Brushes.DimGray;
        MapMarkersOverlay.BorderThickness = new Thickness(1);
    }

    private double AvailableMarkerPanelHeight()
    {
        var available = MapViewerGrid.ActualHeight;
        if (!double.IsFinite(available) || available <= 0)
            available = ActualHeight;

        // Current product marker groups require roughly 560 px. Use the available map
        // viewport when it is smaller, but on normal desktop layouts give enough height
        // for every checkbox so the internal marker list does not require scrolling.
        return Math.Clamp(available - 32, 380, 590);
    }

    private void PrepareSettingsOverlaySurface()
    {
        TxtSettingsTitle.Visibility = Visibility.Collapsed;
        foreach (var button in EnumerateJunhyunDescendants<Button>(SettingsPanel))
        {
            if (string.Equals(button.Content?.ToString(), "✕", StringComparison.Ordinal))
                button.Visibility = Visibility.Collapsed;
        }

        SettingsPanel.Visibility = Visibility.Collapsed;
        SettingsPanel.BorderThickness = new Thickness(0);
        SettingsPanel.Background = Brushes.Transparent;
        SettingsColumn.Width = new GridLength(0);
    }

    private async void JunhyunSettingsButton_Click(object sender, RoutedEventArgs e)
    {
        if (Window.GetWindow(this) is not JunhyunHelper.Desktop.MainWindow mainWindow)
            return;

        if (mainWindow.DismissInAppOverlay("map-settings"))
            return;

        DetachSettingsPanelForOverlay();
        SettingsPanel.Visibility = Visibility.Visible;
        try
        {
            await mainWindow.ShowInAppElementAsync(
                "map-settings",
                "지도 / 미니맵 설정",
                SettingsPanel,
                preferredWidth: 620,
                preferredHeight: 760);
        }
        finally
        {
            SettingsPanel.Visibility = Visibility.Collapsed;
            RestoreSettingsPanelAfterOverlay();
        }
    }

    private void DetachSettingsPanelForOverlay()
    {
        if (SettingsPanel.Parent is not Panel parent)
            return;

        _junhyunSettingsOriginalParent = parent;
        _junhyunSettingsOriginalIndex = parent.Children.IndexOf(SettingsPanel);
        parent.Children.Remove(SettingsPanel);
        SettingsColumn.Width = new GridLength(0);
    }

    private void RestoreSettingsPanelAfterOverlay()
    {
        if (SettingsPanel.Parent is not null || _junhyunSettingsOriginalParent is null)
            return;

        var index = Math.Clamp(_junhyunSettingsOriginalIndex, 0, _junhyunSettingsOriginalParent.Children.Count);
        _junhyunSettingsOriginalParent.Children.Insert(index, SettingsPanel);
        Grid.SetColumn(SettingsPanel, 5);
        SettingsColumn.Width = new GridLength(0);
        _junhyunSettingsOriginalParent = null;
        _junhyunSettingsOriginalIndex = -1;
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
