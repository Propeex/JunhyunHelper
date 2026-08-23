using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace JunhyunHelper.Desktop.Scanner;

public partial class ScannerCorrectionWindow : Window
{
    private enum SelectionTarget
    {
        None,
        DetailWindow,
        ItemName,
    }

    private readonly ScannerRecognitionDebugFrame _frame;
    private readonly ScannerCoordinator? _coordinator;
    private SelectionTarget _selectionTarget;
    private Point? _dragStart;
    private Rectangle? _dragRectangle;
    private Rect? _correctedDetailBounds;
    private Rect? _correctedTitleBounds;

    public ScannerCorrectionWindow(ScannerRecognitionDebugFrame frame, ScannerCoordinator? coordinator)
    {
        InitializeComponent();
        _frame = frame ?? throw new ArgumentNullException(nameof(frame));
        _coordinator = coordinator;
        GroundTruthTextBox.Text = frame.CandidateName ?? string.Empty;
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
        RenderOverlays();

        var candidate = string.IsNullOrWhiteSpace(_frame.CandidateName) ? "-" : _frame.CandidateName;
        var itemId = string.IsNullOrWhiteSpace(_frame.ItemId) ? "-" : _frame.ItemId;
        CaseSummaryText.Text =
            $"Case {_frame.CaseId} · {_frame.Timestamp.ToLocalTime():yyyy-MM-dd HH:mm:ss} · " +
            $"판정={candidate} · Item ID={itemId} · confidence={_frame.Confidence:P1} · " +
            $"reason={_frame.RecognitionReason}";
    }

    private void RenderOverlays()
    {
        OverlayCanvas.Children.Clear();
        AddOverlay(_frame.SelectedBounds, Brushes.Lime, 3);
        AddOverlay(_frame.TitleBounds, Brushes.DeepSkyBlue, 2);
        AddOverlay(_frame.MagnifierBounds, Brushes.Gold, 2);
        AddOverlay(_frame.CloseBounds, Brushes.OrangeRed, 2);
        AddOverlay(_correctedDetailBounds, Brushes.Magenta, 3);
        AddOverlay(_correctedTitleBounds, Brushes.Cyan, 3);
        if (_dragRectangle is not null)
            OverlayCanvas.Children.Add(_dragRectangle);
    }

    private void AddOverlay(Rect? region, Brush brush, double thickness)
    {
        if (region is not { } rect || rect.Width <= 0 || rect.Height <= 0)
            return;
        var rectangle = new Rectangle
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

    private void DetailCorrectionButton_Click(object sender, RoutedEventArgs e) =>
        BeginSelection(SelectionTarget.DetailWindow, "실제 상세보기 창 전체를 드래그해 주세요.");

    private void TitleCorrectionButton_Click(object sender, RoutedEventArgs e) =>
        BeginSelection(SelectionTarget.ItemName, "실제 아이템 이름 텍스트 영역을 드래그해 주세요.");

    private void BeginSelection(SelectionTarget target, string instruction)
    {
        _selectionTarget = target;
        _dragStart = null;
        _dragRectangle = null;
        OverlayCanvas.Cursor = Cursors.Cross;
        SaveStatusText.Text = instruction;
        RenderOverlays();
    }

    private void ResetCorrectionButton_Click(object sender, RoutedEventArgs e)
    {
        _selectionTarget = SelectionTarget.None;
        _correctedDetailBounds = null;
        _correctedTitleBounds = null;
        _dragStart = null;
        _dragRectangle = null;
        OverlayCanvas.Cursor = Cursors.Arrow;
        SaveStatusText.Text = "영역 교정을 초기화했습니다.";
        RenderOverlays();
    }

    private void OverlayCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_selectionTarget == SelectionTarget.None)
            return;
        _dragStart = ClampPoint(e.GetPosition(OverlayCanvas));
        _dragRectangle = new Rectangle
        {
            Stroke = _selectionTarget == SelectionTarget.DetailWindow ? Brushes.Magenta : Brushes.Cyan,
            StrokeThickness = 3,
            Fill = Brushes.Transparent,
            IsHitTestVisible = false,
        };
        OverlayCanvas.CaptureMouse();
        RenderOverlays();
        e.Handled = true;
    }

    private void OverlayCanvas_MouseMove(object sender, MouseEventArgs e)
    {
        if (_dragStart is not { } start || _dragRectangle is null || e.LeftButton != MouseButtonState.Pressed)
            return;
        var current = ClampPoint(e.GetPosition(OverlayCanvas));
        var rect = NormalizeRect(start, current);
        _dragRectangle.Width = rect.Width;
        _dragRectangle.Height = rect.Height;
        Canvas.SetLeft(_dragRectangle, rect.X);
        Canvas.SetTop(_dragRectangle, rect.Y);
        e.Handled = true;
    }

    private void OverlayCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_dragStart is not { } start || _selectionTarget == SelectionTarget.None)
            return;
        var current = ClampPoint(e.GetPosition(OverlayCanvas));
        var rect = NormalizeRect(start, current);
        OverlayCanvas.ReleaseMouseCapture();
        _dragStart = null;
        _dragRectangle = null;

        if (rect.Width < 3 || rect.Height < 3)
        {
            SaveStatusText.Text = "선택 영역이 너무 작습니다. 다시 드래그해 주세요.";
            RenderOverlays();
            return;
        }

        if (_selectionTarget == SelectionTarget.DetailWindow)
        {
            _correctedDetailBounds = rect;
            SaveStatusText.Text = $"상세보기 정답 영역을 {FormatRect(rect)}로 지정했습니다.";
        }
        else
        {
            _correctedTitleBounds = rect;
            SaveStatusText.Text = $"아이템명 정답 영역을 {FormatRect(rect)}로 지정했습니다.";
        }
        _selectionTarget = SelectionTarget.None;
        OverlayCanvas.Cursor = Cursors.Arrow;
        RenderOverlays();
        e.Handled = true;
    }

    private async void ConfirmButton_Click(object sender, RoutedEventArgs e)
    {
        if (_correctedDetailBounds is not null || _correctedTitleBounds is not null)
        {
            MessageBox.Show(
                this,
                "이미 영역 수정이 지정되어 있습니다. 영역 교정을 저장하려면 ‘텍스트/영역 교정 저장’을 사용해 주세요.",
                "Scanner 교정",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }
        if (string.IsNullOrWhiteSpace(_frame.CandidateName))
        {
            MessageBox.Show(
                this,
                "프로그램이 확정한 아이템명이 없어 ‘맞음’으로 저장할 수 없습니다. 정답 아이템명을 입력한 뒤 교정 저장을 사용해 주세요.",
                "Scanner 교정",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }
        await SaveAsync(_frame.CandidateName, userConfirmed: true);
    }

    private async void SaveCorrectionButton_Click(object sender, RoutedEventArgs e)
    {
        var groundTruth = GroundTruthTextBox.Text?.Trim() ?? string.Empty;
        var textChanged = !string.IsNullOrWhiteSpace(groundTruth) &&
            !string.Equals(groundTruth, _frame.CandidateName, StringComparison.Ordinal);
        if (_correctedDetailBounds is null && _correctedTitleBounds is null && !textChanged)
        {
            MessageBox.Show(
                this,
                "수정된 영역이나 정답 텍스트가 없습니다. 결과가 맞다면 ‘맞음’을 눌러 주세요.",
                "Scanner 교정",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }
        await SaveAsync(groundTruth, userConfirmed: false);
    }

    private async Task SaveAsync(string? groundTruth, bool userConfirmed)
    {
        ConfirmButton.IsEnabled = false;
        SaveCorrectionButton.IsEnabled = false;
        DetailCorrectionButton.IsEnabled = false;
        TitleCorrectionButton.IsEnabled = false;
        ResetCorrectionButton.IsEnabled = false;
        SaveStatusText.Text = "교정/진단 데이터를 저장하는 중입니다.";
        try
        {
            ScannerItemSnapshot? presentation = null;
            if (_coordinator is not null && !string.IsNullOrWhiteSpace(_frame.ItemId))
                presentation = _coordinator.CreateDiagnosticSnapshot(_frame.ItemId);

            var result = await ScannerDiagnosticDataset.SaveCorrectionAsync(new ScannerCorrectionSubmission(
                _frame,
                _correctedDetailBounds,
                _correctedTitleBounds,
                groundTruth,
                userConfirmed,
                presentation));
            SaveStatusText.Text = result.Success
                ? $"저장 완료 · {result.CaseId}"
                : $"저장 실패 · {result.Message}";
            if (result.Success)
            {
                MessageBox.Show(
                    this,
                    $"교정/검증 데이터를 저장했습니다.\nCase ID: {result.CaseId}",
                    "Scanner 교정",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }
        catch (Exception exception)
        {
            App.WriteDiagnostic("Scanner correction save failed", exception);
            SaveStatusText.Text = "교정 데이터 저장 중 오류가 발생했습니다.";
        }
        finally
        {
            ConfirmButton.IsEnabled = true;
            SaveCorrectionButton.IsEnabled = true;
            DetailCorrectionButton.IsEnabled = true;
            TitleCorrectionButton.IsEnabled = true;
            ResetCorrectionButton.IsEnabled = true;
        }
    }

    private Point ClampPoint(Point point) => new(
        Math.Clamp(point.X, 0, Math.Max(0, _frame.Image.PixelWidth)),
        Math.Clamp(point.Y, 0, Math.Max(0, _frame.Image.PixelHeight)));

    private static Rect NormalizeRect(Point left, Point right)
    {
        var x = Math.Min(left.X, right.X);
        var y = Math.Min(left.Y, right.Y);
        return new Rect(x, y, Math.Abs(right.X - left.X), Math.Abs(right.Y - left.Y));
    }

    private static string FormatRect(Rect rect) =>
        $"({rect.X:0},{rect.Y:0}) {rect.Width:0}x{rect.Height:0}";

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
}