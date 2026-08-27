using System.Collections.ObjectModel;
using System.Windows;

namespace JunhyunHelper.Desktop.Scanner;

public partial class ScannerSettingsWindow : Window
{
    private readonly ScannerCoordinator _coordinator;
    private readonly ObservableCollection<MiniInfoRow> _rows = [];

    public ScannerSettingsWindow(ScannerCoordinator coordinator)
    {
        InitializeComponent();
        _coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));

        var settings = coordinator.Settings.Clone();
        settings.Normalize();
        foreach (var key in settings.MiniScannerInfoOrder)
        {
            _rows.Add(new MiniInfoRow(
                key,
                LabelFor(key),
                settings.IsInfoVisible(key)));
        }

        InfoOrderList.ItemsSource = _rows;
    }

    private void InfoVisibilityCheckBox_Click(object sender, RoutedEventArgs e) => SaveDisplaySettings();

    private void MoveUpButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: MiniInfoRow row })
            return;
        var index = _rows.IndexOf(row);
        if (index <= 0)
            return;
        _rows.Move(index, index - 1);
        InfoOrderList.SelectedItem = row;
        SaveDisplaySettings();
    }

    private void MoveDownButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: MiniInfoRow row })
            return;
        var index = _rows.IndexOf(row);
        if (index < 0 || index >= _rows.Count - 1)
            return;
        _rows.Move(index, index + 1);
        InfoOrderList.SelectedItem = row;
        SaveDisplaySettings();
    }

    private void SaveDisplaySettings()
    {
        var orderedRows = _rows.ToArray();
        _coordinator.UpdateDisplaySettings(settings =>
        {
            // Identity is always present in Mini Scanner and is no longer exposed as
            // configurable/fixed explanatory rows in this settings surface.
            settings.ShowItemName = true;
            settings.ShowItemIcon = true;
            settings.MiniScannerInfoOrder = orderedRows.Select(row => row.Key).ToList();
            foreach (var row in orderedRows)
                settings.SetInfoVisible(row.Key, row.IsVisible);
        });
    }

    private static string LabelFor(string key) => key switch
    {
        ScannerDisplaySettings.TraderSellPriceField => "상인 판매가",
        ScannerDisplaySettings.FleaAveragePriceField => "플리마켓 평균가",
        ScannerDisplaySettings.TraderPricePerSlotField => "상점가 / 칸",
        ScannerDisplaySettings.FleaPricePerSlotField => "플리 평균가 / 칸",
        ScannerDisplaySettings.CurrentNeededField => "필요 개수",
        _ => key,
    };

    private sealed class MiniInfoRow(string key, string label, bool isVisible)
    {
        public string Key { get; } = key;
        public string Label { get; } = label;
        public bool IsVisible { get; set; } = isVisible;
    }
}
