namespace JunhyunHelper.Desktop.Scanner;

/// <summary>
/// Production pacing override for the v1.7.4 Scanner throughput regression fix.
///
/// v1.7.3 reduced the continuous PeriodicTimer to 200 ms. On machines where a full
/// capture/detection cycle itself takes close to or longer than that budget,
/// PeriodicTimer immediately services the next pending tick and the Scanner can run
/// capture/detection almost back-to-back. That raises CPU/capture pressure and can
/// delay the expensive OCR/semantic work it was intended to reach sooner.
///
/// Restore the previously proven 350 ms observation cadence while retaining the
/// v1.7.3 adaptive semantic retry policy and direct OCR transport. Recognition
/// thresholds, candidate caps, and identity semantics are unchanged.
/// </summary>
public sealed partial class ScannerRuntimeService
{
    static ScannerRuntimeService()
    {
        TickInterval = TimeSpan.FromMilliseconds(350);
    }
}
