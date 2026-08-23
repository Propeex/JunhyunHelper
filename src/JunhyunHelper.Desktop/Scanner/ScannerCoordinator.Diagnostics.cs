namespace JunhyunHelper.Desktop.Scanner;

public sealed partial class ScannerCoordinator
{
    /// <summary>
    /// Rebuilds the same presentation snapshot used by Mini Scanner for a user-reviewed
    /// diagnostic case. No network work is started here; only already-loaded local state
    /// is consulted.
    /// </summary>
    public ScannerItemSnapshot? CreateDiagnosticSnapshot(string? itemId)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return string.IsNullOrWhiteSpace(itemId)
            ? null
            : Presentation.CreateSnapshot(itemId.Trim());
    }
}