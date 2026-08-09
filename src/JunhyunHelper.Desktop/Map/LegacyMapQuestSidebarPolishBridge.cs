using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace JunhyunHelper.Desktop.Map;

/// <summary>
/// Applies the JunhyunHelper visual policy to the current-Quest sidebar without
/// changing Quest data or navigation behavior.
/// </summary>
public sealed class LegacyMapQuestSidebarPolishBridge : IDisposable
{
    private static readonly Color QuestRowColor = Color.FromRgb(40, 40, 40);
    private readonly LegacyMapQuestSidebarV2 _sidebar;
    private bool _applying;
    private bool _disposed;

    public LegacyMapQuestSidebarPolishBridge(LegacyMapQuestSidebarV2 sidebar)
    {
        _sidebar = sidebar ?? throw new ArgumentNullException(nameof(sidebar));
        _sidebar.Loaded += Sidebar_Loaded;
        _sidebar.LayoutUpdated += Sidebar_LayoutUpdated;
        _sidebar.Dispatcher.BeginInvoke(Apply);
    }

    private void Sidebar_Loaded(object sender, RoutedEventArgs e) => Apply();

    private void Sidebar_LayoutUpdated(object? sender, EventArgs e)
    {
        if (!_applying)
            Apply();
    }

    private void Apply()
    {
        if (_disposed || _applying)
            return;

        _applying = true;
        try
        {
            foreach (var row in FindDescendants<Border>(_sidebar).Where(IsQuestRow))
            {
                row.HorizontalAlignment = HorizontalAlignment.Stretch;
                row.VerticalAlignment = VerticalAlignment.Top;
                row.Padding = new Thickness(10, 8, 10, 8);
                row.Margin = new Thickness(0, 0, 0, 7);

                if (row.Child is not Grid grid)
                    continue;

                grid.HorizontalAlignment = HorizontalAlignment.Stretch;
                grid.VerticalAlignment = VerticalAlignment.Center;

                // Reserve the same checkbox lane on every row. Quests without map
                // coordinates then line up with coordinate-bearing Quests instead of
                // shifting left and right like uneven books on a shelf.
                if (grid.ColumnDefinitions.Count >= 2)
                    grid.ColumnDefinitions[0].Width = new GridLength(28);

                foreach (var button in grid.Children.OfType<Button>())
                {
                    button.HorizontalAlignment = HorizontalAlignment.Stretch;
                    button.HorizontalContentAlignment = HorizontalAlignment.Left;
                    button.VerticalContentAlignment = VerticalAlignment.Center;

                    if (button.Content is not StackPanel content)
                        continue;

                    content.HorizontalAlignment = HorizontalAlignment.Left;
                    content.VerticalAlignment = VerticalAlignment.Center;

                    var titleLine = content.Children.OfType<StackPanel>()
                        .FirstOrDefault(panel => panel.Orientation == Orientation.Horizontal);
                    if (titleLine is not null)
                    {
                        titleLine.HorizontalAlignment = HorizontalAlignment.Left;
                        titleLine.VerticalAlignment = VerticalAlignment.Center;
                        EnsureMarkerCodeLane(titleLine);
                    }

                    foreach (var text in FindDescendants<TextBlock>(content))
                    {
                        text.HorizontalAlignment = HorizontalAlignment.Left;
                        text.TextAlignment = TextAlignment.Left;
                    }
                }
            }
        }
        finally
        {
            _applying = false;
        }
    }

    private static void EnsureMarkerCodeLane(StackPanel titleLine)
    {
        if (titleLine.Children.Count == 0 || titleLine.Children[0] is Border)
            return;

        titleLine.Children.Insert(0, new Border
        {
            Width = 22,
            Height = 22,
            Margin = new Thickness(0, 0, 7, 0),
            Background = Brushes.Transparent,
            IsHitTestVisible = false,
        });
    }

    private static bool IsQuestRow(Border border) =>
        border.Background is SolidColorBrush brush && brush.Color == QuestRowColor && border.Child is Grid;

    private static IEnumerable<T> FindDescendants<T>(DependencyObject root)
        where T : DependencyObject
    {
        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var index = 0; index < count; index++)
        {
            var child = VisualTreeHelper.GetChild(root, index);
            if (child is T match)
                yield return match;

            foreach (var descendant in FindDescendants<T>(child))
                yield return descendant;
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _sidebar.Loaded -= Sidebar_Loaded;
        _sidebar.LayoutUpdated -= Sidebar_LayoutUpdated;
    }
}
