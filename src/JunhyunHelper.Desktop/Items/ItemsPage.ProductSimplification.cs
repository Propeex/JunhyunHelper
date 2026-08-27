using System.Windows;
using System.Windows.Controls;

namespace JunhyunHelper.Desktop.Items;

public partial class ItemsPage
{
    private static readonly bool ProductSimplificationHandlerRegistered = RegisterProductSimplificationHandler();
    private bool _productSimplificationApplied;

    private static bool RegisterProductSimplificationHandler()
    {
        EventManager.RegisterClassHandler(
            typeof(ItemsPage),
            FrameworkElement.LoadedEvent,
            new RoutedEventHandler(OnProductSimplificationLoaded));
        return true;
    }

    private static void OnProductSimplificationLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is ItemsPage page)
            page.ApplyProductSimplification();
    }

    private void ApplyProductSimplification()
    {
        if (_productSimplificationApplied)
            return;
        _productSimplificationApplied = true;

        // Needed Items is already the product purpose of this screen. The historical
        // Quest/Hideout usage subdivision added a second filter without adding useful
        // information, so keep its canonical value at All and remove the UI lane.
        if (UsageComboBox.ItemsSource is IEnumerable<UsageChoice> choices)
        {
            UsageComboBox.SelectedItem = choices.FirstOrDefault(choice => choice.Value == ItemUsageFilter.All);
        }
        else if (UsageComboBox.Items.Count > 0)
        {
            UsageComboBox.SelectedIndex = 0;
        }

        UsageComboBox.Visibility = Visibility.Collapsed;
        UsageComboBox.IsHitTestVisible = false;

        if (UsageComboBox.Parent is Grid header)
        {
            var column = Grid.GetColumn(UsageComboBox);
            if (column >= 0 && column < header.ColumnDefinitions.Count)
                header.ColumnDefinitions[column].Width = new GridLength(0);
        }
    }
}
