using JunhyunHelper.Desktop.Scanner;

namespace JunhyunHelper.Desktop;

public partial class MainWindow
{
    internal ScannerCoordinator ScannerCoordinator => _services.Scanner;

    internal ScannerDataContext? GetScannerDataContext()
    {
        if (_activeProfile is null || _activeContent is null || _activeItemsWorkspace is null)
            return null;

        return new ScannerDataContext(
            _activeProfile.GameMode,
            _activeContent,
            _activeItemsWorkspace);
    }
}
