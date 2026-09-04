using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace JunhyunHelper.Desktop.Ammo;

public partial class AmmoPage
{
    protected override void OnInitialized(EventArgs e)
    {
        base.OnInitialized(e);

        // The selector surface is required product UI. Initialize it directly from the
        // page lifecycle so it does not depend on routed Loaded delivery.
        InitializeProductCaliberDropdowns();

        // The remaining presentation setup can run once layout/resources are ready.
        Dispatcher.BeginInvoke(InitializeProductSearchAndDetails, DispatcherPriority.Loaded);
        Dispatcher.BeginInvoke(ApplyProductGridPresentation, DispatcherPriority.Loaded);
    }

    private void ApplyProductGridPresentation()
    {
        var selectedBackground = TryFindResource("BackgroundLightBrush") as Brush
                                 ?? new SolidColorBrush(Color.FromRgb(48, 48, 48));
        var selectedForeground = TryFindResource("TextPrimaryBrush") as Brush
                                 ?? Brushes.White;
        var separator = TryFindResource("BorderBrush") as Brush
                        ?? new SolidColorBrush(Color.FromRgb(70, 70, 70));

        AmmoGrid.GridLinesVisibility = DataGridGridLinesVisibility.All;
        AmmoGrid.HorizontalGridLinesBrush = separator;
        AmmoGrid.VerticalGridLinesBrush = separator;

        // WPF changes a selected DataGrid cell to the system inactive-selection
        // brush when focus moves to another control. Override both active/inactive
        // selection resources locally so the Ammo table always remains dark.
        AmmoGrid.Resources[SystemColors.HighlightBrushKey] = selectedBackground;
        AmmoGrid.Resources[SystemColors.HighlightTextBrushKey] = selectedForeground;
        AmmoGrid.Resources[SystemColors.InactiveSelectionHighlightBrushKey] = selectedBackground;
        AmmoGrid.Resources[SystemColors.InactiveSelectionHighlightTextBrushKey] = selectedForeground;

        var inheritedCellStyle = TryFindResource(typeof(DataGridCell)) as Style;
        var cellStyle = new Style(typeof(DataGridCell), inheritedCellStyle);
        var selectedTrigger = new Trigger
        {
            Property = DataGridCell.IsSelectedProperty,
            Value = true,
        };
        selectedTrigger.Setters.Add(new Setter(DataGridCell.BackgroundProperty, selectedBackground));
        selectedTrigger.Setters.Add(new Setter(DataGridCell.ForegroundProperty, selectedForeground));
        cellStyle.Triggers.Add(selectedTrigger);
        AmmoGrid.CellStyle = cellStyle;
    }
}
