using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace JunhyunHelper.Desktop.Scanner;

public partial class ScannerPage
{
    private bool _v191DetailActionSmokeArmed;
    private bool _v191DetailActionSmokeCompleted;
    private DispatcherTimer? _v191DetailActionSmokeVisibilityTimer;
    private Visibility? _v191DetailActionSmokeOriginalPageVisibility;


    private void ArmDetailActionAlignmentSmoke()
    {
        if (_v191DetailActionSmokeCompleted ||
            !string.Equals(
                Environment.GetEnvironmentVariable("JUNHYUNHELPER_MAP_SMOKE"),
                "1",
                StringComparison.Ordinal))
        {
            return;
        }

        // SelectedItemPanel starts Collapsed, and the normal CI smoke may open its item
        // while the Scanner tab itself is still Collapsed. Watch the local Visibility as
        // well as effective IsVisible so the published executable can briefly put the real
        // ScannerPage into the visible visual tree and verify the action row at Render.
        if (SelectedItemPanel.IsVisible)
        {
            Dispatcher.BeginInvoke(
                VerifyDetailActionAlignmentSmoke,
                DispatcherPriority.Render);
            return;
        }

        if (_v191DetailActionSmokeArmed)
            return;

        _v191DetailActionSmokeArmed = true;
        SelectedItemPanel.IsVisibleChanged += SelectedItemPanel_DetailActionSmokeIsVisibleChanged;

        _v191DetailActionSmokeVisibilityTimer ??= new DispatcherTimer(
            TimeSpan.FromMilliseconds(50),
            DispatcherPriority.Background,
            DetailActionSmokeVisibilityTimer_Tick,
            Dispatcher);
        _v191DetailActionSmokeVisibilityTimer.Start();
    }

    private void DetailActionSmokeVisibilityTimer_Tick(object? sender, EventArgs e)
    {
        if (_v191DetailActionSmokeCompleted)
        {
            StopDetailActionSmokeVisibilityTimer();
            return;
        }

        if (SelectedItemPanel.Visibility != Visibility.Visible)
            return;

        // Smoke-only visibility promotion. Normal product execution never enters this path.
        // This is required because the Scanner page is initially Collapsed in MainWindow,
        // while the existing published item-detail probe intentionally opens its test item
        // without navigating tabs. We must verify ActualHeight on a genuinely visible tree.
        if (!IsVisible)
        {
            _v191DetailActionSmokeOriginalPageVisibility ??= Visibility;
            Visibility = Visibility.Visible;
            UpdateLayout();
        }

        StopDetailActionSmokeVisibilityTimer();
        Dispatcher.BeginInvoke(
            VerifyDetailActionAlignmentSmoke,
            DispatcherPriority.Render);
    }

    private void SelectedItemPanel_DetailActionSmokeIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (!SelectedItemPanel.IsVisible || _v191DetailActionSmokeCompleted)
            return;

        SelectedItemPanel.IsVisibleChanged -= SelectedItemPanel_DetailActionSmokeIsVisibleChanged;
        _v191DetailActionSmokeArmed = false;
        StopDetailActionSmokeVisibilityTimer();
        Dispatcher.BeginInvoke(
            VerifyDetailActionAlignmentSmoke,
            DispatcherPriority.Render);
    }

    private void VerifyDetailActionAlignmentSmoke()
    {
        if (_v191DetailActionSmokeCompleted)
            return;

        // A render can be superseded by navigation before it executes. Do not convert that
        // normal lifecycle race into a false failure; arm the next visible detail instead.
        if (!SelectedItemPanel.IsVisible || !FavoriteItemButton.IsVisible || !WikiButton.IsVisible)
        {
            ArmDetailActionAlignmentSmoke();
            return;
        }

        try
        {
            UpdateLayout();
            if (Math.Abs(FavoriteItemButton.ActualHeight - WikiButton.ActualHeight) > 0.5 ||
                Math.Abs(FavoriteItemButton.ActualHeight - 34d) > 0.5)
            {
                throw new InvalidOperationException(
                    $"Scanner detail action heights drifted: favorite={FavoriteItemButton.ActualHeight:F1}, wiki={WikiButton.ActualHeight:F1}.");
            }

            if (FavoriteItemButton.HorizontalContentAlignment != HorizontalAlignment.Center ||
                FavoriteItemButton.VerticalContentAlignment != VerticalAlignment.Center ||
                WikiButton.VerticalContentAlignment != VerticalAlignment.Center ||
                FavoriteItemButton.Padding != new Thickness(0) ||
                FavoriteItemButton.FontSize > 18.1)
            {
                throw new InvalidOperationException("Scanner detail favorite action is not using the approved centered glyph layout.");
            }

            _v191DetailActionSmokeCompleted = true;
            File.WriteAllText(
                Path.Combine(Path.GetTempPath(), "junhyun-scanner-detail-actions-smoke-success.txt"),
                "favorite-wiki-height=34\n" +
                "favorite-symbol-font=ok\n" +
                "favorite-content-centered=ok\n" +
                "wiki-content-centered=ok\n");
            RestoreDetailActionSmokePageVisibility();
        }
        catch (Exception exception)
        {
            try
            {
                File.WriteAllText(
                    Path.Combine(Path.GetTempPath(), "junhyun-map-smoke-error.txt"),
                    "Scanner detail-action alignment smoke failed.\n" + exception);
            }
            catch
            {
            }

            Environment.Exit(89);
        }
    }

    private void StopDetailActionSmokeVisibilityTimer()
    {
        _v191DetailActionSmokeVisibilityTimer?.Stop();
    }

    private void RestoreDetailActionSmokePageVisibility()
    {
        StopDetailActionSmokeVisibilityTimer();
        SelectedItemPanel.IsVisibleChanged -= SelectedItemPanel_DetailActionSmokeIsVisibleChanged;
        _v191DetailActionSmokeArmed = false;

        if (_v191DetailActionSmokeOriginalPageVisibility is not { } originalVisibility)
            return;

        _v191DetailActionSmokeOriginalPageVisibility = null;
        Visibility = originalVisibility;
    }
}
