using System.Windows;
using System.Windows.Controls;
using JunhyunHelper.Desktop.Map;
using TarkovHelper.Models;
using TarkovHelper.Models.Map;
using TarkovHelper.Services.Map;

namespace JunhyunHelper.Desktop;

public partial class MainWindow
{
    /// <summary>
    /// Direct regression smoke for the Factory screenshot reported before v0.1.4.
    /// This verifies the actual transplanted Main Map containers, not merely the shared
    /// floor-relation helper in isolation.
    /// </summary>
    private static async Task VerifyFactoryMainMapFloorPresentationAsync(
        TarkovHelper.Pages.Map.MapPage page,
        ComboBox mapSelector,
        ComboBox floorSelector)
    {
        // This method is called while Customs is still selected. Verify the live standard
        // marker container before switching to Factory so the regressions where legacy
        // floor filters or overlap suppression hide enabled off-floor markers cannot return.
        await VerifyCurrentMainMapStandardOffFloorVisibilityAsync(page, floorSelector);

        var extractContainer = page.FindName("ExtractMarkersContainer") as Canvas
            ?? throw new InvalidOperationException("Factory smoke could not find Main Map extract container.");
        var pmcExtractToggle = page.FindName("ChkShowPmcExtracts") as CheckBox
            ?? throw new InvalidOperationException("Factory smoke could not find PMC extract toggle.");
        var scavExtractToggle = page.FindName("ChkShowScavExtracts") as CheckBox
            ?? throw new InvalidOperationException("Factory smoke could not find Scav extract toggle.");

        var factoryIndex = -1;
        for (var index = 0; index < mapSelector.Items.Count; index++)
        {
            if (mapSelector.Items[index] is ComboBoxItem item &&
                string.Equals(item.Tag as string, "Factory", StringComparison.OrdinalIgnoreCase))
            {
                factoryIndex = index;
                break;
            }
        }

        if (factoryIndex < 0)
            throw new InvalidOperationException("Factory was not available for Main Map regression smoke.");

        pmcExtractToggle.IsChecked = true;
        scavExtractToggle.IsChecked = true;
        mapSelector.SelectedIndex = factoryIndex;

        await WaitForAsync(
            () => FindFloorIndex(floorSelector, "main") >= 0 &&
                  FindFloorIndex(floorSelector, "level3") >= 0,
            TimeSpan.FromSeconds(5));

        var mainIndex = FindFloorIndex(floorSelector, "main");
        var level3Index = FindFloorIndex(floorSelector, "level3");
        floorSelector.SelectedIndex = mainIndex;

        await WaitForAsync(
            () => ExtractVisuals(extractContainer, "Gate 3").Count >= 2 &&
                  ExtractVisuals(extractContainer, "Office Window").Count >= 1,
            TimeSpan.FromSeconds(6));

        await WaitForAsync(
            () =>
            {
                var gate = VisibleExtractVisuals(extractContainer, "Gate 3");
                var office = VisibleExtractVisuals(extractContainer, "Office Window");
                return gate.Count == 1 &&
                       office.Count == 1 &&
                       JunhyunFloorPresentation.HasFloorIndicator(gate[0], JunhyunFloorRelation.Current) &&
                       JunhyunFloorPresentation.HasFloorIndicator(office[0], JunhyunFloorRelation.Above);
            },
            TimeSpan.FromSeconds(5));

        var gate3Main = VisibleExtractVisuals(extractContainer, "Gate 3").Single();
        var officeOnMain = VisibleExtractVisuals(extractContainer, "Office Window").Single();
        if (officeOnMain.Tag is not MapExtract officeMainExtract ||
            officeMainExtract.Faction != ExtractFaction.Scav ||
            officeOnMain.Opacity < 0.70)
        {
            throw new InvalidOperationException(
                "Factory Office Window did not preserve Scav identity/above-floor visibility on main. " +
                DescribeExtractVisuals(extractContainer, "Office Window", pmcExtractToggle, scavExtractToggle));
        }

        pmcExtractToggle.IsChecked = false;
        await WaitForAsync(
            () =>
            {
                var visible = VisibleExtractVisuals(extractContainer, "Gate 3");
                return visible.Count == 1 &&
                       visible[0].Tag is MapExtract extract &&
                       extract.Faction == ExtractFaction.Scav;
            },
            TimeSpan.FromSeconds(5));

        pmcExtractToggle.IsChecked = true;
        await WaitForAsync(
            () =>
            {
                var visible = VisibleExtractVisuals(extractContainer, "Gate 3");
                return visible.Count == 1 &&
                       JunhyunFloorPresentation.HasFloorIndicator(visible[0], JunhyunFloorRelation.Current);
            },
            TimeSpan.FromSeconds(5));

        floorSelector.SelectedIndex = level3Index;
        await WaitForAsync(
            () => floorSelector.SelectedIndex == level3Index,
            TimeSpan.FromSeconds(3));

        try
        {
            await WaitForAsync(
                () =>
                {
                    var office = VisibleExtractVisuals(extractContainer, "Office Window");
                    var gate = VisibleExtractVisuals(extractContainer, "Gate 3");
                    return office.Count == 1 &&
                           gate.Count == 1 &&
                           JunhyunFloorPresentation.HasFloorIndicator(office[0], JunhyunFloorRelation.Current) &&
                           office[0].Opacity >= 0.95 &&
                           JunhyunFloorPresentation.HasFloorIndicator(gate[0], JunhyunFloorRelation.Below) &&
                           gate[0].Opacity >= 0.70;
                },
                TimeSpan.FromSeconds(5));
        }
        catch (TimeoutException ex)
        {
            throw new InvalidOperationException(
                "Factory floor presentation did not settle after selecting level3. " +
                DescribeExtractVisuals(extractContainer, "Office Window", pmcExtractToggle, scavExtractToggle) +
                " || " +
                DescribeExtractVisuals(extractContainer, "Gate 3", pmcExtractToggle, scavExtractToggle),
                ex);
        }

        var officeOnLevel3 = VisibleExtractVisuals(extractContainer, "Office Window").Single();
        if (officeOnLevel3.Tag is not MapExtract officeLevel3Extract ||
            officeLevel3Extract.Faction != ExtractFaction.Scav)
        {
            throw new InvalidOperationException("Factory Office Window faction identity changed with floor selection.");
        }
    }

    private static async Task VerifyCurrentMainMapStandardOffFloorVisibilityAsync(
        TarkovHelper.Pages.Map.MapPage page,
        ComboBox floorSelector)
    {
        var markerContainer = page.FindName("MapMarkersContainer") as Canvas
            ?? throw new InvalidOperationException("Main Map standard marker container was not found.");

        await WaitForAsync(
            () => markerContainer.Children.OfType<Canvas>().Any(canvas => canvas.Tag is MapMarker),
            TimeSpan.FromSeconds(6));

        // The pinned shared-floor integration used to run a current-floor-only filter for
        // twelve 200 ms ticks while the Junhyun product recovery window ended earlier.
        // Waiting beyond both windows is essential: a 1-second smoke can pass while the
        // marker is still flickering and then disappear shortly afterward.
        await Task.Delay(3200);

        var mapKey = MapTrackerService.Instance.CurrentMapKey;
        var config = string.IsNullOrWhiteSpace(mapKey)
            ? null
            : MapTrackerService.Instance.GetMapConfig(mapKey);
        var selectedFloor = (floorSelector.SelectedItem as ComboBoxItem)?.Tag as string;

        var knownOffFloor = markerContainer.Children
            .OfType<Canvas>()
            .Where(canvas => canvas.Tag is MapMarker)
            .Select(canvas => new
            {
                Canvas = canvas,
                Marker = (MapMarker)canvas.Tag,
                Relation = JunhyunFloorPresentation.Resolve(
                    config,
                    ((MapMarker)canvas.Tag).FloorId,
                    selectedFloor),
            })
            .Where(item => item.Relation.IsOtherFloor)
            .ToArray();

        if (knownOffFloor.Length == 0)
            throw new InvalidOperationException("Main Map smoke found no enabled known off-floor standard marker to verify.");

        var suppressed = knownOffFloor
            .Where(item =>
                item.Canvas.Visibility != Visibility.Visible ||
                item.Canvas.Opacity < 0.70)
            .ToArray();
        if (suppressed.Length > 0)
        {
            var detail = string.Join(
                " | ",
                suppressed.Take(8).Select(item =>
                    $"type={item.Marker.Type},floor={item.Marker.FloorId}," +
                    $"relation={item.Relation.Relation},visibility={item.Canvas.Visibility}," +
                    $"opacity={item.Canvas.Opacity:F2}"));
            throw new InvalidOperationException(
                "Enabled off-floor standard markers were suppressed after all legacy/product settle windows: " + detail);
        }
    }

    private static int FindFloorIndex(ComboBox floorSelector, string floorId)
    {
        for (var index = 0; index < floorSelector.Items.Count; index++)
        {
            if (floorSelector.Items[index] is ComboBoxItem item &&
                string.Equals(item.Tag as string, floorId, StringComparison.OrdinalIgnoreCase))
            {
                return index;
            }
        }

        return -1;
    }

    private static IReadOnlyList<Canvas> ExtractVisuals(Canvas container, string extractName) =>
        container.Children
            .OfType<Canvas>()
            .Where(canvas =>
                canvas.Tag is MapExtract extract &&
                string.Equals(extract.Name, extractName, StringComparison.OrdinalIgnoreCase))
            .ToArray();

    private static IReadOnlyList<Canvas> VisibleExtractVisuals(Canvas container, string extractName) =>
        ExtractVisuals(container, extractName)
            .Where(canvas => canvas.Visibility == Visibility.Visible && canvas.Opacity > 0.05)
            .ToArray();

    private static string DescribeExtractVisuals(
        Canvas container,
        string extractName,
        CheckBox pmcToggle,
        CheckBox scavToggle)
    {
        var visuals = ExtractVisuals(container, extractName);
        var rows = visuals.Select(canvas =>
        {
            var extract = (MapExtract)canvas.Tag;
            return $"id={extract.Id},faction={extract.Faction},floor={extract.FloorId}," +
                   $"visibility={canvas.Visibility},opacity={canvas.Opacity:F2}," +
                   $"currentRing={JunhyunFloorPresentation.HasFloorIndicator(canvas, JunhyunFloorRelation.Current)}," +
                   $"aboveRing={JunhyunFloorPresentation.HasFloorIndicator(canvas, JunhyunFloorRelation.Above)}," +
                   $"belowRing={JunhyunFloorPresentation.HasFloorIndicator(canvas, JunhyunFloorRelation.Below)}";
        });

        return $"pmcToggle={pmcToggle.IsChecked},scavToggle={scavToggle.IsChecked}," +
               $"containerCount={container.Children.Count},matches={visuals.Count}; " +
               string.Join(" | ", rows);
    }
}
