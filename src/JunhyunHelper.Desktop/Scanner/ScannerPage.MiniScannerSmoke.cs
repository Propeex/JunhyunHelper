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
        if (!string.Equals(Environment.GetEnvironmentVariable("JUNHYUNHELPER_MAP_SMOKE"), "1", StringComparison.Ordinal))
            return;

        ScannerTitleFontSmoke.VerifyProductContract();
        VerifyScannerRecognitionProductContract();
        VerifyMiniScannerProductContract();
        VerifyScannerPageProductContract();
    }

    private static void VerifyScannerRecognitionProductContract()
    {
        var settings = new ScannerDisplaySettings();
        settings.Normalize();

        var gestures = new[]
        {
            ParseRequired(settings.OneShotTarkovHotkey),
            ParseRequired(settings.OneShotTestHotkey),
            ParseRequired(settings.ScannerToggleHotkey),
            ParseRequired(settings.AddCorrectionDataHotkey),
            ParseRequired(settings.FarmingGuideAcceptHotkey),
        };

        if (settings.SchemaVersion != ScannerDisplaySettings.CurrentSchemaVersion ||
            settings.SchemaVersion != 10 ||
            settings.OcrSubstitutions.Count != 0 ||
            !settings.ShowItemName ||
            !settings.ShowItemIcon ||
            !settings.ShowAmmoPickup ||
            !settings.ShowFarmingGuide ||
            !settings.MiniScannerInfoOrder.SequenceEqual(ScannerDisplaySettings.DefaultInfoOrder) ||
            gestures[0] != ScannerHotkeyGesture.DefaultOneShotTarkov ||
            gestures[1] != ScannerHotkeyGesture.DefaultOneShotTest ||
            gestures[2] != ScannerHotkeyGesture.DefaultScannerToggle ||
            gestures[3] != new ScannerHotkeyGesture(true, false, true, Key.F9) ||
            gestures[4] != new ScannerHotkeyGesture(true, false, true, Key.F6) ||
            gestures.Distinct().Count() != 5)
        {
            throw new InvalidOperationException("Scanner v1.15.0 settings/hotkey contract failed.");
        }

        var hiddenIdentity = new ScannerDisplaySettings
        {
            SchemaVersion = 5,
            ShowItemName = false,
            ShowItemIcon = false,
        };
        hiddenIdentity.Normalize();
        if (!hiddenIdentity.ShowItemName || !hiddenIdentity.ShowItemIcon)
            throw new InvalidOperationException("Scanner migration must keep Mini Scanner identity visible.");

        var migrated = new ScannerDisplaySettings
        {
            SchemaVersion = 3,
            OneShotHotkey = ScannerHotkeyGesture.DefaultOneShotTest.ToString(),
        };
        migrated.Normalize();
        var migratedHotkeys = new[]
        {
            migrated.OneShotTarkovHotkey,
            migrated.OneShotTestHotkey,
            migrated.ScannerToggleHotkey,
            migrated.AddCorrectionDataHotkey,
            migrated.FarmingGuideAcceptHotkey,
        }.Where(static value => !string.IsNullOrWhiteSpace(value)).ToArray();
        if (migrated.OneShotTarkovHotkey != ScannerHotkeyGesture.DefaultOneShotTest.ToString() ||
            migratedHotkeys.Distinct(StringComparer.OrdinalIgnoreCase).Count() != migratedHotkeys.Length)
        {
            throw new InvalidOperationException("Scanner legacy hotkey migration produced a collision.");
        }

        if (!ScannerCoordinator.ShouldRestoreOneShotMode(ScannerCaptureMode.TarkovWindow, ScannerCaptureMode.TarkovWindow) ||
            ScannerCoordinator.ShouldRestoreOneShotMode(ScannerCaptureMode.TarkovWindow, ScannerCaptureMode.DisplayTest) ||
            ScannerCoordinator.ShouldRestoreOneShotMode(ScannerCaptureMode.TarkovWindow, null))
        {
            throw new InvalidOperationException("Scanner one-shot restoration contract failed.");
        }

        ScannerInspectHeaderLockSmoke.Verify();
    }

    private static ScannerHotkeyGesture ParseRequired(string value)
    {
        if (!ScannerHotkeyGesture.TryParse(value, out var gesture))
            throw new InvalidOperationException($"Scanner hotkey is invalid: {value}");
        return gesture;
    }

    private static void VerifyMiniScannerProductContract()
    {
        var settings = new ScannerDisplaySettings();
        settings.Normalize();
        var window = new MiniScannerWindow();
        try
        {
            if (!window.Topmost || window.ShowActivated || window.ShowInTaskbar)
                throw new InvalidOperationException("Mini Scanner window behavior contract failed.");
            if (window.FindName("ScannerStatusText") is not null || window.FindName("FleaMinimumPriceText") is not null)
                throw new InvalidOperationException("Mini Scanner exposes a removed presentation row.");
            if (window.FindName("DragSurface") is not Border dragSurface ||
                !dragSurface.IsHitTestVisible ||
                !dragSurface.ForceCursor ||
                dragSurface.Cursor != Cursors.Arrow ||
                dragSurface.Background is not SolidColorBrush background ||
                background.Color.A == 0)
            {
                throw new InvalidOperationException("Mini Scanner drag surface contract failed.");
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
                AmmoShouldPickUp = true,
                EvaluatedAmmoName = "5.56x45mm M855",
            };
            window.Render(snapshot, settings, editMode: false);
            window.UpdateLayout();

            if (window.FindName("TraderPriceText") is not TextBlock trader ||
                !trader.Text.Contains("Therapist", StringComparison.Ordinal) ||
                !trader.Text.Contains("42,000", StringComparison.Ordinal) ||
                window.FindName("AmmoPickupText") is not TextBlock ammoPickup ||
                ammoPickup.Visibility != Visibility.Visible ||
                !ammoPickup.Text.Contains("주워야 함", StringComparison.Ordinal) ||
                !ammoPickup.Text.Contains("5.56x45mm M855", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Mini Scanner price/ammo presentation contract failed.");
            }

            if (window.FindName("InfoStackPanel") is not StackPanel infoStack ||
                !ReferenceEquals(infoStack.Children[0], trader) ||
                !ReferenceEquals(infoStack.Children[^1], ammoPickup))
            {
                throw new InvalidOperationException("Mini Scanner default information-order contract failed.");
            }

            settings.MiniScannerInfoOrder =
            [
                ScannerDisplaySettings.AmmoPickupField,
                ScannerDisplaySettings.CurrentNeededField,
                ScannerDisplaySettings.FleaAveragePriceField,
                ScannerDisplaySettings.TraderSellPriceField,
                ScannerDisplaySettings.TraderPricePerSlotField,
                ScannerDisplaySettings.FleaPricePerSlotField,
                ScannerDisplaySettings.FleaMinimumPriceField,
                ScannerDisplaySettings.FarmingGuideField,
            ];
            window.Render(snapshot, settings, editMode: false);
            if (window.FindName("CurrentNeededText") is not TextBlock needed ||
                window.FindName("InfoStackPanel") is not StackPanel reordered ||
                !ReferenceEquals(reordered.Children[0], ammoPickup) ||
                !ReferenceEquals(reordered.Children[1], needed))
            {
                throw new InvalidOperationException("Mini Scanner ammo pickup row did not follow the configured information order.");
            }

            settings.ShowAmmoPickup = false;
            window.Render(snapshot, settings, editMode: false);
            if (ammoPickup.Visibility != Visibility.Collapsed)
                throw new InvalidOperationException("Mini Scanner ammo pickup visibility setting was not applied.");
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
            throw new InvalidOperationException("Scanner normal surface contract failed.");
        }
    }
}
