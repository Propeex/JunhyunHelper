namespace JunhyunHelper.Core.Scanner;

/// <summary>
/// Protects the Scanner from repeatedly invoking an environment-dependent OCR backend
/// after a demonstrably slow empty result. A successful result is never suppressed and
/// a fast empty result is treated as an ordinary recognition miss.
/// </summary>
public sealed class ScannerOcrBackendHealthPolicy
{
    public static readonly TimeSpan DefaultSlowEmptyThreshold = TimeSpan.FromMilliseconds(800);
    public static readonly TimeSpan DefaultCooldown = TimeSpan.FromSeconds(30);

    private readonly TimeSpan _slowEmptyThreshold;
    private readonly TimeSpan _cooldown;
    private DateTimeOffset _degradedUntilUtc = DateTimeOffset.MinValue;

    public ScannerOcrBackendHealthPolicy(
        TimeSpan? slowEmptyThreshold = null,
        TimeSpan? cooldown = null)
    {
        _slowEmptyThreshold = slowEmptyThreshold ?? DefaultSlowEmptyThreshold;
        _cooldown = cooldown ?? DefaultCooldown;
        if (_slowEmptyThreshold <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(slowEmptyThreshold));
        if (_cooldown <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(cooldown));
    }

    public DateTimeOffset DegradedUntilUtc => _degradedUntilUtc;

    public bool ShouldAttempt(DateTimeOffset nowUtc) => nowUtc >= _degradedUntilUtc;

    /// <summary>
    /// Records one completed backend call. Returns true only when this call enters the
    /// degraded state. Slow successful OCR is preserved; only slow + empty degrades.
    /// </summary>
    public bool RecordResult(DateTimeOffset nowUtc, TimeSpan elapsed, bool hasUsableText)
    {
        if (elapsed < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(elapsed));

        if (hasUsableText)
        {
            _degradedUntilUtc = DateTimeOffset.MinValue;
            return false;
        }

        if (elapsed < _slowEmptyThreshold)
            return false;

        _degradedUntilUtc = nowUtc + _cooldown;
        return true;
    }
}
