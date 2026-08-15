using System.Windows;
using System.Windows.Controls;
using JunhyunHelper.Desktop.Map;
using TarkovHelper.Models.Map;

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
