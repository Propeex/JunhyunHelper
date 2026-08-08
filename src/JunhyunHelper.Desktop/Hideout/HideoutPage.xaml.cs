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
            .Select(entry =>
            {
                var currentLevel = entry.CurrentLevel ?? 0;
                return new StationRow(
                    entry,
                    DisplayName(entry.Station.NameKo, entry.Station.NameEn, entry.Station.Id),
                    $"Lv.{currentLevel} / {entry.MaximumLevel}");
            })
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
        SummaryText.Text = $"{filtered.Length}개 시설";

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

        var currentLevel = entry.CurrentLevel ?? 0;
        CurrentLevelText.Text = $"Lv.{currentLevel} / {entry.MaximumLevel}";
        LevelMinusButton.IsEnabled = currentLevel > 0;
        LevelPlusButton.IsEnabled = currentLevel < entry.MaximumLevel;

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

    private void LevelMinusButton_Click(object sender, RoutedEventArgs e) => ChangeSelectedLevel(-1);

    private void LevelPlusButton_Click(object sender, RoutedEventArgs e) => ChangeSelectedLevel(1);

    private void ChangeSelectedLevel(int delta)
    {
        if (StationList.SelectedItem is not StationRow row)
            return;

        var currentLevel = row.Entry.CurrentLevel ?? 0;
        var targetLevel = Math.Clamp(currentLevel + delta, 0, row.Entry.MaximumLevel);
        if (targetLevel == currentLevel)
            return;

        LevelChangeRequested?.Invoke(
            this,
            new HideoutLevelChangeRequestedEventArgs(row.Entry.Station.Id, targetLevel));
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
}
