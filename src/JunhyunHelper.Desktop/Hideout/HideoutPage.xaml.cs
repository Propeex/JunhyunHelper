using System.Windows;
using System.Windows.Controls;
using JunhyunHelper.Application.Hideout;
using JunhyunHelper.Core.Content;

namespace JunhyunHelper.Desktop.Hideout;

public sealed class HideoutLevelChangeRequestedEventArgs(
    string stationId,
    int? level) : EventArgs
{
    public string StationId { get; } = stationId;
    public int? Level { get; } = level;
}

public partial class HideoutPage : UserControl
{
    private GameContentCatalog? _content;
    private HideoutWorkspace? _workspace;
    private IReadOnlyList<StationRow> _rows = [];
    private bool _updatingLevel;

    public HideoutPage()
    {
        InitializeComponent();
    }

    public event EventHandler<HideoutLevelChangeRequestedEventArgs>? LevelChangeRequested;

    public void SetData(GameContentCatalog content, HideoutWorkspace workspace)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(workspace);

        var selectedStationId = (StationList.SelectedItem as StationRow)?.Entry.Station.Id;
        _content = content;
        _workspace = workspace;
        _rows = workspace.Stations
            .Select(entry => new StationRow(
                entry,
                DisplayName(entry.Station.NameKo, entry.Station.NameEn, entry.Station.Id),
                entry.CurrentLevel.HasValue
                    ? $"Lv.{entry.CurrentLevel.Value} / {entry.MaximumLevel}"
                    : "미입력"))
            .OrderBy(row => row.Name, StringComparer.CurrentCulture)
            .ToArray();

        ApplySearch();

        if (!string.IsNullOrWhiteSpace(selectedStationId))
        {
            var selected = (StationList.ItemsSource as IEnumerable<StationRow>)?
                .FirstOrDefault(row => row.Entry.Station.Id == selectedStationId);
            if (selected is not null)
            {
                StationList.SelectedItem = selected;
                StationList.ScrollIntoView(selected);
            }
        }
    }

    public void SetBusy(bool busy) => IsEnabled = !busy;

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => ApplySearch();

    private void ApplySearch()
    {
        if (_workspace is null)
            return;

        var search = SearchBox.Text?.Trim() ?? string.Empty;
        var filtered = _rows
            .Where(row => string.IsNullOrWhiteSpace(search) ||
                          row.Name.Contains(search, StringComparison.CurrentCultureIgnoreCase))
            .ToArray();

        StationList.ItemsSource = filtered;
        SummaryText.Text = $"{filtered.Length}개 표시 · 레벨 입력 {_rows.Count(row => row.Entry.CurrentLevel.HasValue)} / {_rows.Count}";

        if (StationList.SelectedItem is null)
            ClearDetail();
    }

    private void StationList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (StationList.SelectedItem is StationRow row)
            ShowDetail(row.Entry);
        else
            ClearDetail();
    }

    private void ShowDetail(HideoutStationEntry entry)
    {
        if (_content is null)
            return;

        EmptyDetailText.Visibility = Visibility.Collapsed;
        DetailScroll.Visibility = Visibility.Visible;
        DetailName.Text = DisplayName(entry.Station.NameKo, entry.Station.NameEn, entry.Station.Id);

        var levelChoices = new[] { new LevelChoice(null, "미입력") }
            .Concat(Enumerable.Range(0, entry.MaximumLevel + 1)
                .Select(level => new LevelChoice(level, $"Lv.{level}")))
            .ToArray();

        _updatingLevel = true;
        LevelComboBox.ItemsSource = levelChoices;
        LevelComboBox.SelectedItem = levelChoices.First(choice => choice.Level == entry.CurrentLevel);
        _updatingLevel = false;

        if (!entry.CurrentLevel.HasValue)
        {
            NextLevelHeader.Text = "다음 업그레이드";
            NextLevelMessage.Text = "현재 레벨을 입력하면 다음 업그레이드 요구 아이템을 표시합니다.";
            NextLevelItems.ItemsSource = null;
            ConstructionTimeText.Text = string.Empty;
            return;
        }

        if (entry.NextLevel is null)
        {
            NextLevelHeader.Text = "다음 업그레이드";
            NextLevelMessage.Text = "최대 레벨입니다.";
            NextLevelItems.ItemsSource = null;
            ConstructionTimeText.Text = string.Empty;
            return;
        }

        NextLevelHeader.Text = $"Lv.{entry.NextLevel.Level} 업그레이드";
        NextLevelMessage.Text = entry.NextLevel.ItemRequirements.Count > 0
            ? "필요 아이템"
            : "아이템 요구사항이 없습니다.";

        var items = _content.Items.ToDictionary(item => item.Id, StringComparer.Ordinal);
        NextLevelItems.ItemsSource = entry.NextLevel.ItemRequirements
            .Select(requirement =>
            {
                var itemName = items.TryGetValue(requirement.ItemId, out var item)
                    ? DisplayName(item.NameKo, item.NameEn, requirement.ItemId)
                    : requirement.ItemId;
                var fir = requirement.FoundInRaid ? " · FIR" : string.Empty;
                return $"• {itemName} × {requirement.Count}{fir}";
            })
            .ToArray();

        ConstructionTimeText.Text = entry.NextLevel.ConstructionTimeSeconds is > 0
            ? $"건설 시간: {FormatDuration(entry.NextLevel.ConstructionTimeSeconds.Value)}"
            : string.Empty;
    }

    private void LevelComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_updatingLevel || StationList.SelectedItem is not StationRow row ||
            LevelComboBox.SelectedItem is not LevelChoice level)
        {
            return;
        }

        if (row.Entry.CurrentLevel == level.Level)
            return;

        LevelChangeRequested?.Invoke(
            this,
            new HideoutLevelChangeRequestedEventArgs(row.Entry.Station.Id, level.Level));
    }

    private void ClearDetail()
    {
        DetailScroll.Visibility = Visibility.Collapsed;
        EmptyDetailText.Visibility = Visibility.Visible;
    }

    private static string FormatDuration(int seconds)
    {
        var duration = TimeSpan.FromSeconds(seconds);
        if (duration.TotalHours >= 1)
            return $"{(int)duration.TotalHours}시간 {duration.Minutes}분";
        if (duration.TotalMinutes >= 1)
            return $"{(int)duration.TotalMinutes}분";
        return $"{duration.Seconds}초";
    }

    private static string DisplayName(string? korean, string? english, string fallback) =>
        !string.IsNullOrWhiteSpace(korean)
            ? korean
            : !string.IsNullOrWhiteSpace(english)
                ? english
                : fallback;

    private sealed record StationRow(
        HideoutStationEntry Entry,
        string Name,
        string LevelText);

    private sealed record LevelChoice(int? Level, string Label)
    {
        public override string ToString() => Label;
    }
}
