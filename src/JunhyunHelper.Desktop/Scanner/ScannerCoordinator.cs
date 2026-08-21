using System.Net.Http;
using System.Windows.Threading;
using JunhyunHelper.Core.Profiles;
using JunhyunHelper.Infrastructure.Scanner;

namespace JunhyunHelper.Desktop.Scanner;

/// <summary>
/// Desktop composition root for Scanner. Real Scanner and display-test mode use the
/// same detector/OCR/matcher pipeline and differ only in capture source.
/// </summary>
public sealed class ScannerCoordinator : IDisposable
{
    private static readonly TimeSpan ContextMonitorInterval = TimeSpan.FromMilliseconds(750);

    private readonly ScannerCatalogService _catalog;
    private readonly ScannerSettingsService _settings;
    private readonly MiniScannerOverlayService _overlay;
    private readonly ScannerLocalIconService _icons;
    private readonly IScannerInspectDetector _detector;
    private readonly IScannerOcrEngine _ocr;
    private readonly Dispatcher _dispatcher;
    private readonly object _monitorGate = new();

    private Func<ScannerDataContext?> _contextProvider = static () => null;
    private ScannerItemPresentationService? _presentation;
    private ScannerRuntimeService? _runtime;
    private CancellationTokenSource? _contextMonitorCts;
    private Task? _contextMonitorTask;
    private string? _observedContextKey;
    private volatile bool _testEnabled;
    private bool _initialized;
    private bool _disposed;

    public ScannerCoordinator(HttpClient httpClient, string rootDirectory)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentException.ThrowIfNullOrWhiteSpace(rootDirectory);

        _dispatcher = System.Windows.Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
        _settings = new ScannerSettingsService(rootDirectory);
        _catalog = new ScannerCatalogService(httpClient, rootDirectory);
        _icons = new ScannerLocalIconService(rootDirectory);

        try
        {
            _detector = new ScannerLab38InspectDetector();
        }
        catch (Exception exception)
        {
            App.WriteDiagnostic("Scanner Lab 3.8 capture initialization failed", exception);
            _detector = new UnavailableScannerInspectDetector();
        }

        IScannerOcrEngine rawOcr;
        try
        {
            rawOcr = new ScannerLab38OcrEngine();
        }
        catch (Exception exception)
        {
            App.WriteDiagnostic("Scanner Lab 3.8 OCR initialization failed", exception);
            rawOcr = new UnavailableScannerOcrEngine();
        }

        // Title OCR and inventory/stash OCR still share one serialized WinRT boundary.
        // Only the item-title runtime receives the conservative Tarkov-font recovery
        // decorator; inventory-context deep OCR remains the proven OCR-only path.
        var serializedOcr = new SerializedScannerOcrEngine(rawOcr);
        _ocr = new FontAwareScannerOcrEngine(serializedOcr, _catalog, rootDirectory);
        _overlay = new MiniScannerOverlayService(_settings, serializedOcr);
    }

    public event Action<ScannerRuntimeStatus>? StatusChanged;

    public ScannerDisplaySettings Settings => _settings.Current;

    public ScannerRuntimeStatus Status => Runtime.Status;

    public bool TestEnabled => _testEnabled;

    public ScannerCaptureMode? ActiveCaptureMode => _testEnabled
        ? ScannerCaptureMode.DisplayTest
        : _settings.Current.Enabled
            ? ScannerCaptureMode.TarkovWindow
            : null;

    public int CatalogCount => _catalog.Count;

    public GameMode? CatalogMode => _catalog.LoadedMode;

    public DateTimeOffset? CatalogGeneratedAtUtc => _catalog.GeneratedAtUtc;

    public void AttachContextProvider(Func<ScannerDataContext?> contextProvider)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _contextProvider = contextProvider ?? throw new ArgumentNullException(nameof(contextProvider));
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_initialized)
            return;
        _initialized = true;
        _testEnabled = false;

        var mode = ActiveCaptureMode;
        if (mode is null)
        {
            Runtime.PublishExternalState(ScannerRuntimeState.Disabled, "Scanner가 꺼져 있습니다.");
            return;
        }

        StartContextMonitor();
        await PrepareActiveRuntimeAsync(mode.Value, refreshCatalog: true, cancellationToken);
    }

    public async Task SetEnabledAsync(bool enabled, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (enabled)
        {
            _testEnabled = false;
            _settings.Update(settings => settings.Enabled = true);
            StartContextMonitor();
            await PrepareActiveRuntimeAsync(ScannerCaptureMode.TarkovWindow, refreshCatalog: true, cancellationToken);
            return;
        }

        _settings.Update(settings => settings.Enabled = false);
        if (_testEnabled)
        {
            StartContextMonitor();
            await PrepareActiveRuntimeAsync(ScannerCaptureMode.DisplayTest, refreshCatalog: false, cancellationToken);
            return;
        }

        StopContextMonitor();
        SetObservedContext(null);
        Runtime.Stop();
    }

    public async Task SetTestEnabledAsync(bool enabled, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _testEnabled = enabled;

        if (enabled)
        {
            if (_settings.Current.Enabled)
                _settings.Update(settings => settings.Enabled = false);
            StartContextMonitor();
            await PrepareActiveRuntimeAsync(ScannerCaptureMode.DisplayTest, refreshCatalog: true, cancellationToken);
            return;
        }

        var realEnabled = _settings.Current.Enabled;
        if (realEnabled)
        {
            StartContextMonitor();
            await PrepareActiveRuntimeAsync(ScannerCaptureMode.TarkovWindow, refreshCatalog: false, cancellationToken);
            return;
        }

        StopContextMonitor();
        SetObservedContext(null);
        Runtime.Stop();
    }

    public void UpdateDisplaySettings(Action<ScannerDisplaySettings> update)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _settings.Update(update);
    }

    public async Task<bool> SyncCatalogAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var context = GetContext();
        if (context is null)
        {
            Runtime.Suspend(ScannerRuntimeState.NoProfile, "카탈로그를 동기화할 활성 프로필이 없습니다.");
            return false;
        }

        SetObservedContext(context);
        Runtime.Suspend(ScannerRuntimeState.CatalogUnavailable, "전체 아이템 카탈로그를 동기화하는 중입니다.");
        var success = await _catalog.RefreshAsync(context.GameMode, cancellationToken);
        WriteCatalogDiagnostics(context.GameMode, success);
        if (!success)
        {
            Runtime.PublishExternalState(
                ScannerRuntimeState.CatalogUnavailable,
                "카탈로그 동기화에 실패했습니다. 기존 정상 캐시가 없으면 Scanner는 식별을 수행하지 않습니다.");
            return false;
        }

        var mode = ActiveCaptureMode;
        if (mode is not null)
            await Runtime.StartAsync(mode.Value, cancellationToken);
        else
            Runtime.PublishExternalState(ScannerRuntimeState.Disabled, "카탈로그 준비 완료 · Scanner는 꺼져 있습니다.");
        return true;
    }

    public async Task RefreshContextAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var context = GetContext();
        SetObservedContext(context);
        if (context is null)
        {
            Runtime.Suspend(ScannerRuntimeState.NoProfile, "Scanner를 사용할 활성 프로필이 없습니다.");
            return;
        }

        var mode = ActiveCaptureMode;
        if (mode is not null)
        {
            StartContextMonitor();
            Runtime.Suspend(ScannerRuntimeState.Stabilizing, "현재 프로필의 Scanner 데이터를 준비하는 중입니다.");
            await PrepareActiveRuntimeAsync(mode.Value, refreshCatalog: true, cancellationToken);
            return;
        }

        await _catalog.EnsureLoadedAsync(context.GameMode, cancellationToken);
        Runtime.PublishExternalState(ScannerRuntimeState.Disabled, "Scanner가 꺼져 있습니다.");
    }

    public async Task<ScannerItemSnapshot?> ShowPreviewAsync(
        string? itemId = null,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var context = GetContext();
        if (context is null)
        {
            Runtime.Suspend(ScannerRuntimeState.NoProfile, "미리보기를 만들 활성 프로필이 없습니다.");
            return null;
        }

        SetObservedContext(context);
        if (!await _catalog.EnsureLoadedAsync(context.GameMode, cancellationToken))
        {
            Runtime.PublishExternalState(ScannerRuntimeState.CatalogUnavailable, "미리보기 전에 전체 아이템 카탈로그를 동기화해 주세요.");
            return null;
        }

        var snapshot = string.IsNullOrWhiteSpace(itemId)
            ? Presentation.CreateDefaultPreviewSnapshot()
            : Presentation.CreateSnapshot(itemId.Trim());
        if (snapshot is null)
        {
            Runtime.PublishExternalState(ScannerRuntimeState.Uncertain, "해당 Item ID의 안전한 미리보기 데이터를 만들 수 없습니다.");
            return null;
        }

        Runtime.ShowPreview(snapshot);
        return snapshot;
    }

    public Task HidePreviewAsync(CancellationToken cancellationToken = default) =>
        Runtime.HidePreviewAsync(cancellationToken);

    public void PauseForPositionEdit() => Runtime.PauseForPositionEdit();
    public void BeginPositionEdit() => _overlay.BeginPositionEdit();
    public void EndPositionEdit() => _overlay.EndPositionEdit(keepVisible: false);

    public async Task ResumeAfterPositionEditAsync(CancellationToken cancellationToken = default)
    {
        var mode = ActiveCaptureMode;
        if (mode is not null)
            await Runtime.StartAsync(mode.Value, cancellationToken);
        else
            Runtime.PublishExternalState(ScannerRuntimeState.Disabled, "Scanner가 꺼져 있습니다.");
    }

    public void ResetPosition() => _overlay.ResetPosition();

    private async Task PrepareActiveRuntimeAsync(
        ScannerCaptureMode mode,
        bool refreshCatalog,
        CancellationToken cancellationToken)
    {
        var context = GetContext();
        SetObservedContext(context);
        if (context is null)
        {
            Runtime.Suspend(ScannerRuntimeState.NoProfile, "Scanner를 사용할 활성 프로필이 없습니다.");
            return;
        }

        var ready = refreshCatalog
            ? await _catalog.RefreshIfStaleAsync(context.GameMode, cancellationToken)
            : await _catalog.EnsureLoadedAsync(context.GameMode, cancellationToken);
        if (!ready)
        {
            Runtime.Suspend(
                ScannerRuntimeState.CatalogUnavailable,
                "현재 게임 모드의 전체 아이템 카탈로그가 준비되지 않았습니다.");
            return;
        }

        await Runtime.StartAsync(mode, cancellationToken);
    }

    private void WriteCatalogDiagnostics(GameMode gameMode, bool success)
    {
        var diagnostics = _catalog.LastDiagnostics;
        ScannerDiagnosticLog.Write(
            "catalog-sync",
            null,
            ("gameMode", gameMode.ToDataKey()),
            ("success", success),
            ("outcome", diagnostics.Outcome),
            ("items", diagnostics.ItemCount),
            ("traderPrices", diagnostics.TraderPriceCount),
            ("fleaPrices", diagnostics.FleaPriceCount),
            ("usedExistingCatalog", diagnostics.UsedExistingCatalog));
    }

    private void StartContextMonitor()
    {
        if (_disposed || ActiveCaptureMode is null)
            return;

        lock (_monitorGate)
        {
            if (_contextMonitorTask is { IsCompleted: false })
                return;

            _contextMonitorCts?.Dispose();
            _contextMonitorCts = new CancellationTokenSource();
            var token = _contextMonitorCts.Token;
            _contextMonitorTask = Task.Run(() => MonitorContextAsync(token), CancellationToken.None);
        }
    }

    private async Task MonitorContextAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var timer = new PeriodicTimer(ContextMonitorInterval);
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                if (_disposed)
                    return;

                var mode = ActiveCaptureMode;
                if (mode is null)
                    return;

                var context = GetContext();
                var key = ContextKey(context);
                string? previous;
                lock (_monitorGate)
                {
                    previous = _observedContextKey;
                    if (string.Equals(previous, key, StringComparison.Ordinal))
                        continue;
                    _observedContextKey = key;
                }

                if (context is null)
                {
                    Runtime.Suspend(ScannerRuntimeState.NoProfile, "Scanner를 사용할 활성 프로필이 없습니다.");
                    continue;
                }

                Runtime.Suspend(ScannerRuntimeState.Stabilizing, "프로필 변경을 반영하는 중입니다.");
                await PrepareActiveRuntimeAsync(mode.Value, refreshCatalog: true, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            if (!_disposed)
            {
                App.WriteDiagnostic("Scanner context monitor failed", exception);
                Runtime.Suspend(ScannerRuntimeState.Error, "Scanner 프로필 감시 중 오류가 발생했습니다.");
            }
        }
    }

    private void StopContextMonitor()
    {
        CancellationTokenSource? cancellation;
        Task? task;
        lock (_monitorGate)
        {
            cancellation = _contextMonitorCts;
            task = _contextMonitorTask;
            _contextMonitorCts = null;
            _contextMonitorTask = null;
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

    private void SetObservedContext(ScannerDataContext? context)
    {
        lock (_monitorGate)
            _observedContextKey = ContextKey(context);
    }

    private static string? ContextKey(ScannerDataContext? context) => context is null
        ? null
        : $"{context.GameMode.ToDataKey()}|{context.ItemsWorkspace.Profile.ProfileId}";

    private ScannerDataContext? GetContext()
    {
        if (_dispatcher.HasShutdownStarted || _dispatcher.HasShutdownFinished)
            return null;

        try
        {
            return _dispatcher.CheckAccess()
                ? _contextProvider()
                : _dispatcher.Invoke(_contextProvider);
        }
        catch (Exception exception) when (
            exception is TaskCanceledException or InvalidOperationException)
        {
            return null;
        }
    }

    private ScannerItemPresentationService Presentation =>
        _presentation ??= new ScannerItemPresentationService(_catalog, _icons, GetContext);

    private ScannerRuntimeService Runtime
    {
        get
        {
            if (_runtime is not null)
                return _runtime;

            _runtime = new ScannerRuntimeService(
                _settings,
                _catalog,
                Presentation,
                _overlay,
                _detector,
                _ocr,
                GetContext);
            _runtime.StatusChanged += OnRuntimeStatusChanged;
            return _runtime;
        }
    }

    private void OnRuntimeStatusChanged(ScannerRuntimeStatus status) => StatusChanged?.Invoke(status);

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _testEnabled = false;
        StopContextMonitor();

        if (_runtime is not null)
        {
            _runtime.StatusChanged -= OnRuntimeStatusChanged;
            _runtime.Dispose();
        }
        if (_ocr is IDisposable disposableOcr)
            disposableOcr.Dispose();
        _overlay.Dispose();
        _catalog.Dispose();
        GC.SuppressFinalize(this);
    }
}