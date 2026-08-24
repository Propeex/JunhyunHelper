using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using JunhyunHelper.Core.Scanner;

namespace JunhyunHelper.Desktop.Scanner;

public partial class ScannerPage
{
    static ScannerPage()
    {
        if (!string.Equals(
                Environment.GetEnvironmentVariable("JUNHYUNHELPER_MAP_SMOKE"),
                "1",
                StringComparison.Ordinal))
        {
            return;
        }

        ScannerTitleFontSmoke.VerifyProductContract();
        VerifyScannerRecognitionProductContract();
        VerifyMiniScannerProductContract();
    }

    private static void VerifyScannerRecognitionProductContract()
    {
        var settings = new ScannerDisplaySettings();
        settings.Normalize();
        if (settings.SchemaVersion != ScannerDisplaySettings.CurrentSchemaVersion ||
            settings.SchemaVersion != 5 ||
            settings.OcrSubstitutions.Count != 0 ||
            !ScannerHotkeyGesture.TryParse(settings.OneShotTarkovHotkey, out var tarkovGesture) ||
            !ScannerHotkeyGesture.TryParse(settings.OneShotTestHotkey, out var testGesture) ||
            !ScannerHotkeyGesture.TryParse(settings.ScannerToggleHotkey, out var toggleGesture) ||
            tarkovGesture != ScannerHotkeyGesture.DefaultOneShotTarkov ||
            testGesture != ScannerHotkeyGesture.DefaultOneShotTest ||
            toggleGesture != ScannerHotkeyGesture.DefaultScannerToggle ||
            new[] { tarkovGesture, testGesture, toggleGesture }.Distinct().Count() != 3 ||
            ScannerHotkeyGesture.TryParse("F10", out _))
        {
            throw new InvalidOperationException(
                "Scanner v1.5 settings/hotkey/OCR-substitution contract failed.");
        }

        var migrated = new ScannerDisplaySettings
        {
            SchemaVersion = 3,
            OneShotHotkey = ScannerHotkeyGesture.DefaultOneShotTest.ToString(),
        };
        migrated.Normalize();
        var migratedConfigured = new[]
        {
            migrated.OneShotTarkovHotkey,
            migrated.OneShotTestHotkey,
            migrated.ScannerToggleHotkey,
        }
        .Where(value => !string.IsNullOrWhiteSpace(value))
        .ToArray();
        if (migrated.OneShotTarkovHotkey != ScannerHotkeyGesture.DefaultOneShotTest.ToString() ||
            migratedConfigured.Distinct(StringComparer.OrdinalIgnoreCase).Count() != migratedConfigured.Length)
        {
            throw new InvalidOperationException(
                "Scanner schema-v3 hotkey migration did not preserve the old user gesture without collisions.");
        }

        if (!ScannerCoordinator.ShouldRestoreOneShotMode(
                ScannerCaptureMode.TarkovWindow,
                ScannerCaptureMode.TarkovWindow) ||
            ScannerCoordinator.ShouldRestoreOneShotMode(
                ScannerCaptureMode.TarkovWindow,
                ScannerCaptureMode.DisplayTest) ||
            ScannerCoordinator.ShouldRestoreOneShotMode(
                ScannerCaptureMode.TarkovWindow,
                null) ||
            ScannerCoordinator.ShouldRestoreOneShotMode(
                null,
                ScannerCaptureMode.TarkovWindow))
        {
            throw new InvalidOperationException(
                "Scanner one-shot mode restoration contract failed.");
        }

        ScannerInspectHeaderLockSmoke.Verify();
    }

    private static void VerifyMiniScannerProductContract()
    {
        var settings = new ScannerDisplaySettings();
        settings.Normalize();
        if (!settings.ShowItemIcon ||
            !settings.ShowTraderSellPrice ||
            !settings.ShowTraderPricePerSlot)
        {
            throw new InvalidOperationException(
                "Mini Scanner defaults must show icon, trader price, and trader price per slot.");
        }

        var window = new MiniScannerWindow();
        try
        {
            if (!window.Topmost || window.ShowActivated || window.ShowInTaskbar)
            {
                throw new InvalidOperationException(
                    "Mini Scanner must be topmost, non-activating, and absent from the taskbar.");
            }

            if (window.FindName("ScannerStatusText") is not null)
            {
                throw new InvalidOperationException(
                    "Mini Scanner still exposes runtime/status text instead of matched item data only.");
            }

            if (window.FindName("DragSurface") is not Border dragSurface ||
                !dragSurface.IsHitTestVisible ||
                !dragSurface.ForceCursor ||
                dragSurface.Cursor != Cursors.Arrow ||
                dragSurface.Background is not SolidColorBrush background ||
                background.Color.A == 0)
            {
                throw new InvalidOperationException(
                    "Mini Scanner card is not a reliable full-surface Arrow-cursor drag hitbox.");
            }

            var snapshot = new ScannerItemSnapshot(
                "mini-scanner-smoke",
                "Mini Scanner smoke item",
                null,
                42000,
                57000,
                21000,
                28500,
                2,
                3);
            window.Render(snapshot, settings, editMode: false);
            window.UpdateLayout();

            if (window.FindName("TraderPriceText") is not TextBlock trader ||
                trader.Visibility != Visibility.Visible ||
                !trader.Text.Contains("42,000", StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Mini Scanner did not render the matched item's trader price.");
            }

            if (window.FindName("TraderSlotPriceText") is not TextBlock traderPerSlot ||
                traderPerSlot.Visibility != Visibility.Visible ||
                !traderPerSlot.Text.Contains("21,000", StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Mini Scanner did not render the matched item's trader price per slot.");
            }

            if (!window.IsVisible || !window.Topmost)
                throw new InvalidOperationException("Mini Scanner lost topmost visibility after rendering.");
        }
        finally
        {
            window.Close();
        }
    }
}
