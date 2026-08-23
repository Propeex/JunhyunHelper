using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Win32;

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
        var rawOcr = string.IsNullOrWhiteSpace(_frame.OcrText)
            ? "(없음)"
            : _frame.OcrText.Replace("\r", " ").Replace("\n", " / ");
        var matcherText = string.IsNullOrWhiteSpace(_frame.MatcherText)
            ? "(없음)"
            : _frame.MatcherText.Replace("\r", " ").Replace("\n", " / ");
        var magnifierState = _frame.MagnifierBounds is { Width: > 0, Height: > 0 } ? "확인" : "실패";
        var closeState = _frame.CloseBounds is { Width: > 0, Height: > 0 } ? "확인" : "실패";
        DetailText.Text =
            $"캡처: {_frame.Source} · 구조 {_frame.StructuralScore:P1} ({_frame.StructuralReason}) · " +
            $"제목 anchor {_frame.TitleAnchorScore:P1} ({_frame.TitleAnchorReason}) · 돋보기 {magnifierState} · X {closeState}\n" +
            $"pass: {_frame.Pass} · 매칭 입력: {matcherText}\n" +
            $"OCR 원본(진단용): {rawOcr}\n" +
            $"후보: {_frame.CandidateName ?? "(없음)"} · 판단: {_frame.RecognitionReason} · " +
            $"신뢰도 {_frame.Confidence:P1} · 1·2순위 차이 {margin:P1}\n" +
            "초록=선택 상세창 · 파랑=OCR 제목 영역 · 노랑=돋보기 anchor · 빨강=닫기 anchor · " +
            "캡처는 메모리에만 유지되며 '이미지 저장'을 누를 때만 선택한 위치에 원본 PNG를 저장합니다.";
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

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Title = "Scanner 인식 이미지 저장",
            Filter = "PNG 이미지 (*.png)|*.png",
            DefaultExt = ".png",
            AddExtension = true,
            FileName = $"JunhyunHelper-Scanner-{_frame.Timestamp:yyyyMMdd-HHmmss}.png",
        };
        if (dialog.ShowDialog(this) != true)
            return;

        try
        {
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(_frame.Image));
            using var stream = new FileStream(dialog.FileName, FileMode.Create, FileAccess.Write, FileShare.None);
            encoder.Save(stream);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            App.WriteDiagnostic("Scanner recognition image save failed", exception);
            MessageBox.Show(
                this,
                "인식 이미지를 저장하지 못했습니다. 다른 저장 위치를 선택해 다시 시도해 주세요.",
                "Scanner 인식 이미지",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
}
