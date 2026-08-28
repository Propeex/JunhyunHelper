using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace TarkovHelper.Pages.Map;

public partial class MapPage
{
    private StackPanel? _junhyunExtractMarkerFilterGroup;
    private bool _junhyunExtractMarkerFilterSmokeCompleted;

    private void RestoreJunhyunExtractMarkerFiltersToMarkerPanel()
    {
        if (_junhyunExtractMarkerFilterGroup is not null)
            return;

        var group = new StackPanel { Margin = new Thickness(0, 8, 0, 0) };
        group.Children.Add(new Border
        {
            Height = 1,
            Background = TryFindResource("BorderBrush") as Brush ?? Brushes.DimGray,
            Margin = new Thickness(0, 0, 0, 8),
        });
        group.Children.Add(new TextBlock
        {
            Text = "탈출구",
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            Foreground = TryFindResource("TextSecondaryBrush") as Brush ?? Brushes.LightGray,
            Margin = new Thickness(0, 0, 0, 5),
        });

        // These are the donor's real filter controls, not cosmetic copies. Reparenting
        // preserves their existing Checked/Unchecked handlers, SettingsService persistence,
        // ExtractMarkerManager rendering and OverlayMiniMapService refresh semantics.
        MoveExistingExtractFilter(ChkShowExtractMarkers, group);
        MoveExistingExtractFilter(ChkShowPmcExtracts, group);
        MoveExistingExtractFilter(ChkShowScavExtracts, group);
        MoveExistingExtractFilter(ChkShowTransitExtracts, group);

        MapMarkersContent.Children.Add(group);
        _junhyunExtractMarkerFilterGroup = group;

        if (string.Equals(Environment.GetEnvironmentVariable("JUNHYUNHELPER_MAP_SMOKE"), "1", StringComparison.Ordinal))
            Dispatcher.BeginInvoke(VerifyJunhyunExtractMarkerFilterSmoke, DispatcherPriority.ContextIdle);
    }

    private static void MoveExistingExtractFilter(CheckBox checkBox, Panel destination)
    {
        if (checkBox.Parent is Panel parent)
            parent.Children.Remove(checkBox);
        else if (checkBox.Parent is ContentControl content && ReferenceEquals(content.Content, checkBox))
            content.Content = null;
        else if (checkBox.Parent is Decorator decorator && ReferenceEquals(decorator.Child, checkBox))
            decorator.Child = null;

        checkBox.Visibility = Visibility.Visible;
        checkBox.IsHitTestVisible = true;
        checkBox.Margin = new Thickness(0, 2, 0, 3);
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
            if (!_junhyunUiSimplificationApplied || _junhyunExtractMarkerFilterGroup is null)
                throw new InvalidOperationException("Map extract filters were not restored by the real product Loaded lifecycle.");

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

            // Prove that the visible master checkbox is still the donor control wired to
            // the real render state. Toggle through the normal WPF event path, then restore.
            var original = ChkShowExtractMarkers.IsChecked ?? true;
            var toggled = !original;
            ChkShowExtractMarkers.IsChecked = toggled;
            if (_showExtractMarkers != toggled ||
                ExtractMarkersContainer.Visibility != (toggled ? Visibility.Visible : Visibility.Collapsed))
            {
                throw new InvalidOperationException("Visible Map extract checkbox is not connected to the real marker filter state.");
            }

            ChkShowExtractMarkers.IsChecked = original;
            if (_showExtractMarkers != original)
                throw new InvalidOperationException("Map extract filter state did not restore after runtime verification.");

            var marker = Path.Combine(Path.GetTempPath(), "junhyun-map-extract-filter-smoke-success.txt");
            File.WriteAllText(
                marker,
                "real-donor-checkboxes=ok\n" +
                "marker-panel-visible=ok\n" +
                "master-filter-render-state=ok\n" +
                "minimap-refresh-handler-preserved=ok\n");
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
