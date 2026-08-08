using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using JunhyunHelper.Application.Items;
using JunhyunHelper.Core.Content;
using JunhyunHelper.Core.Items;
using JunhyunHelper.Desktop.Services;

namespace JunhyunHelper.Desktop.Items;

public sealed record InventoryChangeRequestedEventArgs(
    string ItemId,
    int Fir,
    int NonFir);

public partial class ItemsPage : UserControl
{
    private GameContentCatalog? _content;
    private ItemsWorkspace? _workspace;
    private ImageCacheService? _imageCache;
    private IReadOnlyList<ItemRow> _allRows = [];
    private ItemRow? _selectedRow;
    private CancellationTokenSource? _iconLoadCts;
    private bool _busy;

    public ItemsPage()
    {
        InitializeComponent();
        FilterComboBox.ItemsSource = new[]
        {
            new FilterChoice(ItemFilter.Needed, "필요"),
            new FilterChoice(ItemFilter.All, "전체"),
            new FilterChoice(ItemFilter.Cleanup, "정리 필요"),
            new FilterChoice(ItemFilter.Satisfied, "충분"),
            new FilterChoice(ItemFilter.Deferred, "판단 보류"),
        };
        FilterComboBox.SelectedIndex = 0;
    }

    public event EventHandler<InventoryChangeRequestedEventArgs>? InventoryChangeRequested;

    public void SetImageCache(ImageCacheService imageCache) =>
        _imageCache = imageCache ?? throw new ArgumentNullException(nameof(imageCache));

    public void SetData(GameContentCatalog content, ItemsWorkspace workspace)
    {
        _content = content ?? throw new ArgumentNullException(nameof(content));
        _workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
        _allRows = BuildRows(content, workspace);
        ApplyFilter();

        _iconLoadCts?.Cancel();
        _iconLoadCts?.Dispose();
        _iconLoadCts = new CancellationTokenSource();
        _ = LoadIconsAsync(_allRows, _iconLoadCts.Token);
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
        FilterComboBox.IsEnabled = !busy;
        ItemList.IsEnabled = !busy;
        OwnedFirTextBox.IsEnabled = !busy;
        OwnedNonFirTextBox.IsEnabled = !busy;
        SaveInventoryButton.IsEnabled = !busy && _selectedRow is not null;
    }

    private IReadOnlyList<ItemRow> BuildRows(
        GameContentCatalog content,
        ItemsWorkspace workspace)
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

        return itemIds
            .Select(itemId =>
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
                var surplusTotal = surplusFir + surplusNonFir;
                var deferred = protections.Length > 0 && surplusTotal == 0 && owned.Total > 0;

                itemById.TryGetValue(itemId, out var item);
                var name = item is null
                    ? itemId
                    : DisplayName(item.NameKo, item.NameEn, item.Id);

                var sources = (needed?.Sources ?? cleanup?.Sources ?? Array.Empty<ItemRequirementSource>())
                    .Select(source => SourceText(source, content))
                    .Distinct(StringComparer.CurrentCulture)
                    .ToArray();

                return new ItemRow(
                    itemId,
                    name,
                    item?.IconUrl,
                    requiredTotal,
                    requiredFir,
                    owned.Fir,
                    owned.NonFir,
                    remainingTotal,
                    remainingFir,
                    surplusFir,
                    surplusNonFir,
                    protections,
                    flexibleProgresses,
                    sources,
                    SourceSummary(sources),
                    BuildStatusText(remainingTotal, remainingFir, surplusTotal, deferred),
                    StatusBrush(remainingTotal, surplusTotal, deferred));
            })
            .OrderBy(row => row.Name, StringComparer.CurrentCulture)
            .ToArray();
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
                if (string.IsNullOrWhiteSpace(row.IconUrl))
                    continue;

                var image = await _imageCache.LoadAsync(
                    $"item-{row.ItemId}",
                    row.IconUrl,
                    cancellationToken);
                if (image is null || cancellationToken.IsCancellationRequested)
                    continue;

                row.Icon = image;
                if (ReferenceEquals(row, _selectedRow))
                    DetailIcon.Source = image;
            }
        }
        catch (OperationCanceledException)
        {
            // A newer workspace replaced these rows. Old image work is intentionally discarded.
        }
    }

    private void ApplyFilter()
    {
        var search = SearchBox.Text?.Trim() ?? string.Empty;
        var filter = (FilterComboBox.SelectedItem as FilterChoice)?.Value ?? ItemFilter.Needed;
        var selectedId = _selectedRow?.ItemId;

        var filtered = _allRows
            .Where(row => string.IsNullOrWhiteSpace(search) ||
                          row.Name.Contains(search, StringComparison.CurrentCultureIgnoreCase) ||
                          row.ItemId.Contains(search, StringComparison.OrdinalIgnoreCase))
            .Where(row => MatchesFilter(row, filter))
            .ToArray();

        ItemList.ItemsSource = filtered;
        ItemList.SelectedItem = filtered.FirstOrDefault(row => row.ItemId == selectedId)
                                ?? filtered.FirstOrDefault();
        if (filtered.Length == 0)
            ShowDetail(null);
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
        DetailStatusText.Text = row.StatusText;
        DetailStatusText.Foreground = row.StatusBrush;
        DetailRequirementText.Text = row.RequiredFir > 0
            ? $"미래 필요 {row.RequiredTotal}개 · FIR 최소 {row.RequiredFir}개"
            : $"미래 필요 {row.RequiredTotal}개";
        DetailRemainingText.Text = row.RemainingFir > 0
            ? $"추가 필요 {row.RemainingTotal}개 · 그중 FIR {row.RemainingFir}개"
            : row.RemainingTotal > 0
                ? $"추가 필요 {row.RemainingTotal}개"
                : row.RequiredTotal > 0
                    ? "현재 보유량으로 미래 필요량을 충족합니다."
                    : "현재 확정된 미래 필요량은 없습니다.";
        DetailCleanupText.Text = row.SurplusTotal > 0
            ? CleanupText(row)
            : string.Empty;

        OwnedFirTextBox.Text = row.OwnedFir.ToString(CultureInfo.InvariantCulture);
        OwnedNonFirTextBox.Text = row.OwnedNonFir.ToString(CultureInfo.InvariantCulture);
        SaveInventoryButton.IsEnabled = !_busy;

        SourceItems.ItemsSource = row.Sources.Length > 0
            ? row.Sources.Select(source => $"• {source}").ToArray()
            : new[] { "• 현재 계산된 필요 출처 없음" };

        FlexibleRequirementItems.ItemsSource = row.FlexibleProgresses
            .Select(BuildFlexibleRequirementText)
            .ToArray();
        FlexibleDetailPanel.Visibility = row.FlexibleProgresses.Count > 0
            ? Visibility.Visible
            : Visibility.Collapsed;

        ProtectionText.Text = BuildProtectionText(row.Protections);
    }

    private string BuildFlexibleRequirementText(FlexibleQuestItemProgress progress)
    {
        var quest = _content?.Quests.FirstOrDefault(candidate => candidate.Id == progress.QuestId);
        var questName = DisplayName(quest?.NameKo, quest?.NameEn, progress.QuestId);
        var candidateNames = progress.AcceptedItemIds.Select(DisplayName).ToArray();
        var remainingText = progress.IsFulfilled
            ? "충족"
            : $"남음 {progress.RemainingTotal}개";
        var ownershipText = progress.RequiredFir > 0
            ? $"FIR 합산 {progress.OwnedFir}/{progress.RequiredFir}"
            : $"합산 보유 {progress.OwnedTotal}/{progress.RequiredTotal}";

        return $"• {questName} · {ownershipText} · {remainingText}\n  후보: {string.Join(", ", candidateNames)}";
    }

    private string DisplayName(string itemId)
    {
        var item = _content?.Items.FirstOrDefault(candidate => candidate.Id == itemId);
        return item is null ? itemId : DisplayName(item.NameKo, item.NameEn, item.Id);
    }

    private static string SourceText(ItemRequirementSource source, GameContentCatalog content) => source.Kind switch
    {
        ItemRequirementSourceKind.Quest =>
            $"퀘스트 · {DisplayName(
                content.Quests.FirstOrDefault(quest => quest.Id == source.SourceId)?.NameKo,
                content.Quests.FirstOrDefault(quest => quest.Id == source.SourceId)?.NameEn,
                source.SourceId)}",
        ItemRequirementSourceKind.Hideout =>
            $"은신처 · {DisplayName(
                content.HideoutStations.FirstOrDefault(station => station.Id == source.SourceId)?.NameKo,
                content.HideoutStations.FirstOrDefault(station => station.Id == source.SourceId)?.NameEn,
                source.SourceId)} Lv.{source.DetailId}",
        _ => source.SourceId,
    };

    private static string SourceSummary(IReadOnlyList<string> sources) => sources.Count switch
    {
        0 => "보유 기록",
        1 => sources[0],
        _ => $"{sources[0]} 외 {sources.Count - 1}곳",
    };

    private static string BuildStatusText(
        int remainingTotal,
        int remainingFir,
        int surplusTotal,
        bool deferred)
    {
        if (remainingFir > 0 && surplusTotal > 0)
            return "FIR 부족";
        if (remainingFir > 0)
            return "FIR 부족";
        if (remainingTotal > 0)
            return $"+{remainingTotal} 필요";
        if (surplusTotal > 0)
            return $"{surplusTotal} 정리";
        if (deferred)
            return "판단 보류";
        return "충분";
    }

    private static Brush StatusBrush(int remainingTotal, int surplusTotal, bool deferred) =>
        (Brush)System.Windows.Application.Current.FindResource(
            surplusTotal > 0 || deferred
                ? "WarningBrush"
                : remainingTotal > 0
                    ? "AccentBrush"
                    : "SuccessBrush");

    private static string CleanupText(ItemRow row)
    {
        var parts = new List<string>();
        if (row.SurplusFir > 0)
            parts.Add($"FIR {row.SurplusFir}개");
        if (row.SurplusNonFir > 0)
            parts.Add($"일반 {row.SurplusNonFir}개");
        return $"정리 가능: {string.Join(" · ", parts)}";
    }

    private static string BuildProtectionText(IReadOnlyList<CleanupProtection> protections)
    {
        if (protections.Count == 0)
            return string.Empty;

        var messages = new List<string>();
        if (protections.Any(protection => protection.Kind == CleanupProtectionKind.AlternativeQuestRequirement))
        {
            messages.Add("유동 제출 후보이므로 목표가 끝나기 전에는 이 아이템만 따로 정리 가능하다고 판단하지 않습니다.");
        }

        return messages.Count > 0
            ? $"정리 판단 보호 · {string.Join(" ", messages)}"
            : "이 아이템은 안전한 정리량을 자동 확정할 수 없어 판단을 보류합니다.";
    }

    private static string DisplayName(string? korean, string? english, string fallback) =>
        !string.IsNullOrWhiteSpace(korean)
            ? korean
            : !string.IsNullOrWhiteSpace(english)
                ? english
                : fallback;

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilter();

    private void FilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (IsLoaded)
            ApplyFilter();
    }

    private void ItemList_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
        ShowDetail(ItemList.SelectedItem as ItemRow);

    private void ShowCleanupButton_Click(object sender, RoutedEventArgs e)
    {
        FilterComboBox.SelectedItem = FilterComboBox.Items
            .Cast<FilterChoice>()
            .First(choice => choice.Value == ItemFilter.Cleanup);
        CleanupNotice.Visibility = Visibility.Collapsed;
        ApplyFilter();
    }

    private void SaveInventoryButton_Click(object sender, RoutedEventArgs e)
    {
        if (_busy || _selectedRow is null)
            return;

        if (!TryParseQuantity(OwnedFirTextBox.Text, out var fir) ||
            !TryParseQuantity(OwnedNonFirTextBox.Text, out var nonFir))
        {
            MessageBox.Show(
                Window.GetWindow(this),
                "보유 수량은 0 이상의 정수로 입력해주세요.",
                "보유량",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        InventoryChangeRequested?.Invoke(
            this,
            new InventoryChangeRequestedEventArgs(_selectedRow.ItemId, fir, nonFir));
    }

    private static bool TryParseQuantity(string? text, out int quantity) =>
        int.TryParse(text?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out quantity) &&
        quantity >= 0;

    private sealed class ItemRow : INotifyPropertyChanged
    {
        private ImageSource? _icon;

        public ItemRow(
            string itemId,
            string name,
            string? iconUrl,
            int requiredTotal,
            int requiredFir,
            int ownedFir,
            int ownedNonFir,
            int remainingTotal,
            int remainingFir,
            int surplusFir,
            int surplusNonFir,
            IReadOnlyList<CleanupProtection> protections,
            IReadOnlyList<FlexibleQuestItemProgress> flexibleProgresses,
            string[] sources,
            string sourceSummary,
            string statusText,
            Brush statusBrush)
        {
            ItemId = itemId;
            Name = name;
            IconUrl = iconUrl;
            RequiredTotal = requiredTotal;
            RequiredFir = requiredFir;
            OwnedFir = ownedFir;
            OwnedNonFir = ownedNonFir;
            RemainingTotal = remainingTotal;
            RemainingFir = remainingFir;
            SurplusFir = surplusFir;
            SurplusNonFir = surplusNonFir;
            Protections = protections;
            FlexibleProgresses = flexibleProgresses;
            Sources = sources;
            SourceSummary = sourceSummary;
            StatusText = statusText;
            StatusBrush = statusBrush;
        }

        public string ItemId { get; }
        public string Name { get; }
        public string? IconUrl { get; }
        public int RequiredTotal { get; }
        public int RequiredFir { get; }
        public int OwnedFir { get; }
        public int OwnedNonFir { get; }
        public int RemainingTotal { get; }
        public int RemainingFir { get; }
        public int SurplusFir { get; }
        public int SurplusNonFir { get; }
        public IReadOnlyList<CleanupProtection> Protections { get; }
        public IReadOnlyList<FlexibleQuestItemProgress> FlexibleProgresses { get; }
        public string[] Sources { get; }
        public string SourceSummary { get; }
        public string StatusText { get; }
        public Brush StatusBrush { get; }
        public int OwnedTotal => OwnedFir + OwnedNonFir;
        public int SurplusTotal => SurplusFir + SurplusNonFir;
        public string FirBadgeText => $"FIR {RequiredFir}";
        public Visibility FirBadgeVisibility => RequiredFir > 0 ? Visibility.Visible : Visibility.Collapsed;

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

    private sealed record FilterChoice(ItemFilter Value, string Label)
    {
        public override string ToString() => Label;
    }

    private enum ItemFilter
    {
        Needed,
        All,
        Cleanup,
        Satisfied,
        Deferred,
    }
}
