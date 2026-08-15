using System.Windows;
using System.Windows.Controls;
using JunhyunHelper.Core.Quests;

namespace JunhyunHelper.Desktop.Quests;

public sealed class SpecialTraderAccessRequestedEventArgs(
    string traderId,
    bool accessAvailable) : EventArgs
{
    public string TraderId { get; } = traderId;

    public bool AccessAvailable { get; } = accessAvailable;
}

public partial class QuestPage
{
    public event EventHandler<SpecialTraderAccessRequestedEventArgs>? SpecialTraderAccessRequested;

    private void QuestPage_SpecialTraderAccessVisibilityChanged(
        object sender,
        DependencyPropertyChangedEventArgs e)
    {
        QuestList.SelectionChanged -= QuestList_SpecialTraderAccessSelectionChanged;
        QuestList.SelectionChanged += QuestList_SpecialTraderAccessSelectionChanged;
        ConfigureSpecialTraderAccessButton();
    }

    private void QuestList_SpecialTraderAccessSelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        ConfigureSpecialTraderAccessButton();
    }

    private void ConfigureSpecialTraderAccessButton()
    {
        SpecialTraderAccessButton.Visibility = Visibility.Collapsed;
        SpecialTraderAccessButton.Tag = null;

        if (_workspace is null ||
            _content is null ||
            QuestList.SelectedItem is not QuestRow row ||
            row.Entry.Quest.SpecialTraderAccessRequirement is not { AllowManualOverride: true } requirement)
        {
            return;
        }

        var profile = _workspace.Profile;
        var effectiveFailed = QuestFailureEvaluator.EffectiveFailedQuestIds(
            _content.Quests,
            profile);
        var unlockTerminal =
            profile.CompletedQuestIds.Contains(requirement.UnlockQuestId) ||
            effectiveFailed.Contains(requirement.UnlockQuestId);
        if (!unlockTerminal)
            return;

        var automaticAvailable = AutomaticSpecialTraderAccessAvailable(
            requirement,
            profile.CompletedQuestIds.Contains(requirement.UnlockQuestId),
            effectiveFailed.Contains(requirement.UnlockQuestId));
        var effectiveAvailable = profile.SpecialTraderAccessOverrides.TryGetValue(
            requirement.TraderId,
            out var overrideAvailable)
            ? overrideAvailable
            : automaticAvailable;

        var traderName = _content.Traders
            .FirstOrDefault(trader => string.Equals(
                trader.Id,
                requirement.TraderId,
                StringComparison.Ordinal));
        var displayName = traderName is null
            ? requirement.TraderId
            : DisplayName(traderName.NameKo, traderName.NameEn, requirement.TraderId);

        var nextAvailable = !effectiveAvailable;
        SpecialTraderAccessButton.Content = effectiveAvailable
            ? $"{displayName} 접근 상실 기록"
            : $"{displayName} 접근 복구 기록";
        SpecialTraderAccessButton.ToolTip = effectiveAvailable
            ? "게임에서 이 상인 접근권을 실제로 잃은 경우에만 사용합니다."
            : "게임에서 이 상인 접근권을 실제로 복구한 경우에만 사용합니다.";
        SpecialTraderAccessButton.Tag = new SpecialTraderAccessAction(
            requirement.TraderId,
            nextAvailable);
        SpecialTraderAccessButton.Visibility = Visibility.Visible;
    }

    private void SpecialTraderAccessButton_Click(object sender, RoutedEventArgs e)
    {
        if (SpecialTraderAccessButton.Tag is not SpecialTraderAccessAction action)
            return;

        SpecialTraderAccessRequested?.Invoke(
            this,
            new SpecialTraderAccessRequestedEventArgs(
                action.TraderId,
                action.AccessAvailable));
    }

    private static bool AutomaticSpecialTraderAccessAvailable(
        QuestSpecialTraderAccessRequirement requirement,
        bool unlockCompleted,
        bool unlockFailed)
    {
        if (unlockCompleted)
        {
            return requirement.AcceptedUnlockStatuses.Contains(QuestRequiredStatus.Complete) ||
                   requirement.AcceptedUnlockStatuses.Contains(QuestRequiredStatus.Active);
        }

        return unlockFailed &&
               requirement.AcceptedUnlockStatuses.Contains(QuestRequiredStatus.Failed);
    }

    private sealed record SpecialTraderAccessAction(
        string TraderId,
        bool AccessAvailable);
}
