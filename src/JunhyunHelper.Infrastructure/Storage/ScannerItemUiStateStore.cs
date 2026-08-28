namespace JunhyunHelper.Infrastructure.Storage;

public sealed record ScannerItemUiState(
    IReadOnlyList<string> FavoriteItemIds,
    IReadOnlyList<string> RecentItemIds);

/// <summary>
/// Persists Scanner presentation-only item identity/order. Item names, icons, prices,
/// requirements and relationships are intentionally resolved from the active catalog.
/// </summary>
public sealed class ScannerItemUiStateStore
{
    public const int MaximumRecentItems = 50;

    private readonly object _gate = new();
    private readonly AtomicJsonFileStore _store;
    private ScannerItemUiStateDocument _document;

    public ScannerItemUiStateStore(string rootDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootDirectory);
        _store = new AtomicJsonFileStore(Path.Combine(
            Path.GetFullPath(rootDirectory),
            "scanner-item-ui-state.json"));
        _document = Normalize(_store.LoadOrDefault(static () => new ScannerItemUiStateDocument()));
    }

    public string FilePath => _store.Path;

    public ScannerItemUiState Current
    {
        get
        {
            lock (_gate)
                return Snapshot(_document);
        }
    }

    public bool IsFavorite(string? itemId)
    {
        var normalized = NormalizeItemId(itemId);
        if (normalized is null)
            return false;

        lock (_gate)
            return _document.FavoriteItemIds.Contains(normalized, StringComparer.Ordinal);
    }

    public ScannerItemUiState ToggleFavorite(string itemId)
    {
        var normalized = RequireItemId(itemId);
        lock (_gate)
        {
            var next = Clone(_document);
            var removed = next.FavoriteItemIds.RemoveAll(value => string.Equals(value, normalized, StringComparison.Ordinal)) > 0;
            if (!removed)
                next.FavoriteItemIds.Insert(0, normalized);
            return Commit(next);
        }
    }

    public ScannerItemUiState RemoveFavorite(string itemId)
    {
        var normalized = RequireItemId(itemId);
        lock (_gate)
        {
            var next = Clone(_document);
            next.FavoriteItemIds.RemoveAll(value => string.Equals(value, normalized, StringComparison.Ordinal));
            return Commit(next);
        }
    }

    public ScannerItemUiState RecordRecent(string itemId)
    {
        var normalized = RequireItemId(itemId);
        lock (_gate)
        {
            var next = Clone(_document);
            next.RecentItemIds.RemoveAll(value => string.Equals(value, normalized, StringComparison.Ordinal));
            next.RecentItemIds.Insert(0, normalized);
            if (next.RecentItemIds.Count > MaximumRecentItems)
                next.RecentItemIds.RemoveRange(MaximumRecentItems, next.RecentItemIds.Count - MaximumRecentItems);
            return Commit(next);
        }
    }

    public ScannerItemUiState RemoveRecent(string itemId)
    {
        var normalized = RequireItemId(itemId);
        lock (_gate)
        {
            var next = Clone(_document);
            next.RecentItemIds.RemoveAll(value => string.Equals(value, normalized, StringComparison.Ordinal));
            return Commit(next);
        }
    }

    public ScannerItemUiState ClearRecents()
    {
        lock (_gate)
        {
            var next = Clone(_document);
            next.RecentItemIds.Clear();
            return Commit(next);
        }
    }

    private ScannerItemUiState Commit(ScannerItemUiStateDocument next)
    {
        next = Normalize(next);
        try
        {
            _store.Save(next);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // This is presentation-only state. Keep the in-memory interaction responsive;
            // the product diagnostic layer reports persistence failures at its composition boundary.
        }

        _document = next;
        return Snapshot(next);
    }

    private static ScannerItemUiState Snapshot(ScannerItemUiStateDocument document) => new(
        document.FavoriteItemIds.ToArray(),
        document.RecentItemIds.ToArray());

    private static ScannerItemUiStateDocument Clone(ScannerItemUiStateDocument document) => new()
    {
        FavoriteItemIds = document.FavoriteItemIds.ToList(),
        RecentItemIds = document.RecentItemIds.ToList(),
    };

    private static ScannerItemUiStateDocument Normalize(ScannerItemUiStateDocument document)
    {
        document.FavoriteItemIds = NormalizeOrdered(document.FavoriteItemIds, int.MaxValue);
        document.RecentItemIds = NormalizeOrdered(document.RecentItemIds, MaximumRecentItems);
        return document;
    }

    private static List<string> NormalizeOrdered(IEnumerable<string>? values, int maximum)
    {
        if (values is null)
            return [];

        var seen = new HashSet<string>(StringComparer.Ordinal);
        var result = new List<string>();
        foreach (var value in values)
        {
            var normalized = NormalizeItemId(value);
            if (normalized is null || !seen.Add(normalized))
                continue;
            result.Add(normalized);
            if (result.Count >= maximum)
                break;
        }
        return result;
    }

    private static string RequireItemId(string itemId) =>
        NormalizeItemId(itemId) ?? throw new ArgumentException("A canonical Item ID is required.", nameof(itemId));

    private static string? NormalizeItemId(string? itemId) =>
        string.IsNullOrWhiteSpace(itemId) ? null : itemId.Trim();

    private sealed class ScannerItemUiStateDocument
    {
        public List<string> FavoriteItemIds { get; set; } = [];
        public List<string> RecentItemIds { get; set; } = [];
    }
}
