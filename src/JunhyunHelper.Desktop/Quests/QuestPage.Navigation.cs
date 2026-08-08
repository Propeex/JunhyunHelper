using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using JunhyunHelper.Desktop.Services;

namespace JunhyunHelper.Desktop.Quests;

public sealed record QuestItemNavigationRequestedEventArgs(string ItemId);

public sealed record QuestNavigationRequestedEventArgs(string QuestId);

public partial class QuestPage
{
    private ImageCacheService? _questImageCache;
    private CancellationTokenSource? _questItemIconCts;
    private bool _linkHandlersAttached;

    public event EventHandler<QuestItemNavigationRequestedEventArgs>? ItemNavigationRequested;

    public event EventHandler<QuestNavigationRequestedEventArgs>? QuestNavigationRequested;

    public void SetImageCache(ImageCacheService imageCache) =>
        _questImageCache = imageCache ?? throw new ArgumentNullException(nameof(imageCache));

    public void NavigateToQuest(string questId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(questId);
        if (_rows.All(row => row.Entry.Quest.Id != questId))
            return;

        _updatingFilters = true;
        SearchBox.Text = string.Empty;
        StatusFilter.SelectedItem = StatusFilter.Items
            .Cast<FilterOption>()
            .First(option => option.Value is null);
        if (TraderFilter.Items.Count > 0)
            TraderFilter.SelectedIndex = 0;
        if (MapFilter.Items.Count > 0)
            MapFilter.SelectedIndex = 0;
        _updatingFilters = false;

        ApplyFilters();
        var target = (QuestList.ItemsSource as IEnumerable<QuestRow>)?
            .FirstOrDefault(row => row.Entry.Quest.Id == questId);
        if (target is null)
            return;

        QuestList.SelectedItem = target;
        QuestList.ScrollIntoView(target);
    }

    private void QuestPage_Loaded(object sender, RoutedEventArgs e)
    {
        if (!_linkHandlersAttached)
        {
            QuestList.SelectionChanged += QuestList_LinkSelectionChanged;
            _linkHandlersAttached = true;
        }

        RefreshLinkPanels();
    }

    private void QuestList_LinkSelectionChanged(object sender, SelectionChangedEventArgs e) =>
        RefreshLinkPanels();

    private void RefreshLinkPanels()
    {
        if (_content is null || QuestList.SelectedItem is not QuestRow row)
            return;

        var quest = row.Entry.Quest;
        var itemsById = _content.Items.ToDictionary(item => item.Id, StringComparer.Ordinal);
        var questsById = _content.Quests.ToDictionary(candidate => candidate.Id, StringComparer.Ordinal);

        var itemRows = _content.QuestItemRequirements
            .Where(requirement => requirement.QuestId == quest.Id)
            .SelectMany(requirement => requirement.AcceptedItemIds.Select(itemId =>
            {
                itemsById.TryGetValue(itemId, out var item);
                var name = DisplayName(item?.NameKo, item?.NameEn, itemId);
                var count = Convert.ToString(requirement.Count, CultureInfo.InvariantCulture) ?? "0";
                var amount = requirement.AcceptedItemIds.Count > 1
                    ? $"그룹 합계 {count}개"
                    : $"{count}개";
                if (requirement.FoundInRaid)
                    amount += " · FIR";

                return new QuestItemLinkRow(
                    itemId,
                    name,
                    item?.IconUrl,
                    amount,
                    requirement.AcceptedItemIds.Count > 1 ? "유동 제출 후보" : string.Empty);
            }))
            .ToArray();

        RequiredItemsHeader.Visibility = itemRows.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
        RequiredItemsBox.Visibility = itemRows.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
        RequiredItemsList.ItemsSource = itemRows;

        var prerequisiteRows = quest.TaskRequirements
            .Select(requirement =>
            {
                var name = questsById.TryGetValue(requirement.RequiredQuestId, out var prerequisite)
                    ? DisplayName(prerequisite.NameKo, prerequisite.NameEn, requirement.RequiredQuestId)
                    : requirement.RequiredQuestId;
                var states = string.Join(" / ", requirement.AcceptedStatuses.Select(StatusText));
                return new PrerequisiteLinkRow(requirement.RequiredQuestId, name, states);
            })
            .ToArray();

        PrerequisitesHeader.Visibility = prerequisiteRows.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
        PrerequisitesBox.Visibility = prerequisiteRows.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
        PrerequisitesList.ItemsSource = prerequisiteRows;

        _questItemIconCts?.Cancel();
        _questItemIconCts?.Dispose();
        _questItemIconCts = new CancellationTokenSource();
        _ = LoadQuestItemIconsAsync(itemRows, _questItemIconCts.Token);
    }

    private async Task LoadQuestItemIconsAsync(
        IReadOnlyList<QuestItemLinkRow> rows,
        CancellationToken cancellationToken)
    {
        if (_questImageCache is null)
            return;

        try
        {
            foreach (var row in rows)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (string.IsNullOrWhiteSpace(row.IconUrl))
                    continue;

                var image = await _questImageCache.LoadAsync(
                    $"quest-item-{row.ItemId}",
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

    private void RequiredItemButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string itemId } && !string.IsNullOrWhiteSpace(itemId))
            ItemNavigationRequested?.Invoke(this, new QuestItemNavigationRequestedEventArgs(itemId));
    }

    private void PrerequisiteButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string questId } && !string.IsNullOrWhiteSpace(questId))
            QuestNavigationRequested?.Invoke(this, new QuestNavigationRequestedEventArgs(questId));
    }

    private sealed class QuestItemLinkRow : INotifyPropertyChanged
    {
        private ImageSource? _icon;

        public QuestItemLinkRow(
            string itemId,
            string name,
            string? iconUrl,
            string amountText,
            string note)
        {
            ItemId = itemId;
            Name = name;
            IconUrl = iconUrl;
            AmountText = amountText;
            Note = note;
        }

        public string ItemId { get; }
        public string Name { get; }
        public string? IconUrl { get; }
        public string AmountText { get; }
        public string Note { get; }
        public Visibility NoteVisibility => string.IsNullOrWhiteSpace(Note) ? Visibility.Collapsed : Visibility.Visible;

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

    private sealed record PrerequisiteLinkRow(
        string QuestId,
        string Name,
        string AcceptedStatusText);
}
