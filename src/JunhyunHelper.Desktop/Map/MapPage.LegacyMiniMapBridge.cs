using System.Windows.Media;

namespace JunhyunHelper.Desktop.Map;

public partial class MapPage
{
    /// <summary>
    /// Called from the always-on-top MiniMap hotkey runtime. The main Map page remains
    /// the authoritative floor selector, so both surfaces immediately render the same floor.
    /// </summary>
    internal void MoveFloorFromMiniMap(int delta)
    {
        if (delta == 0 || FloorComboBox.Items.Count <= 1)
            return;

        var current = FloorComboBox.SelectedIndex;
        if (current < 0)
            current = 0;
        var next = Math.Clamp(current + Math.Sign(delta), 0, FloorComboBox.Items.Count - 1);
        if (next != current)
            FloorComboBox.SelectedIndex = next;
    }

    internal void CenterMiniMapOnDetectedFloor()
    {
        if (_currentChoice is null || _playerPosition is null)
            return;

        var floor = MapCoordinateTransformer.FloorForPosition(_currentChoice.Layout, _playerPosition);
        if (floor is not null)
            FloorComboBox.SelectedItem = floor;
    }

    internal static MapPage? FindLiveMapPage()
    {
        var root = System.Windows.Application.Current?.MainWindow;
        return root is null ? null : FindVisualChild<MapPage>(root);
    }

    private static T? FindVisualChild<T>(System.Windows.DependencyObject root)
        where T : System.Windows.DependencyObject
    {
        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T match)
                return match;
            var nested = FindVisualChild<T>(child);
            if (nested is not null)
                return nested;
        }
        return null;
    }
}