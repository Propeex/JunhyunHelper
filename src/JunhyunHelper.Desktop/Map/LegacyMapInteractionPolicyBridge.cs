using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using TarkovHelper.Models;
using TarkovHelper.Models.Map;
using TarkovHelper.Services;
using TarkovHelper.Services.Map;
using TarkovHelper.Services.Settings;

namespace JunhyunHelper.Desktop.Map;

/// <summary>
/// Enforces V2 product policies on the exact Map engine: manual floor selection,
/// screenshot Map switching, player tracking, current-floor-only presentation and
/// no custom-marker surface.
/// </summary>
public sealed class LegacyMapInteractionPolicyBridge : IDisposable
{
    private readonly TarkovHelper.Pages.Map.MapPage _page;
    private readonly MapTrackerService _tracker = MapTrackerService.Instance;
    private readonly OverlayMiniMapService _overlay = OverlayMiniMapService.Instance;
    private readonly ComboBox? _legacyFloor;
    private readonly ComboBox? _mapSelector;
    private readonly TextBlock? _floorLabel;
    private readonly ComboBox? _productFloor;
    private readonly Canvas? _mapMarkers;
    private readonly Canvas? _extractMarkers;
    private readonly DispatcherTimer _policyTimer;
    private bool _syncingFloor;
    private bool _disposed;

    public LegacyMapInteractionPolicyBridge(TarkovHelper.Pages.Map.MapPage page)
    {
        _page = page ?? throw new ArgumentNullException(nameof(page));
        _page.EnableJunhyunManualFloorPolicy();

        _legacyFloor = _page.FindName("CmbFloorSelect") as ComboBox;
        _mapSelector = _page.FindName("CmbMapSelect") as ComboBox;
        _floorLabel = _page.FindName("TxtFloorLabel") as TextBlock;
        _mapMarkers = _page.FindName("MapMarkersContainer") as Canvas;
        _extractMarkers = _page.FindName("ExtractMarkersContainer") as Canvas;

        _productFloor = CreateProductFloorSelector();
        RemoveCustomMarkers();
        ApplyFixedPolicies();

        _tracker.PositionUpdated += Tracker_PositionUpdated;
        _overlay.SettingsChanged += Overlay_SettingsChanged;
        if (_legacyFloor is not null)
            _legacyFloor.SelectionChanged += LegacyFloor_SelectionChanged;
        if (_productFloor is not null)
            _productFloor.SelectionChanged += ProductFloor_SelectionChanged;
        if (_mapSelector is not null)
            _mapSelector.SelectionChanged += MapSelector_SelectionChanged;

        _policyTimer = new DispatcherTimer(
            TimeSpan.FromMilliseconds(200),
            DispatcherPriority.Background,
            (_, _) => MaintainPolicies(),
            _page.Dispatcher);
        _policyTimer.Start();
        _page.Dispatcher.BeginInvoke(MaintainPolicies, DispatcherPriority.Loaded);
    }

    private ComboBox? CreateProductFloorSelector()
    {
        if (_legacyFloor?.Parent is not StackPanel parent)
            return null;

        var index = parent.Children.IndexOf(_legacyFloor);
        var combo = new ComboBox
        {
            Width = _legacyFloor.Width,
            Visibility = Visibility.Collapsed,
            Margin = _legacyFloor.Margin,
            ToolTip = "현재 층을 직접 선택합니다.",
        };
        parent.Children.Insert(Math.Max(0, index + 1), combo);
        _legacyFloor.Visibility = Visibility.Collapsed;
        return combo;
    }

    private void MaintainPolicies()
    {
        if (_disposed)
            return;

        ApplyFixedPolicies();
        SyncFloorSelector();
        EnforceCurrentFloorOnly();
        RemoveCustomMarkers();
    }

    private void ApplyFixedPolicies()
    {
        MapSettings.Instance.AutoCenterEnabled = true;
        MapSettings.Instance.AutoFloorEnabled = false;

        var settings = _overlay.Settings;
        var changed = settings.OtherFloorOpacity != 0.0 ||
                      settings.AutoFloorSelection ||
                      settings.ViewMode != MiniMapViewMode.PlayerTracking ||
                      !settings.ClickThrough ||
                      settings.Opacity != 1.0 ||
                      settings.ResumeAutoFloorKey != 0;

        settings.OtherFloorOpacity = 0.0;
        settings.AutoFloorSelection = false;
        settings.ViewMode = MiniMapViewMode.PlayerTracking;
        settings.ClickThrough = true;
        settings.Opacity = 1.0;
        settings.ResumeAutoFloorKey = 0;
        GlobalKeyboardHookService.Instance.ResumeAutoFloorKey = 0;

        if (changed)
            _overlay.SaveSettings();
    }

    private void SyncFloorSelector()
    {
        if (_legacyFloor is null || _productFloor is null)
            return;

        _legacyFloor.Visibility = Visibility.Collapsed;

        var shouldShow = _floorLabel?.Visibility == Visibility.Visible && _legacyFloor.Items.Count > 0;
        _productFloor.Visibility = shouldShow ? Visibility.Visible : Visibility.Collapsed;
        if (!shouldShow)
            return;

        var rebuild = _productFloor.Items.Count != _legacyFloor.Items.Count;
        if (!rebuild)
        {
            for (var i = 0; i < _legacyFloor.Items.Count; i++)
            {
                var source = _legacyFloor.Items[i] as ComboBoxItem;
                var copy = _productFloor.Items[i] as ComboBoxItem;
                if (!string.Equals(source?.Tag as string, copy?.Tag as string, StringComparison.OrdinalIgnoreCase) ||
                    !Equals(source?.Content, copy?.Content))
                {
                    rebuild = true;
                    break;
                }
            }
        }

        _syncingFloor = true;
        try
        {
            if (rebuild)
            {
                _productFloor.Items.Clear();
                foreach (var item in _legacyFloor.Items.OfType<ComboBoxItem>())
                {
                    _productFloor.Items.Add(new ComboBoxItem
                    {
                        Content = item.Content,
                        Tag = item.Tag,
                    });
                }
            }

            _productFloor.SelectedIndex = _legacyFloor.SelectedIndex;
        }
        finally
        {
            _syncingFloor = false;
        }
    }

    private void ProductFloor_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_syncingFloor || _legacyFloor is null || _productFloor is null)
            return;

        if (_productFloor.SelectedIndex >= 0 &&
            _productFloor.SelectedIndex < _legacyFloor.Items.Count)
        {
            _legacyFloor.SelectedIndex = _productFloor.SelectedIndex;
        }
    }

    private void LegacyFloor_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_syncingFloor)
            _page.Dispatcher.BeginInvoke(SyncFloorSelector);
    }

    private void MapSelector_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
        _page.Dispatcher.BeginInvoke(SyncFloorSelector, DispatcherPriority.Loaded);

    private void EnforceCurrentFloorOnly()
    {
        var selectedFloor = (_legacyFloor?.SelectedItem as ComboBoxItem)?.Tag as string;
        ApplyFloorVisibility(_mapMarkers, selectedFloor);
        ApplyFloorVisibility(_extractMarkers, selectedFloor);
    }

    private static void ApplyFloorVisibility(Canvas? container, string? selectedFloor)
    {
        if (container is null)
            return;

        foreach (FrameworkElement child in container.Children)
        {
            var floorId = child.Tag switch
            {
                MapMarker marker => marker.FloorId,
                MapExtract extract => extract.FloorId,
                _ => null,
            };

            if (child.Tag is not MapMarker && child.Tag is not MapExtract)
                continue;

            child.Visibility = IsCurrentFloor(floorId, selectedFloor)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }
    }

    private static bool IsCurrentFloor(string? markerFloor, string? selectedFloor)
    {
        if (string.IsNullOrWhiteSpace(selectedFloor))
            return true;
        var effectiveMarkerFloor = string.IsNullOrWhiteSpace(markerFloor) ? "main" : markerFloor;
        return string.Equals(effectiveMarkerFloor, selectedFloor, StringComparison.OrdinalIgnoreCase);
    }

    private void Tracker_PositionUpdated(object? sender, ScreenPosition position)
    {
        if (string.IsNullOrWhiteSpace(position.MapKey))
            return;

        _page.Dispatcher.BeginInvoke(() => SwitchMapFromScreenshot(position.MapKey));
    }

    private void SwitchMapFromScreenshot(string mapKey)
    {
        if (_mapSelector is null ||
            string.Equals(_tracker.CurrentMapKey, mapKey, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        for (var i = 0; i < _mapSelector.Items.Count; i++)
        {
            if (_mapSelector.Items[i] is ComboBoxItem item &&
                string.Equals(item.Tag as string, mapKey, StringComparison.OrdinalIgnoreCase))
            {
                _mapSelector.SelectedIndex = i;
                _tracker.SetCurrentMap(mapKey);
                return;
            }
        }
    }

    private void Overlay_SettingsChanged(OverlayMiniMapSettings settings)
    {
        if (!_disposed)
            _page.Dispatcher.BeginInvoke(ApplyFixedPolicies);
    }

    private void RemoveCustomMarkers()
    {
        Collapse("MarkerListPanel");
        Collapse("BtnToggleCustomMarkersPanel");
        Collapse("CustomMarkersContainer");
        Collapse("MenuAddCustomMarker");

        if (_page.FindName("MarkerListColumn") is ColumnDefinition markerColumn)
        {
            markerColumn.MinWidth = 0;
            markerColumn.Width = new GridLength(0);
        }

        if (_page.FindName("MarkerListPanel") is FrameworkElement panel && panel.Parent is Grid grid &&
            grid.ColumnDefinitions.Count > 4)
        {
            grid.ColumnDefinitions[3].Width = new GridLength(0);
            grid.ColumnDefinitions[4].Width = new GridLength(0);
        }
    }

    private void Collapse(string name)
    {
        if (_page.FindName(name) is FrameworkElement element)
        {
            element.Visibility = Visibility.Collapsed;
            element.IsEnabled = false;
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;

        _policyTimer.Stop();
        _tracker.PositionUpdated -= Tracker_PositionUpdated;
        _overlay.SettingsChanged -= Overlay_SettingsChanged;
        if (_legacyFloor is not null)
            _legacyFloor.SelectionChanged -= LegacyFloor_SelectionChanged;
        if (_productFloor is not null)
            _productFloor.SelectionChanged -= ProductFloor_SelectionChanged;
        if (_mapSelector is not null)
            _mapSelector.SelectionChanged -= MapSelector_SelectionChanged;
    }
}
