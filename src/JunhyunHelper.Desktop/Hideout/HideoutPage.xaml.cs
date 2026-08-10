using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using JunhyunHelper.Application.Hideout;
using JunhyunHelper.Core.Content;
using JunhyunHelper.Desktop.Services;

namespace JunhyunHelper.Desktop.Hideout;

public sealed class HideoutLevelChangeRequestedEventArgs(
    string stationId,
    int? level) : EventArgs
{
    public string StationId { get; } = stationId;
    public int? Level { get; } = level;
}

public sealed record HideoutItemNavigationRequestedEventArgs(string ItemId);

public partial class HideoutPage : UserControl
{
    private static readonly TimeSpan RapidLevelClickWindow = TimeSpan.FromMilliseconds(180);

    private GameContentCatalog? _content;
    private HideoutWorkspace? _workspace;
    private ImageCacheService? _imageCache;
    private IReadOnlyList<StationRow> _rows = [];
    private CancellationTokenSource? _iconLoadCts;
    private CancellationTokenSource? _materialIconLoadCts;
    private readonly DispatcherTimer _levelSaveDebounceTimer;
    private PendingLevelChange? _pendingLevelChange;

    public HideoutPage()
    {
        InitializeComponent();
        _levelSaveDebounceTimer = new DispatcherTimer(
            RapidLevelClickWindow,
            DispatcherPriority.Background,
            (_, _) => FlushPendingLevelChange(),
            Dispatcher);
        _levelSaveDebounceTimer.Stop();
    }

    public event EventHandler<HideoutLevelChangeRequestedEventArgs>? LevelChangeRequested;
    public event EventHandler<HideoutItemNavigationRequestedEventArgs>? ItemNavigationRequested;

    public void SetImageCache(ImageCacheService imageCache) =>
        _imageCache = imageCache ?? throw new ArgumentNullException(nameof(imageCache));

    public void SetData(GameContentCatalog content, HideoutWorkspace workspace)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(workspace);

        var selectedStationId = (StationList.SelectedItem as StationRow)?.Entry.Station.Id;
        var loadedIcons = _rows
            .Where(row => row.Icon is not null)
            .ToDictionary(row => row.Entry.Station.Id, row => row.Icon!, StringComparer.Ordinal);

        _levelSaveDebounceTimer.Stop();
        _pendingLevelChange = null;
        _content = content;
        _workspace = workspace;
        _rows = workspace.Stations
            .Select(entry =>
            {
                var currentLevel = entry.CurrentLevel ?? 0;
                var row = new StationRow(
                    entry,
                    DisplayName(entry.Station.NameKo, entry.Station.NameEn, entry.Station.Id),
                    $"Lv.{currentLevel} / {entry.MaximumLevel}");
                if (loadedIcons.TryGetValue(entry.Station.Id, out var icon))
                    row.Icon = icon;
                return row;
            })
            .OrderBy(row => row.Name, StringComparer.CurrentCulture)
            .ToArray();

        ApplySearch();

        if (!string.IsNullOrWhiteSpace(selectedStationId))
        {
            var selected = (StationList.ItemsSource as IEnumerable<StationRow>)?
                .FirstOrDefault(row => row.Entry.Station.Id == selectedStationId);
            if (selected is not null)
                StationList.SelectedItem = selected;
        }

        _iconLoadCts?.Cancel();
        _iconLoadCts?.Dispose();
        _iconLoadCts = new CancellationTokenSource();
        _ = LoadIconsAsync(_rows, _iconLoadCts.Token);
    }

    public void NavigateToStation(string stationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stationId);
        if (_rows.All(row => !string.Equals(row.Entry.Station.Id, stationId, StringComparison.Ordinal)))
            return;

        SearchBox.Text = string.Empty;
        ApplySearch();
        var target = (StationList.ItemsSource as IEnumerable<StationRow>)?
            .FirstOrDefault(row => string.Equals(row.Entry.Station.Id, stationId, StringComparison.Ordinal));
        if (target is null)
            return;

        StationList.SelectedItem = target;
        StationList.ScrollIntoView(target);
    }

    public void SetBusy(bool busy) => IsEnabled = !busy;

    private async Task LoadIconsAsync(IReadOnlyList<StationRow> rows, CancellationToken cancellationToken)
    {
        if (_imageCache is null)
            return;

        try
        {
            foreach (var row in rows)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (row.Icon is not null)
                    continue;

                var image = await _imageCache.LoadAsync(
                    $"hideout-{row.Entry.Station.Id}",
                    row.Entry.Station.ImageUrl,
                    cancellationToken);
                if (image is null || cancellationToken.IsCancellationRequested)
                    continue;

                row.Icon = image;
                if (ReferenceEquals(row, StationList.SelectedItem))
                    DetailIcon.Source = image;
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task LoadMaterialIconsAsync(
        IReadOnlyList<HideoutMaterialRow> rows,
        CancellationToken cancellationToken)
    {
        if (_imageCache is null)
            return;

        try
        {
            foreach (var row in rows)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (string.IsNullOrWhiteSpace(row.IconUrl))
                    continue;

                var image = await _imageCache.LoadAsync(
                    $"item-{row.ItemId}",
                    row.IconUrl,
                    cancellationToken);
                if (image is not null && !cancellationToken.IsCancellationRequested)
                    row.Icon = image;
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

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
            ShowDetail(row);
        else
            ClearDetail();
    }

    private void ShowDetail(StationRow row)
    {
        if (_content is null)
            return;

        var entry = row.Entry;
        EmptyDetailText.Visibility = Visibility.Collapsed;
        DetailScroll.Visibility = Visibility.Visible;
        DetailIcon.Source = row.Icon;
        DetailName.Text = row.Name;

        var currentLevel = EffectiveLevel(row);
        CurrentLevelText.Text = $"Lv.{currentLevel} / {entry.MaximumLevel}";
        LevelMinusButton.IsEnabled = currentLevel > 0;
        LevelPlusButton.IsEnabled = currentLevel < entry.MaximumLevel;

        _materialIconLoadCts?.Cancel();
        _materialIconLoadCts?.Dispose();
        _materialIconLoadCts = null;

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
        var materialRows = entry.NextLevel.ItemRequirements
            .Select(requirement =>
            {
                items.TryGetValue(requirement.ItemId, out var item);
                var itemName = item is null
                    ? requirement.ItemId
                    : DisplayName(item.NameKo, item.NameEn, item.Id);
                return new HideoutMaterialRow(
                    requirement.ItemId,
                    itemName,
                    item?.IconUrl,
                    $"× {requirement.Count}",
                    requirement.FoundInRaid);
            })
            .OrderBy(material => material.Name, StringComparer.CurrentCulture)
            .ToArray();

        NextLevelItems.ItemsSource = materialRows;
        _materialIconLoadCts = new CancellationTokenSource();
        _ = LoadMaterialIconsAsync(materialRows, _materialIconLoadCts.Token);

        ConstructionTimeText.Text = entry.NextLevel.ConstructionTimeSeconds is > 0
            ? $"건설 시간: {FormatDuration(entry.NextLevel.ConstructionTimeSeconds.Value)}"
            : string.Empty;
    }

    private int EffectiveLevel(StationRow row)
    {
        if (_pendingLevelChange is { } pending &&
            string.Equals(pending.StationId, row.Entry.Station.Id, StringComparison.Ordinal))
        {
            return pending.Level;
        }

        return row.Entry.CurrentLevel ?? 0;
    }

    private void LevelMinusButton_Click(object sender, RoutedEventArgs e) => ChangeSelectedLevel(-1);
    private void LevelPlusButton_Click(object sender, RoutedEventArgs e) => ChangeSelectedLevel(1);

    private void ChangeSelectedLevel(int delta)
    {
        if (StationList.SelectedItem is not StationRow row)
            return;

        var stationId = row.Entry.Station.Id;
        if (_pendingLevelChange is { } existing &&
            !string.Equals(existing.StationId, stationId, StringComparison.Ordinal))
        {
            FlushPendingLevelChange();
            return;
        }

        var currentLevel = EffectiveLevel(row);
        var targetLevel = Math.Clamp(currentLevel + delta, 0, row.Entry.MaximumLevel);
        if (targetLevel == currentLevel)
            return;

        _pendingLevelChange = new PendingLevelChange(stationId, targetLevel);
        row.LevelText = $"Lv.{targetLevel} / {row.Entry.MaximumLevel}";
        CurrentLevelText.Text = row.LevelText;
        LevelMinusButton.IsEnabled = targetLevel > 0;
        LevelPlusButton.IsEnabled = targetLevel < row.Entry.MaximumLevel;

        _levelSaveDebounceTimer.Stop();
        _levelSaveDebounceTimer.Start();
    }

    private void FlushPendingLevelChange()
    {
        _levelSaveDebounceTimer.Stop();
        if (_pendingLevelChange is not { } pending)
            return;

        _pendingLevelChange = null;
        LevelChangeRequested?.Invoke(
            this,
            new HideoutLevelChangeRequestedEventArgs(pending.StationId, pending.Level));
    }

    private void MaterialButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string itemId } && !string.IsNullOrWhiteSpace(itemId))
            ItemNavigationRequested?.Invoke(this, new HideoutItemNavigationRequestedEventArgs(itemId));
    }

    private void ClearDetail()
    {
        _materialIconLoadCts?.Cancel();
        _materialIconLoadCts?.Dispose();
        _materialIconLoadCts = null;
        DetailScroll.Visibility = Visibility.Collapsed;
        EmptyDetailText.Visibility = Visibility.Visible;
        DetailIcon.Source = null;
        NextLevelItems.ItemsSource = null;
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

    private sealed class StationRow : INotifyPropertyChanged
    {
        private ImageSource? _icon;
        private string _levelText;

        public StationRow(HideoutStationEntry entry, string name, string levelText)
        {
            Entry = entry;
            Name = name;
            _levelText = levelText;
        }

        public HideoutStationEntry Entry { get; }
        public string Name { get; }
        public string LevelText
        {
            get => _levelText;
            set
            {
                if (string.Equals(_levelText, value, StringComparison.Ordinal))
                    return;
                _levelText = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LevelText)));
            }
        }

        public ImageSource? Icon
        {
            get => _icon;
            set
            {
                if (ReferenceEquals(_icon, value))
                    return;
                _icon = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Icon)));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }

    private sealed class HideoutMaterialRow : INotifyPropertyChanged
    {
        private ImageSource? _icon;

        public HideoutMaterialRow(string itemId, string name, string? iconUrl, string amountText, bool inRaid)
        {
            ItemId = itemId;
            Name = name;
            IconUrl = iconUrl;
            AmountText = amountText;
            InRaid = inRaid;
        }

        public string ItemId { get; }
        public string Name { get; }
        public string? IconUrl { get; }
        public string AmountText { get; }
        public bool InRaid { get; }
        public Visibility InRaidVisibility => InRaid ? Visibility.Visible : Visibility.Collapsed;

        public ImageSource? Icon
        {
            get => _icon;
            set
            {
                if (ReferenceEquals(_icon, value))
                    return;
                _icon = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Icon)));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }

    private sealed record PendingLevelChange(string StationId, int Level);
}