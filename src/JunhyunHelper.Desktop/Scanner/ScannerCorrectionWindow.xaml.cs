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
    private CandidateField _activeField = CandidateField.DetailWindow;
    private SelectionTarget _selectionTarget;
    private Point? _dragStart;
    private Rectangle? _dragRectangle;

    private Rect? _correctedDetailBounds;
    private Rect? _correctedCloseBounds;
    private Rect? _correctedMagnifierBounds;
    private Rect? _correctedTitleBounds;

    private ScannerGroundTruthSelection _detailSelection = CurrentSelection("detail_window", null);
    private ScannerGroundTruthSelection _closeSelection = CurrentSelection("close_button", null);
    private ScannerGroundTruthSelection _magnifierSelection = CurrentSelection("magnifier", null);
    private ScannerGroundTruthSelection _titleSelection = CurrentSelection("item_name_roi", null);

    public ScannerCorrectionWindow(ScannerRecognitionDebugFrame frame, ScannerCoordinator? coordinator)
        : this(frame, coordinator, null, null)
    {
    }

    public ScannerCorrectionWindow(ScannerStoredCorrectionCase storedCase, ScannerCoordinator? coordinator)
        : this(
            (storedCase ?? throw new ArgumentNullException(nameof(storedCase))).Frame,
            coordinator,
            storedCase.GroundTruthItemName,
            storedCase.Selections)
    {
    }

    private ScannerCorrectionWindow(
        ScannerRecognitionDebugFrame frame,
        ScannerCoordinator? coordinator,
        string? initialGroundTruth,
        IReadOnlyList<ScannerGroundTruthSelection>? initialSelections)
    {
        InitializeComponent();
        _frame = frame ?? throw new ArgumentNullException(nameof(frame));
        _coordinator = coordinator;
        GroundTruthTextBox.Text = string.IsNullOrWhiteSpace(initialGroundTruth)
            ? frame.CandidateName ?? string.Empty
            : initialGroundTruth.Trim();

        SetSelection(CandidateField.DetailWindow, InitialSelection(CandidateField.DetailWindow, initialSelections));
        SetSelection(CandidateField.CloseButton, InitialSelection(CandidateField.CloseButton, initialSelections));
        SetSelection(CandidateField.Magnifier, InitialSelection(CandidateField.Magnifier, initialSelections));
        SetSelection(CandidateField.ItemName, InitialSelection(CandidateField.ItemName, initialSelections));

        Loaded += (_, _) => RenderFrame();
    }

    public bool DatasetChanged { get; private set; }

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
        UpdateActiveFieldUi();

        var candidate = string.IsNullOrWhiteSpace(_frame.CandidateName) ? "-" : _frame.CandidateName;
        var itemId = string.IsNullOrWhiteSpace(_frame.ItemId) ? "-" : _frame.ItemId;
        var candidateCount = _frame.Candidates?.Count ?? 0;
        CaseSummaryText.Text =
            $"Case {_frame.CaseId} · {_frame.Timestamp.ToLocalTime():yyyy-MM-dd HH:mm:ss} · " +
            $"판정={candidate} · Item ID={itemId} · 후보={candidateCount}";
    }

    private ScannerGroundTruthSelection InitialSelection(
        CandidateField field,
        IReadOnlyList<ScannerGroundTruthSelection>? saved)
    {
        var fieldName = FieldName(field);
        var existing = saved?.FirstOrDefault(selection =>
            string.Equals(selection.Field, fieldName, StringComparison.Ordinal));
        if (existing is not null)
            return existing;

        var currentBounds = CurrentBounds(field);
        var matching = (_frame.Candidates ?? []).FirstOrDefault(candidate =>
            RectsEquivalent(CandidateBounds(candidate, field), currentBounds));
        return matching is null
            ? CurrentSelection(fieldName, currentBounds)
            : CandidateSelection(field, matching, CandidateBounds(matching, field));
    }

    private void DetailFieldButton_Click(object sender, RoutedEventArgs e) => SelectField(CandidateField.DetailWindow);
    private void CloseFieldButton_Click(object sender, RoutedEventArgs e) => SelectField(CandidateField.CloseButton);
    private void MagnifierFieldButton_Click(object sender, RoutedEventArgs e) => SelectField(CandidateField.Magnifier);
    private void TitleFieldButton_Click(object sender, RoutedEventArgs e) => SelectField(CandidateField.ItemName);

    private void SelectField(CandidateField field)
    {
        CancelManualSelection();
        _activeField = field;
        SaveStatusText.Text = string.Empty;
        UpdateActiveFieldUi();
        RenderOverlays();
    }

    private void KeepCurrentButton_Click(object sender, RoutedEventArgs e)
    {
        CancelManualSelection();
        SetSelection(_activeField, CurrentSelection(FieldName(_activeField), CurrentBounds(_activeField)));
        SaveStatusText.Text = $"{FieldLabel(_activeField)}을 현재 검출값으로 지정했습니다.";
        UpdateActiveFieldUi();
        RenderOverlays();
    }

    private void NoneButton_Click(object sender, RoutedEventArgs e)
    {
        CancelManualSelection();
        SetSelection(
            _activeField,
            new ScannerGroundTruthSelection(
                FieldName(_activeField),
                ScannerGroundTruthSelectionMode.None,
                null));
        SaveStatusText.Text = $"{FieldLabel(_activeField)} 정답을 ‘없음’으로 지정했습니다.";
        UpdateActiveFieldUi();
        RenderOverlays();
    }

    private void ManualButton_Click(object sender, RoutedEventArgs e) =>
        BeginSelection(ToSelectionTarget(_activeField), ManualInstruction(_activeField));

    private void CandidateOverlay_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_selectionTarget != SelectionTarget.None ||
            sender is not Rectangle { Tag: CandidateHit hit })
        {
            return;
        }

        SetSelection(_activeField, CandidateSelection(_activeField, hit.Candidate, hit.Bounds));
        SaveStatusText.Text = $"{FieldLabel(_activeField)} 후보를 선택했습니다.";
        UpdateActiveFieldUi();
        RenderOverlays();
        e.Handled = true;
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
        if (selection.Mode is ScannerGroundTruthSelectionMode.Current or ScannerGroundTruthSelectionMode.None)
            return null;
        return RectsEquivalent(selection.Bounds, currentBounds) ? null : selection.Bounds;
    }

    private void RenderOverlays()
    {
        OverlayCanvas.Children.Clear();

        // Existing selections remain visible for context. The active field additionally
        // exposes every real detector candidate as a clickable rectangle.
        AddOverlay(SelectedBounds(_detailSelection), Brushes.Magenta, 3);
        AddOverlay(SelectedBounds(_closeSelection), Brushes.HotPink, 3);
        AddOverlay(SelectedBounds(_magnifierSelection), Brushes.MediumPurple, 3);
        AddOverlay(SelectedBounds(_titleSelection), Brushes.Cyan, 3);

        foreach (var candidate in (_frame.Candidates ?? []).OrderByDescending(candidate => candidate.Rank))
        {
            var bounds = CandidateBounds(candidate, _activeField);
            if (bounds is not { Width: > 0, Height: > 0 } rect)
                continue;
            AddCandidateOverlay(candidate, rect);
        }

        if (_dragRectangle is not null)
            OverlayCanvas.Children.Add(_dragRectangle);
    }

    private void AddCandidateOverlay(ScannerDiagnosticCandidateEvidence candidate, Rect rect)
    {
        var selection = SelectionFor(_activeField);
        var selected = selection.Mode == ScannerGroundTruthSelectionMode.Candidate &&
                       string.Equals(selection.CandidateId, candidate.Id, StringComparison.Ordinal) &&
                       RectsEquivalent(selection.Bounds, rect);
        var rectangle = new Rectangle
        {
            Width = rect.Width,
            Height = rect.Height,
            Stroke = selected ? Brushes.Gold : Brushes.WhiteSmoke,
            StrokeThickness = selected ? 5 : 2,
            StrokeDashArray = selected ? null : new DoubleCollection([5, 3]),
            Fill = new SolidColorBrush(Color.FromArgb(selected ? (byte)34 : (byte)18, 255, 255, 255)),
            Cursor = Cursors.Hand,
            Tag = new CandidateHit(candidate, rect),
            ToolTip = "클릭하여 이 영역을 정답 후보로 선택",
        };
        rectangle.MouseLeftButtonDown += CandidateOverlay_MouseLeftButtonDown;
        Canvas.SetLeft(rectangle, rect.X);
        Canvas.SetTop(rectangle, rect.Y);
        OverlayCanvas.Children.Add(rectangle);

        var rank = new TextBlock
        {
            Text = candidate.Rank.ToString(),
            Foreground = selected ? Brushes.Black : Brushes.White,
            Background = selected ? Brushes.Gold : Brushes.Black,
            FontSize = 11,
            Padding = new Thickness(3, 1, 3, 1),
            IsHitTestVisible = false,
        };
        Canvas.SetLeft(rank, rect.X + 2);
        Canvas.SetTop(rank, rect.Y + 2);
        OverlayCanvas.Children.Add(rank);
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

    private void BeginSelection(SelectionTarget target, string instruction)
    {
        _selectionTarget = target;
        _dragStart = null;
        _dragRectangle = null;
        OverlayCanvas.Cursor = Cursors.Cross;
        SaveStatusText.Text = instruction;
        UpdateActiveFieldUi();
        RenderOverlays();
    }

    private void CancelManualSelection()
    {
        if (OverlayCanvas.IsMouseCaptured)
            OverlayCanvas.ReleaseMouseCapture();
        _selectionTarget = SelectionTarget.None;
        _dragStart = null;
        _dragRectangle = null;
        OverlayCanvas.Cursor = Cursors.Arrow;
    }

    private void ResetCorrectionButton_Click(object sender, RoutedEventArgs e)
    {
        CancelManualSelection();
        SetSelection(CandidateField.DetailWindow, DefaultSelection(CandidateField.DetailWindow));
        SetSelection(CandidateField.CloseButton, DefaultSelection(CandidateField.CloseButton));
        SetSelection(CandidateField.Magnifier, DefaultSelection(CandidateField.Magnifier));
        SetSelection(CandidateField.ItemName, DefaultSelection(CandidateField.ItemName));
        SaveStatusText.Text = "영역 선택을 현재 Scanner 검출값 기준으로 초기화했습니다.";
        UpdateActiveFieldUi();
        RenderOverlays();
    }

    private ScannerGroundTruthSelection DefaultSelection(CandidateField field)
    {
        var currentBounds = CurrentBounds(field);
        var matching = (_frame.Candidates ?? []).FirstOrDefault(candidate =>
            RectsEquivalent(CandidateBounds(candidate, field), currentBounds));
        return matching is null
            ? CurrentSelection(FieldName(field), currentBounds)
            : CandidateSelection(field, matching, CandidateBounds(matching, field));
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
        SaveStatusText.Text = $"{FieldLabel(field)} 정답 영역을 직접 지정했습니다.";
        CancelManualSelection();
        UpdateActiveFieldUi();
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
                "영역 또는 텍스트 수정이 지정되어 있습니다. 수정 내용을 저장하려면 ‘교정 저장’을 사용해 주세요.",
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
                "수정된 영역이나 정답 텍스트가 없습니다. 결과가 맞다면 ‘전부 맞음’을 눌러 주세요.",
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

            ScannerCandidateGroundTruth.Save(
                _frame,
                [
                    _detailSelection,
                    _closeSelection,
                    _magnifierSelection,
                    _titleSelection,
                ],
                groundTruth);

            DatasetChanged = true;
            SaveStatusText.Text = $"저장 완료 · {result.CaseId}";
            MessageBox.Show(
                this,
                $"교정/검증 데이터를 저장했습니다.\nCase ID: {result.CaseId}",
                "Scanner 교정",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or NotSupportedException or InvalidDataException)
        {
            App.WriteDiagnostic("Scanner correction save failed", exception);
            SaveStatusText.Text = "교정 데이터 저장 중 오류가 발생했습니다.";
            MessageBox.Show(this, "교정 데이터를 저장하지 못했습니다.", "Scanner 교정", MessageBoxButton.OK, MessageBoxImage.Warning);
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
        DetailFieldButton.IsEnabled = enabled;
        CloseFieldButton.IsEnabled = enabled;
        MagnifierFieldButton.IsEnabled = enabled;
        TitleFieldButton.IsEnabled = enabled;
        KeepCurrentButton.IsEnabled = enabled;
        NoneButton.IsEnabled = enabled;
        ManualButton.IsEnabled = enabled;
        ResetCorrectionButton.IsEnabled = enabled;
        GroundTruthTextBox.IsEnabled = enabled;
    }

    private void UpdateActiveFieldUi()
    {
        var buttons = new Dictionary<CandidateField, Button>
        {
            [CandidateField.DetailWindow] = DetailFieldButton,
            [CandidateField.CloseButton] = CloseFieldButton,
            [CandidateField.Magnifier] = MagnifierFieldButton,
            [CandidateField.ItemName] = TitleFieldButton,
        };
        foreach (var pair in buttons)
        {
            pair.Value.FontWeight = pair.Key == _activeField ? FontWeights.Bold : FontWeights.Normal;
            pair.Value.Opacity = pair.Key == _activeField ? 1 : 0.72;
        }

        ActiveFieldStatusText.Text =
            $"{FieldLabel(_activeField)} · {SelectionDescription(SelectionFor(_activeField))} · " +
            "흰색 후보 사각형을 클릭해 선택할 수 있습니다.";
    }

    private static string SelectionDescription(ScannerGroundTruthSelection selection) => selection.Mode switch
    {
        ScannerGroundTruthSelectionMode.Candidate => "후보 선택됨",
        ScannerGroundTruthSelectionMode.Manual => "직접 지정됨",
        ScannerGroundTruthSelectionMode.None => "없음",
        _ => selection.Bounds is null ? "현재 검출값 없음" : "현재 검출값",
    };

    private ScannerGroundTruthSelection SelectionFor(CandidateField field) => field switch
    {
        CandidateField.DetailWindow => _detailSelection,
        CandidateField.CloseButton => _closeSelection,
        CandidateField.Magnifier => _magnifierSelection,
        CandidateField.ItemName => _titleSelection,
        _ => throw new ArgumentOutOfRangeException(nameof(field)),
    };

    private Rect? CurrentBounds(CandidateField field) => field switch
    {
        CandidateField.DetailWindow => _frame.SelectedBounds,
        CandidateField.CloseButton => _frame.CloseBounds,
        CandidateField.Magnifier => _frame.MagnifierBounds,
        CandidateField.ItemName => _frame.TitleBounds,
        _ => null,
    };

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

    private static ScannerGroundTruthSelection CandidateSelection(
        CandidateField field,
        ScannerDiagnosticCandidateEvidence candidate,
        Rect? bounds)
    {
        var score = field == CandidateField.DetailWindow
            ? candidate.StructuralScore
            : candidate.TitleAnchorScore;
        var reason = field == CandidateField.DetailWindow
            ? candidate.StructuralReason
            : candidate.TitleAnchorReason;
        return new ScannerGroundTruthSelection(
            FieldName(field),
            ScannerGroundTruthSelectionMode.Candidate,
            bounds,
            candidate.Id,
            candidate.Rank,
            score,
            reason);
    }

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
        CandidateField.DetailWindow => "실제 상세보기 창 전체를 이미지에서 드래그해 주세요.",
        CandidateField.CloseButton => "실제 빨간 닫기 X 영역을 이미지에서 드래그해 주세요.",
        CandidateField.Magnifier => "실제 아이템명 왼쪽 돋보기 영역을 이미지에서 드래그해 주세요.",
        CandidateField.ItemName => "실제 아이템 이름 텍스트 영역을 이미지에서 드래그해 주세요.",
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
        CandidateField.ItemName => "아이템명 영역",
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

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

    private sealed record CandidateHit(ScannerDiagnosticCandidateEvidence Candidate, Rect Bounds);
}
