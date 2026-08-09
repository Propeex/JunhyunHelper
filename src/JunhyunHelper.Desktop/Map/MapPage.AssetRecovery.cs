using System.Windows;

namespace JunhyunHelper.Desktop.Map;

public partial class MapPage
{
    public event EventHandler? MapAssetRetryRequested;

    public void SetAssetRecoveryState(string? detail, bool retryEnabled)
    {
        NoMapDetailText.Text = string.IsNullOrWhiteSpace(detail)
            ? "지도 탭에 들어오면 필요한 지도 자산을 자동으로 다시 받습니다."
            : detail;
        RetryMapAssetsButton.IsEnabled = retryEnabled;
    }

    private void RetryMapAssetsButton_Click(object sender, RoutedEventArgs e) =>
        MapAssetRetryRequested?.Invoke(this, EventArgs.Empty);
}
