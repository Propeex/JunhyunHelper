using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace JunhyunHelper.Desktop.Scanner;

internal static class ScannerDiagnosticImageRenderer
{
    internal static BitmapSource Render(ScannerRecognitionDebugFrame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);
        var width = frame.Image.PixelWidth;
        var height = frame.Image.PixelHeight;
        if (width <= 0 || height <= 0)
            throw new InvalidDataException("Recognition image has invalid dimensions.");

        var visual = new DrawingVisual();
        using (var context = visual.RenderOpen())
        {
            context.DrawImage(frame.Image, new Rect(0, 0, width, height));
            DrawRegion(context, frame.SelectedBounds, Brushes.Lime, 3);
            DrawRegion(context, frame.TitleBounds, Brushes.DeepSkyBlue, 2);
            DrawRegion(context, frame.MagnifierBounds, Brushes.Gold, 2);
            DrawRegion(context, frame.CloseBounds, Brushes.OrangeRed, 2);
        }

        var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        bitmap.Freeze();
        return bitmap;
    }

    private static void DrawRegion(DrawingContext context, Rect? region, Brush brush, double thickness)
    {
        if (region is not { } rect || rect.Width <= 0 || rect.Height <= 0)
            return;

        var pen = new Pen(brush, thickness);
        pen.Freeze();
        var inset = thickness / 2.0;
        var drawRect = new Rect(
            rect.X + inset,
            rect.Y + inset,
            Math.Max(1, rect.Width - thickness),
            Math.Max(1, rect.Height - thickness));
        context.DrawRectangle(null, pen, drawRect);
    }
}
