using System.Collections.ObjectModel;
using System.Windows;
using JunhyunHelper.Core.Scanner;

namespace JunhyunHelper.Desktop.Scanner;

public partial class ScannerOcrSubstitutionSettingsWindow : Window
{
    private readonly ObservableCollection<ScannerOcrSubstitutionRule> _rules;

    public ScannerOcrSubstitutionSettingsWindow(IEnumerable<ScannerOcrSubstitutionRule> rules)
    {
        ArgumentNullException.ThrowIfNull(rules);
        InitializeComponent();
        _rules = new ObservableCollection<ScannerOcrSubstitutionRule>(
            rules.Select(rule => rule.Clone()));
        RulesList.ItemsSource = _rules;
    }

    public IReadOnlyList<ScannerOcrSubstitutionRule> ResultRules { get; private set; } = [];

    private void AddButton_Click(object sender, RoutedEventArgs e)
    {
        if (_rules.Count >= ScannerOcrSubstitutionEngine.MaximumRules)
        {
            MessageBox.Show(
                this,
                $"문자 치환 규칙은 최대 {ScannerOcrSubstitutionEngine.MaximumRules}개까지 저장할 수 있습니다.",
                "OCR 문자 치환",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        var rule = new ScannerOcrSubstitutionRule();
        _rules.Add(rule);
        RulesList.SelectedItem = rule;
        RulesList.ScrollIntoView(rule);
    }

    private void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (RulesList.SelectedItem is ScannerOcrSubstitutionRule selected)
            _rules.Remove(selected);
    }

    private void ResetButton_Click(object sender, RoutedEventArgs e)
    {
        if (_rules.Count == 0)
            return;

        var result = MessageBox.Show(
            this,
            "등록한 OCR 문자 치환 규칙을 모두 삭제할까요?",
            "OCR 문자 치환 초기화",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question,
            MessageBoxResult.No);
        if (result == MessageBoxResult.Yes)
            _rules.Clear();
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        var invalidIndex = _rules
            .Select((rule, index) => (rule, index))
            .FirstOrDefault(entry =>
                string.IsNullOrEmpty(entry.rule.Source) ||
                entry.rule.Source.Length > ScannerOcrSubstitutionEngine.MaximumSourceLength ||
                entry.rule.Replacement.Length > ScannerOcrSubstitutionEngine.MaximumReplacementLength);

        if (invalidIndex.rule is not null)
        {
            RulesList.SelectedIndex = invalidIndex.index;
            RulesList.ScrollIntoView(invalidIndex.rule);
            MessageBox.Show(
                this,
                "잘못 읽힌 문자/문자열은 비워 둘 수 없습니다. 각 입력은 최대 32자입니다. 바꿀 문자열은 비워 두면 해당 문자를 제거하는 규칙으로 사용할 수 있습니다.",
                "OCR 문자 치환",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        ResultRules = ScannerOcrSubstitutionEngine.NormalizeRules(_rules)
            .Select(rule => rule.Clone())
            .ToArray();
        DialogResult = true;
    }
}
