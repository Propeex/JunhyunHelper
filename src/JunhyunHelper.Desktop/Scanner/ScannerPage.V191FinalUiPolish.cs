using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace JunhyunHelper.Desktop.Scanner;

public partial class ScannerPage
{
    private const double V191DetailActionHeight = 34d;
    private bool _v191DetailActionSmokeArmed;
    private bool _v191DetailActionSmokeCompleted;
    private DispatcherTimer? _v191DetailActionSmokeVisibilityTimer;
    private Visibility? _v191DetailActionSmokeOriginalPageVisibility;

    private void ApplyV191DetailActionAlignment()
    {
        // Match the favorite action to the adjacent Wiki action exactly. The explicit
        // symbol font and zero padding keep both ☆ and ★ centered instead of clipping the
        // glyph inside the taller default Button content box seen in v1.9.0.
        FavoriteItemButton.Height = V191DetailActionHeight;
        FavoriteItemButton.MinHeight = V191DetailActionHeight;
        FavoriteItemButton.Padding = new Thickness(0);
        FavoriteItemButton.FontFamily = new FontFamily("Segoe UI Symbol");
        FavoriteItemButton.FontSize = 18;
        FavoriteItemButton.HorizontalContentAlignment = HorizontalAlignment.Center;
        FavoriteItemButton.VerticalContentAlignment = VerticalAlignment.Center;
        FavoriteItemButton.VerticalAlignment = VerticalAlignment.Center;

        WikiButton.Height = V191DetailActionHeight;
        WikiButton.MinHeight = V191DetailActionHeight;
        WikiButton.VerticalContentAlignment = VerticalAlignment.Center;
        WikiButton.VerticalAlignment = VerticalAlignment.Center;

        ArmV191DetailActionAlignmentSmoke();
    }

    private void ArmV191DetailActionAlignmentSmoke()
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
                VerifyV191DetailActionAlignmentSmoke,
                DispatcherPriority.Render);
            return;
        }

        if (_v191DetailActionSmokeArmed)
            return;

        _v191DetailActionSmokeArmed = true;
        SelectedItemPanel.IsVisibleChanged += SelectedItemPanel_V191SmokeIsVisibleChanged;

        _v191DetailActionSmokeVisibilityTimer ??= new DispatcherTimer(
            TimeSpan.FromMilliseconds(50),
            DispatcherPriority.Background,
            V191DetailActionSmokeVisibilityTimer_Tick,
            Dispatcher);
        _v191DetailActionSmokeVisibilityTimer.Start();
    }

    private void V191DetailActionSmokeVisibilityTimer_Tick(object? sender, EventArgs e)
    {
        if (_v191DetailActionSmokeCompleted)
        {
            StopV191DetailActionSmokeVisibilityTimer();
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

        StopV191DetailActionSmokeVisibilityTimer();
        Dispatcher.BeginInvoke(
            VerifyV191DetailActionAlignmentSmoke,
            DispatcherPriority.Render);
    }

    private void SelectedItemPanel_V191SmokeIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (!SelectedItemPanel.IsVisible || _v191DetailActionSmokeCompleted)
            return;

        SelectedItemPanel.IsVisibleChanged -= SelectedItemPanel_V191SmokeIsVisibleChanged;
        _v191DetailActionSmokeArmed = false;
        StopV191DetailActionSmokeVisibilityTimer();
        Dispatcher.BeginInvoke(
            VerifyV191DetailActionAlignmentSmoke,
            DispatcherPriority.Render);
    }

    private void VerifyV191DetailActionAlignmentSmoke()
    {
        if (_v191DetailActionSmokeCompleted)
            return;

        // A render can be superseded by navigation before it executes. Do not convert that
        // normal lifecycle race into a false failure; arm the next visible detail instead.
        if (!SelectedItemPanel.IsVisible || !FavoriteItemButton.IsVisible || !WikiButton.IsVisible)
        {
            ArmV191DetailActionAlignmentSmoke();
            return;
        }

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
                WikiButton.VerticalContentAlignment != VerticalAlignment.Center ||
                FavoriteItemButton.Padding != new Thickness(0) ||
                FavoriteItemButton.FontSize > 18.1)
            {
                throw new InvalidOperationException("Scanner detail favorite action is not using the approved centered glyph layout.");
            }

            _v191DetailActionSmokeCompleted = true;
            File.WriteAllText(
                Path.Combine(Path.GetTempPath(), "junhyun-scanner-v191-detail-actions-smoke-success.txt"),
                "favorite-wiki-height=34\n" +
                "favorite-symbol-font=ok\n" +
                "favorite-content-centered=ok\n" +
                "wiki-content-centered=ok\n");
            RestoreV191DetailActionSmokePageVisibility();
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

    private void StopV191DetailActionSmokeVisibilityTimer()
    {
        _v191DetailActionSmokeVisibilityTimer?.Stop();
    }

    private void RestoreV191DetailActionSmokePageVisibility()
    {
        StopV191DetailActionSmokeVisibilityTimer();
        SelectedItemPanel.IsVisibleChanged -= SelectedItemPanel_V191SmokeIsVisibleChanged;
        _v191DetailActionSmokeArmed = false;

        if (_v191DetailActionSmokeOriginalPageVisibility is not { } originalVisibility)
            return;

        _v191DetailActionSmokeOriginalPageVisibility = null;
        Visibility = originalVisibility;
    }
}
