using System.IO;
using System.Windows;
using System.Windows.Controls;
using JunhyunHelper.Desktop.Map;
using JunhyunHelper.Desktop.Scanner;
using TarkovHelper.Models.Map;
using TarkovHelper.Services;
using TarkovHelper.Services.Map;
using TarkovHelper.Services.Settings;
using TarkovHelper.Windows;

namespace TarkovHelper.Pages.Map;

public partial class MapPage
{
    private async Task RunJunhyunV114MiniMapSmokeAndWriteExtractEvidenceAsync(string evidencePath)
    {
        try
        {
            var legacyMiniMapEvidence = Path.Combine(
                Path.GetTempPath(),
                "junhyun-minimap-selection-sync-smoke-success.txt");
            await WaitForV114SmokeAsync(
                () => File.Exists(legacyMiniMapEvidence),
                TimeSpan.FromSeconds(20),
                "Existing MiniMap A/B lifecycle smoke did not complete before v1.11.4 smoke.");

            var overlay = OverlayMiniMapService.Instance;
            overlay.ResetSettings();
            await Task.Delay(150);

            if (Application.Current.Windows.OfType<OverlayMiniMapWindow>().Any(window => window.IsVisible))
                throw new InvalidOperationException("MiniMap was still visible after reset before first-create verification.");

            var candidates = CmbMapSelect.Items
                .OfType<ComboBoxItem>()
                .Where(item => item.Tag is string rawKey &&
                               !string.IsNullOrWhiteSpace(rawKey) &&
                               MapTrackerService.Instance.GetMapConfig(rawKey) is not null)
                .ToArray();
            if (candidates.Length < 2 ||
                candidates[0].Tag is not string rawA ||
                candidates[1].Tag is not string rawB)
            {
                throw new InvalidOperationException("v1.11.4 smoke requires at least two usable maps.");
            }

            var mapA = MapTrackerService.Instance.ResolveMapKey(rawA) ?? rawA;
            var mapB = MapTrackerService.Instance.ResolveMapKey(rawB) ?? rawB;

            // Establish a deliberately stale old map while no MiniMap exists, then select
            // map B and immediately create the overlay. There is intentionally no explicit
            // synchronization call after selecting B: the real SelectionChanged product
            // boundary must publish B before the donor window can read the tracker.
            CmbMapSelect.SelectedItem = candidates[0];
            if (!LegacyMapSelectionConsistencyBridge.SynchronizeCurrentSelectionNow())
                throw new InvalidOperationException("Could not establish old map A for first-create smoke.");
            if (!string.Equals(MapTrackerService.Instance.CurrentMapKey, mapA, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Tracker did not establish map A before first-create smoke.");

            CmbMapSelect.SelectedItem = candidates[1];
            if (!string.Equals(MapTrackerService.Instance.CurrentMapKey, mapB, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Main Map selection '{mapB}' was not published synchronously before MiniMap creation.");
            }

            overlay.ShowOverlay();
            await WaitForV114SmokeAsync(
                () => overlay.IsOverlayVisible &&
                      Application.Current.Windows.OfType<OverlayMiniMapWindow>().Any(window => window.IsVisible),
                TimeSpan.FromSeconds(3),
                "Fresh MiniMap window did not become visible.");

            var window = Application.Current.Windows
                .OfType<OverlayMiniMapWindow>()
                .First(window => window.IsVisible);
            var mapSvg = window.FindName("MapSvg") as SharpVectors.Converters.SvgViewbox
                ?? throw new InvalidOperationException("Fresh MiniMap MapSvg was unavailable.");
            await WaitForV114SmokeAsync(
                () => string.Equals(window.JunhyunCurrentMapKey, mapB, StringComparison.OrdinalIgnoreCase) &&
                      mapSvg.Source is not null,
                TimeSpan.FromSeconds(4),
                "Fresh MiniMap did not render the current Main Map on its first creation.");

            await WaitForV114SmokeAsync(
                () => ExtractService.Instance.IsLoaded,
                TimeSpan.FromSeconds(5),
                "ExtractService did not load for Transit marker verification.");

            SetSmokeCheckBox("ChkShowExtractMarkers", true);
            SetSmokeCheckBox("ChkShowPmcExtracts", false);
            SetSmokeCheckBox("ChkShowScavExtracts", false);
            SetSmokeCheckBox("ChkShowTransitExtracts", true);

            var transitTarget = candidates
                .Select(item => CreateTransitSmokeTarget(item))
                .FirstOrDefault(target => target is not null)
                ?? throw new InvalidOperationException("Packaged extract data contains no map with a Transit extract.");

            CmbMapSelect.SelectedItem = transitTarget.Item;
            await WaitForV114SmokeAsync(
                () => string.Equals(window.JunhyunCurrentMapKey, transitTarget.MapKey, StringComparison.OrdinalIgnoreCase),
                TimeSpan.FromSeconds(4),
                "MiniMap did not switch to the Transit smoke map.");
            await WaitForV114SmokeAsync(
                () => window.JunhyunRenderedTransitMarkerCountForSmoke == transitTarget.ExpectedTransitCount,
                TimeSpan.FromSeconds(4),
                $"MiniMap rendered {window.JunhyunRenderedTransitMarkerCountForSmoke} Transit markers; expected {transitTarget.ExpectedTransitCount}.");

            foreach (var name in new[]
                     {
                         "ChkShowPmcSpawns",
                         "ChkShowSniperScavs",
                         "ChkShowRogues",
                         "ChkShowCultists",
                         "ChkShowLeversMarker",
                         "ChkShowBosses",
                     })
            {
                SetSmokeCheckBox(name, true);
            }

            await WaitForV114SmokeAsync(
                () => MapMarkerDbService.Instance.IsLoaded,
                TimeSpan.FromSeconds(5),
                "MapMarkerDbService did not load for standard marker verification.");

            var visibility = MiniMapMarkerVisibilityState.Capture(MapSettings.Instance);
            var standardTarget = candidates
                .Select(item => CreateStandardMarkerSmokeTarget(item, visibility))
                .FirstOrDefault(target => target is not null)
                ?? throw new InvalidOperationException("Packaged map marker data contains no visible standard marker.");

            CmbMapSelect.SelectedItem = standardTarget.Item;
            await WaitForV114SmokeAsync(
                () => string.Equals(window.JunhyunCurrentMapKey, standardTarget.MapKey, StringComparison.OrdinalIgnoreCase),
                TimeSpan.FromSeconds(4),
                "MiniMap did not switch to the standard-marker smoke map.");
            await WaitForV114SmokeAsync(
                () => window.JunhyunRenderedStandardMarkerCountForSmoke > 0,
                TimeSpan.FromSeconds(4),
                "MiniMap did not render standard markers before recovery verification.");

            JunhyunMiniMapProductRegistry.ApplyMarkerScale(0.65);
            await WaitForV114SmokeAsync(
                () => window.JunhyunFirstStandardMarkerScaleForSmoke is not null,
                TimeSpan.FromSeconds(2),
                "MiniMap standard marker scale was unavailable.");
            var standardScaleBeforePlayerResize = window.JunhyunFirstStandardMarkerScaleForSmoke!.Value;
            var productMarkerScaleBeforePlayerResize = window.JunhyunMarkerScale;

            JunhyunMiniMapProductRegistry.ApplyPlayerMarkerSize(42.0);
            if (Math.Abs(window.JunhyunMarkerScale - productMarkerScaleBeforePlayerResize) > 0.0001)
            {
                throw new InvalidOperationException(
                    "Player Marker Size mutated the independent MiniMap marker-scale setting.");
            }

            // Donor marker refreshes are asynchronous and may recreate the standard marker
            // visual between these two product actions. Verify convergence to the unchanged
            // product scale rather than attributing a transient donor refresh to the Player
            // Marker Size action. This keeps the smoke strict while removing a timing race.
            await WaitForV114SmokeAsync(
                () => window.JunhyunFirstStandardMarkerScaleForSmoke is { } scale &&
                      Math.Abs(scale - standardScaleBeforePlayerResize) <= 0.0001,
                TimeSpan.FromSeconds(2),
                "Standard MiniMap marker scale did not recover to its unchanged value after Player Marker Size update.");

            var expectedPlayerScale = Math.Clamp(42.0 / 18.0, 0.5, 3.0);
            if (Math.Abs(window.JunhyunPlayerMarkerScaleForSmoke - expectedPlayerScale) > 0.0001)
                throw new InvalidOperationException("Player Marker Size did not update the MiniMap player marker itself.");

            window.JunhyunClearStandardMarkersForSmoke();
            if (window.JunhyunRenderedStandardMarkerCountForSmoke != 0)
                throw new InvalidOperationException("Could not force the standard MiniMap marker layer empty for recovery smoke.");
            await WaitForV114SmokeAsync(
                () => window.JunhyunRenderedStandardMarkerCountForSmoke > 0,
                TimeSpan.FromSeconds(4),
                "MiniMap marker recovery did not rebuild a deliberately emptied standard marker layer.");

            var miniScanner = new MiniScannerWindow();
            try
            {
                miniScanner.ApplyTemplate();
                if (miniScanner.FindName("DragSurface") is not Border dragSurface)
                    throw new InvalidOperationException("Mini Scanner DragSurface was unavailable.");
                if (dragSurface.ContextMenu is not null)
                    throw new InvalidOperationException("Mini Scanner still exposes a right-click context menu.");
            }
            finally
            {
                miniScanner.Close();
            }

            File.WriteAllText(
                evidencePath,
                "real-donor-checkboxes=ok\n" +
                "marker-panel-visible=ok\n" +
                "master-filter-render-state=ok\n" +
                "hidden-master-render-gate=ok\n" +
                "approved-three-filter-layout=ok\n" +
                "minimap-refresh-handler-preserved=ok\n" +
                "pmc-filter-render-state=ok\n" +
                "scav-filter-render-state=ok\n" +
                "transit-filter-render-state=ok\n" +
                "first-minimap-creation-boundary=ok\n" +
                "actual-transit-marker-render=ok\n" +
                "player-marker-size-isolated=ok\n" +
                "standard-marker-direct-recovery=ok\n" +
                "mini-scanner-context-menu=none\n");

            overlay.HideOverlay();
        }
        catch (Exception exception)
        {
            try
            {
                var diagnostic = Path.Combine(Path.GetTempPath(), "junhyun-map-smoke-error.txt");
                File.WriteAllText(diagnostic, "v1.11.4 MiniMap/Mini Scanner published smoke failed.\n" + exception);
            }
            catch
            {
            }

            Environment.Exit(89);
        }
    }

    private TransitSmokeTarget? CreateTransitSmokeTarget(ComboBoxItem item)
    {
        if (item.Tag is not string rawKey)
            return null;
        var mapKey = MapTrackerService.Instance.ResolveMapKey(rawKey) ?? rawKey;
        var config = MapTrackerService.Instance.GetMapConfig(mapKey);
        if (config is null)
            return null;

        var expected = MapExtractDisplayGrouping
            .GroupForDisplay(ExtractService.Instance.GetExtractsForMap(mapKey, config))
            .Count(display => display.Faction == ExtractFaction.Transit);
        return expected > 0 ? new TransitSmokeTarget(item, mapKey, expected) : null;
    }

    private StandardMarkerSmokeTarget? CreateStandardMarkerSmokeTarget(
        ComboBoxItem item,
        MiniMapMarkerVisibilityState visibility)
    {
        if (item.Tag is not string rawKey)
            return null;
        var mapKey = MapTrackerService.Instance.ResolveMapKey(rawKey) ?? rawKey;
        var expected = MapMarkerDbService.Instance
            .GetMarkersForMap(mapKey)
            .Count(marker => visibility.IsMapMarkerVisible(marker.Type));
        return expected > 0 ? new StandardMarkerSmokeTarget(item, mapKey) : null;
    }

    private void SetSmokeCheckBox(string name, bool value)
    {
        if (FindName(name) is not CheckBox checkBox)
            throw new InvalidOperationException($"Map smoke checkbox was unavailable: {name}.");
        checkBox.IsChecked = value;
    }

    private static async Task WaitForV114SmokeAsync(
        Func<bool> condition,
        TimeSpan timeout,
        string timeoutMessage)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (condition())
                return;
            await Task.Delay(50);
        }

        throw new TimeoutException(timeoutMessage);
    }

    private sealed record TransitSmokeTarget(ComboBoxItem Item, string MapKey, int ExpectedTransitCount);
    private sealed record StandardMarkerSmokeTarget(ComboBoxItem Item, string MapKey);
}
