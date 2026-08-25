namespace JunhyunHelper.Core.Scanner;

/// <summary>
/// Conservative retry pacing for repeated semantic/OCR failures on the same stable
/// detail-window geometry. A new candidate lineage is eligible immediately; persistent
/// failures back off to the previous 1200 ms ceiling to bound CPU usage.
/// </summary>
public static class ScannerSemanticRetryPolicy
{
    public static TimeSpan DelayAfterFailure(int consecutiveFailures) =>
        consecutiveFailures switch
        {
            <= 0 => TimeSpan.Zero,
            1 => TimeSpan.FromMilliseconds(250),
            2 => TimeSpan.FromMilliseconds(500),
            3 => TimeSpan.FromMilliseconds(800),
            _ => TimeSpan.FromMilliseconds(1200),
        };
}
