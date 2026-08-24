using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace JunhyunHelper.Desktop.Scanner;

public partial class ScannerSettingsWindow : Window
{
    private readonly ScannerCoordinator _coordinator;
    private readonly ObservableCollection<MiniInfoRow> _rows = [];
    private ScannerHotkeyGesture _oneShotTarkov;
    private ScannerHotkeyGesture _oneShotTest;
    private ScannerHotkeyGesture _scannerToggle;

    public ScannerSettingsWindow(ScannerCoordinator coordinator)
    {
        InitializeComponent();
        _coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));

        var settings = coordinator.Settings.Clone();
        settings.Normalize();
        _oneShotTarkov = ParseOrDefault(settings.OneShotTarkovHotkey, ScannerHotkeyGesture.DefaultOneShotTarkov);
        _oneShotTest = ParseOrDefault(settings.OneShotTestHotkey, ScannerHotkeyGesture.DefaultOneShotTest);
        _scannerToggle = ParseOrDefault(settings.ScannerToggleHotkey, ScannerHotkeyGesture.DefaultScannerToggle);

        foreach (var key in settings.MiniScannerInfoOrder)
        {
            _rows.Add(new MiniInfoRow(
                key,
                LabelFor(key),
                settings.IsInfoVisible(key)));
        }

        InfoOrderList.ItemsSource = _rows;
        UpdateHotkeySummary();
    }

    private void HotkeySettingsButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ScannerHotkeySettingsWindow(
            _oneShotTarkov.ToString(),
            _oneShotTest.ToString(),
            _scannerToggle.ToString())
        {
            Owner = this,
        };
        if (dialog.ShowDialog() != true)
            return;

        _oneShotTarkov = dialog.OneShotTarkovGesture;
        _oneShotTest = dialog.OneShotTestGesture;
        _scannerToggle = dialog.ScannerToggleGesture;
        UpdateHotkeySummary();
    }

    private void MoveUpButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: MiniInfoRow row })
            return;
        var index = _rows.IndexOf(row);
        if (index <= 0)
            return;
        _rows.Move(index, index - 1);
        InfoOrderList.SelectedItem = row;
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
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        var orderedRows = _rows.ToArray();
        _coordinator.UpdateDisplaySettings(settings =>
        {
            settings.ShowItemName = true;
            settings.ShowItemIcon = true;
            settings.MiniScannerInfoOrder = orderedRows.Select(row => row.Key).ToList();
            foreach (var row in orderedRows)
                settings.SetInfoVisible(row.Key, row.IsVisible);
        });

        _coordinator.SetOneShotTarkovHotkey(_oneShotTarkov);
        _coordinator.SetOneShotTestHotkey(_oneShotTest);
        _coordinator.SetScannerToggleHotkey(_scannerToggle);
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void UpdateHotkeySummary()
    {
        HotkeySummaryText.Text =
            $"1회 인게임: {_oneShotTarkov}   ·   1회 테스트: {_oneShotTest}   ·   ON/OFF: {_scannerToggle}";
    }

    private static ScannerHotkeyGesture ParseOrDefault(string value, ScannerHotkeyGesture fallback) =>
        ScannerHotkeyGesture.TryParse(value, out var gesture) ? gesture : fallback;

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
