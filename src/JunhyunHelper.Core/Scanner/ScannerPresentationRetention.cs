namespace JunhyunHelper.Core.Scanner;

/// <summary>
/// Keeps the last confirmed Scanner item stable across short-lived recognition misses.
/// A confirmed item replaces the previous item immediately and resets the miss budget.
/// The held item is released only after the configured number of consecutive misses.
/// </summary>
public sealed class ScannerPresentationRetention
{
    public const int DefaultMissesToHide = 3;

    private readonly int _missesToHide;

    public ScannerPresentationRetention(int missesToHide = DefaultMissesToHide)
    {
        if (missesToHide < 1)
            throw new ArgumentOutOfRangeException(nameof(missesToHide));

        _missesToHide = missesToHide;
    }

    public string? ItemId { get; private set; }

    public int ConsecutiveMisses { get; private set; }

    public bool HasItem => !string.IsNullOrWhiteSpace(ItemId);

    /// <summary>
    /// Records a successful, authoritative item identification. The new item becomes the
    /// held presentation immediately, even when it differs from the previous item.
    /// </summary>
    public bool Confirm(string itemId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(itemId);

        var normalized = itemId.Trim();
        var changed = !string.Equals(ItemId, normalized, StringComparison.Ordinal);
        ItemId = normalized;
        ConsecutiveMisses = 0;
        return changed;
    }

    /// <summary>
    /// Records one completed recognition miss. Returns true exactly when the current
    /// presentation should be hidden because the consecutive-miss budget was exhausted.
    /// </summary>
    public bool ReportMiss()
    {
        if (!HasItem)
            return false;

        ConsecutiveMisses++;
        if (ConsecutiveMisses < _missesToHide)
            return false;

        Reset();
        return true;
    }

    public void Reset()
    {
        ItemId = null;
        ConsecutiveMisses = 0;
    }
}
