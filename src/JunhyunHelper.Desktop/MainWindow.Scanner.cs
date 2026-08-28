using JunhyunHelper.Desktop.Scanner;
using JunhyunHelper.Infrastructure.Storage;

namespace JunhyunHelper.Desktop;

public partial class MainWindow
{
    internal ScannerCoordinator ScannerCoordinator => _services.Scanner;

    internal ScannerItemUiStateStore ScannerItemUiState => _services.ScannerItemUiState;

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
