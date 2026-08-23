using System.IO;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace JunhyunHelper.Desktop.Scanner;

public partial class ScannerRecognitionDebugWindow : Window
{
    private readonly ScannerRecognitionDebugFrame _frame;

    public ScannerRecognitionDebugWindow(ScannerRecognitionDebugFrame frame)
    {
        InitializeComponent();
        _frame = frame;
        Loaded += (_, _) => RenderFrame();
    }

    private void RenderFrame()
    {
        CaptureImage.Source = _frame.Image;
        ImageCanvasHost.Width = _frame.Image.PixelWidth;
        ImageCanvasHost.Height = _frame.Image.PixelHeight;
        CaptureImage.Width = _frame.Image.PixelWidth;
        CaptureImage.Height = _frame.Image.PixelHeight;
        OverlayCanvas.Width = _frame.Image.PixelWidth;
        OverlayCanvas.Height = _frame.Image.PixelHeight;
        OverlayCanvas.Children.Clear();

        AddOverlay(_frame.SelectedBounds, Brushes.Lime, 3);
        AddOverlay(_frame.TitleBounds, Brushes.DeepSkyBlue, 2);
        AddOverlay(_frame.MagnifierBounds, Brushes.Gold, 2);
        AddOverlay(_frame.CloseBounds, Brushes.OrangeRed, 2);

        SummaryText.Text =
            $"{_frame.Timestamp.ToLocalTime():yyyy-MM-dd HH:mm:ss} | {_frame.Source} | " +
            $"선택={FormatRegion(_frame.SelectedBounds)} | 제목={FormatRegion(_frame.TitleBounds)} | " +
            $"돋보기={FormatRegion(_frame.MagnifierBounds)} | X={FormatRegion(_frame.CloseBounds)} | " +
            $"anchor={_frame.TitleAnchorScore:P1}/{_frame.TitleAnchorReason}";
        OcrText.Text = BuildOcrSummary(_frame);
    }

    private void AddOverlay(Rect? region, Brush brush, double thickness)
    {
        if (region is not { } rect || rect.Width <= 0 || rect.Height <= 0)
            return;

        var rectangle = new System.Windows.Shapes.Rectangle
        {
            Width = rect.Width,
            Height = rect.Height,
            Stroke = brush,
            StrokeThickness = thickness,
            IsHitTestVisible = false,
        };
        Canvas.SetLeft(rectangle, rect.X);
        Canvas.SetTop(rectangle, rect.Y);
        OverlayCanvas.Children.Add(rectangle);
    }

    private static string BuildOcrSummary(ScannerRecognitionDebugFrame frame)
    {
        var candidate = string.IsNullOrWhiteSpace(frame.CandidateName) ? "-" : frame.CandidateName;
        var rawOcr = string.IsNullOrWhiteSpace(frame.OcrText)
            ? "-"
            : frame.OcrText.ReplaceLineEndings(" / ");
        var matcherText = string.IsNullOrWhiteSpace(frame.MatcherText)
            ? "-"
            : frame.MatcherText.ReplaceLineEndings(" / ");
        return
            $"pass={frame.Pass} | rawOcr={rawOcr} | matcherText={matcherText} | " +
            $"candidate={candidate} | confidence={frame.Confidence:P1} | " +
            $"second={frame.SecondScore:P1} | reason={frame.RecognitionReason}";
    }

    private static string FormatRegion(Rect? region) =>
        region is not { } rect || rect.Width <= 0 || rect.Height <= 0
            ? "-"
            : $"({rect.X:0},{rect.Y:0}) {rect.Width:0}x{rect.Height:0}";

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Title = "Scanner 진단 이미지 저장",
            Filter = "PNG 이미지 (*.png)|*.png",
            DefaultExt = ".png",
            AddExtension = true,
            FileName = $"JunhyunHelper-Scanner-Diagnostic-{_frame.Timestamp.ToLocalTime():yyyyMMdd-HHmmss}.png",
        };

        if (dialog.ShowDialog(this) != true)
            return;

        try
        {
            var diagnosticBitmap = RenderDiagnosticBitmap(_frame);
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(diagnosticBitmap));
            await using var stream = new FileStream(
                dialog.FileName,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 64 * 1024,
                useAsync: true);
            encoder.Save(stream);
            await stream.FlushAsync();
            ScannerDiagnosticLog.Write(
                $"Recognition diagnostic image exported path='{dialog.FileName}' " +
                $"source='{_frame.Source}' size={_frame.Image.PixelWidth}x{_frame.Image.PixelHeight} " +
                "overlay=detail,title,magnifier,close");
            MessageBox.Show(
                this,
                "진단 영역 표시가 포함된 인식 이미지를 저장했습니다.\n문제 피드백 시 이 PNG를 보내주세요.",
                "Scanner",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            ScannerDiagnosticLog.Write($"Recognition diagnostic image export failed: {ex.GetType().Name}: {ex.Message}");
            MessageBox.Show(
                this,
                $"이미지를 저장하지 못했습니다.\n{ex.Message}",
                "Scanner",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    internal static BitmapSource RenderDiagnosticBitmap(ScannerRecognitionDebugFrame frame)
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

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
}
