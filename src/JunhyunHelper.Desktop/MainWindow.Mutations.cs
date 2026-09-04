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

    private async void QuestPage_ActionRequested(object? sender, QuestActionRequestedEventArgs e)
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
            SetBusy(true);

            var previousPlan = _activeItemsWorkspace?.Plan;
            var questWorkspace = e.Action switch
            {
                QuestActionKind.Complete => await _services.Quests.CompleteAsync(
                    _activeContent, _activeProfile.ProfileId, e.QuestId, _windowLifetimeCts.Token),
                QuestActionKind.UndoCompletion => await _services.Quests.UndoCompletionAsync(
                    _activeContent, _activeProfile.ProfileId, e.QuestId, restoreInventory, _windowLifetimeCts.Token),
                QuestActionKind.Fail => await _services.Quests.FailAsync(
                    _activeContent, _activeProfile.ProfileId, e.QuestId, _windowLifetimeCts.Token),
                QuestActionKind.UndoFailure => await _services.Quests.UndoFailureAsync(
                    _activeContent, _activeProfile.ProfileId, e.QuestId, _windowLifetimeCts.Token),
                _ => throw new ArgumentOutOfRangeException(nameof(e.Action), e.Action, null),
            };

            _activeProfile = questWorkspace.Profile;
            var itemsWorkspace = _services.Items.BuildFromProfile(_activeContent, _activeProfile);
            _activeItemsWorkspace = itemsWorkspace;

            QuestPage.SetDataPreservingScroll(_activeContent, questWorkspace);
            ItemsPage.SetData(_activeContent, itemsWorkspace);
            ApplyCleanupChanges(previousPlan, itemsWorkspace);

        }
        catch (OperationCanceledException) when (_windowLifetimeCts.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            await RecoverMutationPresentationAsync("Quest mutation presentation recovery failed");
            ShowFailure("퀘스트 진행 상태를 변경하지 못했습니다.", exception);
        }
        finally
        {
            if (!_windowLifetimeCts.IsCancellationRequested)
                SetBusy(false);
        }
    }

    private async void HideoutPage_LevelChangeRequested(
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
                {
                    // Hideout +/- controls update their row optimistically before the
                    // debounced mutation reaches this handler. A cancelled rollback is
                    // not a persistence failure, so restore the authoritative profile-
                    // derived presentation explicitly.
                    var authoritative = _services.Hideout.BuildFromProfile(_activeContent, _activeProfile);
                    HideoutPage.SetData(_activeContent, authoritative);
                    return;
                }
                restoreInventory = decision == MessageBoxResult.Yes;
            }
        }

        try
        {
            SetBusy(true);
            var previousPlan = _activeItemsWorkspace?.Plan;
            var hideoutWorkspace = await _services.Hideout.SetLevelAsync(
                _activeContent,
                _activeProfile.ProfileId,
                e.StationId,
                e.Level,
                restoreInventory,
                _windowLifetimeCts.Token);

            _activeProfile = hideoutWorkspace.Profile;

            // Hideout levels and inventory quantities do not participate in current
            // quest availability, so this mutation refreshes only affected workspaces.
            var itemsWorkspace = _services.Items.BuildFromProfile(_activeContent, _activeProfile);
            _activeItemsWorkspace = itemsWorkspace;

            HideoutPage.SetData(_activeContent, hideoutWorkspace);
            ItemsPage.SetData(_activeContent, itemsWorkspace);
            ApplyCleanupChanges(previousPlan, itemsWorkspace);

        }
        catch (OperationCanceledException) when (_windowLifetimeCts.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            await RecoverMutationPresentationAsync("Hideout mutation presentation recovery failed");
            ShowFailure("은신처 진행 상태를 변경하지 못했습니다.", exception);
        }
        finally
        {
            if (!_windowLifetimeCts.IsCancellationRequested)
                SetBusy(false);
        }
    }

    private async void ItemsPage_InventoryChangeRequested(
        object? sender,
        InventoryChangeRequestedEventArgs e)
    {
        if (_activeProfile is null || _activeContent is null)
            return;

        try
        {
            SetBusy(true);
            var previousPlan = _activeItemsWorkspace?.Plan;
            var itemsWorkspace = await _services.Items.SetInventoryAsync(
                _activeContent,
                _activeProfile.ProfileId,
                e.ItemId,
                e.Fir,
                e.NonFir,
                _windowLifetimeCts.Token);

            _activeProfile = itemsWorkspace.Profile;
            _activeItemsWorkspace = itemsWorkspace;

            // Inventory quantities affect Needed Items/Cleanup only, so this mutation
            // refreshes the Items workspace without rebuilding unrelated Quest state.
            ItemsPage.SetInventoryData(_activeContent, itemsWorkspace);
            ApplyCleanupChanges(previousPlan, itemsWorkspace);

        }
        catch (OperationCanceledException) when (_windowLifetimeCts.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            await RecoverMutationPresentationAsync("Inventory mutation presentation recovery failed");
            ShowFailure("보유 아이템 수량을 저장하지 못했습니다.", exception);
        }
        finally
        {
            if (!_windowLifetimeCts.IsCancellationRequested)
                SetBusy(false);
        }
    }

    private async Task RecoverMutationPresentationAsync(string diagnosticContext)
    {
        if (_activeProfile is null || _activeContent is null)
            return;

        try
        {
            // Mutation pages may optimistically show a debounced +/- result before the
            // authoritative SQLite write finishes. If that write fails, rebuild all
            // profile-derived pages from UserProfileStore so the UI cannot keep showing
            // a value that was never persisted. If the write committed but a later
            // presentation rebuild failed, the store cache exposes the committed value.
            await RefreshActiveWorkspacesAsync(detectCleanupChanges: false);
        }
        catch (OperationCanceledException) when (_windowLifetimeCts.IsCancellationRequested)
        {
        }
        catch (Exception recoveryException)
        {
            App.WriteDiagnostic(diagnosticContext, recoveryException);
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
