using System.Windows;
using JunhyunHelper.Desktop.Quests;

namespace JunhyunHelper.Desktop;

public partial class MainWindow
{
    private async void QuestPage_SpecialTraderAccessRequested(
        object? sender,
        SpecialTraderAccessRequestedEventArgs e)
    {
        if (_activeProfile is null || _activeContent is null)
            return;

        var actionText = e.AccessAvailable ? "접근 복구" : "접근 상실";
        var decision = MessageBox.Show(
            this,
            $"게임 안에서 실제로 상인 {actionText} 상태가 된 경우에만 기록합니다.\n\n{actionText} 상태로 동기화할까요?",
            "상인 접근 상태 동기화",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question,
            MessageBoxResult.No);
        if (decision != MessageBoxResult.Yes)
            return;

        try
        {
            SetBusy(true);
            _ = await _services.Quests.SetSpecialTraderAccessAsync(
                _activeContent,
                _activeProfile.ProfileId,
                e.TraderId,
                e.AccessAvailable);

            await RefreshActiveWorkspacesAsync(detectCleanupChanges: true);

        }
        catch (Exception exception)
        {
            ShowFailure("상인 접근 상태를 변경하지 못했습니다.", exception);
        }
        finally
        {
            SetBusy(false);
        }
    }
}
