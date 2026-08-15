using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using TarkovHelper.Models;
using TarkovHelper.Models.Map;
using TarkovHelper.Services.Map;

namespace JunhyunHelper.Desktop.Map;

/// <summary>
/// Normalizes the transplanted Main Map extract visuals after the pinned renderer finishes
/// a real refresh. The source can contain separate PMC/Scav rows for the same physical exit;
/// rendering those rows independently produces the green+gray duplicates seen in Factory.
/// This bridge keeps the pinned loader/filter/calibration behavior, but collapses only true
/// same-name/same-floor/same-position duplicates and applies the shared floor presentation.
/// </summary>
public sealed class LegacyExtractMarkerPresentationBridge : IDisposable
{
    private readonly TarkovHelper.Pages.Map.MapPage _page;
    private readonly MapTrackerService _tracker = MapTrackerService.Instance;
    private readonly Canvas? _container;
    private readonly ComboBox? _floorSelector;
    private readonly DispatcherTimer _debounceTimer;
    private int _lastObservedSignature = int.MinValue;
    private bool _disposed;

    public LegacyExtractMarkerPresentationBridge(TarkovHelper.Pages.Map.MapPage page)
    {
        _page = page ?? throw new ArgumentNullException(nameof(page));
        _container = _page.FindName("ExtractMarkersContainer") as Canvas;
        _floorSelector = _page.FindName("CmbFloorSelect") as ComboBox;

        _debounceTimer = new DispatcherTimer(
            TimeSpan.FromMilliseconds(80),
            DispatcherPriority.Background,
            (_, _) => ApplyPending(),
            _page.Dispatcher)
        {
            IsEnabled = false,
        };

        _tracker.MapChanged += Tracker_MapChanged;
        if (_floorSelector is not null)
            _floorSelector.SelectionChanged += FloorSelector_SelectionChanged;
        if (_container is not null)
            _container.LayoutUpdated += Container_LayoutUpdated;
        _page.Loaded += Page_Loaded;
    }

    private void Page_Loaded(object sender, RoutedEventArgs e) => ScheduleApply(force: true);

    private void Tracker_MapChanged(string mapKey) =>
        _page.Dispatcher.BeginInvoke(() => ScheduleApply(force: true));

    private void FloorSelector_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
        _page.Dispatcher.BeginInvoke(() => ScheduleApply(force: true));

    private void Container_LayoutUpdated(object? sender, EventArgs e) => ScheduleApply(force: false);

    private void ScheduleApply(bool force)
    {
        if (_disposed || _container is null)
            return;

        var signature = ObserveSignature();
        if (!force && signature == _lastObservedSignature)
            return;

        _lastObservedSignature = signature;
        _debounceTimer.Stop();
        _debounceTimer.Start();
    }

    private int ObserveSignature()
    {
        var signature = new System.HashCode();
        signature.Add(_tracker.CurrentMapKey, StringComparer.OrdinalIgnoreCase);
        signature.Add((_floorSelector?.SelectedItem as ComboBoxItem)?.Tag as string, StringComparer.OrdinalIgnoreCase);

        var count = _container?.Children.Count ?? 0;
        signature.Add(count);
        if (_container is not null && count > 0)
        {
            signature.Add(RuntimeHelpers.GetHashCode(_container.Children[0]));
            signature.Add(RuntimeHelpers.GetHashCode(_container.Children[count - 1]));
        }

        return signature.ToHashCode();
    }

    private void ApplyPending()
    {
        _debounceTimer.Stop();
        Apply();
    }

    private void Apply()
    {
        if (_disposed || _container is null || string.IsNullOrWhiteSpace(_tracker.CurrentMapKey))
            return;

        var config = _tracker.GetMapConfig(_tracker.CurrentMapKey);
        if (config is null)
            return;

        var selectedFloor = (_floorSelector?.SelectedItem as ComboBoxItem)?.Tag as string;
        var visuals = _container.Children
            .OfType<Canvas>()
            .Where(static canvas => canvas.Tag is MapExtract)
            .ToList();

        CollapseDuplicateExtractVisuals(visuals);

        foreach (var canvas in _container.Children.OfType<Canvas>().ToArray())
        {
            if (canvas.Tag is not MapExtract extract)
                continue;

            RemoveLegacyFloorBadge(canvas);
            RestoreFactionBrushStrength(canvas, extract.Faction);

            var relation = JunhyunFloorPresentation.Resolve(config, extract.FloorId, selectedFloor);
            JunhyunFloorPresentation.ApplyToMarker(
                canvas,
                relation,
                badgeOffsetX: 8,
                badgeOffsetY: -13);
        }
    }

    private void CollapseDuplicateExtractVisuals(IReadOnlyList<Canvas> visuals)
    {
        if (_container is null || visuals.Count < 2)
            return;

        var consumed = new HashSet<Canvas>();
        foreach (var anchor in visuals)
        {
            if (consumed.Contains(anchor) || anchor.Tag is not MapExtract anchorExtract)
                continue;

            var group = visuals
                .Where(candidate =>
                    !consumed.Contains(candidate) &&
                    candidate.Tag is MapExtract candidateExtract &&
                    IsSamePhysicalExit(anchorExtract, candidateExtract))
                .ToArray();

            if (group.Length <= 1)
            {
                consumed.Add(anchor);
                continue;
            }

            var keep = group
                .OrderBy(candidate => FactionPriority(((MapExtract)candidate.Tag).Faction))
                .First();

            foreach (var candidate in group)
            {
                consumed.Add(candidate);
                if (!ReferenceEquals(candidate, keep))
                    _container.Children.Remove(candidate);
            }
        }
    }

    internal static bool IsSamePhysicalExit(MapExtract left, MapExtract right)
    {
        if (!string.Equals(left.Name, right.Name, StringComparison.OrdinalIgnoreCase))
            return false;

        if (!string.Equals(NormalizeFloor(left.FloorId), NormalizeFloor(right.FloorId), StringComparison.OrdinalIgnoreCase))
            return false;

        var dx = left.X - right.X;
        var dz = left.Z - right.Z;
        return (dx * dx) + (dz * dz) <= 1.0;
    }

    private static string NormalizeFloor(string? floorId) =>
        string.IsNullOrWhiteSpace(floorId) ? "main" : floorId;

    private static int FactionPriority(ExtractFaction faction) => faction switch
    {
        ExtractFaction.Shared => 0,
        ExtractFaction.Pmc => 1,
        ExtractFaction.Scav => 2,
        ExtractFaction.Transit => 3,
        _ => 4,
    };

    private static void RemoveLegacyFloorBadge(Canvas canvas)
    {
        foreach (var stack in canvas.Children.OfType<StackPanel>())
        {
            // The pinned extract renderer puts the name at index 0 and its large
            // arrow/floor badge at index 1. Floor relation is product-owned now.
            while (stack.Children.Count > 1)
                stack.Children.RemoveAt(stack.Children.Count - 1);
        }
    }

    private static void RestoreFactionBrushStrength(Canvas canvas, ExtractFaction faction)
    {
        var fill = faction switch
        {
            ExtractFaction.Pmc or ExtractFaction.Shared => Color.FromRgb(76, 175, 80),
            ExtractFaction.Scav => Color.FromRgb(158, 158, 158),
            ExtractFaction.Transit => Color.FromRgb(255, 152, 0),
            _ => Color.FromRgb(158, 158, 158),
        };

        foreach (var ellipse in canvas.Children.OfType<Ellipse>())
        {
            // Do not rewrite JunhyunHelper's floor relation ring.
            if (JunhyunFloorPresentation.IsFloorIndicator(ellipse))
                continue;

            if (ellipse.Stroke is not null)
            {
                ellipse.Fill = new SolidColorBrush(fill);
                ellipse.Stroke = Brushes.White;
            }
            else
            {
                ellipse.Fill = new SolidColorBrush(Color.FromArgb(80, fill.R, fill.G, fill.B));
            }
        }

        foreach (var path in canvas.Children.OfType<System.Windows.Shapes.Path>())
            path.Fill = Brushes.White;

        foreach (var stack in canvas.Children.OfType<StackPanel>())
        {
            if (stack.Children.OfType<Border>().FirstOrDefault()?.Child is TextBlock name)
                name.Foreground = new SolidColorBrush(fill);
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;

        _debounceTimer.Stop();
        _tracker.MapChanged -= Tracker_MapChanged;
        if (_floorSelector is not null)
            _floorSelector.SelectionChanged -= FloorSelector_SelectionChanged;
        if (_container is not null)
            _container.LayoutUpdated -= Container_LayoutUpdated;
        _page.Loaded -= Page_Loaded;
    }
}
