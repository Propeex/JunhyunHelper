namespace JunhyunHelper.Desktop.Scanner;

public sealed partial class ScannerCoordinator
{
    public event Action<int>? FarmingGuideQuantitySubmitted
    {
        add => _overlay.FarmingGuideQuantitySubmitted += value;
        remove => _overlay.FarmingGuideQuantitySubmitted -= value;
    }

    public bool IsFarmingGuideQuantityPending => _overlay.IsFarmingGuideQuantityPending;

    public void RequestFarmingGuideQuantityInput() => _overlay.RequestFarmingGuideQuantityInput();

    public void CancelFarmingGuideQuantityInput() => _overlay.CancelFarmingGuideQuantityInput();
}
