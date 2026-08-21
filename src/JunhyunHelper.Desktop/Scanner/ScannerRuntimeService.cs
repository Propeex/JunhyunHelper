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
    private string _lastGeometrySignature = string.Empty;
    private int _stableGeometryHits;
    private string _lastTitleSignature = string.Empty;
    private int _stableTitleHits;
    private int _consecutiveMisses;
    private string _lastSuccessfulTitleSignature = string.Empty;
    private string _lastFailedTitleSignature = string.Empty;
    private DateTimeOffset _lastFailedAtUtc = DateTimeOffset.MinValue;
    private ScannerItemSnapshot? _currentSnapshot;

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
            "Mini Scanner가 꺼져 있습니다.");
    }

    public event Action<ScannerRuntimeStatus>? StatusChanged;

    public ScannerRuntimeStatus Status { get; private set; }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!_settings.Current.Enabled)
        {
            Stop();
            return Task.CompletedTask;
        }

        var context = _contextProvider();
        if (context is null)
        {
            StopLoop();
            ResetObservationState(hideOverlay: true);
            Publish(ScannerRuntimeState.NoProfile, "Scanner를 사용할 활성 프로필이 없습니다.");
            return Task.CompletedTask;
        }

        if (_catalog.LoadedMode != context.GameMode || !_catalog.HasHealthyCatalog)
        {
            StopLoop();
            ResetObservationState(hideOverlay: true);
            Publish(ScannerRuntimeState.CatalogUnavailable, "현재 게임 모드의 전체 아이템 카탈로그가 준비되지 않았습니다.");
            return Task.CompletedTask;
        }

        if (!_detector.IsAvailable || !_ocr.IsAvailable)
        {
            StopLoop();
            ResetObservationState(hideOverlay: true);
            var message = !_detector.IsAvailable
                ? _detector.AvailabilityMessage
                : _ocr.AvailabilityMessage;
            Publish(ScannerRuntimeState.WaitingForVision, message);
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
            _loopTask = Task.Run(() => RunLoopAsync(token, epoch), CancellationToken.None);
        }

        return Task.CompletedTask;
    }

    public void Stop()
    {
        StopLoop();
        ResetObservationState(hideOverlay: true);
        Publish(ScannerRuntimeState.Disabled, "Mini Scanner가 꺼져 있습니다.");
    }

    public void Suspend(ScannerRuntimeState state, string message)
    {
        StopLoop();
        ResetObservationState(hideOverlay: true);
        Publish(state, message);
    }

    public void PauseForPositionEdit()
    {
        StopLoop();
        Publish(ScannerRuntimeState.Stabilizing, "Mini Scanner 위치를 편집하는 중입니다.");
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
            snapshot);
    }

    public async Task HidePreviewAsync(CancellationToken cancellationToken = default)
    {
        _overlay.Hide();
        _currentSnapshot = null;
        if (_settings.Current.Enabled)
            await StartAsync(cancellationToken);
        else
            Publish(ScannerRuntimeState.Disabled, "Mini Scanner가 꺼져 있습니다.");
    }

    public void PublishExternalState(ScannerRuntimeState state, string message) =>
        Publish(state, message);

    private async Task RunLoopAsync(CancellationToken cancellationToken, int epoch)
    {
        Publish(ScannerRuntimeState.WaitingForInspectWindow, "Tarkov 아이템 상세창을 기다리는 중입니다.");

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
                    ResetObservationState(hideOverlay: true);
                    Publish(ScannerRuntimeState.NoProfile, "Scanner를 사용할 활성 프로필이 없습니다.");
                    continue;
                }

                if (_catalog.LoadedMode != context.GameMode)
                {
                    ResetObservationState(hideOverlay: true);
                    await _catalog.LoadCacheAsync(context.GameMode, cancellationToken);
                    if (!_catalog.HasHealthyCatalog)
                    {
                        Publish(ScannerRuntimeState.CatalogUnavailable, "현재 게임 모드의 전체 아이템 카탈로그가 준비되지 않았습니다.");
                        continue;
                    }
                }

                var candidate = await _detector.ObserveAsync(cancellationToken);
                if (epoch != Volatile.Read(ref _loopEpoch))
                    return;
                if (candidate is null)
                {
                    HandleMiss();
                    continue;
                }

                _consecutiveMisses = 0;
                if (!string.Equals(candidate.GeometrySignature, _lastGeometrySignature, StringComparison.Ordinal))
                {
                    _lastGeometrySignature = candidate.GeometrySignature;
                    _stableGeometryHits = 1;
                    ResetTitleStability();
                    Publish(ScannerRuntimeState.Stabilizing, "상세창 위치를 확인하는 중입니다.");
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

                    if (!string.IsNullOrEmpty(_lastSuccessfulTitleSignature))
                    {
                        _overlay.Hide();
                        _currentSnapshot = null;
                        _lastSuccessfulTitleSignature = string.Empty;
                    }
                    Publish(ScannerRuntimeState.Stabilizing, "아이템 제목을 확인하는 중입니다.");
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

                Publish(ScannerRuntimeState.ReadingTitle, "아이템 이름을 읽는 중입니다.");
                var ocrText = await _ocr.ReadTextAsync(candidate.TitleImage, cancellationToken);
                if (epoch != Volatile.Read(ref _loopEpoch))
                    return;

                var recognition = _catalog.ResolveOcrText(ocrText);
                if (!recognition.Success || string.IsNullOrWhiteSpace(recognition.ItemId))
                {
                    RecordFailedTitle(titleSignature);
                    _overlay.Hide();
                    _currentSnapshot = null;
                    Publish(
                        ScannerRuntimeState.Uncertain,
                        $"확실하게 식별하지 못했습니다. ({recognition.Reason}, {recognition.Confidence:P0})");
                    continue;
                }

                var snapshot = _presentation.CreateSnapshot(recognition.ItemId);
                if (snapshot is null)
                {
                    RecordFailedTitle(titleSignature);
                    _overlay.Hide();
                    _currentSnapshot = null;
                    Publish(ScannerRuntimeState.Uncertain, "Item ID는 확정했지만 현재 표시 데이터를 만들 수 없습니다.");
                    continue;
                }

                _lastSuccessfulTitleSignature = titleSignature;
                _lastFailedTitleSignature = string.Empty;
                _currentSnapshot = snapshot;
                _overlay.Show(snapshot);
                Publish(ScannerRuntimeState.ShowingItem, snapshot.OfficialName, snapshot);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            App.WriteDiagnostic("Scanner runtime loop failed", exception);
            _overlay.Hide();
            _currentSnapshot = null;
            Publish(ScannerRuntimeState.Error, "Scanner 런타임 오류가 발생했습니다.");
        }
    }

    private void HandleMiss()
    {
        _consecutiveMisses++;
        _stableGeometryHits = 0;
        _lastGeometrySignature = string.Empty;
        ResetTitleStability();

        if (_consecutiveMisses < 2)
            return;

        _overlay.Hide();
        _currentSnapshot = null;
        _lastSuccessfulTitleSignature = string.Empty;
        Publish(ScannerRuntimeState.WaitingForInspectWindow, "Tarkov 아이템 상세창을 기다리는 중입니다.");
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

    private void Publish(
        ScannerRuntimeState state,
        string message,
        ScannerItemSnapshot? snapshot = null)
    {
        Status = new ScannerRuntimeStatus(
            state,
            message,
            snapshot?.ItemId,
            snapshot?.OfficialName,
            DateTimeOffset.Now);
        StatusChanged?.Invoke(Status);
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        StopLoop();
        _overlay.Hide();
        GC.SuppressFinalize(this);
    }
}
