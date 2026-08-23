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
            settings.SchemaVersion != 4 ||
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
                "Scanner v1.3 three-hotkey/settings contract failed.");
        }

        // Schema v3 users may have customized the old one-shot gesture to one of the
        // keys that v1.3 now uses as a new default. Preserve that user choice and move
        // only the newly introduced commands to non-conflicting fallbacks.
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
                "Scanner v1.3 schema-v3 hotkey migration did not preserve the old user gesture without collisions.");
        }

        // v1.2.1 lifecycle contract remains: a one-shot may only restore the exact mode
        // that is still requested after the scan. Turning Scanner/Test off or switching
        // the active mode while the one-shot runs must not resurrect the previous mode.
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
                "Scanner v1.2.1 one-shot mode restoration contract failed.");
        }

        const int width = 800;
        const int height = 600;
        const int stride = width * 4;
        var pixels = new byte[stride * height];
        for (var offset = 3; offset < pixels.Length; offset += 4)
            pixels[offset] = 255;

        static void Fill(
            byte[] target,
            int targetStride,
            int x,
            int y,
            int regionWidth,
            int regionHeight,
            byte b,
            byte g,
            byte r)
        {
            for (var yy = y; yy < y + regionHeight; yy++)
            {
                for (var xx = x; xx < x + regionWidth; xx++)
                {
                    var offset = yy * targetStride + xx * 4;
                    target[offset] = b;
                    target[offset + 1] = g;
                    target[offset + 2] = r;
                    target[offset + 3] = 255;
                }
            }
        }

        var panel = new ScannerDetectedRegion(100, 100, 520, 400, 0.996);
        var fallback = ScannerDetailGeometryDetector.GetTitleRegion(panel);
        var close = new ScannerDetectedRegion(598, 100, 20, 20, 0.996);

        // Bright neutral component at Tarkov's magnifier position. It deliberately
        // reaches into the coarse title's left boundary if the old ROI is used.
        Fill(pixels, stride, 104, 104, 12, 12, 160, 160, 160);
        // Broad dark title field matching the real inspect-header luminance class.
        Fill(pixels, stride, 118, 99, 383, 21, 30, 30, 30);
        // Red close-control evidence at the right edge.
        Fill(pixels, stride, 598, 100, 20, 20, 10, 10, 120);

        var candidate = new ScannerDetectedCandidate(
            panel,
            fallback,
            close,
            "STRUCTURE_MATCH");
        var refinement = ScannerTitleAnchorRefiner.Refine(
            pixels,
            width,
            height,
            stride,
            candidate);

        if (refinement.Magnifier.Width <= 0 ||
            refinement.Title.X <= fallback.X ||
            refinement.Title.X <= refinement.Magnifier.X + refinement.Magnifier.Width ||
            refinement.Title.Width < 100 ||
            refinement.Score < 0.70)
        {
            throw new InvalidOperationException(
                "Scanner title-anchor smoke failed to exclude magnifier pixels from OCR ROI.");
        }
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
