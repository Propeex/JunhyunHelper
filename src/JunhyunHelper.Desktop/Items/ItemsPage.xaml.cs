using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using JunhyunHelper.Application.Items;
using JunhyunHelper.Core.Content;
using JunhyunHelper.Core.Items;

namespace JunhyunHelper.Desktop.Items;

public sealed record InventoryChangeRequestedEventArgs(
    string ItemId,
    int Fir,
    int NonFir);

public partial class ItemsPage : UserControl
{
    private GameContentCatalog? _content;
    private ItemsWorkspace? _workspace;
    private IReadOnlyList<ItemRow> _allRows = [];
    private ItemRow? _selectedRow;
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

    public void SetData(GameContentCatalog content, ItemsWorkspace workspace)
    {
        _content = content ?? throw new ArgumentNullException(nameof(content));
        _workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
        _allRows = BuildRows(content, workspace);
        UpdatePlanningWarning(workspace);
        ApplyFilter();
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

        var itemIds = new HashSet<string>(neededById.Keys, StringComparer.Ordinal);
        itemIds.UnionWith(cleanupById.Keys);
        itemIds.UnionWith(workspace.Profile.Inventory.Keys);
        itemIds.UnionWith(protectionsById.Keys.Where(workspace.Profile.Inventory.ContainsKey));

        return itemIds
            .Select(itemId =>
            {
                neededById.TryGetValue(itemId, out var needed);
                cleanupById.TryGetValue(itemId, out var cleanup);
                workspace.Profile.Inventory.TryGetValue(itemId, out var ownedRaw);
                var owned = ownedRaw.Normalize();
                protectionsById.TryGetValue(itemId, out var protections);
                protections ??= Array.Empty<CleanupProtection>();

                var requiredTotal = needed?.RequiredTotal ?? cleanup?.RequiredTotal ?? 0;
                var requiredFir = needed?.RequiredFir ?? cleanup?.RequiredFir ?? 0;
                var remainingTotal = needed?.RemainingTotal ?? 0;
                var remainingFir = needed?.RemainingFir ?? 0;
                var surplusFir = cleanup?.SurplusFir ?? 0;
                var surplusNonFir = cleanup?.SurplusNonFir ?? 0;
                var surplusTotal = surplusFir + surplusNonFir;
                var deferred = protections.Length > 0 && surplusTotal == 0 && owned.Total > 0;

                var name = itemById.TryGetValue(itemId, out var item)
                    ? DisplayName(item.NameKo, item.NameEn, item.Id)
                    : itemId;

                var sources = (needed?.Sources ?? cleanup?.Sources ?? Array.Empty<ItemRequirementSource>())
                    .Select(source => SourceText(source, content))
                    .Distinct(StringComparer.CurrentCulture)
                    .ToArray();

                return new ItemRow(
                    itemId,
                    name,
                    requiredTotal,
                    requiredFir,
                    owned.Fir,
                    owned.NonFir,
                    remainingTotal,
                    remainingFir,
                    surplusFir,
                    surplusNonFir,
                    protections,
                    sources,
                    BuildSummary(requiredTotal, requiredFir, owned.Total),
                    BuildStatusText(remainingTotal, remainingFir, surplusTotal, deferred),
                    StatusBrush(remainingTotal, surplusTotal, deferred));
            })
            .OrderBy(row => row.Name, StringComparer.CurrentCulture)
            .ToArray();
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
            return;
        }

        EmptyDetailText.Visibility = Visibility.Collapsed;
        DetailScroll.Visibility = Visibility.Visible;
        DetailName.Text = row.Name;
        DetailRequirementText.Text = row.RequiredFir > 0
            ? $"미래 필요 {row.RequiredTotal}개 · FIR 최소 {row.RequiredFir}개"
            : $"미래 필요 {row.RequiredTotal}개";
        DetailRemainingText.Text = row.RemainingFir > 0
            ? $"추가 필요 {row.RemainingTotal}개 · 그중 FIR {row.RemainingFir}개"
            : row.RemainingTotal > 0
                ? $"추가 필요 {row.RemainingTotal}개"
                : "현재 보유량으로 계산된 필요량을 충족합니다.";

        DetailCleanupText.Text = row.SurplusTotal > 0
            ? CleanupText(row)
            : string.Empty;
        OwnedFirTextBox.Text = row.OwnedFir.ToString(CultureInfo.InvariantCulture);
        OwnedNonFirTextBox.Text = row.OwnedNonFir.ToString(CultureInfo.InvariantCulture);
        SaveInventoryButton.IsEnabled = !_busy;
        SourceItems.ItemsSource = row.Sources.Length > 0
            ? row.Sources.Select(source => $"• {source}").ToArray()
            : new[] { "• 현재 계산된 필요 출처 없음" };
        ProtectionText.Text = BuildProtectionText(row.Protections);
    }

    private void UpdatePlanningWarning(ItemsWorkspace workspace)
    {
        var parts = new List<string>();
        if (workspace.Plan.UnenteredHideoutStationIds.Count > 0)
        {
            parts.Add($"은신처 레벨 미입력 {workspace.Plan.UnenteredHideoutStationIds.Count}개 시설의 관련 아이템은 정리 판단을 보류합니다.");
        }

        if (workspace.FlexibleQuestItemProgresses.Count > 0)
        {
            parts.Add($"유동 제출 요구 {workspace.FlexibleQuestItemProgresses.Count}건은 그룹 단위로 계산하고 후보별 정리 판단은 보호합니다.");
        }

        PlanningWarningText.Text = string.Join("  ", parts);
        PlanningWarningText.Visibility = parts.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

        FlexibleRequirementItems.ItemsSource = workspace.FlexibleQuestItemProgresses
            .Select(BuildFlexibleRequirementText)
            .ToArray();
        FlexibleRequirementsPanel.Visibility = workspace.FlexibleQuestItemProgresses.Count > 0
            ? Visibility.Visible
            : Visibility.Collapsed;
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

        return $"• {questName} · {ownershipText} · {remainingText} · 후보: {string.Join(", ", candidateNames)}";
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

    private static string BuildSummary(int requiredTotal, int requiredFir, int ownedTotal) =>
        requiredFir > 0
            ? $"필요 {requiredTotal} · FIR {requiredFir} · 보유 {ownedTotal}"
            : $"필요 {requiredTotal} · 보유 {ownedTotal}";

    private static string BuildStatusText(
        int remainingTotal,
        int remainingFir,
        int surplusTotal,
        bool deferred)
    {
        if (remainingFir > 0 && surplusTotal > 0)
            return "FIR 부족 · 정리 가능";
        if (remainingFir > 0)
            return "FIR 부족";
        if (remainingTotal > 0)
            return "더 필요";
        if (surplusTotal > 0)
            return $"정리 {surplusTotal}";
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

        var kinds = protections.Select(protection => protection.Kind).Distinct().ToArray();
        var messages = new List<string>();
        if (kinds.Contains(CleanupProtectionKind.UnenteredHideoutLevel))
            messages.Add("은신처 현재 레벨이 입력되지 않아 관련 수량의 정리 판단을 보류합니다.");
        if (kinds.Contains(CleanupProtectionKind.AlternativeQuestRequirement))
            messages.Add("유동 제출 후보 아이템이므로 목표가 끝날 때까지 후보별 정리 가능 수량을 자동 판단하지 않습니다.");
        return string.Join(" ", messages);
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

    private sealed record ItemRow(
        string ItemId,
        string Name,
        int RequiredTotal,
        int RequiredFir,
        int OwnedFir,
        int OwnedNonFir,
        int RemainingTotal,
        int RemainingFir,
        int SurplusFir,
        int SurplusNonFir,
        IReadOnlyList<CleanupProtection> Protections,
        string[] Sources,
        string Summary,
        string StatusText,
        Brush StatusBrush)
    {
        public int OwnedTotal => OwnedFir + OwnedNonFir;
        public int SurplusTotal => SurplusFir + SurplusNonFir;
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
