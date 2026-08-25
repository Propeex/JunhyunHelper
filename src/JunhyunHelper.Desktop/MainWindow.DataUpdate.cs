using System.IO;
using System.Windows;
using JunhyunHelper.Infrastructure.Scanner;
using JunhyunHelper.Infrastructure.Validation;

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
                var reason = ContentValidationUserMessageFormatter.FormatFirstFatal(result.Validation);
                throw new InvalidDataException(
                    $"{reason} 기존 정상 데이터는 그대로 유지했습니다.");
            }

            var snapshot = await _services.Content.ReadActiveOrRecoverAsync(gameMode);
            _activeContent = snapshot.Content;
            AmmoPage.SetData(_activeContent);
            var cleanupChanges = await RefreshActiveWorkspacesAsync(detectCleanupChanges: true);
            ShowActiveSection();

            // Refresh Scanner only after canonical content/workspaces have switched to
            // the newly activated snapshot. Scanner context then points at one coherent
            // game mode/content/profile set throughout the refresh/restart operation.
            // Network retry/timeout policy is owned by ScannerCatalogService so this UI
            // path never stacks another full retry sequence on top of service retries.
            StatusText.Text = "Scanner 아이템·가격 데이터를 업데이트하는 중...";
            var scannerUsable = await ScannerCoordinator.SyncCatalogAsync();
            var scannerDiagnostics = ScannerCoordinator.CatalogDiagnostics;

            // `fresh-cache` deliberately uses the already-current local cache without a
            // network request. v1.6.0 treated every UsedExistingCatalog outcome other
            // than `success` as a failed download, so a healthy fresh cache incorrectly
            // produced the "Scanner 기존 데이터 유지" modal. Only actual failure
            // outcomes count as fallback.
            var scannerUsedFallback = scannerUsable &&
                                      scannerDiagnostics.UsedExistingCatalog &&
                                      ScannerCatalogOutcomePolicy.IsRefreshFailure(scannerDiagnostics.Outcome);

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

            // A verified same-mode Scanner cache is a successful fail-soft recovery, not
            // a user-blocking condition. Keep the failure visible in the status/log but
            // do not interrupt every data update with an informational MessageBox.
            StatusText.Text = scannerUsedFallback
                ? $"{BuildLoadedStatus(gameMode)} · Scanner 기존 정상 캐시 유지"
                : BuildLoadedStatus(gameMode);
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
