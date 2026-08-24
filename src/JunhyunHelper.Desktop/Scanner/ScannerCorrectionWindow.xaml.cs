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
        CloseButton,
        Magnifier,
        ItemName,
    }

    private enum CandidateField
    {
        DetailWindow,
        CloseButton,
        Magnifier,
        ItemName,
    }

    private readonly ScannerRecognitionDebugFrame _frame;
    private readonly ScannerCoordinator? _coordinator;
    private SelectionTarget _selectionTarget;
    private Point? _dragStart;
    private Rectangle? _dragRectangle;
    private bool _updatingSelectors;

    private Rect? _correctedDetailBounds;
    private Rect? _correctedCloseBounds;
    private Rect? _correctedMagnifierBounds;
    private Rect? _correctedTitleBounds;

    private ScannerGroundTruthSelection _detailSelection;
    private ScannerGroundTruthSelection _closeSelection;
    private ScannerGroundTruthSelection _magnifierSelection;
    private ScannerGroundTruthSelection _titleSelection;

    public ScannerCorrectionWindow(ScannerRecognitionDebugFrame frame, ScannerCoordinator? coordinator)
    {
        InitializeComponent();
        _frame = frame ?? throw new ArgumentNullException(nameof(frame));
        _coordinator = coordinator;
        GroundTruthTextBox.Text = frame.CandidateName ?? string.Empty;

        _detailSelection = CurrentSelection("detail_window", frame.SelectedBounds);
        _closeSelection = CurrentSelection("close_button", frame.CloseBounds);
        _magnifierSelection = CurrentSelection("magnifier", frame.MagnifierBounds);
        _titleSelection = CurrentSelection("item_name_roi", frame.TitleBounds);

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

        InitializeCandidateSelectors();
        RenderOverlays();

        var candidate = string.IsNullOrWhiteSpace(_frame.CandidateName) ? "-" : _frame.CandidateName;
        var itemId = string.IsNullOrWhiteSpace(_frame.ItemId) ? "-" : _frame.ItemId;
        var candidateCount = _frame.Candidates?.Count ?? 0;
        CaseSummaryText.Text =
            $"Case {_frame.CaseId} · {_frame.Timestamp.ToLocalTime():yyyy-MM-dd HH:mm:ss} · " +
            $"판정={candidate} · Item ID={itemId} · confidence={_frame.Confidence:P1} · " +
            $"reason={_frame.RecognitionReason} · detector 후보={candidateCount}";
    }

    private void InitializeCandidateSelectors()
    {
        _updatingSelectors = true;
        try
        {
            InitializeCombo(DetailCandidateComboBox, CandidateField.DetailWindow, _frame.SelectedBounds);
            InitializeCombo(CloseCandidateComboBox, CandidateField.CloseButton, _frame.CloseBounds);
            InitializeCombo(MagnifierCandidateComboBox, CandidateField.Magnifier, _frame.MagnifierBounds);
            InitializeCombo(TitleCandidateComboBox, CandidateField.ItemName, _frame.TitleBounds);
        }
        finally
        {
            _updatingSelectors = false;
        }
    }

    private void InitializeCombo(ComboBox comboBox, CandidateField field, Rect? currentBounds)
    {
        var options = BuildOptions(field, currentBounds);
        comboBox.ItemsSource = options;

        var matchingCandidate = options.FirstOrDefault(option =>
            option.Mode == ScannerGroundTruthSelectionMode.Candidate &&
            RectsEquivalent(option.Bounds, currentBounds));
        var selected = matchingCandidate ?? options.First(option => option.Mode == ScannerGroundTruthSelectionMode.Current);
        comboBox.SelectedItem = selected;
        ApplyOption(field, selected, beginManual: false);
    }

    private IReadOnlyList<CandidateOption> BuildOptions(CandidateField field, Rect? currentBounds)
    {
        var options = new List<CandidateOption>
        {
            new("현재 검출값 유지" + (currentBounds is { } current ? $" · {FormatRect(current)}" : " · 없음"),
                ScannerGroundTruthSelectionMode.Current,
                null,
                currentBounds),
        };

        foreach (var candidate in _frame.Candidates ?? [])
        {
            var bounds = CandidateBounds(candidate, field);
            if (bounds is not { Width: > 0, Height: > 0 } rect)
                continue;

            var score = field == CandidateField.DetailWindow
                ? candidate.StructuralScore
                : candidate.TitleAnchorScore;
            options.Add(new CandidateOption(
                $"후보 {candidate.Rank} · {score:P0} · {FormatRect(rect)}",
                ScannerGroundTruthSelectionMode.Candidate,
                candidate,
                rect));
        }

        options.Add(new CandidateOption("없음 · 해당 대상이 검출되면 안 됨", ScannerGroundTruthSelectionMode.None, null, null));
        options.Add(new CandidateOption("직접 지정…", ScannerGroundTruthSelectionMode.Manual, null, null));
        return options;
    }

    private void CandidateComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_updatingSelectors || sender is not ComboBox comboBox || comboBox.SelectedItem is not CandidateOption option)
            return;

        var field = comboBox == DetailCandidateComboBox
            ? CandidateField.DetailWindow
            : comboBox == CloseCandidateComboBox
                ? CandidateField.CloseButton
                : comboBox == MagnifierCandidateComboBox
                    ? CandidateField.Magnifier
                    : CandidateField.ItemName;
        ApplyOption(field, option, beginManual: true);
        RenderOverlays();
    }

    private void ApplyOption(CandidateField field, CandidateOption option, bool beginManual)
    {
        var selection = BuildSelection(field, option);
        SetSelection(field, selection);

        if (option.Mode == ScannerGroundTruthSelectionMode.Manual && beginManual)
        {
            BeginSelection(ToSelectionTarget(field), ManualInstruction(field));
            return;
        }

        _selectionTarget = SelectionTarget.None;
        _dragStart = null;
        _dragRectangle = null;
        OverlayCanvas.Cursor = Cursors.Arrow;
        SaveStatusText.Text = option.Mode switch
        {
            ScannerGroundTruthSelectionMode.Candidate => $"{FieldLabel(field)} 정답으로 {option.Label}를 선택했습니다.",
            ScannerGroundTruthSelectionMode.None => $"{FieldLabel(field)} 정답을 ‘없음’으로 지정했습니다.",
            _ => string.Empty,
        };
    }

    private ScannerGroundTruthSelection BuildSelection(CandidateField field, CandidateOption option)
    {
        var fieldName = FieldName(field);
        if (option.Mode != ScannerGroundTruthSelectionMode.Candidate || option.Candidate is null)
            return new ScannerGroundTruthSelection(fieldName, option.Mode, option.Bounds);

        var candidate = option.Candidate;
        var score = field == CandidateField.DetailWindow
            ? candidate.StructuralScore
            : candidate.TitleAnchorScore;
        var reason = field == CandidateField.DetailWindow
            ? candidate.StructuralReason
            : candidate.TitleAnchorReason;
        return new ScannerGroundTruthSelection(
            fieldName,
            ScannerGroundTruthSelectionMode.Candidate,
            option.Bounds,
            candidate.Id,
            candidate.Rank,
            score,
            reason);
    }

    private void SetSelection(CandidateField field, ScannerGroundTruthSelection selection)
    {
        switch (field)
        {
            case CandidateField.DetailWindow:
                _detailSelection = selection;
                _correctedDetailBounds = CorrectionBounds(selection, _frame.SelectedBounds);
                break;
            case CandidateField.CloseButton:
                _closeSelection = selection;
                _correctedCloseBounds = CorrectionBounds(selection, _frame.CloseBounds);
                break;
            case CandidateField.Magnifier:
                _magnifierSelection = selection;
                _correctedMagnifierBounds = CorrectionBounds(selection, _frame.MagnifierBounds);
                break;
            case CandidateField.ItemName:
                _titleSelection = selection;
                _correctedTitleBounds = CorrectionBounds(selection, _frame.TitleBounds);
                break;
        }
    }

    private static Rect? CorrectionBounds(ScannerGroundTruthSelection selection, Rect? currentBounds)
    {
        if (selection.Mode == ScannerGroundTruthSelectionMode.Current || selection.Mode == ScannerGroundTruthSelectionMode.None)
            return null;
        return RectsEquivalent(selection.Bounds, currentBounds) ? null : selection.Bounds;
    }

    private void RenderOverlays()
    {
        OverlayCanvas.Children.Clear();
        AddOverlay(_frame.SelectedBounds, Brushes.Lime, 3);
        AddOverlay(_frame.TitleBounds, Brushes.DeepSkyBlue, 2);
        AddOverlay(_frame.MagnifierBounds, Brushes.Gold, 2);
        AddOverlay(_frame.CloseBounds, Brushes.OrangeRed, 2);

        AddOverlay(SelectedBounds(_detailSelection), Brushes.Magenta, 3);
        AddOverlay(SelectedBounds(_titleSelection), Brushes.Cyan, 3);
        AddOverlay(SelectedBounds(_magnifierSelection), Brushes.MediumPurple, 3);
        AddOverlay(SelectedBounds(_closeSelection), Brushes.HotPink, 3);
        if (_dragRectangle is not null)
            OverlayCanvas.Children.Add(_dragRectangle);
    }

    private static Rect? SelectedBounds(ScannerGroundTruthSelection selection) =>
        selection.Mode == ScannerGroundTruthSelectionMode.None ? null : selection.Bounds;

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
        SelectManual(CandidateField.DetailWindow);

    private void CloseCorrectionButton_Click(object sender, RoutedEventArgs e) =>
        SelectManual(CandidateField.CloseButton);

    private void MagnifierCorrectionButton_Click(object sender, RoutedEventArgs e) =>
        SelectManual(CandidateField.Magnifier);

    private void TitleCorrectionButton_Click(object sender, RoutedEventArgs e) =>
        SelectManual(CandidateField.ItemName);

    private void SelectManual(CandidateField field)
    {
        var combo = ComboFor(field);
        if (combo.ItemsSource is not IEnumerable<CandidateOption> options)
            return;
        var manual = options.First(option => option.Mode == ScannerGroundTruthSelectionMode.Manual);
        _updatingSelectors = true;
        combo.SelectedItem = manual;
        _updatingSelectors = false;
        ApplyOption(field, manual, beginManual: true);
        RenderOverlays();
    }

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
        _correctedCloseBounds = null;
        _correctedMagnifierBounds = null;
        _correctedTitleBounds = null;
        _dragStart = null;
        _dragRectangle = null;
        OverlayCanvas.Cursor = Cursors.Arrow;
        InitializeCandidateSelectors();
        SaveStatusText.Text = "후보 선택과 직접 지정 영역을 현재 검출값 기준으로 초기화했습니다.";
        RenderOverlays();
    }

    private void OverlayCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_selectionTarget == SelectionTarget.None)
            return;
        _dragStart = ClampPoint(e.GetPosition(OverlayCanvas));
        _dragRectangle = new Rectangle
        {
            Stroke = SelectionBrush(_selectionTarget),
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

        var field = ToCandidateField(_selectionTarget);
        SetSelection(
            field,
            new ScannerGroundTruthSelection(
                FieldName(field),
                ScannerGroundTruthSelectionMode.Manual,
                rect));
        SaveStatusText.Text = $"{FieldLabel(field)} 정답 영역을 직접 {FormatRect(rect)}로 지정했습니다.";
        _selectionTarget = SelectionTarget.None;
        OverlayCanvas.Cursor = Cursors.Arrow;
        RenderOverlays();
        e.Handled = true;
    }

    private async void ConfirmButton_Click(object sender, RoutedEventArgs e)
    {
        var groundTruth = GroundTruthTextBox.Text?.Trim() ?? string.Empty;
        var textChanged = !string.IsNullOrWhiteSpace(groundTruth) &&
                          !string.Equals(groundTruth, _frame.CandidateName, StringComparison.Ordinal);
        if (HasSelectionChanges() || textChanged)
        {
            MessageBox.Show(
                this,
                "후보/영역/텍스트 수정이 지정되어 있습니다. 수정 내용을 저장하려면 ‘교정 저장’을 사용해 주세요.",
                "Scanner 교정",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }
        if (string.IsNullOrWhiteSpace(_frame.CandidateName))
        {
            MessageBox.Show(
                this,
                "프로그램이 확정한 아이템명이 없어 ‘전부 맞음’으로 저장할 수 없습니다. 정답 아이템명을 입력한 뒤 교정 저장을 사용해 주세요.",
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
        if (!HasSelectionChanges() && !textChanged)
        {
            MessageBox.Show(
                this,
                "수정된 후보/영역이나 정답 텍스트가 없습니다. 결과가 맞다면 ‘전부 맞음’을 눌러 주세요.",
                "Scanner 교정",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        var onlyAnchorChange = _correctedDetailBounds is null &&
                               _correctedTitleBounds is null &&
                               (SelectionChanged(_closeSelection, _frame.CloseBounds) ||
                                SelectionChanged(_magnifierSelection, _frame.MagnifierBounds));
        if (onlyAnchorChange && string.IsNullOrWhiteSpace(groundTruth) && string.IsNullOrWhiteSpace(_frame.CandidateName))
        {
            MessageBox.Show(
                this,
                "X/돋보기만 교정하는 경우에도 이 화면의 정답 아이템명을 함께 입력해 주세요. 그래야 해당 Case를 검토 완료 Ground Truth로 보존할 수 있습니다.",
                "Scanner 교정",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            GroundTruthTextBox.Focus();
            return;
        }

        await SaveAsync(groundTruth, userConfirmed: false);
    }

    private bool HasSelectionChanges() =>
        SelectionChanged(_detailSelection, _frame.SelectedBounds) ||
        SelectionChanged(_closeSelection, _frame.CloseBounds) ||
        SelectionChanged(_magnifierSelection, _frame.MagnifierBounds) ||
        SelectionChanged(_titleSelection, _frame.TitleBounds);

    private static bool SelectionChanged(ScannerGroundTruthSelection selection, Rect? currentBounds) =>
        selection.Mode switch
        {
            ScannerGroundTruthSelectionMode.Current => false,
            ScannerGroundTruthSelectionMode.None => currentBounds is not null,
            _ => !RectsEquivalent(selection.Bounds, currentBounds),
        };

    private async Task SaveAsync(string? groundTruth, bool userConfirmed)
    {
        SetControlsEnabled(false);
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
            if (!result.Success)
            {
                SaveStatusText.Text = $"저장 실패 · {result.Message}";
                return;
            }

            try
            {
                ScannerCandidateGroundTruth.Save(
                    _frame,
                    [
                        _detailSelection,
                        _closeSelection,
                        _magnifierSelection,
                        _titleSelection,
                    ],
                    groundTruth);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NotSupportedException)
            {
                App.WriteDiagnostic("Scanner candidate Ground Truth sidecar save failed", exception);
                SaveStatusText.Text = "기본 교정 데이터는 저장했지만 후보 선택 상세 정보를 저장하지 못했습니다.";
                MessageBox.Show(
                    this,
                    "기본 교정 데이터는 저장했지만 후보 선택 상세 정보 저장에 실패했습니다. 같은 Case를 다시 교정해 주세요.",
                    "Scanner 교정",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            SaveStatusText.Text = $"저장 완료 · {result.CaseId}";
            MessageBox.Show(
                this,
                $"교정/검증 데이터와 후보 선택 Ground Truth를 저장했습니다.\nCase ID: {result.CaseId}",
                "Scanner 교정",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception exception)
        {
            App.WriteDiagnostic("Scanner correction save failed", exception);
            SaveStatusText.Text = "교정 데이터 저장 중 오류가 발생했습니다.";
        }
        finally
        {
            SetControlsEnabled(true);
        }
    }

    private void SetControlsEnabled(bool enabled)
    {
        ConfirmButton.IsEnabled = enabled;
        SaveCorrectionButton.IsEnabled = enabled;
        DetailCorrectionButton.IsEnabled = enabled;
        CloseCorrectionButton.IsEnabled = enabled;
        MagnifierCorrectionButton.IsEnabled = enabled;
        TitleCorrectionButton.IsEnabled = enabled;
        ResetCorrectionButton.IsEnabled = enabled;
        DetailCandidateComboBox.IsEnabled = enabled;
        CloseCandidateComboBox.IsEnabled = enabled;
        MagnifierCandidateComboBox.IsEnabled = enabled;
        TitleCandidateComboBox.IsEnabled = enabled;
        GroundTruthTextBox.IsEnabled = enabled;
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

    private static Rect? CandidateBounds(ScannerDiagnosticCandidateEvidence candidate, CandidateField field) => field switch
    {
        CandidateField.DetailWindow => candidate.Bounds,
        CandidateField.CloseButton => candidate.CloseBounds,
        CandidateField.Magnifier => candidate.MagnifierBounds,
        CandidateField.ItemName => candidate.TitleBounds,
        _ => null,
    };

    private ComboBox ComboFor(CandidateField field) => field switch
    {
        CandidateField.DetailWindow => DetailCandidateComboBox,
        CandidateField.CloseButton => CloseCandidateComboBox,
        CandidateField.Magnifier => MagnifierCandidateComboBox,
        CandidateField.ItemName => TitleCandidateComboBox,
        _ => throw new ArgumentOutOfRangeException(nameof(field)),
    };

    private static SelectionTarget ToSelectionTarget(CandidateField field) => field switch
    {
        CandidateField.DetailWindow => SelectionTarget.DetailWindow,
        CandidateField.CloseButton => SelectionTarget.CloseButton,
        CandidateField.Magnifier => SelectionTarget.Magnifier,
        CandidateField.ItemName => SelectionTarget.ItemName,
        _ => SelectionTarget.None,
    };

    private static CandidateField ToCandidateField(SelectionTarget target) => target switch
    {
        SelectionTarget.DetailWindow => CandidateField.DetailWindow,
        SelectionTarget.CloseButton => CandidateField.CloseButton,
        SelectionTarget.Magnifier => CandidateField.Magnifier,
        SelectionTarget.ItemName => CandidateField.ItemName,
        _ => throw new InvalidOperationException("선택 대상을 확인할 수 없습니다."),
    };

    private static string ManualInstruction(CandidateField field) => field switch
    {
        CandidateField.DetailWindow => "실제 상세보기 창 전체를 드래그해 주세요.",
        CandidateField.CloseButton => "실제 빨간 닫기 X 영역을 드래그해 주세요.",
        CandidateField.Magnifier => "실제 아이템명 왼쪽 돋보기 영역을 드래그해 주세요.",
        CandidateField.ItemName => "실제 아이템 이름 텍스트 영역을 드래그해 주세요.",
        _ => "정답 영역을 드래그해 주세요.",
    };

    private static string FieldName(CandidateField field) => field switch
    {
        CandidateField.DetailWindow => "detail_window",
        CandidateField.CloseButton => "close_button",
        CandidateField.Magnifier => "magnifier",
        CandidateField.ItemName => "item_name_roi",
        _ => "unknown",
    };

    private static string FieldLabel(CandidateField field) => field switch
    {
        CandidateField.DetailWindow => "상세보기 창",
        CandidateField.CloseButton => "빨간 X",
        CandidateField.Magnifier => "돋보기",
        CandidateField.ItemName => "아이템명 ROI",
        _ => "영역",
    };

    private static ScannerGroundTruthSelection CurrentSelection(string field, Rect? bounds) =>
        new(field, ScannerGroundTruthSelectionMode.Current, bounds);

    private static Brush SelectionBrush(SelectionTarget target) => target switch
    {
        SelectionTarget.DetailWindow => Brushes.Magenta,
        SelectionTarget.CloseButton => Brushes.HotPink,
        SelectionTarget.Magnifier => Brushes.MediumPurple,
        SelectionTarget.ItemName => Brushes.Cyan,
        _ => Brushes.White,
    };

    private static bool RectsEquivalent(Rect? left, Rect? right)
    {
        if (left is null || right is null)
            return left is null && right is null;
        return Math.Abs(left.Value.X - right.Value.X) < 0.5 &&
               Math.Abs(left.Value.Y - right.Value.Y) < 0.5 &&
               Math.Abs(left.Value.Width - right.Value.Width) < 0.5 &&
               Math.Abs(left.Value.Height - right.Value.Height) < 0.5;
    }

    private static string FormatRect(Rect rect) =>
        $"({rect.X:0},{rect.Y:0}) {rect.Width:0}x{rect.Height:0}";

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

    private sealed record CandidateOption(
        string Label,
        ScannerGroundTruthSelectionMode Mode,
        ScannerDiagnosticCandidateEvidence? Candidate,
        Rect? Bounds)
    {
        public override string ToString() => Label;
    }
}