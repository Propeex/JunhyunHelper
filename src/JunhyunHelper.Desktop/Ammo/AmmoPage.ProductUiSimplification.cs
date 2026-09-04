namespace JunhyunHelper.Desktop.Ammo;

public partial class AmmoPage
{
    private void ApplyProductUiSimplification()
    {
        // A new application session always starts with the detail panel collapsed.
        _productDetailsExpanded = false;
        ApplyProductDetailExpansionState();
    }
}
