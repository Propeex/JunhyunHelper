using System.Windows;
using System.Windows.Controls;
using JunhyunHelper.Desktop.Map;
using TarkovHelper.Models;

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

        await Task.Delay(700);

        await WaitForAsync(
            () => VisibleExtractVisuals(extractContainer, "Gate 3").Count == 1,
            TimeSpan.FromSeconds(4));

        var gate3Main = VisibleExtractVisuals(extractContainer, "Gate 3").Single();
        if (!JunhyunFloorPresentation.HasFloorIndicator(gate3Main, JunhyunFloorRelation.Current))
            throw new InvalidOperationException("Factory Gate 3 did not resolve to the current-floor green ring on main.");

        var officeOnMain = VisibleExtractVisuals(extractContainer, "Office Window").SingleOrDefault()
            ?? throw new InvalidOperationException("Factory Office Window was hidden while main floor was selected.");
        if (officeOnMain.Tag is not MapExtract officeMainExtract ||
            officeMainExtract.Faction != ExtractFaction.Scav)
        {
            throw new InvalidOperationException("Factory Office Window no longer carries its Scav faction identity.");
        }
        if (!JunhyunFloorPresentation.HasFloorIndicator(officeOnMain, JunhyunFloorRelation.Above) ||
            officeOnMain.Opacity < 0.70)
        {
            throw new InvalidOperationException("Factory Office Window did not remain visible as an above-floor marker on main.");
        }

        // Gate 3 has PMC and Scav source rows at the same physical exit. Turning PMC off
        // must reveal the Scav representative rather than losing the exit because a source
        // visual was deleted during deduplication.
        pmcExtractToggle.IsChecked = false;
        await WaitForAsync(
            () =>
            {
                var visible = VisibleExtractVisuals(extractContainer, "Gate 3");
                return visible.Count == 1 &&
                       visible[0].Tag is MapExtract extract &&
                       extract.Faction == ExtractFaction.Scav;
            },
            TimeSpan.FromSeconds(4));

        pmcExtractToggle.IsChecked = true;
        await WaitForAsync(
            () => VisibleExtractVisuals(extractContainer, "Gate 3").Count == 1,
            TimeSpan.FromSeconds(4));

        floorSelector.SelectedIndex = level3Index;
        await WaitForAsync(
            () => floorSelector.SelectedIndex == level3Index,
            TimeSpan.FromSeconds(3));
        await Task.Delay(700);

        var officeOnLevel3 = VisibleExtractVisuals(extractContainer, "Office Window").SingleOrDefault()
            ?? throw new InvalidOperationException("Factory Office Window disappeared on its own level3 floor.");
        if (officeOnLevel3.Tag is not MapExtract officeLevel3Extract ||
            officeLevel3Extract.Faction != ExtractFaction.Scav)
        {
            throw new InvalidOperationException("Factory Office Window faction identity changed with floor selection.");
        }
        if (!JunhyunFloorPresentation.HasFloorIndicator(officeOnLevel3, JunhyunFloorRelation.Current) ||
            officeOnLevel3.Opacity < 0.95)
        {
            throw new InvalidOperationException("Factory Office Window did not resolve to the current-floor green ring on level3.");
        }

        var gate3Below = VisibleExtractVisuals(extractContainer, "Gate 3").SingleOrDefault()
            ?? throw new InvalidOperationException("Factory Gate 3 disappeared when level3 was selected.");
        if (!JunhyunFloorPresentation.HasFloorIndicator(gate3Below, JunhyunFloorRelation.Below) ||
            gate3Below.Opacity < 0.70)
        {
            throw new InvalidOperationException("Factory Gate 3 did not remain visible as a below-floor marker on level3.");
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
}
