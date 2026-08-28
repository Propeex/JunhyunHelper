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
            settings.SchemaVersion != 7 ||
            settings.OcrSubstitutions.Count != 0 ||
            !settings.ShowItemName ||
            !settings.ShowItemIcon ||
            !settings.ShowFleaMinimumPrice ||
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

        var migratedInfo = new ScannerDisplaySettings
        {
            SchemaVersion = 6,
            ShowFleaMinimumPrice = false,
            MiniScannerInfoOrder =
            [
                ScannerDisplaySettings.CurrentNeededField,
                ScannerDisplaySettings.FleaAveragePriceField,
                ScannerDisplaySettings.TraderSellPriceField,
                ScannerDisplaySettings.TraderPricePerSlotField,
                ScannerDisplaySettings.FleaPricePerSlotField,
            ],
        };
        migratedInfo.Normalize();
        if (!migratedInfo.ShowFleaMinimumPrice ||
            migratedInfo.MiniScannerInfoOrder.Count != ScannerDisplaySettings.DefaultInfoOrder.Count ||
            migratedInfo.MiniScannerInfoOrder[^1] != ScannerDisplaySettings.FleaMinimumPriceField ||
            migratedInfo.MiniScannerInfoOrder.Count(key =>
                string.Equals(key, ScannerDisplaySettings.FleaMinimumPriceField, StringComparison.Ordinal)) != 1)
        {
            throw new InvalidOperationException(
                "Scanner schema-v6 Mini Scanner order migration did not append the flea minimum field exactly once.");
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
            !settings.ShowFleaMinimumPrice ||
            !settings.ShowTraderPricePerSlot)
        {
            throw new InvalidOperationException(
                "Mini Scanner defaults must show fixed identity header and approved market information.");
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
                "Therapist")
            {
                FleaMinimumPrice = 51000,
            };
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

            if (window.FindName("FleaMinimumPriceText") is not TextBlock fleaMinimum ||
                fleaMinimum.Visibility != Visibility.Visible ||
                !fleaMinimum.Text.Contains("플리 최저", StringComparison.Ordinal) ||
                !fleaMinimum.Text.Contains("51,000", StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Mini Scanner flea minimum row did not render the cached current low price.");
            }

            if (window.FindName("InfoStackPanel") is not StackPanel infoStack ||
                infoStack.Children.Count != ScannerDisplaySettings.DefaultInfoOrder.Count ||
                !ReferenceEquals(infoStack.Children[0], trader) ||
                !ReferenceEquals(infoStack.Children[2], fleaMinimum))
            {
                throw new InvalidOperationException(
                    "Mini Scanner information rows did not follow the schema-v7 default order.");
            }

            settings.MiniScannerInfoOrder =
            [
                ScannerDisplaySettings.CurrentNeededField,
                ScannerDisplaySettings.FleaAveragePriceField,
                ScannerDisplaySettings.TraderSellPriceField,
                ScannerDisplaySettings.TraderPricePerSlotField,
                ScannerDisplaySettings.FleaPricePerSlotField,
                ScannerDisplaySettings.FleaMinimumPriceField,
            ];
            window.Render(snapshot, settings, editMode: false);
            if (window.FindName("CurrentNeededText") is not TextBlock needed ||
                window.FindName("InfoStackPanel") is not StackPanel reordered ||
                !ReferenceEquals(reordered.Children[0], needed) ||
                !ReferenceEquals(reordered.Children[^1], fleaMinimum))
            {
                throw new InvalidOperationException(
                    "Mini Scanner did not apply user information order including flea minimum price.");
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
