using System.Windows;

namespace JunhyunHelper.Desktop.Scanner;

public sealed partial class ScannerCoordinator
{
    private const int OneShotTarkovHotkeyId = 0x4A53;
    private const int OneShotTestHotkeyId = 0x4A54;
    private const int ScannerToggleHotkeyId = 0x4A55;

    private readonly SemaphoreSlim _oneShotCoordinatorGate = new(1, 1);
    // Keep the original field name for the in-game hotkey because ScannerCoordinator.Dispose
    // already owns its explicit disposal path. Its Disposed callback closes the two new
    // registrations as part of the same lifetime boundary.
    private ScannerGlobalHotkeyService? _hotkeyService;
    private ScannerGlobalHotkeyService? _testHotkeyService;
    private ScannerGlobalHotkeyService? _scannerToggleHotkeyService;
    private bool _hotkeySubscribed;
    private bool _extraHotkeysSubscribed;

    public event Action<string>? HotkeyStatusChanged;

    public string OneShotHotkeyText => OneShotTarkovHotkeyText;
    public string OneShotTarkovHotkeyText => _settings.Current.OneShotTarkovHotkey;
    public string OneShotTestHotkeyText => _settings.Current.OneShotTestHotkey;
    public string ScannerToggleHotkeyText => _settings.Current.ScannerToggleHotkey;

    public string HotkeyStatusText
    {
        get
        {
            var statuses = new List<string>(3);
            if (_hotkeyService is not null)
                statuses.Add(_hotkeyService.StatusText);
            if (_testHotkeyService is not null)
                statuses.Add(_testHotkeyService.StatusText);
            if (_scannerToggleHotkeyService is not null)
                statuses.Add(_scannerToggleHotkeyService.StatusText);
            return statuses.Count == 0
                ? "Scanner 단축키가 아직 초기화되지 않았습니다."
                : string.Join(" · ", statuses);
        }
    }

    public void AttachHotkeyHost(Window window)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(window);

        _hotkeyService ??= new ScannerGlobalHotkeyService(OneShotTarkovHotkeyId, "1회 인게임 스캔");
        _testHotkeyService ??= new ScannerGlobalHotkeyService(OneShotTestHotkeyId, "1회 테스트 스캔");
        _scannerToggleHotkeyService ??= new ScannerGlobalHotkeyService(ScannerToggleHotkeyId, "스캐너 ON/OFF");

        if (!_hotkeySubscribed)
        {
            _hotkeyService.RegistrationChanged += OnHotkeyRegistrationChanged;
            _hotkeyService.Disposed += OnPrimaryHotkeyDisposed;
            _hotkeySubscribed = true;
        }
        if (!_extraHotkeysSubscribed)
        {
            _testHotkeyService.RegistrationChanged += OnHotkeyRegistrationChanged;
            _scannerToggleHotkeyService.RegistrationChanged += OnHotkeyRegistrationChanged;
            _extraHotkeysSubscribed = true;
        }

        _hotkeyService.Attach(
            window,
            ParseHotkey(_settings.Current.OneShotTarkovHotkey),
            () => TriggerOneShotTarkovAsync());
        _testHotkeyService.Attach(
            window,
            ParseHotkey(_settings.Current.OneShotTestHotkey),
            () => TriggerOneShotTestAsync());
        _scannerToggleHotkeyService.Attach(
            window,
            ParseHotkey(_settings.Current.ScannerToggleHotkey),
            ToggleScannerFromHotkeyAsync);
    }

    public void SetOneShotHotkey(ScannerHotkeyGesture? gesture) => SetOneShotTarkovHotkey(gesture);

    public void SetOneShotTarkovHotkey(ScannerHotkeyGesture? gesture)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var text = gesture?.ToString() ?? string.Empty;
        _settings.Update(settings => settings.OneShotTarkovHotkey = text);
        _hotkeyService?.UpdateGesture(gesture);
        HotkeyStatusChanged?.Invoke(_hotkeyService?.StatusText ?? "1회 인게임 스캔 단축키 설정을 저장했습니다.");
    }

    public void SetOneShotTestHotkey(ScannerHotkeyGesture? gesture)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var text = gesture?.ToString() ?? string.Empty;
        _settings.Update(settings => settings.OneShotTestHotkey = text);
        _testHotkeyService?.UpdateGesture(gesture);
        HotkeyStatusChanged?.Invoke(_testHotkeyService?.StatusText ?? "1회 테스트 스캔 단축키 설정을 저장했습니다.");
    }

    public void SetScannerToggleHotkey(ScannerHotkeyGesture? gesture)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var text = gesture?.ToString() ?? string.Empty;
        _settings.Update(settings => settings.ScannerToggleHotkey = text);
        _scannerToggleHotkeyService?.UpdateGesture(gesture);
        HotkeyStatusChanged?.Invoke(_scannerToggleHotkeyService?.StatusText ?? "스캐너 ON/OFF 단축키 설정을 저장했습니다.");
    }

    public Task<bool> TriggerOneShotAsync(CancellationToken cancellationToken = default) =>
        TriggerOneShotTarkovAsync(cancellationToken);

    public Task<bool> TriggerOneShotTarkovAsync(CancellationToken cancellationToken = default) =>
        TriggerOneShotModeAsync(ScannerCaptureMode.TarkovWindow, cancellationToken);

    public Task<bool> TriggerOneShotTestAsync(CancellationToken cancellationToken = default) =>
        TriggerOneShotModeAsync(ScannerCaptureMode.DisplayTest, cancellationToken);

    private async Task<bool> TriggerOneShotModeAsync(
        ScannerCaptureMode requestedMode,
        CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!await _oneShotCoordinatorGate.WaitAsync(0, cancellationToken))
            return false;

        var label = requestedMode == ScannerCaptureMode.DisplayTest
            ? "1회 테스트 스캔"
            : "1회 인게임 스캔";
        try
        {
            var context = GetContext();
            if (context is null)
            {
                Runtime.PublishExternalState(ScannerRuntimeState.NoProfile, $"{label}을 사용할 활성 프로필이 없습니다.");
                return false;
            }

            SetObservedContext(context);

            // One-shot recognition is still a scan-time offline feature. Loading the
            // local Scanner cache is allowed; a network refresh is never started here.
            if (!await _catalog.EnsureLoadedAsync(context.GameMode, cancellationToken))
            {
                Runtime.PublishExternalState(
                    ScannerRuntimeState.CatalogUnavailable,
                    $"{label} 전에 현재 게임 모드의 아이템 목록을 최신화해 주세요.");
                return false;
            }

            var resumeMode = ActiveCaptureMode;
            try
            {
                if (resumeMode is not null)
                {
                    Runtime.PublishExternalState(
                        ScannerRuntimeState.Stabilizing,
                        $"{label}을 위해 실시간 스캔을 잠시 멈추는 중입니다.");
                    await Runtime.PauseForOneShotAsync(cancellationToken);
                }

                // Global-hotkey callbacks enter through the WPF window message pump.
                // Several Scanner APIs can complete their first awaits synchronously
                // (capture gate, detector Task.FromResult, local catalog), so invoking
                // ScanOnceAsync directly here can run capture/detection/OCR setup on the
                // UI dispatcher before an asynchronous boundary exists. Execute the scan
                // worker explicitly on the thread pool; Runtime status subscribers and
                // Mini Scanner window access already marshal to the dispatcher.
                ScannerPerformanceTrace.Mark(
                    "one-shot-worker-dispatch",
                    ("mode", requestedMode));
                return await Task.Run(async () =>
                {
                    ScannerPerformanceTrace.Mark(
                        "one-shot-worker-start",
                        ("mode", requestedMode));
                    return await Runtime.ScanOnceAsync(requestedMode, cancellationToken).ConfigureAwait(false);
                }, cancellationToken);
            }
            finally
            {
                if (resumeMode is not null &&
                    !_disposed &&
                    ActiveCaptureMode == resumeMode)
                {
                    await Runtime.StartAsync(resumeMode.Value, CancellationToken.None);
                }
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            App.WriteDiagnostic($"Scanner {label} failed", exception);
            Runtime.PublishExternalState(ScannerRuntimeState.Error, $"{label} 중 오류가 발생했습니다.");
            return false;
        }
        finally
        {
            _oneShotCoordinatorGate.Release();
        }
    }

    private async Task ToggleScannerFromHotkeyAsync()
    {
        await _oneShotCoordinatorGate.WaitAsync();
        try
        {
            if (_disposed)
                return;
            await SetEnabledAsync(!_settings.Current.Enabled);
        }
        catch (Exception exception)
        {
            App.WriteDiagnostic("Scanner ON/OFF hotkey failed", exception);
            Runtime.PublishExternalState(ScannerRuntimeState.Error, "스캐너 ON/OFF 단축키 처리 중 오류가 발생했습니다.");
        }
        finally
        {
            _oneShotCoordinatorGate.Release();
        }
    }

    private void OnHotkeyRegistrationChanged(string status) => HotkeyStatusChanged?.Invoke(status);

    private void OnPrimaryHotkeyDisposed()
    {
        if (_testHotkeyService is not null)
        {
            if (_extraHotkeysSubscribed)
                _testHotkeyService.RegistrationChanged -= OnHotkeyRegistrationChanged;
            _testHotkeyService.Dispose();
            _testHotkeyService = null;
        }
        if (_scannerToggleHotkeyService is not null)
        {
            if (_extraHotkeysSubscribed)
                _scannerToggleHotkeyService.RegistrationChanged -= OnHotkeyRegistrationChanged;
            _scannerToggleHotkeyService.Dispose();
            _scannerToggleHotkeyService = null;
        }
        _extraHotkeysSubscribed = false;
    }

    private static ScannerHotkeyGesture? ParseHotkey(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : ScannerHotkeyGesture.TryParse(value, out var gesture)
                ? gesture
                : null;

    internal static bool ShouldRestoreOneShotMode(
        ScannerCaptureMode? pausedMode,
        ScannerCaptureMode? currentMode) =>
        pausedMode is not null && currentMode == pausedMode;
}
