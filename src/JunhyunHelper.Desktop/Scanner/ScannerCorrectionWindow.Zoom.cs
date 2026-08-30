using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace JunhyunHelper.Desktop.Scanner;

public partial class ScannerCorrectionWindow
{
    private const double CorrectionZoomStep = 1.15;
    private const double CorrectionZoomMinimumMultiplier = 1.0;
    private const double CorrectionZoomMaximumMultiplier = 8.0;

    private readonly ScaleTransform _correctionImageScale = new(1, 1);
    private double _correctionImageZoomMultiplier = 1.0;
    private bool _correctionImageScaleRefreshScheduled;

    private void ImageScrollViewer_SizeChanged(object sender, SizeChangedEventArgs e) =>
        ScheduleCorrectionImageScaleRefresh();

    private void ImageScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (_frame.Image.PixelWidth <= 0 || _frame.Image.PixelHeight <= 0)
            return;

        var pointer = e.GetPosition(ImageScrollViewer);
        AdjustCorrectionImageZoom(e.Delta, pointer);
        e.Handled = true;
    }

    private void AdjustCorrectionImageZoom(int wheelDelta, Point pointer)
    {
        if (wheelDelta == 0)
            return;

        EnsureCorrectionImageScaleTransform();
        UpdateCorrectionImageScale();
        ImageScrollViewer.UpdateLayout();

        var oldExtentWidth = Math.Max(1, ImageScrollViewer.ExtentWidth);
        var oldExtentHeight = Math.Max(1, ImageScrollViewer.ExtentHeight);
        var anchorX = Math.Clamp(
            (ImageScrollViewer.HorizontalOffset + pointer.X) / oldExtentWidth,
            0,
            1);
        var anchorY = Math.Clamp(
            (ImageScrollViewer.VerticalOffset + pointer.Y) / oldExtentHeight,
            0,
            1);

        var factor = wheelDelta > 0 ? CorrectionZoomStep : 1.0 / CorrectionZoomStep;
        var next = Math.Clamp(
            _correctionImageZoomMultiplier * factor,
            CorrectionZoomMinimumMultiplier,
            CorrectionZoomMaximumMultiplier);
        if (Math.Abs(next - _correctionImageZoomMultiplier) <= 1e-9)
            return;

        _correctionImageZoomMultiplier = next;
        UpdateCorrectionImageScale();
        ImageScrollViewer.UpdateLayout();

        var newExtentWidth = Math.Max(1, ImageScrollViewer.ExtentWidth);
        var newExtentHeight = Math.Max(1, ImageScrollViewer.ExtentHeight);
        ImageScrollViewer.ScrollToHorizontalOffset(Math.Max(0, (anchorX * newExtentWidth) - pointer.X));
        ImageScrollViewer.ScrollToVerticalOffset(Math.Max(0, (anchorY * newExtentHeight) - pointer.Y));
    }

    private void ScheduleCorrectionImageScaleRefresh()
    {
        if (_correctionImageScaleRefreshScheduled)
            return;

        _correctionImageScaleRefreshScheduled = true;
        Dispatcher.BeginInvoke(
            new Action(() =>
            {
                _correctionImageScaleRefreshScheduled = false;
                EnsureCorrectionImageScaleTransform();
                UpdateCorrectionImageScale();
            }),
            DispatcherPriority.Render);
    }

    private void EnsureCorrectionImageScaleTransform()
    {
        if (!ReferenceEquals(ImageCanvasHost.LayoutTransform, _correctionImageScale))
            ImageCanvasHost.LayoutTransform = _correctionImageScale;
    }

    private void UpdateCorrectionImageScale()
    {
        var sourceWidth = Math.Max(1, _frame.Image.PixelWidth);
        var sourceHeight = Math.Max(1, _frame.Image.PixelHeight);

        // Use the ScrollViewer control's stable arranged size rather than ViewportWidth /
        // ViewportHeight. The viewport shrinks when Auto scrollbars appear during zoom,
        // which made the nominal fit scale depend on the previous zoom state. Basing fit
        // on the stable control bounds keeps one deterministic fit scale while window
        // resizing still updates it through SizeChanged.
        var viewportWidth = ImageScrollViewer.ActualWidth
            - ImageScrollViewer.Padding.Left
            - ImageScrollViewer.Padding.Right
            - ImageScrollViewer.BorderThickness.Left
            - ImageScrollViewer.BorderThickness.Right;
        var viewportHeight = ImageScrollViewer.ActualHeight
            - ImageScrollViewer.Padding.Top
            - ImageScrollViewer.Padding.Bottom
            - ImageScrollViewer.BorderThickness.Top
            - ImageScrollViewer.BorderThickness.Bottom;

        var fitScale = 1.0;
        if (double.IsFinite(viewportWidth) && viewportWidth > 1 &&
            double.IsFinite(viewportHeight) && viewportHeight > 1)
        {
            fitScale = Math.Min(
                1.0,
                Math.Min(viewportWidth / sourceWidth, viewportHeight / sourceHeight));
        }

        if (!double.IsFinite(fitScale) || fitScale <= 0)
            fitScale = 1.0;

        var displayScale = fitScale * _correctionImageZoomMultiplier;
        _correctionImageScale.ScaleX = displayScale;
        _correctionImageScale.ScaleY = displayScale;
    }

    internal double CorrectionImageScaleForSmoke
    {
        get
        {
            EnsureCorrectionImageScaleTransform();
            UpdateCorrectionImageScale();
            return _correctionImageScale.ScaleX;
        }
    }

    internal void ZoomCorrectionImageForSmoke(int wheelDelta)
    {
        ImageScrollViewer.UpdateLayout();
        var point = new Point(
            Math.Max(1, ImageScrollViewer.ViewportWidth) / 2,
            Math.Max(1, ImageScrollViewer.ViewportHeight) / 2);
        AdjustCorrectionImageZoom(wheelDelta, point);
    }

    internal bool CorrectionImageCoordinatesRemainSourcePixelsForSmoke =>
        Math.Abs(ImageCanvasHost.Width - _frame.Image.PixelWidth) <= 0.01 &&
        Math.Abs(ImageCanvasHost.Height - _frame.Image.PixelHeight) <= 0.01 &&
        Math.Abs(OverlayCanvas.Width - _frame.Image.PixelWidth) <= 0.01 &&
        Math.Abs(OverlayCanvas.Height - _frame.Image.PixelHeight) <= 0.01;
}
