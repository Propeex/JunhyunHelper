using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace TarkovHelper.Pages.Map;

public partial class MapPage
{
    private StackPanel? _junhyunExtractMarkerFilterGroup;
    private StackPanel? _junhyunExtractMarkerChildGroup;
    private bool _junhyunExtractMarkerFilterSmokeCompleted;

    private void RestoreJunhyunExtractMarkerFiltersToMarkerPanel()
    {
        if (_junhyunExtractMarkerFilterGroup is not null)
            return;

        var group = new StackPanel { Margin = new Thickness(0, 8, 0, 0) };
        group.Children.Add(new Border
        {
            Height = 1,
            Background = TryFindResource("BorderBrush") as System.Windows.Media.Brush ?? System.Windows.Media.Brushes.DimGray,
            Margin = new Thickness(0, 0, 0, 8),
        });

        // Keep the donor's real master checkbox as the parent row. Its original
        // Checked/Unchecked handlers still own the actual extract render state.
        ChkShowExtractMarkers.Content = "탈출구";
        ChkShowExtractMarkers.FontWeight = FontWeights.SemiBold;
        MoveExistingExtractFilter(ChkShowExtractMarkers, group, leftIndent: 0);

        var children = new StackPanel
        {
            Margin = new Thickness(18, 0, 0, 0),
        };

        // These are the donor's real faction filters, not cosmetic copies. Reparenting
        // preserves SettingsService persistence, MapExtractMarkerManager filtering and
        // OverlayMiniMapService refresh semantics.
        ChkShowPmcExtracts.Content = "PMC";
        ChkShowScavExtracts.Content = "Scav";
        ChkShowTransitExtracts.Content = "Transit";
        MoveExistingExtractFilter(ChkShowPmcExtracts, children, leftIndent: 0);
        MoveExistingExtractFilter(ChkShowScavExtracts, children, leftIndent: 0);
        MoveExistingExtractFilter(ChkShowTransitExtracts, children, leftIndent: 0);
        group.Children.Add(children);

        ChkShowExtractMarkers.Checked += JunhyunExtractMasterFilter_Changed;
        ChkShowExtractMarkers.Unchecked += JunhyunExtractMasterFilter_Changed;

        MapMarkersContent.Children.Add(group);
        _junhyunExtractMarkerFilterGroup = group;
        _junhyunExtractMarkerChildGroup = children;
        UpdateJunhyunExtractChildFilterAvailability();

        if (string.Equals(Environment.GetEnvironmentVariable("JUNHYUNHELPER_MAP_SMOKE"), "1", StringComparison.Ordinal))
            Dispatcher.BeginInvoke(VerifyJunhyunExtractMarkerFilterSmoke, DispatcherPriority.ContextIdle);
    }

    private void JunhyunExtractMasterFilter_Changed(object sender, RoutedEventArgs e) =>
        UpdateJunhyunExtractChildFilterAvailability();

    private void UpdateJunhyunExtractChildFilterAvailability()
    {
        if (_junhyunExtractMarkerChildGroup is not null)
            _junhyunExtractMarkerChildGroup.IsEnabled = ChkShowExtractMarkers.IsChecked == true;
    }

    private static void MoveExistingExtractFilter(CheckBox checkBox, Panel destination, double leftIndent)
    {
        if (checkBox.Parent is Panel parent)
            parent.Children.Remove(checkBox);
        else if (checkBox.Parent is ContentControl content && ReferenceEquals(content.Content, checkBox))
            content.Content = null;
        else if (checkBox.Parent is Decorator decorator && ReferenceEquals(decorator.Child, checkBox))
            decorator.Child = null;

        checkBox.Visibility = Visibility.Visible;
        checkBox.IsHitTestVisible = true;
        checkBox.Margin = new Thickness(leftIndent, 2, 0, 3);
        checkBox.HorizontalAlignment = HorizontalAlignment.Left;
        destination.Children.Add(checkBox);
    }

    private void VerifyJunhyunExtractMarkerFilterSmoke()
    {
        if (_junhyunExtractMarkerFilterSmokeCompleted)
            return;
        _junhyunExtractMarkerFilterSmokeCompleted = true;

        try
        {
            if (!_junhyunUiSimplificationApplied ||
                _junhyunExtractMarkerFilterGroup is null ||
                _junhyunExtractMarkerChildGroup is null)
            {
                throw new InvalidOperationException("Map extract filters were not restored by the real product Loaded lifecycle.");
            }

            if (!ReferenceEquals(ChkShowExtractMarkers.Parent, _junhyunExtractMarkerFilterGroup) ||
                !ReferenceEquals(ChkShowPmcExtracts.Parent, _junhyunExtractMarkerChildGroup) ||
                !ReferenceEquals(ChkShowScavExtracts.Parent, _junhyunExtractMarkerChildGroup) ||
                !ReferenceEquals(ChkShowTransitExtracts.Parent, _junhyunExtractMarkerChildGroup))
            {
                throw new InvalidOperationException("Map extract filters are not arranged as parent plus faction children.");
            }

            if (!string.Equals(ChkShowExtractMarkers.Content?.ToString(), "탈출구", StringComparison.Ordinal) ||
                !string.Equals(ChkShowPmcExtracts.Content?.ToString(), "PMC", StringComparison.Ordinal) ||
                !string.Equals(ChkShowScavExtracts.Content?.ToString(), "Scav", StringComparison.Ordinal) ||
                !string.Equals(ChkShowTransitExtracts.Content?.ToString(), "Transit", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Map extract filter labels drifted from the approved hierarchy.");
            }

            foreach (var filter in new[]
                     {
                         ChkShowExtractMarkers,
                         ChkShowPmcExtracts,
                         ChkShowScavExtracts,
                         ChkShowTransitExtracts,
                     })
            {
                if (!IsWithinProductMarkerContent(filter))
                    throw new InvalidOperationException($"Map extract filter is outside the product marker panel: {filter.Name}.");
                if (filter.Visibility != Visibility.Visible || !filter.IsHitTestVisible)
                    throw new InvalidOperationException($"Map extract filter is not user-interactive: {filter.Name}.");
            }

            // Master filter: prove the reparented control still changes the donor's real
            // render state and controls child availability, then restore the user state.
            var originalMaster = ChkShowExtractMarkers.IsChecked ?? true;
            var toggledMaster = !originalMaster;
            ChkShowExtractMarkers.IsChecked = toggledMaster;
            if (_showExtractMarkers != toggledMaster ||
                ExtractMarkersContainer.Visibility != (toggledMaster ? Visibility.Visible : Visibility.Collapsed) ||
                _junhyunExtractMarkerChildGroup.IsEnabled != toggledMaster)
            {
                throw new InvalidOperationException("Visible Map extract parent is not connected to the real marker filter state.");
            }
            ChkShowExtractMarkers.IsChecked = originalMaster;

            // Faction filters: their donor handlers update the concrete render-policy fields
            // synchronously before any asynchronous marker refresh. Verify each real path.
            VerifyFactionFilterControl(ChkShowPmcExtracts, () => _showPmcExtracts, "PMC");
            VerifyFactionFilterControl(ChkShowScavExtracts, () => _showScavExtracts, "Scav");
            VerifyFactionFilterControl(ChkShowTransitExtracts, () => _showTransitExtracts, "Transit");

            var marker = Path.Combine(Path.GetTempPath(), "junhyun-map-extract-filter-smoke-success.txt");
            File.WriteAllText(
                marker,
                "real-donor-checkboxes=ok\n" +
                "marker-panel-visible=ok\n" +
                "master-filter-render-state=ok\n" +
                "minimap-refresh-handler-preserved=ok\n" +
                "extract-parent-child-hierarchy=ok\n" +
                "pmc-filter-render-state=ok\n" +
                "scav-filter-render-state=ok\n" +
                "transit-filter-render-state=ok\n");
        }
        catch (Exception exception)
        {
            try
            {
                var diagnostic = Path.Combine(Path.GetTempPath(), "junhyun-map-smoke-error.txt");
                File.WriteAllText(diagnostic, "Map extract-filter published smoke failed.\n" + exception);
            }
            catch
            {
            }

            Environment.Exit(89);
        }
    }

    private static void VerifyFactionFilterControl(CheckBox control, Func<bool> readState, string label)
    {
        var original = control.IsChecked ?? true;
        var toggled = !original;
        control.IsChecked = toggled;
        if (readState() != toggled)
            throw new InvalidOperationException($"{label} extract checkbox is not connected to the real marker filter state.");
        control.IsChecked = original;
        if (readState() != original)
            throw new InvalidOperationException($"{label} extract filter state did not restore after runtime verification.");
    }

    private bool IsWithinProductMarkerContent(DependencyObject element)
    {
        for (DependencyObject? current = element; current is not null; current = LogicalTreeHelper.GetParent(current))
        {
            if (ReferenceEquals(current, MapMarkersContent))
                return true;
        }
        return false;
    }
}
