using JunhyunHelper.Infrastructure.Scanner;

namespace JunhyunHelper.Desktop.Scanner;

public sealed partial class ScannerCoordinator
{
    /// <summary>
    /// Non-sensitive result of the most recent catalog load/refresh. The main data
    /// updater uses this to distinguish a fresh Scanner refresh from a safe fallback
    /// to an already healthy local catalog.
    /// </summary>
    public ScannerCatalogDiagnostics CatalogDiagnostics => _catalog.LastDiagnostics;
}
