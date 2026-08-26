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
        VerifyScannerPageProductContract();
    }

    private static void VerifyScannerRecognitionProductContract()
    {
        var settings = new ScannerDisplaySettings();
        settings.Normalize();
        if (settings.SchemaVersion != ScannerDisplaySettings.CurrentSchemaVersion ||
            settings.SchemaVersion != 6 ||
            settings.OcrSubstitutions.Count != 0 ||
            !settings.ShowItemName ||
            !settings.ShowItemIcon ||
            !settings.MiniScannerInfoOrder.SequenceEqual(ScannerDisplaySettings.DefaultInfoOrder) ||
            !ScannerHotkeyGesture.TryParse(settings.OneShotTarkovHotkey, out var tarkovGesture) ||
            !ScannerHotkeyGesture.TryParse(settings.OneShotTestHotkey, out var testGesture) ||
            !ScannerHotkeyGesture.TryParse(settings.ScannerToggleHotkey, out var toggleGesture) ||
            tarkovGesture != ScannerHotkeyGesture.DefaultOneShotTarkov ||
            testGesture != ScannerHotkeyGesture.DefaultOneShotTest ||
            toggleGesture != ScannerHotkeyGesture.DefaultScannerToggle ||
            new[] { tarkovGesture, testGesture, toggleGesture }.Distinct().Count() != 3 ||
            !ScannerHotkeyGesture.TryParse("F10", out var bareGesture) ||
            bareGesture != new ScannerHotkeyGesture(false, false, false, Key.F10) ||
            !ScannerHotkeyGesture.TryParse("Ctrl+Alt+F9", out var combinedGesture) ||
            combinedGesture != new ScannerHotkeyGesture(true, true, false, Key.F9) ||
            ScannerHotkeyGesture.TryParse("Ctrl", out _) ||
            ScannerHotkeyGesture.TryParse("Win+F10", out _))
        {
            throw new InvalidOperationException(
                "Scanner settings/hotkey/fixed-header/order contract failed.");
        }

        var hiddenIdentity = new ScannerDisplaySettings
        {
            SchemaVersion = 5,
            ShowItemName = false,
            ShowItemIcon = false,
        };
        hiddenIdentity.Normalize();
        if (!hiddenIdentity.ShowItemName || !hiddenIdentity.ShowItemIcon)
        {
            throw new InvalidOperationException(
                "Scanner v1.6 migration must force Mini Scanner icon/name visible.");
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
            !settings.ShowItemName ||
            !settings.ShowTraderSellPrice ||
            !settings.ShowTraderPricePerSlot)
        {
            throw new InvalidOperationException(
                "Mini Scanner defaults must show fixed identity header and trader information.");
        }

        if (!MiniScannerOverlayService.CanOpenConfirmedItem(
                preview: true,
                scannerEnabled: true,
                foregroundTarkov: false) ||
            !MiniScannerOverlayService.CanOpenConfirmedItem(
                preview: false,
                scannerEnabled: false,
                foregroundTarkov: false) ||
            !MiniScannerOverlayService.CanOpenConfirmedItem(
                preview: false,
                scannerEnabled: true,
                foregroundTarkov: true) ||
            MiniScannerOverlayService.CanOpenConfirmedItem(
                preview: false,
                scannerEnabled: true,
                foregroundTarkov: false))
        {
            throw new InvalidOperationException(
                "Mini Scanner confirmed-item initial visibility guard contract failed.");
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
                3,
                "Therapist");
            window.Render(snapshot, settings, editMode: false);
            window.UpdateLayout();

            if (window.FindName("ItemIcon") is not Image icon || icon.Visibility != Visibility.Visible ||
                window.FindName("ItemNameText") is not TextBlock name || name.Visibility != Visibility.Visible)
            {
                throw new InvalidOperationException(
                    "Mini Scanner icon/name fixed header is not visible.");
            }

            if (window.FindName("TraderPriceText") is not TextBlock trader ||
                trader.Visibility != Visibility.Visible ||
                !trader.Text.Contains("Therapist", StringComparison.Ordinal) ||
                !trader.Text.Contains("42,000", StringComparison.Ordinal) ||
                trader.Text.Contains("최고 상점가", StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Mini Scanner trader row did not use the approved trader-name + price form.");
            }

            if (window.FindName("InfoStackPanel") is not StackPanel infoStack ||
                infoStack.Children.Count != ScannerDisplaySettings.DefaultInfoOrder.Count ||
                !ReferenceEquals(infoStack.Children[0], trader))
            {
                throw new InvalidOperationException(
                    "Mini Scanner information rows did not follow persisted order.");
            }

            settings.MiniScannerInfoOrder =
            [
                ScannerDisplaySettings.CurrentNeededField,
                ScannerDisplaySettings.FleaAveragePriceField,
                ScannerDisplaySettings.TraderSellPriceField,
                ScannerDisplaySettings.TraderPricePerSlotField,
                ScannerDisplaySettings.FleaSlotPriceField,
            ];
            window.Render(snapshot, settings, editMode: false);
            if (window.FindName("CurrentNeededText") is not TextBlock needed ||
                window.FindName("InfoStackPanel") is not StackPanel reordered ||
                !ReferenceEquals(reordered.Children[0], needed))
            {
                throw new InvalidOperationException(
                    "Mini Scanner did not apply user information order.");
            }

            if (!window.IsVisible || !window.Topmost)
                throw new InvalidOperationException("Mini Scanner lost topmost visibility after rendering.");
        }
        finally
        {
            window.Close();
        }
    }

    private static void VerifyScannerPageProductContract()
    {
        var page = new ScannerPage();
        if (page.FindName("ScannerToggleButton") is not Button ||
            page.FindName("ItemSearchBox") is not TextBox ||
            page.FindName("ActivityItems") is not ItemsControl ||
            page.FindName("OneShotScanButton") is not null ||
            page.FindName("SyncCatalogButton") is not null ||
            page.FindName("ClearLogButton") is not null)
        {
            throw new InvalidOperationException(
                "Scanner v1.6 normal surface does not match the approved three-action/search/log contract.");
        }
    }
}
