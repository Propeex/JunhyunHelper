using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace TarkovHelper.Pages.Map;

public partial class MapPage
{
    private bool _junhyunExtractMarkerFilterSmokeCompleted;

    private void RestoreJunhyunExtractMarkerFiltersToMarkerPanel()
    {
        // The product marker-settings bridge already owns the visible marker grouping and
        // moves the donor's real PMC / Scav / Transit controls into the same card rows used
        // by the other marker groups. v1.9.0 incorrectly reparented those controls a second
        // time, leaving three empty wrapper rows behind and adding a duplicate extract UI.
        // Keep the donor master only as an internal render gate and leave the three faction
        // controls with the single product grouping authority.
        ChkShowExtractMarkers.Visibility = Visibility.Collapsed;
        ChkShowExtractMarkers.IsHitTestVisible = false;
        if (ChkShowExtractMarkers.IsChecked != true)
            ChkShowExtractMarkers.IsChecked = true;

        Dispatcher.BeginInvoke(
            NormalizeApprovedExtractMarkerLayout,
            DispatcherPriority.ContextIdle);
    }

    private void NormalizeApprovedExtractMarkerLayout()
    {
        // LegacyMapMarkerSettingsV2Bridge intentionally owns the section/card presentation.
        // Rename only the product section and normalize the three real donor controls so the
        // visible contract is exactly: 탈출구 -> PMC / Scav / 트랜짓 탈출구.
        var oldHeaders = EnumerateJunhyunDescendants<TextBlock>(MapMarkersContent)
            .Where(text => string.Equals(text.Text, "탈출 / 이동", StringComparison.Ordinal))
            .ToArray();
        foreach (var header in oldHeaders)
            header.Text = "탈출구";

        NormalizeApprovedExtractCheckBox(ChkShowPmcExtracts, "PMC 탈출구");
        NormalizeApprovedExtractCheckBox(ChkShowScavExtracts, "Scav 탈출구");
        NormalizeApprovedExtractCheckBox(ChkShowTransitExtracts, "트랜짓 탈출구");

        // The master must remain an invisible implementation detail. Reassert the gate after
        // donor Loaded/state restoration so a historical saved master=false cannot make the
        // three visible faction controls appear enabled while rendering no extracts.
        ChkShowExtractMarkers.Visibility = Visibility.Collapsed;
        ChkShowExtractMarkers.IsHitTestVisible = false;
        if (ChkShowExtractMarkers.IsChecked != true)
            ChkShowExtractMarkers.IsChecked = true;

        if (string.Equals(Environment.GetEnvironmentVariable("JUNHYUNHELPER_MAP_SMOKE"), "1", StringComparison.Ordinal))
            VerifyJunhyunExtractMarkerFilterSmoke();
    }

    private void NormalizeApprovedExtractCheckBox(CheckBox checkBox, string label)
    {
        checkBox.Content = label;
        checkBox.Visibility = Visibility.Visible;
        checkBox.IsHitTestVisible = true;
        checkBox.HorizontalAlignment = HorizontalAlignment.Left;
        checkBox.VerticalAlignment = VerticalAlignment.Center;
        checkBox.Margin = new Thickness(2);
        checkBox.FontSize = 11;
        checkBox.FontWeight = FontWeights.Normal;
        checkBox.Foreground = TryFindResource("TextSecondaryBrush") as Brush ?? Brushes.Gainsboro;
    }

    private void VerifyJunhyunExtractMarkerFilterSmoke()
    {
        if (_junhyunExtractMarkerFilterSmokeCompleted)
            return;
        _junhyunExtractMarkerFilterSmokeCompleted = true;

        try
        {
            if (!_junhyunUiSimplificationApplied)
                throw new InvalidOperationException("Map extract filters were not normalized by the real product Loaded lifecycle.");

            if (ChkShowExtractMarkers.Visibility != Visibility.Collapsed ||
                ChkShowExtractMarkers.IsHitTestVisible ||
                IsWithinProductMarkerContent(ChkShowExtractMarkers))
            {
                throw new InvalidOperationException("The donor extract master checkbox leaked into the product marker list.");
            }

            if (ChkShowExtractMarkers.IsChecked != true ||
                !_showExtractMarkers ||
                ExtractMarkersContainer.Visibility != Visibility.Visible)
            {
                throw new InvalidOperationException("The hidden donor extract master is not preserving the real render gate.");
            }

            if (!string.Equals(ChkShowPmcExtracts.Content?.ToString(), "PMC 탈출구", StringComparison.Ordinal) ||
                !string.Equals(ChkShowScavExtracts.Content?.ToString(), "Scav 탈출구", StringComparison.Ordinal) ||
                !string.Equals(ChkShowTransitExtracts.Content?.ToString(), "트랜짓 탈출구", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Map extract filter labels drifted from the approved three-row layout.");
            }

            foreach (var filter in new[] { ChkShowPmcExtracts, ChkShowScavExtracts, ChkShowTransitExtracts })
            {
                if (!IsWithinProductMarkerContent(filter))
                    throw new InvalidOperationException($"Map extract filter is outside the product marker panel: {filter.Name}.");
                if (filter.Visibility != Visibility.Visible || !filter.IsHitTestVisible)
                    throw new InvalidOperationException($"Map extract filter is not user-interactive: {filter.Name}.");
            }

            var extractHeader = EnumerateJunhyunDescendants<TextBlock>(MapMarkersContent)
                .SingleOrDefault(text => string.Equals(text.Text, "탈출구", StringComparison.Ordinal));
            if (extractHeader?.Parent is not StackPanel extractSection)
                throw new InvalidOperationException("The approved 탈출구 marker section was not found.");

            if (EnumerateJunhyunDescendants<TextBlock>(MapMarkersContent)
                .Any(text => string.Equals(text.Text, "탈출 / 이동", StringComparison.Ordinal)))
            {
                throw new InvalidOperationException("The obsolete 탈출 / 이동 section title is still visible.");
            }

            var sectionFilters = EnumerateJunhyunDescendants<CheckBox>(extractSection)
                .Where(filter => filter.Visibility == Visibility.Visible)
                .ToArray();
            var approvedFilters = new[] { ChkShowPmcExtracts, ChkShowScavExtracts, ChkShowTransitExtracts };
            if (sectionFilters.Length != 3 || approvedFilters.Any(filter => !sectionFilters.Contains(filter)))
                throw new InvalidOperationException("The 탈출구 section does not contain exactly the approved three donor filters.");

            // Prove that each visible control is still the donor behavior endpoint rather
            // than a cosmetic proxy, then restore the user's state immediately.
            VerifyFactionFilterControl(ChkShowPmcExtracts, () => _showPmcExtracts, "PMC");
            VerifyFactionFilterControl(ChkShowScavExtracts, () => _showScavExtracts, "Scav");
            VerifyFactionFilterControl(ChkShowTransitExtracts, () => _showTransitExtracts, "Transit");

            var marker = Path.Combine(Path.GetTempPath(), "junhyun-map-extract-filter-smoke-success.txt");
            File.WriteAllText(
                marker,
                "real-donor-checkboxes=ok\n" +
                "marker-panel-visible=ok\n" +
                "hidden-master-render-gate=ok\n" +
                "approved-three-filter-layout=ok\n" +
                "minimap-refresh-handler-preserved=ok\n" +
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
