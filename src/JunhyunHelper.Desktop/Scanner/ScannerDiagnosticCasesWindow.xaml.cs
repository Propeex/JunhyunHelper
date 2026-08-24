using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;

namespace JunhyunHelper.Desktop.Scanner;

public partial class ScannerDiagnosticCasesWindow : Window
{
    private readonly ObservableCollection<ScannerDiagnosticCaseSummary> _cases = [];
    private readonly ScannerCoordinator? _coordinator;
    private bool _openingEditor;
    private bool _exporting;

    public ScannerDiagnosticCasesWindow()
        : this(null)
    {
    }

    public ScannerDiagnosticCasesWindow(ScannerCoordinator? coordinator)
    {
        InitializeComponent();
        _coordinator = coordinator;
        CaseList.ItemsSource = _cases;
        Loaded += (_, _) => Reload();
    }

    public bool DatasetChanged { get; private set; }

    private void Reload()
    {
        _cases.Clear();
        foreach (var item in ScannerDiagnosticCaseBrowser.GetCases())
            _cases.Add(item);

        var reviewed = _cases.Count(item => item.ReviewStatus == "reviewed");
        SummaryText.Text = _cases.Count == 0
            ? "저장된 Case가 없습니다."
            : $"총 {_cases.Count}건 · 사용자 검증 {reviewed}건 · 자동/미검증 {_cases.Count - reviewed}건";
        SelectionText.Text = "Case를 클릭하면 교정 화면이 열립니다. 선택 Case 삭제 또는 전체 데이터 삭제도 여기서 관리할 수 있습니다.";
        DeleteSelectedButton.IsEnabled = false;
        DeleteAllButton.IsEnabled = _cases.Count > 0;
    }

    private void CaseList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CaseList.SelectedItem is not ScannerDiagnosticCaseSummary selected)
        {
            DeleteSelectedButton.IsEnabled = false;
            return;
        }

        DeleteSelectedButton.IsEnabled = true;
        SelectionText.Text =
            $"{selected.TimestampText} · {selected.ReviewText} · 판정 {selected.ResultText} · 정답 {selected.GroundTruthText}";
    }

    private void CaseList_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_openingEditor || CaseList.SelectedItem is not ScannerDiagnosticCaseSummary selected)
            return;
        OpenCaseEditor(selected);
    }

    private void OpenCaseEditor(ScannerDiagnosticCaseSummary selected)
    {
        _openingEditor = true;
        try
        {
            if (!ScannerDiagnosticCaseBrowser.TryLoadCase(selected, out var storedCase, out var error))
            {
                MessageBox.Show(
                    this,
                    error,
                    "Scanner 교정",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            var window = new ScannerCorrectionWindow(storedCase, _coordinator)
            {
                Owner = this,
            };
            window.ShowDialog();
            if (window.DatasetChanged)
            {
                DatasetChanged = true;
                Reload();
            }
        }
        finally
        {
            _openingEditor = false;
        }
    }

    private async void ExportButton_Click(object sender, RoutedEventArgs e)
    {
        if (_exporting)
            return;

        var dialog = new SaveFileDialog
        {
            Title = "Scanner 개발 자료 내보내기",
            Filter = "ZIP 파일 (*.zip)|*.zip",
            AddExtension = true,
            DefaultExt = ".zip",
            FileName = $"JunhyunHelper-Scanner-Diagnostics-{DateTime.Now:yyyyMMdd-HHmmss}.zip",
        };
        if (dialog.ShowDialog(this) != true)
            return;

        _exporting = true;
        ExportButton.IsEnabled = false;
        var previousText = SelectionText.Text;
        SelectionText.Text = "Scanner 교정 데이터와 로그를 ZIP으로 정리하는 중...";
        try
        {
            await ScannerDiagnosticDataset.ExportAsync(dialog.FileName);
            SelectionText.Text = "개발 자료 ZIP을 저장했습니다. 이 파일 하나를 개발 분석용으로 전달하면 됩니다.";
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or NotSupportedException or InvalidDataException)
        {
            App.WriteDiagnostic("Scanner diagnostic export failed", exception);
            SelectionText.Text = previousText;
            MessageBox.Show(
                this,
                "Scanner 개발 자료 ZIP을 저장하지 못했습니다. 기존 교정 데이터는 변경하지 않았습니다.",
                "Scanner 개발 자료 내보내기",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
        finally
        {
            _exporting = false;
            ExportButton.IsEnabled = true;
        }
    }

    private void DeleteSelectedButton_Click(object sender, RoutedEventArgs e)
    {
        if (CaseList.SelectedItem is not ScannerDiagnosticCaseSummary selected)
            return;

        var answer = MessageBox.Show(
            this,
            $"이 Scanner Case만 삭제하시겠습니까?\n\n{selected.CaseId}\n{selected.TimestampText}\n\n삭제 후 dataset index와 통계는 자동으로 다시 생성됩니다.",
            "Scanner Case 삭제",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No);
        if (answer != MessageBoxResult.Yes)
            return;

        DeleteSelectedButton.IsEnabled = false;
        if (!ScannerDiagnosticDataset.DeleteCase(selected.CaseId))
        {
            MessageBox.Show(
                this,
                "선택한 Case를 삭제하지 못했습니다.",
                "Scanner Case 삭제",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            DeleteSelectedButton.IsEnabled = true;
            return;
        }

        DatasetChanged = true;
        Reload();
    }

    private void DeleteAllButton_Click(object sender, RoutedEventArgs e)
    {
        if (_cases.Count == 0)
            return;

        var storage = ScannerDiagnosticDataset.GetStorageInfo();
        var answer = MessageBox.Show(
            this,
            $"저장된 Scanner 교정/진단 데이터 {storage.CaseCount}건 ({storage.SizeText})을 모두 삭제하시겠습니까?\n\n일반 Scanner 로그와 사용자가 이미 내보낸 ZIP은 삭제되지 않습니다.",
            "Scanner 교정 데이터 전체 삭제",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No);
        if (answer != MessageBoxResult.Yes)
            return;

        if (!ScannerDiagnosticDataset.ClearAll())
        {
            MessageBox.Show(
                this,
                "일부 Scanner 교정/진단 데이터를 삭제하지 못했습니다.",
                "Scanner 교정 데이터 삭제",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            Reload();
            return;
        }

        DatasetChanged = true;
        Reload();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
}
