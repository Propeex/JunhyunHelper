using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace JunhyunHelper.Desktop.FarmingGuide;

public partial class FarmingGuidePage
{
    protected override void OnInitialized(EventArgs e)
    {
        base.OnInitialized(e);
        MouseDoubleClick += FarmingGuidePage_MouseDoubleClick;
        InitializeV1160UiHooks();
    }

    private void FarmingGuidePage_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        DependencyObject? current = InputHitTest(e.GetPosition(this)) as DependencyObject;
        while (current is not null && !ReferenceEquals(current, this))
        {
            if (current is FrameworkElement element)
            {
                switch (element.Tag)
                {
                    case PlacedItemSource placed:
                        EditPlacedItem(placed);
                        e.Handled = true;
                        return;
                    case EquipmentDropTarget equipment:
                        EditEquipmentTarget(equipment);
                        e.Handled = true;
                        return;
                    case CarrierDropTarget carrier:
                        EditCarrierTarget(carrier);
                        e.Handled = true;
                        return;
                }
            }
            current = VisualTreeHelper.GetParent(current);
        }
    }
}
