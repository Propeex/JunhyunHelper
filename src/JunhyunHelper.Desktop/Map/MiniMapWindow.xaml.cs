using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;
using JunhyunHelper.Core.Maps;

namespace JunhyunHelper.Desktop.Map;

public sealed record MiniMapMarker(
    MapWorldPosition Position,
    string Name,
    MapMarkerKind? Kind,
    bool IsQuest,
    string? UserColor = null);

public partial class MiniMapWindow : Window
{
    private const int GwlExStyle = -20;
    private const int WsExTransparent = 0x00000020;
    private const int WsExLayered = 0x00080000;
    private const int VkControl = 0x11;
    private const int VkShift = 0x10;
    private const int VkM = 0x4D;
    private const double SurfaceWidth = 1600;

    private readonly LegacyMiniMapSettingsStore _settingsStore = new();
    private readonly LegacyMiniMapHotkeyService _hotkeys = new();
    private LegacyMiniMapSettings _settings = new();
    private MapLayoutDefinition? _layout;
    private MapWorldPosition? _playerPosition;
    private double? _playerHeading;
    private double _effectiveScale = 1;
    private string? _floorName;
    private IntPtr _hwnd;
    private bool _clickThroughApplied;
    private bool _loaded;
    private bool _disposed;

    private bool _panning;
    private Point _panStart;
    private double _panStartOffsetX;
    private double _panStartOffsetY;

    public MiniMapWindow()
    {
        _settings = _settingsStore.Load();
        InitializeComponent();
        ApplyWindowSettings();
    }

    public event EventHandler? UserClosed;

    public void SetState(
        MapLayoutDefinition layout,
        string svgPath,
        string? floorName,
        IReadOnlyList<MiniMapMarker> markers,
        MapWorldPosition? playerPosition,
        double? playerHeading,
        IReadOnlyList<MapWorldPosition> trail,
        bool showTrail)
    {
        _layout = layout;
        _playerPosition = playerPosition;
        _playerHeading = playerHeading;
        _floorName = floorName;

        var aspect = MapCoordinateTransformer.SurfaceAspectRatio(layout);
        MiniSurface.Width = SurfaceWidth;
        MiniSurface.Height = SurfaceWidth * aspect;
        MiniSvg.Source = new Uri(svgPath, UriKind.Absolute);

        if (string.IsNullOrWhiteSpace(floorName))
        {
            FloorIndicator.Visibility = Visibility.Collapsed;
        }
        else
        {
            FloorNameText.Text = floorName;
            FloorModeText.Text = "공유";
            FloorIndicator.Visibility = Visibility.Visible;
        }

        MiniMarkerCanvas.Children.Clear();
        foreach (var marker in markers)
        {
            if (!MapCoordinateTransformer.TryWorldToSurface(
                    layout,
                    marker.Position,
                    MiniSurface.Width,
                    MiniSurface.Height,
                    out var point))
                continue;

            FrameworkElement visual = marker.IsQuest
                ? MapVisualFactory.CreateQuestMarker(marker.Name, 32)
                : marker.UserColor is not null
                    ? MapVisualFactory.CreateUserMarker(marker.UserColor, marker.Name, 30)
                    : MapVisualFactory.CreateMarker(marker.Kind ?? MapMarkerKind.Hazard, marker.Name, 28);
            visual.Tag = 0d;
            Canvas.SetLeft(visual, point.X - visual.Width / 2);
            Canvas.SetTop(visual, point.Y - visual.Height / 2);
            MiniMarkerCanvas.Children.Add(visual);
        }

        MiniTrailCanvas.Children.Clear();
        if (showTrail && trail.Count > 1)
        {
            var points = new PointCollection();
            foreach (var world in trail)
            {
                if (MapCoordinateTransformer.TryWorldToSurface(
                        layout,
                        world,
                        MiniSurface.Width,
                        MiniSurface.Height,
                        out var point))
                    points.Add(point);
            }

            if (points.Count > 1)
            {
                MiniTrailCanvas.Children.Add(new Polyline
                {
                    Points = points,
                    Stroke = Brushes.DeepSkyBlue,
                    StrokeThickness = 3,
                    Opacity = 0.75,
                });
            }
        }

        MiniPlayerCanvas.Children.Clear();
        if (playerPosition is not null &&
            MapCoordinateTransformer.TryWorldToSurface(
                layout,
                playerPosition,
                MiniSurface.Width,
                MiniSurface.Height,
                out var playerPoint))
        {
            var heading = playerHeading is null
                ? 0
                : MapCoordinateTransformer.SurfaceHeading(layout, playerHeading.Value);
            var player = MapVisualFactory.CreatePlayerMarker(heading, 34);
            player.Tag = heading;
            Canvas.SetLeft(player, playerPoint.X - player.Width / 2);
            Canvas.SetTop(player, playerPoint.Y - player.Height / 2);
            MiniPlayerCanvas.Children.Add(player);
        }

        UpdateViewport();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        if (_loaded)
            return;
        _loaded = true;
        _hwnd = new WindowInteropHelper(this).Handle;

        if (_settings.PositionX < 0 || _settings.PositionY < 0)
            PositionToTopRight();
        else
        {
            Left = _settings.PositionX;
            Top = _settings.PositionY;
        }

        ApplyClickThrough(_settings.ClickThrough);
        _hotkeys.Start(HandleGlobalHotkey);
        UpdateViewport();
    }

    private void ApplyWindowSettings()
    {
        _settings.Normalize();
        Width = _settings.Width;
        Height = _settings.Height;
        MainBorder.Opacity = _settings.Opacity;
        if (_settings.PositionX >= 0 && _settings.PositionY >= 0)
        {
            Left = _settings.PositionX;
            Top = _settings.PositionY;
        }
    }

    private void PositionToTopRight()
    {
        var area = SystemParameters.WorkArea;
        Left = area.Right - ActualWidth - 20;
        Top = area.Top + 20;
        _settings.PositionX = Left;
        _settings.PositionY = Top;
        SaveSettings();
    }

    private void UpdateViewport()
    {
        if (_layout is null || MiniViewport.ActualWidth <= 0 || MiniViewport.ActualHeight <= 0 ||
            MiniSurface.Width <= 0 || MiniSurface.Height <= 0)
            return;

        var fitScale = Math.Min(
            MiniViewport.ActualWidth / MiniSurface.Width,
            MiniViewport.ActualHeight / MiniSurface.Height);
        if (!double.IsFinite(fitScale) || fitScale <= 0)
            return;

        var tracking = _settings.ViewMode == LegacyMiniMapViewMode.PlayerTracking && _playerPosition is not null;
        var hasPlayer = _playerPosition is not null;
        var zoomMultiplier = hasPlayer || _settings.ViewMode == LegacyMiniMapViewMode.Fixed
            ? _settings.ZoomMultiplier
            : 0.96;
        _effectiveScale = fitScale * zoomMultiplier;

        var offsetX = (MiniViewport.ActualWidth - MiniSurface.Width * _effectiveScale) / 2;
        var offsetY = (MiniViewport.ActualHeight - MiniSurface.Height * _effectiveScale) / 2;

        if (tracking && _playerPosition is not null &&
            MapCoordinateTransformer.TryWorldToSurface(
                _layout,
                _playerPosition,
                MiniSurface.Width,
                MiniSurface.Height,
                out var playerPoint))
        {
            offsetX = MiniViewport.ActualWidth / 2 - playerPoint.X * _effectiveScale;
            offsetY = MiniViewport.ActualHeight / 2 - playerPoint.Y * _effectiveScale;
        }
        else if (_settings.ViewMode == LegacyMiniMapViewMode.Fixed && _settings.HasFixedOffset)
        {
            offsetX = _settings.FixedOffsetX;
            offsetY = _settings.FixedOffsetY;
        }

        (offsetX, offsetY) = ClampOffset(offsetX, offsetY);
        if (_settings.ViewMode == LegacyMiniMapViewMode.Fixed)
        {
            _settings.FixedOffsetX = offsetX;
            _settings.FixedOffsetY = offsetY;
            _settings.HasFixedOffset = true;
        }

        MiniSurface.RenderTransform = new MatrixTransform(
            _effectiveScale,
            0,
            0,
            _effectiveScale,
            offsetX,
            offsetY);
        ApplyInverseScale(MiniMarkerCanvas, 1);
        ApplyInverseScale(MiniPlayerCanvas, _settings.PlayerMarkerSize);
        UpdateStatusText();
    }

    private (double X, double Y) ClampOffset(double x, double y)
    {
        var viewWidth = MiniViewport.ActualWidth > 0 ? MiniViewport.ActualWidth : ActualWidth;
        var viewHeight = MiniViewport.ActualHeight > 0 ? MiniViewport.ActualHeight : ActualHeight;
        var scaledWidth = MiniSurface.Width * _effectiveScale;
        var scaledHeight = MiniSurface.Height * _effectiveScale;
        var minX = viewWidth * 0.25 - scaledWidth;
        var minY = viewHeight * 0.25 - scaledHeight;
        var maxX = viewWidth * 0.75;
        var maxY = viewHeight * 0.75;
        return (Math.Clamp(x, minX, maxX), Math.Clamp(y, minY, maxY));
    }

    private void ApplyInverseScale(Canvas canvas, double multiplier)
    {
        if (_effectiveScale <= 0)
            return;
        var inverse = multiplier / _effectiveScale;
        foreach (var child in canvas.Children.OfType<FrameworkElement>())
        {
            var heading = child.Tag is double angle ? angle : 0;
            child.RenderTransformOrigin = new Point(0.5, 0.5);
            var transforms = new TransformGroup();
            if (Math.Abs(heading) > 0.001)
                transforms.Children.Add(new RotateTransform(heading));
            transforms.Children.Add(new ScaleTransform(inverse, inverse));
            child.RenderTransform = transforms;
        }
    }

    private void UpdateStatusText()
    {
        if (_settings.ClickThrough)
        {
            ZoomHintText.Text = "클릭 통과 · Ctrl+Shift+M 해제";
            return;
        }

        if (_settings.ViewMode == LegacyMiniMapViewMode.PlayerTracking && _playerPosition is not null)
            ZoomHintText.Text = $"플레이어 추적 · {_settings.ZoomMultiplier:0.0}×";
        else if (_settings.ViewMode == LegacyMiniMapViewMode.Fixed)
            ZoomHintText.Text = $"고정 보기 · {_settings.ZoomMultiplier:0.0}×";
        else
            ZoomHintText.Text = "전체 지도 · 우클릭 설정";
    }

    private void HandleGlobalHotkey(int virtualKey)
    {
        if (!IsVisible)
            return;

        if (virtualKey == VkM && IsKeyDown(VkControl) && IsKeyDown(VkShift))
        {
            ToggleClickThrough();
            return;
        }

        if (virtualKey == _settings.ZoomInKey)
            ChangeZoom(1.12);
        else if (virtualKey == _settings.ZoomOutKey)
            ChangeZoom(1 / 1.12);
        else if (virtualKey == _settings.FloorUpKey)
            MapPage.FindLiveMapPage()?.MoveFloorFromMiniMap(+1);
        else if (virtualKey == _settings.FloorDownKey)
            MapPage.FindLiveMapPage()?.MoveFloorFromMiniMap(-1);
    }

    private static bool IsKeyDown(int virtualKey) =>
        (GetAsyncKeyState(virtualKey) & 0x8000) != 0;

    private void ChangeZoom(double factor)
    {
        _settings.ZoomMultiplier = Math.Clamp(_settings.ZoomMultiplier * factor, 0.5, 8);
        UpdateViewport();
        SaveSettings();
    }

    private void ToggleViewMode()
    {
        if (_settings.ViewMode == LegacyMiniMapViewMode.PlayerTracking)
        {
            _settings.ViewMode = LegacyMiniMapViewMode.Fixed;
            CaptureCurrentOffset();
        }
        else
        {
            _settings.ViewMode = LegacyMiniMapViewMode.PlayerTracking;
        }
        UpdateViewport();
        SaveSettings();
    }

    private void CenterPlayer()
    {
        if (_playerPosition is null || _layout is null)
            return;

        _settings.ViewMode = LegacyMiniMapViewMode.Fixed;
        if (MapCoordinateTransformer.TryWorldToSurface(
                _layout,
                _playerPosition,
                MiniSurface.Width,
                MiniSurface.Height,
                out var point))
        {
            var fitScale = Math.Min(
                MiniViewport.ActualWidth / MiniSurface.Width,
                MiniViewport.ActualHeight / MiniSurface.Height);
            _effectiveScale = fitScale * _settings.ZoomMultiplier;
            _settings.FixedOffsetX = MiniViewport.ActualWidth / 2 - point.X * _effectiveScale;
            _settings.FixedOffsetY = MiniViewport.ActualHeight / 2 - point.Y * _effectiveScale;
            _settings.HasFixedOffset = true;
        }
        UpdateViewport();
        SaveSettings();
    }

    private void ResetView()
    {
        _settings.ZoomMultiplier = 2.35;
        _settings.ViewMode = LegacyMiniMapViewMode.PlayerTracking;
        _settings.FixedOffsetX = 0;
        _settings.FixedOffsetY = 0;
        _settings.HasFixedOffset = false;
        UpdateViewport();
        SaveSettings();
    }

    private void ChangeOpacity(double delta)
    {
        _settings.Opacity = Math.Clamp(_settings.Opacity + delta, 0.1, 1);
        MainBorder.Opacity = _settings.Opacity;
        SaveSettings();
    }

    private void ToggleClickThrough() =>
        ApplyClickThrough(!_clickThroughApplied);

    private void ApplyClickThrough(bool enabled)
    {
        _settings.ClickThrough = enabled;
        if (_hwnd != IntPtr.Zero)
        {
            var style = GetWindowLong(_hwnd, GwlExStyle);
            if (enabled)
                style |= WsExTransparent | WsExLayered;
            else
                style &= ~WsExTransparent;
            SetWindowLong(_hwnd, GwlExStyle, style);
            _clickThroughApplied = enabled;
        }
        else
        {
            _clickThroughApplied = enabled;
        }
        UpdateStatusText();
        SaveSettings();
    }

    private void CaptureCurrentOffset()
    {
        if (MiniSurface.RenderTransform is MatrixTransform matrix)
        {
            _settings.FixedOffsetX = matrix.Matrix.OffsetX;
            _settings.FixedOffsetY = matrix.Matrix.OffsetY;
            _settings.HasFixedOffset = true;
        }
    }

    private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (!_loaded)
            return;
        _settings.Width = ActualWidth;
        _settings.Height = ActualHeight;
        UpdateViewport();
        SaveSettings();
    }

    private void Window_LocationChanged(object? sender, EventArgs e)
    {
        if (!_loaded)
            return;
        _settings.PositionX = Left;
        _settings.PositionY = Top;
        SaveSettings();
    }

    private void Window_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (_clickThroughApplied)
            return;
        ChangeZoom(e.Delta > 0 ? 1.12 : 1 / 1.12);
        e.Handled = true;
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_clickThroughApplied || e.ChangedButton != MouseButton.Left)
            return;
        if (e.ClickCount >= 2)
        {
            PositionToTopRight();
            e.Handled = true;
            return;
        }
        if (!_panning && e.ButtonState == MouseButtonState.Pressed)
        {
            try { DragMove(); }
            catch (InvalidOperationException) { }
        }
    }

    private void Window_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (_clickThroughApplied || e.ChangedButton != MouseButton.Middle)
            return;

        _settings.ViewMode = LegacyMiniMapViewMode.Fixed;
        CaptureCurrentOffset();
        _panning = true;
        _panStart = e.GetPosition(MiniViewport);
        _panStartOffsetX = _settings.FixedOffsetX;
        _panStartOffsetY = _settings.FixedOffsetY;
        Mouse.Capture(MiniViewport);
        Cursor = Cursors.ScrollAll;
        e.Handled = true;
    }

    private void Window_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (!_panning || e.MiddleButton != MouseButtonState.Pressed)
            return;
        var current = e.GetPosition(MiniViewport);
        var (x, y) = ClampOffset(
            _panStartOffsetX + current.X - _panStart.X,
            _panStartOffsetY + current.Y - _panStart.Y);
        _settings.FixedOffsetX = x;
        _settings.FixedOffsetY = y;
        _settings.HasFixedOffset = true;
        UpdateViewport();
        e.Handled = true;
    }

    private void Window_PreviewMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Middle || !_panning)
            return;
        _panning = false;
        Mouse.Capture(null);
        Cursor = Cursors.Arrow;
        SaveSettings();
        e.Handled = true;
    }

    private void Window_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_clickThroughApplied)
            return;

        var menu = new ContextMenu();
        menu.Items.Add(MenuItem(
            _settings.ViewMode == LegacyMiniMapViewMode.PlayerTracking ? "고정 보기" : "플레이어 추적",
            (_, _) => ToggleViewMode()));
        menu.Items.Add(MenuItem("현재 위치 중앙", (_, _) => CenterPlayer(), _playerPosition is not null));
        menu.Items.Add(MenuItem("위층 · PageUp", (_, _) => MapPage.FindLiveMapPage()?.MoveFloorFromMiniMap(+1)));
        menu.Items.Add(MenuItem("아래층 · PageDown", (_, _) => MapPage.FindLiveMapPage()?.MoveFloorFromMiniMap(-1)));
        menu.Items.Add(MenuItem("감지 층으로 복귀", (_, _) => MapPage.FindLiveMapPage()?.CenterMiniMapOnDetectedFloor()));
        menu.Items.Add(new Separator());
        menu.Items.Add(MenuItem("투명도 +", (_, _) => ChangeOpacity(+0.05)));
        menu.Items.Add(MenuItem("투명도 -", (_, _) => ChangeOpacity(-0.05)));
        menu.Items.Add(MenuItem("클릭 통과 켜기 · Ctrl+Shift+M 해제", (_, _) => ToggleClickThrough()));
        menu.Items.Add(MenuItem("보기 초기화", (_, _) => ResetView()));
        menu.Items.Add(new Separator());
        menu.Items.Add(MenuItem("미니맵 닫기", (_, _) => HideByUser()));
        menu.PlacementTarget = this;
        menu.IsOpen = true;
        e.Handled = true;
    }

    private static MenuItem MenuItem(string header, RoutedEventHandler onClick, bool enabled = true)
    {
        var item = new MenuItem { Header = header, IsEnabled = enabled };
        item.Click += onClick;
        return item;
    }

    private void HideByUser()
    {
        Hide();
        UserClosed?.Invoke(this, EventArgs.Empty);
    }

    private void SaveSettings()
    {
        _settings.PositionX = Left;
        _settings.PositionY = Top;
        _settings.Width = ActualWidth > 0 ? ActualWidth : _settings.Width;
        _settings.Height = ActualHeight > 0 ? ActualHeight : _settings.Height;
        _settingsStore.QueueSave(_settings);
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        if (Application.Current?.MainWindow?.IsVisible == true)
        {
            e.Cancel = true;
            HideByUser();
            return;
        }

        if (!_disposed)
        {
            _disposed = true;
            _hotkeys.Dispose();
            _settingsStore.Flush(_settings);
        }
        base.OnClosing(e);
    }

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr window, int index);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr window, int index, int newStyle);

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int virtualKey);
}