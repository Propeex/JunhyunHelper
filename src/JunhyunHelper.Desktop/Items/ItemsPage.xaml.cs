using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using JunhyunHelper.Application.Items;
using JunhyunHelper.Core.Content;
using JunhyunHelper.Core.Items;
using JunhyunHelper.Desktop.Controls;
using JunhyunHelper.Desktop.Services;

namespace JunhyunHelper.Desktop.Items;

public sealed record InventoryChangeRequestedEventArgs(string ItemId, int Fir, int NonFir);
public sealed record ItemQuestNavigationRequestedEventArgs(string QuestId);
public sealed record ItemHideoutNavigationRequestedEventArgs(string StationId);

public partial class ItemsPage : UserControl
{
    private static readonly TimeSpan RapidInventoryClickWindow = TimeSpan.FromMilliseconds(160);

    private GameContentCatalog? _content;
    private ItemsWorkspace? _workspace;
    private ImageCacheService? _imageCache;
    private IReadOnlyList<ItemRow> _allRows = [];
    private ItemRow? _selectedRow;
    private CancellationTokenSource? _iconLoadCts;
    private readonly DispatcherTimer _inventorySaveDebounceTimer;
    private PendingInventorySave? _pendingInventorySave;
    private ItemViewMode _viewMode = ItemViewMode.Normal;
    private bool _busy;
    private bool _updatingFilters;

    public ItemsPage()
    {
        InitializeComponent();
        ProductSearchClearButtonBehavior.Attach(SearchBox);
        FilterComboBox.ItemsSource = new[]
        {
            new FilterChoice(ItemFilter.Needed, "필요"),
            new FilterChoice(ItemFilter.All, "전체"),
            new FilterChoice(ItemFilter.Cleanup, "정리 필요"),
            new FilterChoice(ItemFilter.Satisfied, "충분"),
            new FilterChoice(ItemFilter.Deferred, "판단 보류"),
        };
        FilterComboBox.SelectedIndex = 0;
        CategoryComboBox.ItemsSource = new[] { new CategoryChoice(null, "모든 종류") };
        CategoryComboBox.SelectedIndex = 0;
        _inventorySaveDebounceTimer = new DispatcherTimer(
            RapidInventoryClickWindow,
            DispatcherPriority.Background,
            (_, _) => FlushPendingInventorySave(),
            Dispatcher);
        _inventorySaveDebounceTimer.Stop();
        UpdateModeControls();
    }

    public event EventHandler<InventoryChangeRequestedEventArgs>? InventoryChangeRequested;
    public event EventHandler<ItemQuestNavigationRequestedEventArgs>? QuestNavigationRequested;
    public event EventHandler<ItemHideoutNavigationRequestedEventArgs>? HideoutNavigationRequested;

    public void SetImageCache(ImageCacheService imageCache) =>
        _imageCache = imageCache ?? throw new ArgumentNullException(nameof(imageCache));

    public void SetData(GameContentCatalog content, ItemsWorkspace workspace)
    {
        _content = content ?? throw new ArgumentNullException(nameof(content));
        _workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));

        var selectedItemId = _selectedRow?.ItemId;
        var loadedIcons = _allRows
            .Where(row => row.Icon is not null)
            .ToDictionary(row => row.ItemId, row => row.Icon!, StringComparer.Ordinal);

        _allRows = BuildRows(content, workspace);
        foreach (var row in _allRows)
        {
            if (loadedIcons.TryGetValue(row.ItemId, out var icon))
                row.Icon = icon;
        }

        ApplyFilter();

        if (!string.IsNullOrWhiteSpace(selectedItemId))
            SelectVisibleItem(selectedItemId, scrollIntoView: false);

        _iconLoadCts?.Cancel();
        _iconLoadCts?.Dispose();
        _iconLoadCts = new CancellationTokenSource();
        _ = LoadIconsAsync(_allRows, _iconLoadCts.Token);
    }

    public void NavigateToItem(string itemId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(itemId);
        var target = _allRows.FirstOrDefault(row => row.ItemId == itemId);
        if (target is null)
            return;

        _viewMode = target.IsFlexibleOnly ? ItemViewMode.Flexible : ItemViewMode.Normal;
        _updatingFilters = true;
        SearchBox.Text = string.Empty;
        if (CategoryComboBox.Items.Count > 0)
            CategoryComboBox.SelectedIndex = 0;
        FilterComboBox.SelectedItem = FilterComboBox.Items
            .Cast<FilterChoice>()
            .First(choice => choice.Value == ItemFilter.All);
        _updatingFilters = false;
        UpdateModeControls();
        ApplyFilter();
        SelectVisibleItem(itemId, scrollIntoView: true);
    }

    public void SetCleanupChanges(IReadOnlyList<InventoryCleanupIncrease> changes)
    {
        ArgumentNullException.ThrowIfNull(changes);
        if (changes.Count == 0)
        {
            CleanupNotice.Visibility = Visibility.Collapsed;
            return;
        }

        CleanupNoticeText.Text = changes.Count == 1
            ? $"{DisplayName(changes[0].ItemId)}의 정리 가능 수량이 {changes[0].IncreasedBy}개 늘었습니다."
            : $"새로 정리할 수 있는 보유 아이템이 {changes.Count}종 생겼습니다.";
        CleanupNotice.Visibility = Visibility.Visible;
    }

    public void ClearCleanupNotice() => CleanupNotice.Visibility = Visibility.Collapsed;

    public void SetBusy(bool busy)
    {
        _busy = busy;
        SearchBox.IsEnabled = !busy;
        CategoryComboBox.IsEnabled = !busy;
        FilterComboBox.IsEnabled = !busy && _viewMode == ItemViewMode.Normal;
        ViewModeButton.IsEnabled = !busy;
        ItemList.IsEnabled = !busy;
        FlexibleGroupScroll.IsEnabled = !busy;
        InventoryControls.IsEnabled = !busy;
        SaveInventoryButton.IsEnabled = !busy && _selectedRow is not null;
    }

    private IReadOnlyList<ItemRow> BuildRows(GameContentCatalog content, ItemsWorkspace workspace)
    {
        var itemById = content.Items.ToDictionary(item => item.Id, StringComparer.Ordinal);
        var neededById = workspace.Plan.NeededItems.ToDictionary(item => item.ItemId, StringComparer.Ordinal);
        var cleanupById = workspace.Plan.CleanupItems.ToDictionary(item => item.ItemId, StringComparer.Ordinal);
        var protectionsById = workspace.Plan.CleanupProtections
            .GroupBy(protection => protection.ItemId, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);
        var flexibleByItemId = workspace.FlexibleQuestItemProgresses
            .SelectMany(progress => progress.AcceptedItemIds.Select(itemId => (itemId, progress)))
            .GroupBy(entry => entry.itemId, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<FlexibleQuestItemProgress>)group.Select(entry => entry.progress).ToArray(),
                StringComparer.Ordinal);

        var itemIds = new HashSet<string>(neededById.Keys, StringComparer.Ordinal);
        itemIds.UnionWith(cleanupById.Keys);
        itemIds.UnionWith(workspace.Profile.Inventory.Keys);
        itemIds.UnionWith(protectionsById.Keys.Where(workspace.Profile.Inventory.ContainsKey));
        itemIds.UnionWith(flexibleByItemId.Keys);

        return itemIds.Select(itemId =>
            {
                neededById.TryGetValue(itemId, out var needed);
                cleanupById.TryGetValue(itemId, out var cleanup);
                workspace.Profile.Inventory.TryGetValue(itemId, out var ownedRaw);
                var owned = ownedRaw.Normalize();
                protectionsById.TryGetValue(itemId, out var protections);
                protections ??= Array.Empty<CleanupProtection>();
                flexibleByItemId.TryGetValue(itemId, out var flexibleProgresses);
                flexibleProgresses ??= Array.Empty<FlexibleQuestItemProgress>();

                var requiredTotal = needed?.RequiredTotal ?? cleanup?.RequiredTotal ?? 0;
                var requiredFir = needed?.RequiredFir ?? cleanup?.RequiredFir ?? 0;
                var remainingTotal = needed?.RemainingTotal ?? 0;
                var remainingFir = needed?.RemainingFir ?? 0;
                var surplusFir = cleanup?.SurplusFir ?? 0;
                var surplusNonFir = cleanup?.SurplusNonFir ?? 0;

                itemById.TryGetValue(itemId, out var item);
                var name = item is null ? itemId : DisplayName(item.NameKo, item.NameEn, item.Id);
                var category = ItemCategoryClassifier.Classify(item);
                var sourceRows = (needed?.Sources ?? cleanup?.Sources ?? Array.Empty<ItemRequirementSource>())
                    .Select(source => BuildSourceRow(source, content))
                    .DistinctBy(source => (source.Title, source.Detail, source.Kind, source.TargetId))
                    .ToArray();
                var isFlexibleOnly = needed is null && cleanup is null && !workspace.Profile.Inventory.ContainsKey(itemId);

                return new ItemRow(
                    itemId, name, item?.IconUrl, category, ItemCategoryClassifier.Label(category),
                    isFlexibleOnly, requiredTotal, requiredFir, owned.Fir, owned.NonFir,
                    remainingTotal, remainingFir, surplusFir, surplusNonFir,
                    protections, flexibleProgresses, sourceRows);
            })
            .OrderBy(row => ItemCategoryClassifier.Order(row.Category))
            .ThenBy(row => row.Name, StringComparer.CurrentCulture)
            .ToArray();
    }

    private static SourceRow BuildSourceRow(ItemRequirementSource source, GameContentCatalog content)
    {
        if (source.Kind == ItemRequirementSourceKind.Quest)
        {
            var quest = content.Quests.FirstOrDefault(candidate => candidate.Id == source.SourceId);
            var questName = DisplayName(quest?.NameKo, quest?.NameEn, source.SourceId);
            return new SourceRow(
                $"퀘스트 · {questName}",
                "클릭해서 퀘스트 정보로 이동",
                SourceNavigationKind.Quest,
                source.SourceId);
        }

        if (source.Kind == ItemRequirementSourceKind.Hideout)
        {
            var station = content.HideoutStations.FirstOrDefault(candidate => candidate.Id == source.SourceId);
            var stationName = DisplayName(station?.NameKo, station?.NameEn, source.SourceId);
            return new SourceRow(
                $"은신처 · {stationName}",
                string.IsNullOrWhiteSpace(source.DetailId) ? "업그레이드 재료" : $"Lv.{source.DetailId} 업그레이드",
                SourceNavigationKind.Hideout,
                source.SourceId);
        }

        return new SourceRow(source.SourceId, string.Empty, SourceNavigationKind.None, null);
    }

    private async Task LoadIconsAsync(IReadOnlyList<ItemRow> rows, CancellationToken cancellationToken)
    {
        if (_imageCache is null)
            return;
        try
        {
            foreach (var row in rows)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (row.Icon is not null || string.IsNullOrWhiteSpace(row.IconUrl))
                    continue;
                var image = await _imageCache.LoadAsync($"item-{row.ItemId}", row.IconUrl, cancellationToken);
                if (image is null || cancellationToken.IsCancellationRequested)
                    continue;
                row.Icon = image;
                if (ReferenceEquals(row, _selectedRow))
                    DetailIcon.Source = image;
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private void RefreshCategoryFilter(IReadOnlyList<ItemRow> availableRows)
    {
        var selected = (CategoryComboBox.SelectedItem as CategoryChoice)?.Value;
        var choices = new[] { new CategoryChoice(null, "모든 종류") }
            .Concat(availableRows.Select(row => row.Category).Distinct()
                .OrderBy(ItemCategoryClassifier.Order)
                .Select(category => new CategoryChoice(category, ItemCategoryClassifier.Label(category))))
            .ToArray();

        _updatingFilters = true;
        CategoryComboBox.ItemsSource = choices;
        CategoryComboBox.SelectedItem = choices.FirstOrDefault(choice => choice.Value == selected) ?? choices[0];
        _updatingFilters = false;
    }

    private void ApplyFilter()
    {
        var search = SearchBox.Text?.Trim() ?? string.Empty;
        var filter = (FilterComboBox.SelectedItem as FilterChoice)?.Value ?? ItemFilter.Needed;
        var selectedId = _selectedRow?.ItemId;

        IEnumerable<ItemRow> query = _allRows;
        query = _viewMode == ItemViewMode.Flexible
            ? query.Where(row => row.FlexibleProgresses.Count > 0)
            : query.Where(row => !row.IsFlexibleOnly);
        query = query.Where(row => string.IsNullOrWhiteSpace(search) ||
                                   row.Name.Contains(search, StringComparison.CurrentCultureIgnoreCase) ||
                                   row.ItemId.Contains(search, StringComparison.OrdinalIgnoreCase));
        if (_viewMode == ItemViewMode.Normal)
        {
            query = query.Where(row => MatchesFilter(row, filter));
        }

        var availableBeforeCategory = query.ToArray();
        RefreshCategoryFilter(availableBeforeCategory);
        var category = (CategoryComboBox.SelectedItem as CategoryChoice)?.Value;
        var filtered = availableBeforeCategory.Where(row => category is null || row.Category == category).ToArray();

        if (_viewMode == ItemViewMode.Flexible)
        {
            ItemList.Visibility = Visibility.Collapsed;
            FlexibleGroupScroll.Visibility = Visibility.Visible;
            var groups = BuildFlexibleGroups(filtered);
            FlexibleGroupItems.ItemsSource = groups;
            var candidateIds = groups.SelectMany(group => group.Candidates).Select(row => row.ItemId).ToHashSet(StringComparer.Ordinal);
            var selected = filtered.FirstOrDefault(row => row.ItemId == selectedId && candidateIds.Contains(row.ItemId))
                           ?? groups.SelectMany(group => group.Candidates).FirstOrDefault();
            ShowDetail(selected);
            EmptyListText.Visibility = groups.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }
        else
        {
            FlexibleGroupScroll.Visibility = Visibility.Collapsed;
            ItemList.Visibility = Visibility.Visible;
            ItemList.ItemsSource = filtered;
            EmptyListText.Visibility = filtered.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
            ItemList.SelectedItem = filtered.FirstOrDefault(row => row.ItemId == selectedId) ?? filtered.FirstOrDefault();
            if (filtered.Length == 0)
                ShowDetail(null);
        }
    }

    private IReadOnlyList<FlexibleGroupRow> BuildFlexibleGroups(IReadOnlyList<ItemRow> visibleCandidates)
    {
        if (_workspace is null)
            return Array.Empty<FlexibleGroupRow>();

        var rowsById = visibleCandidates.ToDictionary(row => row.ItemId, StringComparer.Ordinal);
        return _workspace.FlexibleQuestItemProgresses
            .GroupBy(progress => progress.QuestId, StringComparer.Ordinal)
            .Select(group =>
            {
                var candidates = group.SelectMany(progress => progress.AcceptedItemIds)
                    .Distinct(StringComparer.Ordinal).Where(rowsById.ContainsKey).Select(itemId => rowsById[itemId])
                    .OrderBy(row => ItemCategoryClassifier.Order(row.Category))
                    .ThenBy(row => row.Name, StringComparer.CurrentCulture).ToArray();
                if (candidates.Length == 0)
                    return null;

                var quest = _content?.Quests.FirstOrDefault(candidate => candidate.Id == group.Key);
                var questName = DisplayName(quest?.NameKo, quest?.NameEn, group.Key);
                var progressText = string.Join(" · ", group.Select(progress =>
                {
                    var state = progress.IsFulfilled ? "충족" : $"남음 {progress.RemainingTotal}개";
                    return progress.RequiredFir > 0
                        ? $"인레이드 {progress.OwnedFir}/{progress.RequiredFir} · 전체 {progress.OwnedTotal}/{progress.RequiredTotal} · {state}"
                        : $"보유 {progress.OwnedTotal}/{progress.RequiredTotal} · {state}";
                }));
                return new FlexibleGroupRow(group.Key, questName, progressText, candidates);
            })
            .Where(group => group is not null).Cast<FlexibleGroupRow>()
            .OrderBy(group => group.QuestName, StringComparer.CurrentCulture).ToArray();
    }

    private static bool MatchesFilter(ItemRow row, ItemFilter filter) => filter switch
    {
        ItemFilter.All => true,
        ItemFilter.Needed => row.RemainingTotal > 0 || row.RemainingFir > 0,
        ItemFilter.Cleanup => row.SurplusTotal > 0,
        ItemFilter.Satisfied => row.RequiredTotal > 0 && row.RemainingTotal == 0 && row.SurplusTotal == 0,
        ItemFilter.Deferred => row.Protections.Count > 0 && row.OwnedTotal > 0 && row.SurplusTotal == 0,
        _ => false,
    };


    private void ShowDetail(ItemRow? row)
    {
        _selectedRow = row;
        if (row is null)
        {
            EmptyDetailText.Visibility = Visibility.Visible;
            DetailScroll.Visibility = Visibility.Collapsed;
            SaveInventoryButton.IsEnabled = false;
            DetailIcon.Source = null;
            return;
        }

        EmptyDetailText.Visibility = Visibility.Collapsed;
        DetailScroll.Visibility = Visibility.Visible;
        DetailIcon.Source = row.Icon;
        DetailName.Text = row.Name;
        DetailCategoryText.Text = row.CategoryLabel;
        DetailRequiredFirText.Text = $"{row.RequiredFir}개";
        DetailRequiredGeneralText.Text = $"{row.RequiredGeneral}개";
        DetailCleanupText.Text = row.SurplusTotal > 0 ? CleanupText(row) : string.Empty;
        OwnedFirTextBox.Text = row.OwnedFir.ToString(CultureInfo.InvariantCulture);
        OwnedNonFirTextBox.Text = row.OwnedNonFir.ToString(CultureInfo.InvariantCulture);
        SaveInventoryButton.IsEnabled = !_busy;

        var hasFlexible = row.FlexibleProgresses.Count > 0;
        SourceItems.ItemsSource = row.Sources.Length > 0
            ? row.Sources
            : hasFlexible
                ? [new SourceRow("유동 제출", "아래 그룹별 제출 정보를 확인하세요.", SourceNavigationKind.None, null)]
                : [new SourceRow("현재 계산된 필요 출처 없음", string.Empty, SourceNavigationKind.None, null)];
        FlexibleRequirementItems.ItemsSource = row.FlexibleProgresses.Select(BuildFlexibleRow).ToArray();
        FlexibleDetailPanel.Visibility = hasFlexible ? Visibility.Visible : Visibility.Collapsed;
        ProtectionText.Text = BuildProtectionText(row.Protections);
    }

    private FlexibleSourceRow BuildFlexibleRow(FlexibleQuestItemProgress progress)
    {
        var quest = _content?.Quests.FirstOrDefault(candidate => candidate.Id == progress.QuestId);
        var questName = DisplayName(quest?.NameKo, quest?.NameEn, progress.QuestId);
        var candidateNames = progress.AcceptedItemIds.Select(DisplayName).ToArray();
        var remainingText = progress.IsFulfilled ? "충족" : $"남음 {progress.RemainingTotal}개";
        var ownershipText = progress.RequiredFir > 0
            ? $"인레이드 합산 {progress.OwnedFir}/{progress.RequiredFir} · 전체 {progress.OwnedTotal}/{progress.RequiredTotal}"
            : $"합산 보유 {progress.OwnedTotal}/{progress.RequiredTotal}";
        return new FlexibleSourceRow(progress.QuestId, questName, $"{ownershipText} · {remainingText}", $"후보: {string.Join(", ", candidateNames)}");
    }

    private void SelectVisibleItem(string itemId, bool scrollIntoView)
    {
        var target = _allRows.FirstOrDefault(row => row.ItemId == itemId);
        if (target is null)
            return;
        if (_viewMode == ItemViewMode.Flexible)
        {
            ShowDetail(target);
            return;
        }

        var item = (ItemList.ItemsSource as IEnumerable<ItemRow>)?.FirstOrDefault(row => row.ItemId == itemId);
        if (item is null)
            return;
        ItemList.SelectedItem = item;
        if (scrollIntoView)
            ItemList.ScrollIntoView(item);
    }

    private void UpdateModeControls()
    {
        var groupCount = _workspace?.FlexibleQuestItemProgresses.Select(progress => progress.QuestId)
            .Distinct(StringComparer.Ordinal).Count() ?? 0;
        ViewModeButton.Content = _viewMode == ItemViewMode.Normal
            ? $"유동 제출 보기 ({groupCount})"
            : "일반 목록 보기";
        FilterComboBox.IsEnabled = !_busy && _viewMode == ItemViewMode.Normal;
    }

    private string DisplayName(string itemId)
    {
        var item = _content?.Items.FirstOrDefault(candidate => candidate.Id == itemId);
        return item is null ? itemId : DisplayName(item.NameKo, item.NameEn, item.Id);
    }

    private static string CleanupText(ItemRow row)
    {
        var parts = new List<string>();
        if (row.SurplusFir > 0) parts.Add($"인레이드 {row.SurplusFir}개");
        if (row.SurplusNonFir > 0) parts.Add($"일반 {row.SurplusNonFir}개");
        return $"정리 가능: {string.Join(" · ", parts)}";
    }

    private static string BuildProtectionText(IReadOnlyList<CleanupProtection> protections)
    {
        if (protections.Count == 0) return string.Empty;
        if (protections.Any(protection => protection.Kind == CleanupProtectionKind.AlternativeQuestRequirement))
            return "정리 판단 보호 · 유동 제출 후보이므로 그룹 목표가 끝나기 전에는 이 아이템만 따로 정리 가능하다고 판단하지 않습니다.";
        return "이 아이템은 안전한 정리량을 자동 확정할 수 없어 판단을 보류합니다.";
    }

    private static string DisplayName(string? korean, string? english, string fallback) =>
        !string.IsNullOrWhiteSpace(korean) ? korean : !string.IsNullOrWhiteSpace(english) ? english : fallback;

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) { if (!_updatingFilters) ApplyFilter(); }
    private void FilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) { if (IsLoaded && !_updatingFilters) ApplyFilter(); }
    private void CategoryComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) { if (IsLoaded && !_updatingFilters) ApplyFilter(); }

    private void ViewModeButton_Click(object sender, RoutedEventArgs e)
    {
        _viewMode = _viewMode == ItemViewMode.Normal ? ItemViewMode.Flexible : ItemViewMode.Normal;
        UpdateModeControls();
        ApplyFilter();
    }

    private void ItemList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_viewMode == ItemViewMode.Normal)
            ShowDetail(ItemList.SelectedItem as ItemRow);
    }

    private void FlexibleCandidateButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string itemId } || string.IsNullOrWhiteSpace(itemId)) return;
        var row = _allRows.FirstOrDefault(candidate => candidate.ItemId == itemId);
        if (row is not null) ShowDetail(row);
    }

    private void SourceButton_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not SourceRow row || !row.IsNavigable || row.TargetId is null)
            return;
        if (row.Kind == SourceNavigationKind.Quest)
            QuestNavigationRequested?.Invoke(this, new ItemQuestNavigationRequestedEventArgs(row.TargetId));
        else if (row.Kind == SourceNavigationKind.Hideout)
            HideoutNavigationRequested?.Invoke(this, new ItemHideoutNavigationRequestedEventArgs(row.TargetId));
    }

    private void FlexibleQuestButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string questId } && !string.IsNullOrWhiteSpace(questId))
            QuestNavigationRequested?.Invoke(this, new ItemQuestNavigationRequestedEventArgs(questId));
    }

    private void ShowCleanupButton_Click(object sender, RoutedEventArgs e)
    {
        _viewMode = ItemViewMode.Normal;
        _updatingFilters = true;
        if (CategoryComboBox.Items.Count > 0) CategoryComboBox.SelectedIndex = 0;
        FilterComboBox.SelectedItem = FilterComboBox.Items.Cast<FilterChoice>().First(choice => choice.Value == ItemFilter.Cleanup);
        _updatingFilters = false;
        CleanupNotice.Visibility = Visibility.Collapsed;
        UpdateModeControls();
        ApplyFilter();
    }

    private void OwnedFirMinusButton_Click(object sender, RoutedEventArgs e) => AdjustInventory(true, -1);
    private void OwnedFirPlusButton_Click(object sender, RoutedEventArgs e) => AdjustInventory(true, 1);
    private void OwnedNonFirMinusButton_Click(object sender, RoutedEventArgs e) => AdjustInventory(false, -1);
    private void OwnedNonFirPlusButton_Click(object sender, RoutedEventArgs e) => AdjustInventory(false, 1);

    private void AdjustInventory(bool inRaid, int delta)
    {
        if (_busy || _selectedRow is null) return;
        if (!TryParseQuantity(OwnedFirTextBox.Text, out var fir) || !TryParseQuantity(OwnedNonFirTextBox.Text, out var nonFir))
        {
            ShowQuantityValidation();
            return;
        }
        if (inRaid) fir = Math.Max(0, fir + delta); else nonFir = Math.Max(0, nonFir + delta);
        OwnedFirTextBox.Text = fir.ToString(CultureInfo.InvariantCulture);
        OwnedNonFirTextBox.Text = nonFir.ToString(CultureInfo.InvariantCulture);
        ScheduleInventorySave(_selectedRow.ItemId, fir, nonFir);
    }

    private void SaveInventoryButton_Click(object sender, RoutedEventArgs e)
    {
        if (_busy || _selectedRow is null) return;
        if (!TryParseQuantity(OwnedFirTextBox.Text, out var fir) || !TryParseQuantity(OwnedNonFirTextBox.Text, out var nonFir))
        {
            ShowQuantityValidation();
            return;
        }

        _inventorySaveDebounceTimer.Stop();
        _pendingInventorySave = null;
        RequestInventorySave(_selectedRow.ItemId, fir, nonFir);
    }

    private void ScheduleInventorySave(string itemId, int fir, int nonFir)
    {
        _pendingInventorySave = new PendingInventorySave(itemId, fir, nonFir);
        _inventorySaveDebounceTimer.Stop();
        _inventorySaveDebounceTimer.Start();
    }

    private void FlushPendingInventorySave()
    {
        _inventorySaveDebounceTimer.Stop();
        if (_pendingInventorySave is not { } pending)
            return;

        _pendingInventorySave = null;
        RequestInventorySave(pending.ItemId, pending.Fir, pending.NonFir);
    }

    private void RequestInventorySave(string itemId, int fir, int nonFir) =>
        InventoryChangeRequested?.Invoke(this, new InventoryChangeRequestedEventArgs(itemId, fir, nonFir));

    private static bool TryParseQuantity(string? text, out int quantity) =>
        int.TryParse(text?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out quantity) && quantity >= 0;

    private void ShowQuantityValidation() => MessageBox.Show(
        Window.GetWindow(this), "보유 수량은 0 이상의 정수로 입력해주세요.", "보유량",
        MessageBoxButton.OK, MessageBoxImage.Information);

    private sealed class ItemRow : INotifyPropertyChanged
    {
        private ImageSource? _icon;
        public ItemRow(string itemId, string name, string? iconUrl, ItemDisplayCategory category, string categoryLabel,
            bool isFlexibleOnly, int requiredTotal, int requiredFir, int ownedFir, int ownedNonFir,
            int remainingTotal, int remainingFir, int surplusFir, int surplusNonFir,
            IReadOnlyList<CleanupProtection> protections, IReadOnlyList<FlexibleQuestItemProgress> flexibleProgresses,
            SourceRow[] sources)
        {
            ItemId = itemId; Name = name; IconUrl = iconUrl; Category = category; CategoryLabel = categoryLabel;
            IsFlexibleOnly = isFlexibleOnly; RequiredTotal = requiredTotal; RequiredFir = requiredFir;
            OwnedFir = ownedFir; OwnedNonFir = ownedNonFir; RemainingTotal = remainingTotal; RemainingFir = remainingFir;
            SurplusFir = surplusFir; SurplusNonFir = surplusNonFir; Protections = protections;
            FlexibleProgresses = flexibleProgresses; Sources = sources;
        }
        public string ItemId { get; }
        public string Name { get; }
        public string? IconUrl { get; }
        public ItemDisplayCategory Category { get; }
        public string CategoryLabel { get; }
        public bool IsFlexibleOnly { get; }
        public int RequiredTotal { get; }
        public int RequiredFir { get; }
        public int RequiredGeneral => Math.Max(0, RequiredTotal - RequiredFir);
        public int OwnedFir { get; }
        public int OwnedNonFir { get; }
        public int RemainingTotal { get; }
        public int RemainingFir { get; }
        public int SurplusFir { get; }
        public int SurplusNonFir { get; }
        public IReadOnlyList<CleanupProtection> Protections { get; }
        public IReadOnlyList<FlexibleQuestItemProgress> FlexibleProgresses { get; }
        public SourceRow[] Sources { get; }
        public int OwnedTotal => OwnedFir + OwnedNonFir;
        public int SurplusTotal => SurplusFir + SurplusNonFir;
        public string FlexibleOwnedText => $"인레이드 {OwnedFir} · 일반 {OwnedNonFir}";
        public ImageSource? Icon
        {
            get => _icon;
            set { if (ReferenceEquals(_icon, value)) return; _icon = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Icon))); }
        }
        public event PropertyChangedEventHandler? PropertyChanged;
    }

    private sealed record PendingInventorySave(string ItemId, int Fir, int NonFir);
    private sealed record SourceRow(string Title, string Detail, SourceNavigationKind Kind, string? TargetId)
    {
        public bool IsNavigable => Kind != SourceNavigationKind.None && !string.IsNullOrWhiteSpace(TargetId);
    }
    private sealed record FlexibleSourceRow(string QuestId, string QuestName, string ProgressText, string CandidatesText);
    private sealed record FlexibleGroupRow(string QuestId, string QuestName, string ProgressText, IReadOnlyList<ItemRow> Candidates);
    private sealed record FilterChoice(ItemFilter Value, string Label) { public override string ToString() => Label; }
    private sealed record CategoryChoice(ItemDisplayCategory? Value, string Label) { public override string ToString() => Label; }

    private enum SourceNavigationKind { None, Quest, Hideout }
    private enum ItemFilter { Needed, All, Cleanup, Satisfied, Deferred }
    private enum ItemViewMode { Normal, Flexible }
}