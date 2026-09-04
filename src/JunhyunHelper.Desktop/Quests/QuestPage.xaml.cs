using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using JunhyunHelper.Application.Quests;
using JunhyunHelper.Core.Content;
using JunhyunHelper.Core.Items;
using JunhyunHelper.Core.Profiles;
using JunhyunHelper.Core.Quests;
using JunhyunHelper.Core.Reference;
using JunhyunHelper.Desktop.Controls;
using JunhyunHelper.Desktop.Services;

namespace JunhyunHelper.Desktop.Quests;

public enum QuestActionKind
{
    Complete,
    UndoCompletion,
    Fail,
    UndoFailure,
}

public sealed class QuestActionRequestedEventArgs(
    string questId,
    QuestActionKind action) : EventArgs
{
    public string QuestId { get; } = questId;

    public QuestActionKind Action { get; } = action;
}

public partial class QuestPage : UserControl
{
    private GameContentCatalog? _content;
    private GameContentCatalog? _indexedContent;
    private IReadOnlyDictionary<string, TraderDefinition> _tradersById = new Dictionary<string, TraderDefinition>(StringComparer.Ordinal);
    private IReadOnlyDictionary<string, MapReference> _mapsById = new Dictionary<string, MapReference>(StringComparer.Ordinal);
    private IReadOnlyDictionary<string, GameItem> _itemsById = new Dictionary<string, GameItem>(StringComparer.Ordinal);
    private IReadOnlyDictionary<string, QuestDefinition> _questsById = new Dictionary<string, QuestDefinition>(StringComparer.Ordinal);
    private IReadOnlyDictionary<string, IReadOnlyList<QuestObjective>> _objectivesByQuestId =
        new Dictionary<string, IReadOnlyList<QuestObjective>>(StringComparer.Ordinal);
    private IReadOnlyDictionary<string, IReadOnlyList<QuestItemRequirement>> _itemRequirementsByQuestId =
        new Dictionary<string, IReadOnlyList<QuestItemRequirement>>(StringComparer.Ordinal);
    private QuestWorkspace? _workspace;
    private IReadOnlyList<QuestRow> _rows = [];
    private bool _updatingFilters;

    public QuestPage()
    {
        InitializeComponent();
        ProductSearchClearButtonBehavior.Attach(SearchBox);
        PopulateStatusFilter();
    }

    public event EventHandler<QuestActionRequestedEventArgs>? ActionRequested;

    public void SetData(GameContentCatalog content, QuestWorkspace workspace)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(workspace);

        var selectedQuestId = (QuestList.SelectedItem as QuestRow)?.Entry.Quest.Id;
        EnsureContentIndexes(content);
        _content = content;
        _workspace = workspace;

        _rows = workspace.Quests
            .Select(entry => new QuestRow(
                entry,
                DisplayName(entry.Quest.NameKo, entry.Quest.NameEn, entry.Quest.Id),
                BuildSubtitle(entry.Quest, _tradersById, _mapsById),
                StatusText(entry.Availability.State),
                StatusBrush(entry.Availability.State),
                MapFilterKey(entry.Quest.MapId, _mapsById)))
            .OrderBy(row => row.Name, StringComparer.CurrentCulture)
            .ToArray();

        PopulateReferenceFilters(_tradersById, _mapsById);
        ProblemsButton.Content = $"확인 필요 원인 {workspace.Problems.Count}";
        ProblemsButton.IsEnabled = workspace.Problems.Count > 0;

        ApplyFilters();

        if (!string.IsNullOrWhiteSpace(selectedQuestId))
        {
            var selected = (QuestList.ItemsSource as IEnumerable<QuestRow>)?
                .FirstOrDefault(row => row.Entry.Quest.Id == selectedQuestId);
            if (selected is not null)
            {
                QuestList.SelectedItem = selected;
                QuestList.ScrollIntoView(selected);
            }
        }
    }

    public void SetBusy(bool busy)
    {
        IsEnabled = !busy;
    }

    private void EnsureContentIndexes(GameContentCatalog content)
    {
        if (ReferenceEquals(_indexedContent, content))
            return;

        _tradersById = content.Traders.ToDictionary(trader => trader.Id, StringComparer.Ordinal);
        _mapsById = content.Maps.ToDictionary(map => map.Id, StringComparer.Ordinal);
        _itemsById = content.Items.ToDictionary(item => item.Id, StringComparer.Ordinal);
        _questsById = content.Quests.ToDictionary(quest => quest.Id, StringComparer.Ordinal);
        _objectivesByQuestId = content.QuestObjectives
            .GroupBy(objective => objective.QuestId, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<QuestObjective>)group.ToArray(),
                StringComparer.Ordinal);
        _itemRequirementsByQuestId = content.QuestItemRequirements
            .GroupBy(requirement => requirement.QuestId, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<QuestItemRequirement>)group.ToArray(),
                StringComparer.Ordinal);
        _indexedContent = content;
    }

    private IReadOnlyList<QuestObjective> QuestObjectivesFor(string questId) =>
        _objectivesByQuestId.TryGetValue(questId, out var values)
            ? values
            : Array.Empty<QuestObjective>();

    private IReadOnlyList<QuestItemRequirement> QuestItemRequirementsFor(string questId) =>
        _itemRequirementsByQuestId.TryGetValue(questId, out var values)
            ? values
            : Array.Empty<QuestItemRequirement>();

    private void PopulateStatusFilter()
    {
        _updatingFilters = true;
        StatusFilter.ItemsSource = new[]
        {
            new FilterOption("진행 중", QuestAvailabilityState.Current.ToString()),
            new FilterOption("전체", null),
            new FilterOption("확인 필요", QuestAvailabilityState.Indeterminate.ToString()),
            new FilterOption("잠김", QuestAvailabilityState.Locked.ToString()),
            new FilterOption("사용 불가", QuestAvailabilityState.Unavailable.ToString()),
            new FilterOption("완료", QuestAvailabilityState.Completed.ToString()),
        };
        StatusFilter.SelectedIndex = 0;
        _updatingFilters = false;
    }

    private void PopulateReferenceFilters(
        IReadOnlyDictionary<string, Core.Reference.TraderDefinition> traders,
        IReadOnlyDictionary<string, Core.Reference.MapReference> maps)
    {
        var selectedTrader = (TraderFilter.SelectedItem as FilterOption)?.Value;
        var selectedMap = (MapFilter.SelectedItem as FilterOption)?.Value;

        var traderIds = _rows
            .Select(row => row.Entry.Quest.TraderId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Cast<string>()
            .Distinct(StringComparer.Ordinal)
            .OrderBy(id => traders.TryGetValue(id, out var trader)
                ? UiReferenceOrder.TraderRank(trader)
                : int.MaxValue)
            .ThenBy(id => traders.TryGetValue(id, out var trader)
                ? DisplayName(trader.NameKo, trader.NameEn, id)
                : id,
                StringComparer.CurrentCulture)
            .ToArray();

        var mapGroups = _rows
            .Select(row => row.Entry.Quest.MapId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Cast<string>()
            .Distinct(StringComparer.Ordinal)
            .Select(id =>
            {
                maps.TryGetValue(id, out var map);
                var label = map is null
                    ? id
                    : DisplayName(map.NameKo, map.NameEn, id);
                var key = map is null
                    ? $"map:{id}"
                    : UiReferenceOrder.MapFilterKey(map);
                var rank = UiReferenceOrder.MapRank(map);
                var highVariant = UiReferenceOrder.IsGroundZeroHighVariant(map);
                return new MapFilterCandidate(id, key, label, rank, highVariant);
            })
            .GroupBy(candidate => candidate.FilterKey, StringComparer.Ordinal)
            .Select(group =>
            {
                var representative = group
                    .OrderBy(candidate => candidate.IsGroundZeroHighVariant)
                    .ThenBy(candidate => candidate.Label, StringComparer.CurrentCulture)
                    .First();
                return new MapFilterGroup(
                    group.Key,
                    representative.Label,
                    group.Min(candidate => candidate.Rank));
            })
            .OrderBy(group => group.Rank)
            .ThenBy(group => group.Label, StringComparer.CurrentCulture)
            .ToArray();

        _updatingFilters = true;
        TraderFilter.ItemsSource = new[] { new FilterOption("모든 상인", null) }
            .Concat(traderIds.Select(id => new FilterOption(
                traders.TryGetValue(id, out var trader)
                    ? DisplayName(trader.NameKo, trader.NameEn, id)
                    : id,
                id)))
            .ToArray();
        TraderFilter.SelectedItem = TraderFilter.Items
            .Cast<FilterOption>()
            .FirstOrDefault(option => option.Value == selectedTrader) ?? TraderFilter.Items[0];

        MapFilter.ItemsSource = new[] { new FilterOption("모든 지도", null) }
            .Concat(mapGroups.Select(group => new FilterOption(group.Label, group.FilterKey)))
            .ToArray();
        MapFilter.SelectedItem = MapFilter.Items
            .Cast<FilterOption>()
            .FirstOrDefault(option => option.Value == selectedMap) ?? MapFilter.Items[0];
        _updatingFilters = false;
    }

    private void FilterChanged(object sender, EventArgs e)
    {
        if (!_updatingFilters)
            ApplyFilters();
    }

    private void ApplyFilters()
    {
        if (_workspace is null)
            return;

        var search = SearchBox.Text?.Trim() ?? string.Empty;
        var status = (StatusFilter.SelectedItem as FilterOption)?.Value;
        var traderId = (TraderFilter.SelectedItem as FilterOption)?.Value;
        var mapKey = (MapFilter.SelectedItem as FilterOption)?.Value;

        var filtered = _rows.Where(row =>
        {
            if (!string.IsNullOrWhiteSpace(search) &&
                !row.Name.Contains(search, StringComparison.CurrentCultureIgnoreCase) &&
                !(row.Entry.Quest.NameEn?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false))
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(status) &&
                !string.Equals(row.Entry.Availability.State.ToString(), status, StringComparison.Ordinal))
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(traderId) &&
                !string.Equals(row.Entry.Quest.TraderId, traderId, StringComparison.Ordinal))
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(mapKey) &&
                !string.Equals(row.MapFilterKey, mapKey, StringComparison.Ordinal))
            {
                return false;
            }

            return true;
        }).ToArray();

        QuestList.ItemsSource = filtered;
        SummaryText.Text = $"{filtered.Length}개 표시 · 진행 중 {_rows.Count(row => row.Entry.Availability.State == QuestAvailabilityState.Current)} · " +
                           $"확인 필요 {_rows.Count(row => row.Entry.Availability.State == QuestAvailabilityState.Indeterminate)} · " +
                           $"잠김 {_rows.Count(row => row.Entry.Availability.State == QuestAvailabilityState.Locked)} · " +
                           $"사용 불가 {_rows.Count(row => row.Entry.Availability.State == QuestAvailabilityState.Unavailable)} · " +
                           $"완료 {_rows.Count(row => row.Entry.Availability.State == QuestAvailabilityState.Completed)}";

        if (QuestList.SelectedItem is null)
            ClearDetail();
    }

    private void QuestList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (QuestList.SelectedItem is QuestRow row)
            ShowDetail(row.Entry);
        else
            ClearDetail();
    }

    private void ShowDetail(QuestCatalogEntry entry)
    {
        if (_content is null)
            return;

        EmptyDetailText.Visibility = Visibility.Collapsed;
        DetailScroll.Visibility = Visibility.Visible;

        var quest = entry.Quest;

        DetailName.Text = DisplayName(quest.NameKo, quest.NameEn, quest.Id);
        DetailMeta.Text = BuildSubtitle(quest, _tradersById, _mapsById) + $" · {StatusText(entry.Availability.State)}";

        WikiButton.IsEnabled = !string.IsNullOrWhiteSpace(quest.WikiUrl);
        PrimaryActionButton.Visibility = entry.Availability.State switch
        {
            QuestAvailabilityState.Current => Visibility.Visible,
            QuestAvailabilityState.Indeterminate => Visibility.Visible,
            QuestAvailabilityState.Completed => Visibility.Visible,
            _ => Visibility.Collapsed,
        };
        PrimaryActionButton.Content = entry.Availability.State == QuestAvailabilityState.Completed
            ? "완료 취소"
            : "완료";
        PrimaryActionButton.Tag = entry;

        var explicitlyFailed = _workspace?.Profile.FailedQuestIds.Contains(quest.Id) == true;
        var canRecordFailure = quest.RequiresExplicitFailureInput &&
                               entry.Availability.State is QuestAvailabilityState.Current or QuestAvailabilityState.Indeterminate;
        FailureActionButton.Visibility =
            (canRecordFailure ||
             (entry.Availability.State == QuestAvailabilityState.Unavailable && explicitlyFailed))
                ? Visibility.Visible
                : Visibility.Collapsed;
        FailureActionButton.Content = explicitlyFailed ? "실패 취소" : "실패 처리";
        FailureActionButton.Tag = entry;

        var objectives = QuestObjectivesFor(quest.Id)
            .Select(objective =>
            {
                var description = DisplayName(
                    objective.DescriptionKo,
                    objective.DescriptionEn,
                    objective.Type);
                var optional = objective.Optional ? " [선택 사항]" : string.Empty;
                return $"• {description}{optional}";
            })
            .ToArray();
        ObjectivesList.ItemsSource = objectives.Length > 0 ? objectives : ["표시할 목표 정보가 없습니다."];

        var requiredItems = QuestItemRequirementsFor(quest.Id)
            .Select(requirement =>
            {
                var accepted = requirement.AcceptedItemIds
                    .Select(id => _itemsById.TryGetValue(id, out var item)
                        ? DisplayName(item.NameKo, item.NameEn, id)
                        : id)
                    .ToArray();
                var itemText = string.Join(" 또는 ", accepted);
                var fir = requirement.FoundInRaid ? " · FIR" : string.Empty;
                return $"• {itemText} × {requirement.Count}{fir}";
            })
            .ToArray();
        RequiredItemsHeader.Visibility = requiredItems.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
        RequiredItemsBox.Visibility = requiredItems.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
        RequiredItemsList.ItemsSource = requiredItems;

        RequirementsList.ItemsSource = BuildRequirements(quest, _content, _tradersById);

        var prerequisites = quest.TaskRequirements
            .Select(requirement =>
            {
                var name = _questsById.TryGetValue(requirement.RequiredQuestId, out var prerequisite)
                    ? DisplayName(prerequisite.NameKo, prerequisite.NameEn, requirement.RequiredQuestId)
                    : requirement.RequiredQuestId;
                var states = string.Join(" / ", requirement.AcceptedStatuses.Select(StatusText));
                return $"• {name} ({states})";
            })
            .ToArray();
        PrerequisitesHeader.Visibility = prerequisites.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
        PrerequisitesBox.Visibility = prerequisites.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
        PrerequisitesList.ItemsSource = prerequisites;

        var reasons = entry.Availability.Reasons
            .Select(reason => $"• {ReasonText(reason, _content, _tradersById)}")
            .ToArray();
        StateReasonsHeader.Visibility = reasons.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
        StateReasonsBox.Visibility = reasons.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
        StateReasonsList.ItemsSource = reasons;

        ExperienceText.Text = quest.Experience is > 0
            ? $"보상 경험치: {quest.Experience:N0} XP"
            : string.Empty;
    }

    private static IReadOnlyList<string> BuildRequirements(
        QuestDefinition quest,
        GameContentCatalog content,
        IReadOnlyDictionary<string, Core.Reference.TraderDefinition> traders)
    {
        var lines = new List<string>();

        if (quest.MinimumPlayerLevel > 1)
            lines.Add($"• 레벨 {quest.MinimumPlayerLevel} 이상");
        if (quest.RequiredFaction is { } faction)
            lines.Add($"• 진영: {FactionText(faction)}");
        if (quest.RequiredPrestigeLevel is { } prestige)
            lines.Add($"• 프레스티지 {prestige} 이상");

        foreach (var requirement in quest.TraderStandingRequirements)
        {
            var traderName = traders.TryGetValue(requirement.TraderId, out var trader)
                ? DisplayName(trader.NameKo, trader.NameEn, requirement.TraderId)
                : requirement.TraderId;
            lines.Add($"• {traderName} 평판 {StandingOperatorText(requirement.Operator)} {requirement.RequiredStanding}");
        }

        foreach (var requirement in quest.TraderLoyaltyRequirements)
        {
            var traderName = traders.TryGetValue(requirement.TraderId, out var trader)
                ? DisplayName(trader.NameKo, trader.NameEn, requirement.TraderId)
                : requirement.TraderId;
            lines.Add($"• {traderName} LL {requirement.RequiredLoyaltyLevel} 이상");
        }

        var exclusive = content.Editions
            .Where(edition => edition.ExclusiveQuestIds.Contains(quest.Id))
            .Select(edition => edition.Title)
            .ToArray();
        if (exclusive.Length > 0)
            lines.Add($"• 에디션 전용: {string.Join(", ", exclusive)}");

        var excluded = content.Editions
            .Where(edition => edition.ExcludedQuestIds.Contains(quest.Id))
            .Select(edition => edition.Title)
            .ToArray();
        if (excluded.Length > 0)
            lines.Add($"• 제외 에디션: {string.Join(", ", excluded)}");

        foreach (var unsupported in quest.UnsupportedAvailabilityRequirements)
            lines.Add($"• 현재 판정 미지원 조건: {unsupported}");

        if (quest.RequiresExplicitFailureInput)
        {
            lines.Add($"• 영구 실패 시 수동 동기화 필요: {string.Join(", ", quest.UnsupportedFailureConditions)}");
        }

        return lines.Count > 0 ? lines : ["특별한 해금 조건이 없습니다."];
    }

    private void ClearDetail()
    {
        DetailScroll.Visibility = Visibility.Collapsed;
        EmptyDetailText.Visibility = Visibility.Visible;
    }

    private void PrimaryActionButton_Click(object sender, RoutedEventArgs e)
    {
        if (PrimaryActionButton.Tag is not QuestCatalogEntry entry)
            return;

        var action = entry.Availability.State == QuestAvailabilityState.Completed
            ? QuestActionKind.UndoCompletion
            : QuestActionKind.Complete;
        ActionRequested?.Invoke(this, new QuestActionRequestedEventArgs(entry.Quest.Id, action));
    }

    private void FailureActionButton_Click(object sender, RoutedEventArgs e)
    {
        if (FailureActionButton.Tag is not QuestCatalogEntry entry || _workspace is null)
            return;

        var explicitlyFailed = _workspace.Profile.FailedQuestIds.Contains(entry.Quest.Id);
        var action = explicitlyFailed
            ? QuestActionKind.UndoFailure
            : QuestActionKind.Fail;
        ActionRequested?.Invoke(this, new QuestActionRequestedEventArgs(entry.Quest.Id, action));
    }

    private void WikiButton_Click(object sender, RoutedEventArgs e)
    {
        if (QuestList.SelectedItem is not QuestRow row ||
            string.IsNullOrWhiteSpace(row.Entry.Quest.WikiUrl))
        {
            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = row.Entry.Quest.WikiUrl,
            UseShellExecute = true,
        });
    }

    private void ProblemsButton_Click(object sender, RoutedEventArgs e)
    {
        if (_workspace is null || _content is null || _workspace.Problems.Count == 0)
            return;

        var text = string.Join(
            Environment.NewLine + Environment.NewLine,
            _workspace.Problems.Select(problem =>
            {
                var name = DisplayName(problem.Quest.NameKo, problem.Quest.NameEn, problem.Quest.Id);
                var reasons = string.Join(
                    Environment.NewLine,
                    problem.Availability.Reasons.Select(reason =>
                        $"  • {ReasonText(reason, _content, _tradersById)}"));
                return $"{name}{Environment.NewLine}{reasons}";
            }));

        MessageBox.Show(
            Window.GetWindow(this),
            text,
            $"확인 필요 원인 {_workspace.Problems.Count}건",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
    }

    private static string BuildSubtitle(
        QuestDefinition quest,
        IReadOnlyDictionary<string, Core.Reference.TraderDefinition> traders,
        IReadOnlyDictionary<string, Core.Reference.MapReference> maps)
    {
        var trader = quest.TraderId is not null && traders.TryGetValue(quest.TraderId, out var traderValue)
            ? DisplayName(traderValue.NameKo, traderValue.NameEn, quest.TraderId)
            : "상인 없음";
        var map = quest.MapId is not null && maps.TryGetValue(quest.MapId, out var mapValue)
            ? DisplayName(mapValue.NameKo, mapValue.NameEn, quest.MapId)
            : "지도 없음";
        return $"{trader} · {map}";
    }

    private static string? MapFilterKey(
        string? mapId,
        IReadOnlyDictionary<string, Core.Reference.MapReference> maps)
    {
        if (string.IsNullOrWhiteSpace(mapId))
            return null;

        return maps.TryGetValue(mapId, out var map)
            ? UiReferenceOrder.MapFilterKey(map)
            : $"map:{mapId}";
    }

    private static string ReasonText(
        QuestAvailabilityReason reason,
        GameContentCatalog content,
        IReadOnlyDictionary<string, Core.Reference.TraderDefinition> traders)
    {
        return reason.Kind switch
        {
            QuestAvailabilityReasonKind.Disabled => "현재 데이터에서 비활성화된 퀘스트입니다.",
            QuestAvailabilityReasonKind.Failed => "게임에서 영구 실패한 것으로 기록한 퀘스트입니다.",
            QuestAvailabilityReasonKind.FailedByQuest => $"퀘스트 '{QuestName(reason.ReferenceId, content)}' 완료로 이 진행 경로가 종료되었습니다.",
            QuestAvailabilityReasonKind.MinimumLevel => "요구 레벨을 충족하지 않았습니다.",
            QuestAvailabilityReasonKind.Faction => "현재 진영에서 수행할 수 없습니다.",
            QuestAvailabilityReasonKind.Edition => "현재 에디션 조건을 충족하지 않았습니다.",
            QuestAvailabilityReasonKind.Prestige => "요구 프레스티지를 충족하지 않았습니다.",
            QuestAvailabilityReasonKind.TraderStanding => $"{TraderName(reason.ReferenceId, traders)} 평판 조건을 충족하지 않았습니다.",
            QuestAvailabilityReasonKind.TraderLoyalty => $"{TraderName(reason.ReferenceId, traders)} 로열티 조건을 충족하지 않았습니다.",
            QuestAvailabilityReasonKind.Prerequisite => $"선행 퀘스트 '{QuestName(reason.ReferenceId, content)}' 조건을 충족하지 않았습니다.",
            QuestAvailabilityReasonKind.PrerequisiteUnavailable => $"선행 퀘스트 '{QuestName(reason.ReferenceId, content)}' 경로가 더 이상 사용 가능하지 않습니다.",
            QuestAvailabilityReasonKind.MissingProfileValue => $"프로필 값이 필요합니다: {reason.ReferenceId ?? "알 수 없음"}",
            QuestAvailabilityReasonKind.UnsupportedAvailabilityRequirement => $"아직 지원하지 않는 해금 조건입니다: {reason.ReferenceId ?? "알 수 없음"}",
            QuestAvailabilityReasonKind.MissingReferencedQuest => $"참조 퀘스트를 찾을 수 없습니다: {reason.ReferenceId ?? "알 수 없음"}",
            QuestAvailabilityReasonKind.DependencyCycle => $"퀘스트 선행 관계에 순환 참조가 있습니다: {reason.ReferenceId ?? "알 수 없음"}",
            _ => reason.Kind.ToString(),
        };
    }

    private static string QuestName(string? questId, GameContentCatalog content)
    {
        if (questId is null)
            return "알 수 없음";
        var quest = content.Quests.FirstOrDefault(candidate => candidate.Id == questId);
        return quest is null ? questId : DisplayName(quest.NameKo, quest.NameEn, questId);
    }

    private static string TraderName(
        string? traderId,
        IReadOnlyDictionary<string, Core.Reference.TraderDefinition> traders)
    {
        if (traderId is null)
            return "상인";
        return traders.TryGetValue(traderId, out var trader)
            ? DisplayName(trader.NameKo, trader.NameEn, traderId)
            : traderId;
    }

    private static string DisplayName(string? korean, string? english, string fallback) =>
        !string.IsNullOrWhiteSpace(korean)
            ? korean
            : !string.IsNullOrWhiteSpace(english)
                ? english
                : fallback;

    private static string StatusText(QuestAvailabilityState state) => state switch
    {
        QuestAvailabilityState.Current => "진행 중",
        QuestAvailabilityState.Locked => "잠김",
        QuestAvailabilityState.Unavailable => "사용 불가",
        QuestAvailabilityState.Completed => "완료",
        QuestAvailabilityState.Indeterminate => "확인 필요",
        _ => state.ToString(),
    };

    private static string StatusText(QuestRequiredStatus state) => state switch
    {
        QuestRequiredStatus.Complete => "완료",
        QuestRequiredStatus.Active => "진행 중",
        QuestRequiredStatus.Failed => "실패",
        _ => state.ToString(),
    };

    private static string FactionText(PmcFaction faction) => faction switch
    {
        PmcFaction.Usec => "USEC",
        PmcFaction.Bear => "BEAR",
        _ => faction.ToString(),
    };

    private static string StandingOperatorText(StandingRequirementOperator value) => value switch
    {
        StandingRequirementOperator.AtLeast => "≥",
        StandingRequirementOperator.AtMost => "≤",
        StandingRequirementOperator.LessThan => "<",
        _ => value.ToString(),
    };

    private static Brush StatusBrush(QuestAvailabilityState state) =>
        (Brush)System.Windows.Application.Current.FindResource(state switch
        {
            QuestAvailabilityState.Current => "SuccessBrush",
            QuestAvailabilityState.Completed => "AccentBrush",
            QuestAvailabilityState.Locked => "BackgroundLightBrush",
            QuestAvailabilityState.Unavailable => "WarningBrush",
            QuestAvailabilityState.Indeterminate => "WarningBrush",
            _ => "BackgroundLightBrush",
        });

    private sealed record QuestRow(
        QuestCatalogEntry Entry,
        string Name,
        string Subtitle,
        string StatusText,
        Brush StatusBrush,
        string? MapFilterKey);

    private sealed record FilterOption(string Label, string? Value)
    {
        public override string ToString() => Label;
    }

    private sealed record MapFilterCandidate(
        string MapId,
        string FilterKey,
        string Label,
        int Rank,
        bool IsGroundZeroHighVariant);

    private sealed record MapFilterGroup(
        string FilterKey,
        string Label,
        int Rank);
}
