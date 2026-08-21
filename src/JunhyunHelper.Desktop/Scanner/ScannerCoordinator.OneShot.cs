using System.Windows;

namespace JunhyunHelper.Desktop.Scanner;

public sealed partial class ScannerCoordinator
{
    private readonly SemaphoreSlim _oneShotCoordinatorGate = new(1, 1);
    private ScannerGlobalHotkeyService? _hotkeyService;
    private bool _hotkeySubscribed;

    public event Action<string>? HotkeyStatusChanged;

    public string OneShotHotkeyText => _settings.Current.OneShotHotkey;

    public string HotkeyStatusText => _hotkeyService?.StatusText ??
        (string.IsNullOrWhiteSpace(_settings.Current.OneShotHotkey)
            ? "1회 스캔 단축키 사용 안 함"
            : $"1회 스캔 단축키: {_settings.Current.OneShotHotkey}");

    public void AttachHotkeyHost(Window window)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(window);

        _hotkeyService ??= new ScannerGlobalHotkeyService();
        if (!_hotkeySubscribed)
        {
            _hotkeyService.RegistrationChanged += OnHotkeyRegistrationChanged;
            _hotkeySubscribed = true;
        }

        var gesture = ParseHotkey(_settings.Current.OneShotHotkey);
        _hotkeyService.Attach(window, gesture, () => TriggerOneShotAsync());
    }

    public void SetOneShotHotkey(ScannerHotkeyGesture? gesture)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var text = gesture?.ToString() ?? string.Empty;
        _settings.Update(settings => settings.OneShotHotkey = text);
        _hotkeyService?.UpdateGesture(gesture);
        HotkeyStatusChanged?.Invoke(HotkeyStatusText);
    }

    public async Task<bool> TriggerOneShotAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!await _oneShotCoordinatorGate.WaitAsync(0, cancellationToken))
            return false;

        try
        {
            var context = GetContext();
            if (context is null)
            {
                Runtime.PublishExternalState(ScannerRuntimeState.NoProfile, "1회 스캔을 사용할 활성 프로필이 없습니다.");
                return false;
            }

            SetObservedContext(context);

            // One-shot recognition is still a scan-time offline feature. Loading the
            // local Scanner cache is allowed; a network refresh is never started here.
            if (!await _catalog.EnsureLoadedAsync(context.GameMode, cancellationToken))
            {
                Runtime.PublishExternalState(
                    ScannerRuntimeState.CatalogUnavailable,
                    "1회 스캔 전에 현재 게임 모드의 아이템 목록을 최신화해 주세요.");
                return false;
            }

            var resumeMode = ActiveCaptureMode;
            try
            {
                if (resumeMode is not null)
                {
                    Runtime.PublishExternalState(
                        ScannerRuntimeState.Stabilizing,
                        "1회 고정밀 스캔을 위해 실시간 스캔을 잠시 멈추는 중입니다.");
                    await Runtime.PauseForOneShotAsync(cancellationToken);
                }

                var mode = resumeMode ?? ScannerCaptureMode.TarkovWindow;
                return await Runtime.ScanOnceAsync(mode, cancellationToken);
            }
            finally
            {
                // Preserve the user's continuous Scanner/Test mode. StartAsync still
                // honors the latest current settings, so a mode disabled meanwhile
                // remains disabled rather than being forcibly resurrected.
                if (resumeMode is not null && !_disposed)
                    await Runtime.StartAsync(resumeMode.Value, CancellationToken.None);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            App.WriteDiagnostic("Scanner one-shot scan failed", exception);
            Runtime.PublishExternalState(ScannerRuntimeState.Error, "1회 고정밀 스캔 중 오류가 발생했습니다.");
            return false;
        }
        finally
        {
            _oneShotCoordinatorGate.Release();
        }
    }

    private void OnHotkeyRegistrationChanged(string status) => HotkeyStatusChanged?.Invoke(status);

    private static ScannerHotkeyGesture? ParseHotkey(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : ScannerHotkeyGesture.TryParse(value, out var gesture)
                ? gesture
                : ScannerHotkeyGesture.Default;
}
