using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace JunhyunHelper.Desktop.Scanner;

public partial class ScannerPage
{
    private const double V191DetailActionHeight = 34d;
    private bool _v191DetailActionSmokeCompleted;

    static ScannerPage()
    {
        EventManager.RegisterClassHandler(
            typeof(ScannerPage),
            LoadedEvent,
            new RoutedEventHandler(ScannerPage_V191DetailActionLoaded));
    }

    private static void ScannerPage_V191DetailActionLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is ScannerPage page)
            page.ApplyV191DetailActionAlignment();
    }

    private void ApplyV191DetailActionAlignment()
    {
        FavoriteItemButton.Height = V191DetailActionHeight;
        FavoriteItemButton.MinHeight = V191DetailActionHeight;
        FavoriteItemButton.Padding = new Thickness(0);
        FavoriteItemButton.HorizontalContentAlignment = HorizontalAlignment.Center;
        FavoriteItemButton.VerticalContentAlignment = VerticalAlignment.Center;
        FavoriteItemButton.VerticalAlignment = VerticalAlignment.Center;

        WikiButton.Height = V191DetailActionHeight;
        WikiButton.MinHeight = V191DetailActionHeight;
        WikiButton.VerticalContentAlignment = VerticalAlignment.Center;
        WikiButton.VerticalAlignment = VerticalAlignment.Center;

        if (!_v191DetailActionSmokeCompleted &&
            string.Equals(
                Environment.GetEnvironmentVariable("JUNHYUNHELPER_MAP_SMOKE"),
                "1",
                StringComparison.Ordinal))
        {
            Dispatcher.BeginInvoke(
                VerifyV191DetailActionAlignmentSmoke,
                DispatcherPriority.ContextIdle);
        }
    }

    private void VerifyV191DetailActionAlignmentSmoke()
    {
        if (_v191DetailActionSmokeCompleted)
            return;
        _v191DetailActionSmokeCompleted = true;

        try
        {
            UpdateLayout();
            if (Math.Abs(FavoriteItemButton.ActualHeight - WikiButton.ActualHeight) > 0.5 ||
                Math.Abs(FavoriteItemButton.ActualHeight - V191DetailActionHeight) > 0.5)
            {
                throw new InvalidOperationException(
                    $"Scanner detail action heights drifted: favorite={FavoriteItemButton.ActualHeight:F1}, wiki={WikiButton.ActualHeight:F1}.");
            }

            if (FavoriteItemButton.HorizontalContentAlignment != HorizontalAlignment.Center ||
                FavoriteItemButton.VerticalContentAlignment != VerticalAlignment.Center ||
                WikiButton.VerticalContentAlignment != VerticalAlignment.Center)
            {
                throw new InvalidOperationException("Scanner detail action content is not centered.");
            }

            File.WriteAllText(
                Path.Combine(Path.GetTempPath(), "junhyun-scanner-v191-detail-actions-smoke-success.txt"),
                "favorite-wiki-height=34\n" +
                "favorite-content-centered=ok\n" +
                "wiki-content-centered=ok\n");
        }
        catch (Exception exception)
        {
            try
            {
                File.WriteAllText(
                    Path.Combine(Path.GetTempPath(), "junhyun-map-smoke-error.txt"),
                    "Scanner v1.9.1 detail-action alignment smoke failed.\n" + exception);
            }
            catch
            {
            }

            Environment.Exit(89);
        }
    }
}
