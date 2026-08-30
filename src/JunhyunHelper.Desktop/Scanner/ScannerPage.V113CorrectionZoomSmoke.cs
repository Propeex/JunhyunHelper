using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace JunhyunHelper.Desktop.Scanner;

internal static class ScannerPageV113CorrectionZoomSmokeRegistration
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        EventManager.RegisterClassHandler(
            typeof(ScannerPage),
            FrameworkElement.LoadedEvent,
            new RoutedEventHandler(ScannerPage.HandleV113CorrectionZoomSmokeLoaded),
            handledEventsToo: true);
    }
}

public partial class ScannerPage
{
    private bool _v113CorrectionZoomSmokeScheduled;

    internal static void HandleV113CorrectionZoomSmokeLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is not ScannerPage page ||
            page._v113CorrectionZoomSmokeScheduled ||
            !string.Equals(
                Environment.GetEnvironmentVariable("JUNHYUNHELPER_MAP_SMOKE"),
                "1",
                StringComparison.Ordinal))
        {
            return;
        }

        page._v113CorrectionZoomSmokeScheduled = true;
        _ = page.Dispatcher.BeginInvoke(
            DispatcherPriority.ApplicationIdle,
            new Action(page.VerifyV113CorrectionImageZoom));
    }

    private void VerifyV113CorrectionImageZoom()
    {
        ScannerCorrectionWindow? window = null;
        try
        {
            var bitmap = new WriteableBitmap(
                1280,
                720,
                96,
                96,
                PixelFormats.Bgra32,
                null);
            var frame = new ScannerRecognitionDebugFrame(
                bitmap,
                CaptureOriginX: 0,
                CaptureOriginY: 0,
                Source: "v1.11.3-published-smoke",
                SelectedBounds: new Rect(120, 80, 760, 480),
                TitleBounds: new Rect(180, 120, 620, 28),
                MagnifierBounds: null,
                CloseBounds: new Rect(840, 88, 24, 24),
                StructuralScore: 1,
                StructuralReason: "SMOKE",
                TitleAnchorScore: 1,
                TitleAnchorReason: "SMOKE",
                CaseId: "case_v113_zoom_smoke");

            window = new ScannerCorrectionWindow(frame, coordinator: null)
            {
                Owner = Window.GetWindow(this),
                Width = 1040,
                Height = 760,
                ShowInTaskbar = false,
            };
            window.Show();
            window.UpdateLayout();

            if (!window.CorrectionImageCoordinatesRemainSourcePixelsForSmoke)
            {
                throw new InvalidOperationException(
                    "Correction image zoom changed the source-pixel coordinate canvas contract.");
            }

            var before = window.CorrectionImageScaleForSmoke;
            window.ZoomCorrectionImageForSmoke(+120);
            window.UpdateLayout();
            var zoomed = window.CorrectionImageScaleForSmoke;
            if (!(zoomed > before * 1.05))
            {
                throw new InvalidOperationException(
                    $"Correction image did not zoom in from the published UI path. before={before:0.###}, after={zoomed:0.###}.");
            }

            window.ZoomCorrectionImageForSmoke(-120);
            window.UpdateLayout();
            var restored = window.CorrectionImageScaleForSmoke;
            if (Math.Abs(restored - before) > 0.01)
            {
                throw new InvalidOperationException(
                    $"Correction image zoom-out did not return to the fit scale. before={before:0.###}, restored={restored:0.###}.");
            }

            var marker = Path.Combine(Path.GetTempPath(), "junhyun-scanner-correction-zoom-smoke-success.txt");
            File.WriteAllText(
                marker,
                $"mouse-wheel-zoom=ok\nsource-pixel-coordinates=ok\nfitScale={before:0.###}\nzoomedScale={zoomed:0.###}\n");
        }
        catch (Exception exception)
        {
            try
            {
                var diagnostic = Path.Combine(Path.GetTempPath(), "junhyun-map-smoke-error.txt");
                File.WriteAllText(diagnostic, "Scanner correction zoom smoke failed.\n" + exception);
            }
            catch
            {
            }

            Environment.Exit(90);
        }
        finally
        {
            window?.Close();
        }
    }
}
