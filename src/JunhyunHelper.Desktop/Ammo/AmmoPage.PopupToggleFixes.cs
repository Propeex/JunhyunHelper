using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace JunhyunHelper.Desktop.Ammo;

public partial class AmmoPage
{
    protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left &&
            FindAncestor<Button>(e.OriginalSource as DependencyObject) is { } button)
        {
            if (ReferenceEquals(button, FavoriteCaliberMenuButton) && FavoriteCaliberPopup.IsOpen)
            {
                FavoriteCaliberPopup.IsOpen = false;
                e.Handled = true;
                return;
            }

            if (ReferenceEquals(button, ColumnMenuButton) && ColumnMenuPopup.IsOpen)
            {
                ColumnMenuPopup.IsOpen = false;
                e.Handled = true;
                return;
            }
        }

        base.OnPreviewMouseDown(e);
    }

    private static T? FindAncestor<T>(DependencyObject? source)
        where T : DependencyObject
    {
        for (var current = source; current is not null; current = VisualTreeHelper.GetParent(current))
        {
            if (current is T match)
                return match;
        }

        return null;
    }
}
