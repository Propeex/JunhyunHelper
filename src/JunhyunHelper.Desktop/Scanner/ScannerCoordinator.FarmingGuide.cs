namespace JunhyunHelper.Desktop.Scanner;

public sealed partial class ScannerCoordinator
{
    public ScannerItemSnapshot? CreateFarmingGuideSnapshot(string itemId)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (string.IsNullOrWhiteSpace(itemId))
            return null;
        return Presentation.CreateSnapshot(itemId.Trim());
    }

    /// <summary>
    /// Farming Guide may publish a short instruction/acceptance message into the existing
    /// Mini Scanner overlay without owning Scanner recognition state.
    /// </summary>
    public void ShowFarmingGuideStatus(string message)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (string.IsNullOrWhiteSpace(message))
            return;
        _overlay.ShowTransientStatus(message.Trim());
    }
}
