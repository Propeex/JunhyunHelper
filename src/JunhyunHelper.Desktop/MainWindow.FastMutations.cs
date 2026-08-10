using System.Windows;
using JunhyunHelper.Application.Hideout;
using JunhyunHelper.Application.Items;
using JunhyunHelper.Application.Quests;
using JunhyunHelper.Core.Items;
using JunhyunHelper.Desktop.Hideout;
using JunhyunHelper.Desktop.Items;
using JunhyunHelper.Desktop.Quests;

namespace JunhyunHelper.Desktop;

public partial class MainWindow
{
    private void EnableFastMutationHandlers()
    {
        QuestPage.ActionRequested -= QuestPage_ActionRequested;
        QuestPage.ActionRequested -= QuestPage_ActionRequestedFast;
        QuestPage.ActionRequested += QuestPage_ActionRequestedFast;

        HideoutPage.LevelChangeRequested -= HideoutPage_LevelChangeRequested;
        HideoutPage.LevelChangeRequested -= HideoutPage_LevelChangeRequestedFast;
        HideoutPage.LevelChangeRequested += HideoutPage_LevelChangeRequestedFast;

        ItemsPage.InventoryChangeRequested -= ItemsPage_InventoryChangeRequested;
        ItemsPage.InventoryChangeRequested -= ItemsPage_InventoryChangeRequestedFast;
        ItemsPage.InventoryChangeRequested += ItemsPage_InventoryChangeRequestedFast;
    }

    private async void QuestPage_ActionRequestedFast(object? sender, QuestActionRequestedEventArgs e)
    {
        if (_activeProfile is null || _activeContent is null)
            return;

        var restoreInventory = false;
        if (e.Action == QuestActionKind.UndoCompletion &&
            _activeProfile.QuestConsumptions.TryGetValue(e.QuestId, out var consumption) &&
            !consumption.IsEmpty)
        {
            var decision = MessageBox.Show(
                this,
                "이 퀘스트를 완료할 때 자동으로 차감한 보유 아이템 기록이 있습니다.\n\n차감했던 수량을 보유량에 다시 복원할까요?",
                "퀘스트 완료 취소",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question,
                MessageBoxResult.Yes);
            if (decision == MessageBoxResult.Cancel)
                return;
            restoreInventory = decision == MessageBoxResult.Yes;
        }

        try
        {
            SetBusy(true, e.Action switch
            {
                QuestActionKind.Complete => "퀘스트 완료를 저장하는 중...",
                QuestActionKind.UndoCompletion => "퀘스트 완료를 취소하는 중...",
                QuestActionKind.Fail => "퀘스트 실패를 저장하는 중...",
                QuestActionKind.UndoFailure => "퀘스트 실패를 취소하는 중...",
                _ => "퀘스트 진행 상태를 저장하는 중...",
            });

            var previousPlan = _activeItemsWorkspace?.Plan;
            var questWorkspace = e.Action switch
            {
                QuestActionKind.Complete => await _services.Quests.CompleteAsync(
                    _activeContent, _activeProfile.ProfileId, e.QuestId),
                QuestActionKind.UndoCompletion => await _services.Quests.UndoCompletionAsync(
                    _activeContent, _activeProfile.ProfileId, e.QuestId, restoreInventory),
                QuestActionKind.Fail => await _services.Quests.FailAsync(
                    _activeContent, _activeProfile.ProfileId, e.QuestId),
                QuestActionKind.UndoFailure => await _services.Quests.UndoFailureAsync(
                    _activeContent, _activeProfile.ProfileId, e.QuestId),
                _ => throw new ArgumentOutOfRangeException(nameof(e.Action), e.Action, null),
            };

            _activeProfile = questWorkspace.Profile;
            var itemsWorkspace = _services.Items.BuildFromProfile(_activeContent, _activeProfile);
            _activeItemsWorkspace = itemsWorkspace;

            QuestPage.SetDataPreservingScroll(_activeContent, questWorkspace);
            ItemsPage.SetData(_activeContent, itemsWorkspace);
            ApplyCleanupChanges(previousPlan, itemsWorkspace);
            StatusText.Text = BuildLoadedStatus(_activeProfile.GameMode);
        }
        catch (Exception exception)
        {
            ShowFailure("퀘스트 진행 상태를 변경하지 못했습니다.", exception);
        }
        finally
        {
            SetBusy(false, StatusText.Text);
        }
    }

    private async void HideoutPage_LevelChangeRequestedFast(
        object? sender,
        HideoutLevelChangeRequestedEventArgs e)
    {
        if (_activeProfile is null || _activeContent is null)
            return;

        var currentLevel = _activeProfile.HideoutLevels.TryGetValue(e.StationId, out var savedLevel)
            ? savedLevel
            : 0;
        var targetLevel = e.Level ?? 0;
        var restoreInventory = false;

        if (targetLevel < currentLevel)
        {
            var hasConsumption = Enumerable.Range(targetLevel + 1, currentLevel - targetLevel)
                .Any(level => _activeProfile.HideoutUpgradeConsumptions.ContainsKey(
                    HideoutApplicationService.UpgradeConsumptionKey(e.StationId, level)));
            if (hasConsumption)
            {
                var decision = MessageBox.Show(
                    this,
                    "되돌리는 은신처 업그레이드에서 자동으로 차감한 보유 아이템 기록이 있습니다.\n\n차감했던 수량을 보유량에 다시 복원할까요?",
                    "은신처 레벨 되돌리기",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question,
                    MessageBoxResult.Yes);
                if (decision == MessageBoxResult.Cancel)
                    return;
                restoreInventory = decision == MessageBoxResult.Yes;
            }
        }

        try
        {
            SetBusy(true, "은신처 레벨을 저장하는 중...");
            var previousPlan = _activeItemsWorkspace?.Plan;
            var hideoutWorkspace = await _services.Hideout.SetLevelAsync(
                _activeContent,
                _activeProfile.ProfileId,
                e.StationId,
                e.Level,
                restoreInventory);

            _activeProfile = hideoutWorkspace.Profile;

            var questWorkspace = _services.Quests.BuildFromProfile(_activeContent, _activeProfile);
            var itemsWorkspace = _services.Items.BuildFromProfile(_activeContent, _activeProfile);
            _activeItemsWorkspace = itemsWorkspace;

            HideoutPage.SetData(_activeContent, hideoutWorkspace);
            QuestPage.SetDataPreservingScroll(_activeContent, questWorkspace);
            ItemsPage.SetData(_activeContent, itemsWorkspace);
            ApplyCleanupChanges(previousPlan, itemsWorkspace);
            StatusText.Text = BuildLoadedStatus(_activeProfile.GameMode);
        }
        catch (Exception exception)
        {
            ShowFailure("은신처 진행 상태를 변경하지 못했습니다.", exception);
        }
        finally
        {
            SetBusy(false, StatusText.Text);
        }
    }

    private async void ItemsPage_InventoryChangeRequestedFast(
        object? sender,
        InventoryChangeRequestedEventArgs e)
    {
        if (_activeProfile is null || _activeContent is null)
            return;

        try
        {
            SetBusy(true, "보유 아이템 수량을 저장하는 중...");
            var previousPlan = _activeItemsWorkspace?.Plan;
            var itemsWorkspace = await _services.Items.SetInventoryAsync(
                _activeContent,
                _activeProfile.ProfileId,
                e.ItemId,
                e.Fir,
                e.NonFir);

            _activeProfile = itemsWorkspace.Profile;
            _activeItemsWorkspace = itemsWorkspace;

            var questWorkspace = _services.Quests.BuildFromProfile(_activeContent, _activeProfile);
            QuestPage.SetDataPreservingScroll(_activeContent, questWorkspace);
            ItemsPage.SetData(_activeContent, itemsWorkspace);
            ApplyCleanupChanges(previousPlan, itemsWorkspace);
            StatusText.Text = BuildLoadedStatus(_activeProfile.GameMode);
        }
        catch (Exception exception)
        {
            ShowFailure("보유 아이템 수량을 저장하지 못했습니다.", exception);
        }
        finally
        {
            SetBusy(false, StatusText.Text);
        }
    }

    private void ApplyCleanupChanges(FutureNeededItemsPlan? previousPlan, ItemsWorkspace current)
    {
        if (previousPlan is null)
        {
            ItemsPage.ClearCleanupNotice();
            return;
        }

        var changes = InventoryCleanupChangeDetector.FindIncreases(previousPlan, current.Plan);
        ItemsPage.SetCleanupChanges(changes);
    }
}
