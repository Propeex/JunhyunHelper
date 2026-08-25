namespace JunhyunHelper.Core.Scanner;

/// <summary>
/// Computes a non-backlogging delay for the continuous Scanner observation loop.
/// The normal target interval is preserved when work finishes early. If the previous
/// cycle already consumed or exceeded that budget, a small cooperative yield is still
/// required so capture/detection cannot run continuously and starve OCR/UI work.
/// </summary>
public static class ScannerObservationPacingPolicy
{
    public static readonly TimeSpan DefaultMinimumOverrunYield = TimeSpan.FromMilliseconds(25);

    public static TimeSpan NextDelay(
        TimeSpan targetInterval,
        TimeSpan elapsedSincePreviousTick,
        TimeSpan? minimumOverrunYield = null)
    {
        if (targetInterval <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(targetInterval));
        if (elapsedSincePreviousTick < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(elapsedSincePreviousTick));

        var minimumYield = minimumOverrunYield ?? DefaultMinimumOverrunYield;
        if (minimumYield <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(minimumOverrunYield));

        var remaining = targetInterval - elapsedSincePreviousTick;
        return remaining > minimumYield ? remaining : minimumYield;
    }
}
