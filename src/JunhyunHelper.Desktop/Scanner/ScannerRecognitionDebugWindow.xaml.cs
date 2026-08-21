using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace JunhyunHelper.Desktop.Scanner;

public partial class ScannerRecognitionDebugWindow : Window
{
    private readonly ScannerRecognitionDebugFrame _frame;

    public ScannerRecognitionDebugWindow(ScannerRecognitionDebugFrame frame)
    {
        _frame = frame ?? throw new ArgumentNullException(nameof(frame));
        InitializeComponent();
        RenderFrame();
    }

    private void RenderFrame()
    {
        CaptureImage.Source = _frame.Image;
        ImageCanvasHost.Width = _frame.Image.PixelWidth;
        ImageCanvasHost.Height = _frame.Image.PixelHeight;
        OverlayCanvas.Width = _frame.Image.PixelWidth;
        OverlayCanvas.Height = _frame.Image.PixelHeight;

        AddOverlay(_frame.SelectedBounds, Brushes.Lime, 2.0);
        AddOverlay(_frame.TitleBounds, Brushes.DeepSkyBlue, 2.0);
        AddOverlay(_frame.MagnifierBounds, Brushes.Gold, 2.0);
        AddOverlay(_frame.CloseBounds, Brushes.OrangeRed, 2.0);

        var margin = Math.Max(0, _frame.Confidence - _frame.SecondScore);
        var ocr = string.IsNullOrWhiteSpace(_frame.OcrText)
            ? "(없음)"
            : _frame.OcrText.Replace("\r", " ").Replace("\n", " / ");
        DetailText.Text =
            $"캡처: {_frame.Source} · 구조 {_frame.StructuralScore:P1} ({_frame.StructuralReason}) · " +
            $"제목 anchor {_frame.TitleAnchorScore:P1} ({_frame.TitleAnchorReason})\n" +
            $"pass: {_frame.Pass} · OCR: {ocr}\n" +
            $"후보: {_frame.CandidateName ?? "(없음)"} · 판단: {_frame.RecognitionReason} · " +
            $"신뢰도 {_frame.Confidence:P1} · 1·2순위 차이 {margin:P1}\n" +
            "초록=선택 상세창 · 파랑=OCR 제목 영역 · 노랑=돋보기 anchor · 빨강=닫기 anchor · 이미지는 메모리에만 보관됩니다.";
    }

    private void AddOverlay(Rect? bounds, Brush brush, double thickness)
    {
        if (bounds is not { } rect || rect.Width <= 0 || rect.Height <= 0)
            return;

        var shape = new Rectangle
        {
            Width = rect.Width,
            Height = rect.Height,
            Stroke = brush,
            StrokeThickness = thickness,
            Fill = Brushes.Transparent,
            IsHitTestVisible = false,
        };
        Canvas.SetLeft(shape, rect.X);
        Canvas.SetTop(shape, rect.Y);
        OverlayCanvas.Children.Add(shape);
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
}
