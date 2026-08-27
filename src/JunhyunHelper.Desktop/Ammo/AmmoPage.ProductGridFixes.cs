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

        // Ammo presentation setup is owned by AmmoPage itself. Schedule it explicitly
        // instead of relying on a class-level Loaded handler being awakened by an
        // unrelated parent XAML Loaded subscription.
        Dispatcher.BeginInvoke(InitializeProductSearchAndDetails, DispatcherPriority.Loaded);
        Dispatcher.BeginInvoke(ApplyProductGridFixes, DispatcherPriority.Loaded);
    }

    private void ApplyProductGridFixes()
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
