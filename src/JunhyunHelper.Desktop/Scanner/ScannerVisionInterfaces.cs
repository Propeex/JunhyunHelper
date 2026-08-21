using System.Windows.Media.Imaging;

namespace JunhyunHelper.Desktop.Scanner;

public interface IScannerInspectDetector
{
    bool IsAvailable { get; }
    string AvailabilityMessage { get; }
    Task<ScannerInspectCandidate?> ObserveAsync(CancellationToken cancellationToken);
}

public interface IScannerOcrEngine
{
    bool IsAvailable { get; }
    string AvailabilityMessage { get; }
    Task<string> ReadTextAsync(BitmapSource titleImage, CancellationToken cancellationToken);
}

/// <summary>
/// Safe Foundation implementation. No game capture/OCR loop can start until real
/// Tarkov window capture and current-client OCR are implemented and validated.
/// </summary>
public sealed class UnavailableScannerInspectDetector : IScannerInspectDetector
{
    public bool IsAvailable => false;
    public string AvailabilityMessage => "실제 Tarkov 창 캡처/상세창 탐지는 인게임 검증 후 연결됩니다.";
    public Task<ScannerInspectCandidate?> ObserveAsync(CancellationToken cancellationToken) =>
        Task.FromResult<ScannerInspectCandidate?>(null);
}

public sealed class UnavailableScannerOcrEngine : IScannerOcrEngine
{
    public bool IsAvailable => false;
    public string AvailabilityMessage => "현재 한국어 Tarkov OCR 런타임은 인게임 검증 단계에서 연결됩니다.";
    public Task<string> ReadTextAsync(BitmapSource titleImage, CancellationToken cancellationToken) =>
        Task.FromResult(string.Empty);
}
