using JunhyunHelper.Infrastructure.Scanner;

namespace JunhyunHelper.Desktop.Scanner;

public sealed class ScannerRuntimeService : IDisposable
{
    private static readonly TimeSpan TickInterval = TimeSpan.FromMilliseconds(350);
    private static readonly TimeSpan FailedTitleCooldown = TimeSpan.FromSeconds(2.5);

    private readonly ScannerSettingsService _settings;
    private readonly ScannerCatalogService _catalog;
    private readonly ScannerItemPresentationService _presentation;
    private readonly MiniScannerOverlayService _overlay;
    private readonly IScannerInspectDetector _detector;
    private readonly IScannerOcrEngine _ocr;
    private readonly Func<ScannerDataContext?> _contextProvider;
    private readonly object _loopGate = new();

    private CancellationTokenSource? _loopCts;
    private Task? _loopTask;
    private int _loopEpoch;
    private bool _disposed;
    private ScannerCaptureMode? _activeMode;
    private string _lastGeometrySignature = string.Empty;
    private int _stableGeometryHits;
    private string _lastTitleSignature = string.Empty;
    private int _stableTitleHits;
    private int _consecutiveMisses;
    private string _lastSuccessfulTitleSignature = string.Empty;
    private string _lastFailedTitleSignature = string.Empty;
    private DateTimeOffset _lastFailedAtUtc = DateTimeOffset.MinValue;
    private ScannerItemSnapshot? _currentSnapshot;
    private string _lastDiagnosticStatusKey = string.Empty;

    public ScannerRuntimeService(
        ScannerSettingsService settings,
        ScannerCatalogService catalog,
        ScannerItemPresentationService presentation,
        MiniScannerOverlayService overlay,
        IScannerInspectDetector detector,
        IScannerOcrEngine ocr,
        Func<ScannerDataContext?> contextProvider)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        _presentation = presentation ?? throw new ArgumentNullException(nameof(presentation));
        _overlay = overlay ?? throw new ArgumentNullException(nameof(overlay));
        _detector = detector ?? throw new ArgumentNullException(nameof(detector));
        _ocr = ocr ?? throw new ArgumentNullException(nameof(ocr));
        _contextProvider = contextProvider ?? throw new ArgumentNullException(nameof(contextProvider));
        Status = new ScannerRuntimeStatus(
            ScannerRuntimeState.Disabled,
            "Scanner가 꺼져 있습니다.");
    }

    public event Action<ScannerRuntimeStatus>? StatusChanged;

    public ScannerRuntimeStatus Status { get; private set; }

    public ScannerCaptureMode? ActiveCaptureMode
    {
        get
        {
            lock (_loopGate)
                return _activeMode;
        }
    }

    public Task StartAsync(ScannerCaptureMode mode, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (mode == ScannerCaptureMode.TarkovWindow && !_settings.Current.Enabled)
        {
            Stop();
            return Task.CompletedTask;
        }

        if (ActiveCaptureMode != mode)
        {
            StopLoop();
            ResetObservationState(hideOverlay: false);
        }

        lock (_loopGate)
            _activeMode = mode;
        _detector.SetCaptureMode(mode);

        ScannerDiagnosticLog.Write(
            "runtime-start",
            mode,
            ("detectorAvailable", _detector.IsAvailable),
            ("ocrAvailable", _ocr.IsAvailable),
            ("catalogCount", _catalog.Count));

        var initialMessage = ModeInitialMessage(mode);
        _overlay.ShowStandby(initialMessage);

        var context = _contextProvider();
        if (context is null)
        {
            StopLoop();
            ResetObservationState(hideOverlay: false);
            const string message = "Scanner를 사용할 활성 프로필이 없습니다.";
            _overlay.ShowStandby(message);
            Publish(ScannerRuntimeState.NoProfile, message, captureMode: mode);
            return Task.CompletedTask;
        }

        if (_catalog.LoadedMode != context.GameMode || !_catalog.HasHealthyCatalog)
        {
            StopLoop();
            ResetObservationState(hideOverlay: false);
            const string message = "현재 게임 모드의 전체 아이템 카탈로그가 준비되지 않았습니다.";
            _overlay.ShowStandby(message);
            Publish(ScannerRuntimeState.CatalogUnavailable, message, captureMode: mode);
            return Task.CompletedTask;
        }

        if (!_detector.IsAvailable || !_ocr.IsAvailable)
        {
            StopLoop();
            ResetObservationState(hideOverlay: false);
            var message = !_detector.IsAvailable
                ? _detector.AvailabilityMessage
                : _ocr.AvailabilityMessage;
            _overlay.ShowStandby(message);
            Publish(ScannerRuntimeState.WaitingForVision, message, captureMode: mode);
            return Task.CompletedTask;
        }

        lock (_loopGate)
        {
            if (_loopTask is { IsCompleted: false })
                return Task.CompletedTask;

            _loopCts?.Dispose();
            _loopCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var token = _loopCts.Token;
            var epoch = Interlocked.Increment(ref _loopEpoch);
            _loopTask = Task.Run(() => RunLoopAsync(mode, token, epoch), CancellationToken.None);
        }

        return Task.CompletedTask;
    }

    public Task ResumeActiveAsync(CancellationToken cancellationToken = default)
    {
        var mode = ActiveCaptureMode;
        return mode is null
            ? Task.CompletedTask
            : StartAsync(mode.Value, cancellationToken);
    }

    public void Stop()
    {
        var stoppedMode = ActiveCaptureMode;
        StopLoop();
        lock (_loopGate)
            _activeMode = null;
        ResetObservationState(hideOverlay: true);
        ScannerDiagnosticLog.Write("runtime-stop", stoppedMode);
        Publish(ScannerRuntimeState.Disabled, "Scanner가 꺼져 있습니다.");
    }

    public void Suspend(ScannerRuntimeState state, string message)
    {
        StopLoop();
        ResetObservationState(hideOverlay: false);
        var mode = ActiveCaptureMode;
        if (mode is not null)
            _overlay.ShowStandby(message);
        else
            _overlay.Hide();
        Publish(state, message, captureMode: mode);
    }

    public void PauseForPositionEdit()
    {
        StopLoop();
        Publish(
            ScannerRuntimeState.Stabilizing,
            "Mini Scanner 위치를 편집하는 중입니다.",
            captureMode: ActiveCaptureMode);
    }

    public void ShowPreview(ScannerItemSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        StopLoop();
        ResetObservationState(hideOverlay: false);
        _currentSnapshot = snapshot;
        _overlay.Show(snapshot, preview: true);
        Publish(
            ScannerRuntimeState.ShowingItem,
            $"미리보기: {snapshot.OfficialName}",
            snapshot,
            ActiveCaptureMode);
    }

    public async Task HidePreviewAsync(CancellationToken cancellationToken = default)
    {
        _overlay.Hide();
        _currentSnapshot = null;
        var mode = ActiveCaptureMode;
        if (mode is not null)
            await StartAsync(mode.Value, cancellationToken);
        else
            Publish(ScannerRuntimeState.Disabled, "Scanner가 꺼져 있습니다.");
    }

    public void PublishExternalState(ScannerRuntimeState state, string message) =>
        Publish(state, message, captureMode: ActiveCaptureMode);

    private async Task RunLoopAsync(ScannerCaptureMode mode, CancellationToken cancellationToken, int epoch)
    {
        var waitingMessage = ModeInitialMessage(mode);
        _overlay.ShowStandby(waitingMessage);
        Publish(ScannerRuntimeState.WaitingForInspectWindow, waitingMessage, captureMode: mode);

        try
        {
            using var timer = new PeriodicTimer(TickInterval);
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                if (epoch != Volatile.Read(ref _loopEpoch))
                    return;

                var context = _contextProvider();
                if (context is null)
                {
                    ResetObservationState(hideOverlay: false);
                    const string message = "Scanner를 사용할 활성 프로필이 없습니다.";
                    _overlay.ShowStandby(message);
                    Publish(ScannerRuntimeState.NoProfile, message, captureMode: mode);
                    continue;
                }

                if (_catalog.LoadedMode != context.GameMode)
                {
                    ResetObservationState(hideOverlay: false);
                    await _catalog.LoadCacheAsync(context.GameMode, cancellationToken);
                    if (!_catalog.HasHealthyCatalog)
                    {
                        const string message = "현재 게임 모드의 전체 아이템 카탈로그가 준비되지 않았습니다.";
                        _overlay.ShowStandby(message);
                        Publish(ScannerRuntimeState.CatalogUnavailable, message, captureMode: mode);
                        continue;
                    }
                }

                if (!_detector.IsAvailable || !_ocr.IsAvailable)
                {
                    var message = !_detector.IsAvailable
                        ? _detector.AvailabilityMessage
                        : _ocr.AvailabilityMessage;
                    _overlay.ShowStandby(message);
                    Publish(ScannerRuntimeState.WaitingForVision, message, captureMode: mode);
                    continue;
                }

                var candidate = await _detector.ObserveAsync(cancellationToken);
                if (epoch != Volatile.Read(ref _loopEpoch))
                    return;
                if (candidate is null)
                {
                    HandleMiss(mode, _detector.StatusMessage);
                    continue;
                }

                _consecutiveMisses = 0;
                if (!string.Equals(candidate.GeometrySignature, _lastGeometrySignature, StringComparison.Ordinal))
                {
                    _lastGeometrySignature = candidate.GeometrySignature;
                    _stableGeometryHits = 1;
                    ResetTitleStability();
                    ScannerDiagnosticLog.Write(
                        "geometry-candidate",
                        mode,
                        ("x", candidate.Bounds.X),
                        ("y", candidate.Bounds.Y),
                        ("width", candidate.Bounds.Width),
                        ("height", candidate.Bounds.Height),
                        ("signature", candidate.GeometrySignature));
                    const string message = "상세창 위치를 확인하는 중입니다.";
                    _overlay.ShowStandby(message);
                    Publish(ScannerRuntimeState.Stabilizing, message, captureMode: mode);
                    continue;
                }

                _stableGeometryHits++;
                if (_stableGeometryHits < 2)
                    continue;

                var titleSignature = candidate.TitleSignature;
                if (string.IsNullOrWhiteSpace(titleSignature) || candidate.TitleImage is null)
                {
                    ResetTitleStability();
                    continue;
                }

                if (!string.Equals(titleSignature, _lastTitleSignature, StringComparison.Ordinal))
                {
                    _lastTitleSignature = titleSignature;
                    _stableTitleHits = 1;
                    _currentSnapshot = null;
                    _lastSuccessfulTitleSignature = string.Empty;
                    const string message = "아이템 제목을 확인하는 중입니다.";
                    _overlay.ShowStandby(message);
                    Publish(ScannerRuntimeState.Stabilizing, message, captureMode: mode);
                    continue;
                }

                _stableTitleHits++;
                if (_stableTitleHits < 2)
                    continue;

                if (string.Equals(titleSignature, _lastSuccessfulTitleSignature, StringComparison.Ordinal))
                {
                    if (_currentSnapshot is not null)
                        _overlay.Show(_currentSnapshot);
                    continue;
                }

                if (string.Equals(titleSignature, _lastFailedTitleSignature, StringComparison.Ordinal) &&
                    DateTimeOffset.UtcNow - _lastFailedAtUtc < FailedTitleCooldown)
                {
                    continue;
                }

                const string readingMessage = "아이템 이름을 읽는 중입니다.";
                _overlay.ShowStandby(readingMessage);
                Publish(ScannerRuntimeState.ReadingTitle, readingMessage, captureMode: mode);
                var ocrText = await _ocr.ReadTextAsync(candidate.TitleImage, cancellationToken);
                if (epoch != Volatile.Read(ref _loopEpoch))
                    return;

                ScannerDiagnosticLog.Write(
                    "ocr-result",
                    mode,
                    ("titleSignature", titleSignature),
                    ("text", ocrText));

                var recognition = _catalog.ResolveOcrText(ocrText);
                ScannerDiagnosticLog.Write(
                    "match-result",
                    mode,
                    ("success", recognition.Success),
                    ("reason", recognition.Reason),
                    ("itemId", recognition.ItemId),
                    ("officialName", recognition.OfficialName),
                    ("confidence", recognition.Confidence),
                    ("secondScore", recognition.SecondScore));

                if (!recognition.Success || string.IsNullOrWhiteSpace(recognition.ItemId))
                {
                    RecordFailedTitle(titleSignature);
                    _currentSnapshot = null;
                    var message = $"확실하게 식별하지 못했습니다. ({recognition.Reason}, {recognition.Confidence:P0})";
                    _overlay.ShowStandby(message);
                    Publish(ScannerRuntimeState.Uncertain, message, captureMode: mode);
                    continue;
                }

                var snapshot = _presentation.CreateSnapshot(recognition.ItemId);
                if (snapshot is null)
                {
                    RecordFailedTitle(titleSignature);
                    _currentSnapshot = null;
                    const string message = "Item ID는 확정했지만 현재 표시 데이터를 만들 수 없습니다.";
                    _overlay.ShowStandby(message);
                    Publish(ScannerRuntimeState.Uncertain, message, captureMode: mode);
                    continue;
                }

                _lastSuccessfulTitleSignature = titleSignature;
                _lastFailedTitleSignature = string.Empty;
                _currentSnapshot = snapshot;
                _overlay.Show(snapshot);
                Publish(ScannerRuntimeState.ShowingItem, snapshot.OfficialName, snapshot, mode);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            App.WriteDiagnostic("Scanner runtime loop failed", exception);
            ScannerDiagnosticLog.Write(
                "runtime-error",
                mode,
                ("type", exception.GetType().Name),
                ("message", exception.Message));
            _currentSnapshot = null;
            const string message = "Scanner 런타임 오류가 발생했습니다.";
            _overlay.ShowStandby(message);
            Publish(ScannerRuntimeState.Error, message, captureMode: mode);
        }
    }

    private void HandleMiss(ScannerCaptureMode mode, string detectorMessage)
    {
        _consecutiveMisses++;
        _stableGeometryHits = 0;
        _lastGeometrySignature = string.Empty;
        ResetTitleStability();

        if (_consecutiveMisses < 2)
            return;

        _currentSnapshot = null;
        _lastSuccessfulTitleSignature = string.Empty;
        var message = string.IsNullOrWhiteSpace(detectorMessage)
            ? ModeInitialMessage(mode)
            : detectorMessage;
        _overlay.ShowStandby(message);
        Publish(ScannerRuntimeState.WaitingForInspectWindow, message, captureMode: mode);
    }

    private void RecordFailedTitle(string titleSignature)
    {
        _lastFailedTitleSignature = titleSignature;
        _lastFailedAtUtc = DateTimeOffset.UtcNow;
    }

    private void ResetTitleStability()
    {
        _lastTitleSignature = string.Empty;
        _stableTitleHits = 0;
    }

    private void ResetObservationState(bool hideOverlay)
    {
        _lastGeometrySignature = string.Empty;
        _stableGeometryHits = 0;
        _consecutiveMisses = 0;
        ResetTitleStability();
        _lastSuccessfulTitleSignature = string.Empty;
        _lastFailedTitleSignature = string.Empty;
        _lastFailedAtUtc = DateTimeOffset.MinValue;
        _currentSnapshot = null;
        _lastDiagnosticStatusKey = string.Empty;
        if (hideOverlay)
            _overlay.Hide();
    }

    private void StopLoop()
    {
        Interlocked.Increment(ref _loopEpoch);

        CancellationTokenSource? cancellation;
        Task? task;
        lock (_loopGate)
        {
            cancellation = _loopCts;
            task = _loopTask;
            _loopCts = null;
            _loopTask = null;
        }

        if (cancellation is null)
            return;

        cancellation.Cancel();
        if (task is { IsCompleted: false })
        {
            _ = task.ContinueWith(
                _ => cancellation.Dispose(),
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
        }
        else
        {
            cancellation.Dispose();
        }
    }

    private static string ModeInitialMessage(ScannerCaptureMode mode) =>
        mode == ScannerCaptureMode.DisplayTest
            ? "테스트 모드 · 전체 디스플레이에서 상세창을 찾는 중입니다."
            : "Tarkov 게임 창을 찾는 중입니다. (Borderless 지원)";

    private void Publish(
        ScannerRuntimeState state,
        string message,
        ScannerItemSnapshot? snapshot = null,
        ScannerCaptureMode? captureMode = null)
    {
        Status = new ScannerRuntimeStatus(
            state,
            message,
            snapshot?.ItemId,
            snapshot?.OfficialName,
            DateTimeOffset.Now,
            captureMode);

        var diagnosticKey = $"{captureMode}:{state}:{message}:{snapshot?.ItemId}";
        if (!string.Equals(_lastDiagnosticStatusKey, diagnosticKey, StringComparison.Ordinal))
        {
            _lastDiagnosticStatusKey = diagnosticKey;
            ScannerDiagnosticLog.Write(
                "status",
                captureMode,
                ("state", state),
                ("message", message),
                ("itemId", snapshot?.ItemId),
                ("officialName", snapshot?.OfficialName));
        }

        StatusChanged?.Invoke(Status);
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        StopLoop();
        lock (_loopGate)
            _activeMode = null;
        _overlay.Hide();
        ScannerDiagnosticLog.Write("runtime-dispose");
        GC.SuppressFinalize(this);
    }
}
