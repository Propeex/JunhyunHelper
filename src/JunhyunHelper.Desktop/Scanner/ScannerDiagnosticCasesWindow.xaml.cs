using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace JunhyunHelper.Desktop.Scanner;

public partial class ScannerDiagnosticCasesWindow : Window
{
    private readonly ObservableCollection<ScannerDiagnosticCaseSummary> _cases = [];

    public ScannerDiagnosticCasesWindow()
    {
        InitializeComponent();
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
        SelectionText.Text = "Case를 선택하면 해당 Case만 삭제할 수 있습니다. 전체 삭제도 일반 Scanner 로그와 직접 내보낸 ZIP에는 영향을 주지 않습니다.";
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
