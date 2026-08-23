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

        VerifyInspectHeaderAnchorRegression();
    }

    private static void VerifyInspectHeaderAnchorRegression()
    {
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
            for (var xx = x; xx < x + regionWidth; xx++)
            {
                var offset = yy * targetStride + xx * 4;
                target[offset] = b;
                target[offset + 1] = g;
                target[offset + 2] = r;
                target[offset + 3] = 255;
            }
        }

        static void DrawMagnifier(byte[] target, int targetStride, int x, int y)
        {
            const byte bright = 178;
            // 19x19 hollow ring.
            Fill(target, targetStride, x, y, 19, 3, bright, bright, bright);
            Fill(target, targetStride, x, y + 16, 19, 3, bright, bright, bright);
            Fill(target, targetStride, x, y, 3, 19, bright, bright, bright);
            Fill(target, targetStride, x + 16, y, 3, 19, bright, bright, bright);
            // Connected lower-right handle extends the icon to roughly 27x26 pixels,
            // matching the live evidence while remaining clearly larger than glyphs.
            for (var step = 0; step < 9; step++)
                Fill(target, targetStride, x + 17 + step, y + 17 + step, 3, 3, bright, bright, bright);
        }

        static void DrawGlyph(byte[] target, int targetStride, int x, int y)
        {
            const byte bright = 172;
            // Hollow/square-ish glyph intentionally resembles the old generic bright
            // component heuristic. It must never outrank the preceding magnifier.
            Fill(target, targetStride, x, y, 17, 3, bright, bright, bright);
            Fill(target, targetStride, x, y + 16, 17, 3, bright, bright, bright);
            Fill(target, targetStride, x, y, 3, 19, bright, bright, bright);
            Fill(target, targetStride, x + 7, y + 3, 3, 13, bright, bright, bright);
            Fill(target, targetStride, x + 14, y, 3, 19, bright, bright, bright);
        }

        // Deliberately drift the structural panel left edge to the right of the real
        // magnifier start. This recreates the live failure where the first Korean title
        // glyph became closer to the old panel-relative magnifier expectation.
        var panel = new ScannerDetectedRegion(120, 100, 520, 400, 0.996);
        var fallback = ScannerDetailGeometryDetector.GetTitleRegion(panel);
        const int fieldX = 106;
        const int fieldY = 98;
        const int fieldWidth = 450;
        const int fieldHeight = 27;
        const int magnifierX = 108;
        const int magnifierY = 99;
        const int firstGlyphX = 141;
        const int glyphY = 103;

        Fill(pixels, stride, fieldX, fieldY, fieldWidth, fieldHeight, 30, 30, 30);
        DrawMagnifier(pixels, stride, magnifierX, magnifierY);
        DrawGlyph(pixels, stride, firstGlyphX, glyphY);
        DrawGlyph(pixels, stride, firstGlyphX + 22, glyphY);
        DrawGlyph(pixels, stride, firstGlyphX + 44, glyphY);
        DrawGlyph(pixels, stride, firstGlyphX + 66, glyphY);

        // Actual close control is detected from pixels rather than handed to the
        // refiner, so the regression also covers the right-side red anchor.
        Fill(pixels, stride, 620, 100, 20, 20, 10, 10, 126);

        var candidate = new ScannerDetectedCandidate(
            panel,
            fallback,
            default,
            "STRUCTURE_MATCH");
        var refinement = ScannerTitleAnchorRefiner.Refine(
            pixels,
            width,
            height,
            stride,
            candidate);

        var titleRight = refinement.Title.X + refinement.Title.Width;
        var magnifierRight = refinement.Magnifier.X + refinement.Magnifier.Width;
        if (refinement.Magnifier.Width < 20 ||
            refinement.Magnifier.X > magnifierX + 4 ||
            refinement.CloseButton.Width <= 0 ||
            refinement.Title.X <= magnifierRight ||
            refinement.Title.X > firstGlyphX ||
            titleRight < firstGlyphX + 17 ||
            refinement.Title.Width < 120 ||
            refinement.Score < 0.68 ||
            refinement.Reason != "TITLE_HEADER_TEXT_REFINED")
        {
            throw new InvalidOperationException(
                $"Scanner inspect-header regression failed: magnifier={refinement.Magnifier}, " +
                $"title={refinement.Title}, close={refinement.CloseButton}, " +
                $"score={refinement.Score:F3}, reason={refinement.Reason}.");
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
                traderSlotPrice.Visibility != Visibility.Visible ||
                !traderSlotPrice.Text.Contains("21,000", StringComparison.Ordinal))
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
