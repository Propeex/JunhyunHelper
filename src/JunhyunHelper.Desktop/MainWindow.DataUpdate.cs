using System.IO;
using System.Windows;

namespace JunhyunHelper.Desktop;

public partial class MainWindow
{
    /// <summary>
    /// User-facing data update entry point. Canonical game content is committed first;
    /// Scanner identity/market data is then refreshed for the same active profile mode.
    /// A Scanner-only refresh failure never rolls back already-validated game content.
    /// </summary>
    private async void UnifiedUpdateDataButton_Click(object sender, RoutedEventArgs e)
    {
        if (_activeProfile is null)
            return;

        try
        {
            SetBusy(true, "최신 게임 데이터를 업데이트하는 중...");
            var gameMode = _activeProfile.GameMode;
            var result = await RunContentUpdateAsync(gameMode);
            if (!result.Applied)
            {
                throw new InvalidDataException(
                    "새 데이터가 검증을 통과하지 못해 기존 정상 데이터를 유지했습니다.");
            }

            var snapshot = await _services.Content.ReadActiveOrRecoverAsync(gameMode);
            _activeContent = snapshot.Content;
            AmmoPage.SetData(_activeContent);
            var cleanupChanges = await RefreshActiveWorkspacesAsync(detectCleanupChanges: true);
            ShowActiveSection();

            // Refresh Scanner only after canonical content/workspaces have switched to
            // the newly activated snapshot. Scanner context then points at one coherent
            // game mode/content/profile set throughout the refresh/restart operation.
            StatusText.Text = "Scanner 아이템·가격 데이터를 업데이트하는 중...";
            var scannerUsable = await ScannerCoordinator.SyncCatalogAsync();
            var scannerDiagnostics = ScannerCoordinator.CatalogDiagnostics;

            var scannerUsedFallback = scannerUsable &&
                                      scannerDiagnostics.UsedExistingCatalog &&
                                      !string.Equals(scannerDiagnostics.Outcome, "success", StringComparison.Ordinal);

            if (cleanupChanges.Count > 0)
            {
                MessageBox.Show(
                    this,
                    $"게임 데이터 변경으로 정리 가능한 보유 아이템이 {cleanupChanges.Count}종 생기거나 늘었습니다. 아이템 탭의 '정리 필요'에서 확인할 수 있습니다.",
                    "필요 아이템 변경",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }

            if (!scannerUsable)
            {
                MessageBox.Show(
                    this,
                    "일반 게임 데이터 업데이트는 완료했습니다.\n\n" +
                    "다만 Scanner 아이템·가격 데이터는 이번에 갱신하지 못했고 사용할 수 있는 기존 정상 캐시도 없습니다. " +
                    "인터넷 연결이 정상일 때 데이터 업데이트를 다시 실행하면 Scanner 데이터도 함께 복구됩니다.",
                    "Scanner 데이터 부분 업데이트 실패",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            else if (scannerUsedFallback)
            {
                MessageBox.Show(
                    this,
                    "일반 게임 데이터 업데이트는 완료했습니다.\n\n" +
                    "Scanner 최신 데이터 다운로드에는 실패했지만 기존에 검증된 정상 Scanner 캐시를 그대로 유지했습니다. " +
                    "인식 기능은 기존 데이터로 계속 사용할 수 있습니다.",
                    "Scanner 기존 데이터 유지",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }

            StatusText.Text = BuildLoadedStatus(gameMode);
        }
        catch (Exception exception)
        {
            ShowFailure("게임 데이터를 업데이트하지 못했습니다.", exception);
        }
        finally
        {
            SetBusy(false, StatusText.Text);
        }
    }
}
