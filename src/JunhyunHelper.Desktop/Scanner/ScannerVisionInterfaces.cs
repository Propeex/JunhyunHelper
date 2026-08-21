using System.Windows.Media.Imaging;

namespace JunhyunHelper.Desktop.Scanner;

public interface IScannerInspectDetector
{
    bool IsAvailable { get; }
    string AvailabilityMessage { get; }
    string StatusMessage { get; }
    ScannerCaptureMode CaptureMode { get; }
    void SetCaptureMode(ScannerCaptureMode mode);
    Task<IReadOnlyList<ScannerInspectCandidate>> ObserveAsync(CancellationToken cancellationToken);
}

public interface IScannerOcrEngine
{
    bool IsAvailable { get; }
    string AvailabilityMessage { get; }

    /// <summary>
    /// Scanner Lab 3.8 first pass: adaptive 4x/6x/8x title enlargement and one OCR pass.
    /// </summary>
    Task<string> ReadTextAsync(BitmapSource titleImage, CancellationToken cancellationToken);

    /// <summary>
    /// Scanner Lab 3.8 deep pass: enlarged original + contrast + binary + inverse variants.
    /// </summary>
    Task<string> ReadDeepTextAsync(BitmapSource titleImage, CancellationToken cancellationToken);
}

/// <summary>
/// Safe fallback used when a platform-specific vision implementation cannot be created.
/// It never starts game/process memory access and leaves the rest of the app usable.
/// </summary>
public sealed class UnavailableScannerInspectDetector : IScannerInspectDetector
{
    public bool IsAvailable => false;
    public string AvailabilityMessage => "Windows 화면 캡처/상세창 탐지 기능을 사용할 수 없습니다.";
    public string StatusMessage => AvailabilityMessage;
    public ScannerCaptureMode CaptureMode { get; private set; } = ScannerCaptureMode.TarkovWindow;

    public void SetCaptureMode(ScannerCaptureMode mode) => CaptureMode = mode;

    public Task<IReadOnlyList<ScannerInspectCandidate>> ObserveAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<ScannerInspectCandidate>>([]);
}

public sealed class UnavailableScannerOcrEngine : IScannerOcrEngine
{
    public bool IsAvailable => false;
    public string AvailabilityMessage => "한국어 OCR 런타임을 사용할 수 없습니다.";

    public Task<string> ReadTextAsync(BitmapSource titleImage, CancellationToken cancellationToken) =>
        Task.FromResult(string.Empty);

    public Task<string> ReadDeepTextAsync(BitmapSource titleImage, CancellationToken cancellationToken) =>
        Task.FromResult(string.Empty);
}
