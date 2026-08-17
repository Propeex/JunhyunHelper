using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace JunhyunHelper.Desktop.Map;

/// <summary>
/// Applies the JunhyunHelper visual policy to the current-Quest sidebar without
/// changing Quest data or navigation behavior.
/// </summary>
public sealed class LegacyMapQuestSidebarPolishBridge : IDisposable
{
    private static readonly Color QuestRowColor = Color.FromRgb(40, 40, 40);
    private const double CheckBoxLaneWidth = 28;
    private const double MarkerBadgeLaneWidth = 29;
    private const double QuestRowHeight = 62;

    private readonly LegacyMapQuestSidebarV2 _sidebar;
    private bool _applyQueued;
    private bool _applying;
    private bool _disposed;

    public LegacyMapQuestSidebarPolishBridge(LegacyMapQuestSidebarV2 sidebar)
    {
        _sidebar = sidebar ?? throw new ArgumentNullException(nameof(sidebar));
        _sidebar.Loaded += Sidebar_Loaded;
        _sidebar.LayoutUpdated += Sidebar_LayoutUpdated;
        QueueApply();
    }

    private void Sidebar_Loaded(object sender, RoutedEventArgs e) => QueueApply();

    private void Sidebar_LayoutUpdated(object? sender, EventArgs e) => QueueApply();

    private void QueueApply()
    {
        if (_disposed || _applyQueued || _applying)
            return;

        _applyQueued = true;
        _sidebar.Dispatcher.BeginInvoke(
            () =>
            {
                _applyQueued = false;
                Apply();
            },
            DispatcherPriority.ContextIdle);
    }

    private void Apply()
    {
        if (_disposed || _applying)
            return;

        _applying = true;
        try
        {
            foreach (var row in FindDescendants<Border>(_sidebar).Where(IsQuestRow))
                PolishRow(row);
        }
        finally
        {
            _applying = false;
        }
    }

    private static void PolishRow(Border row)
    {
        row.Height = QuestRowHeight;
        row.MinHeight = QuestRowHeight;
        row.MaxHeight = QuestRowHeight;
        row.HorizontalAlignment = HorizontalAlignment.Stretch;
        row.VerticalAlignment = VerticalAlignment.Top;
        row.Padding = new Thickness(10, 7, 10, 7);
        row.Margin = new Thickness(0, 0, 0, 7);

        if (row.Child is not Grid grid)
            return;

        grid.HorizontalAlignment = HorizontalAlignment.Stretch;
        grid.VerticalAlignment = VerticalAlignment.Center;
        EnsureThreeColumnLayout(grid);

        var markerToggle = grid.Children.OfType<CheckBox>().FirstOrDefault();
        if (markerToggle is not null)
        {
            Grid.SetColumn(markerToggle, 0);
            markerToggle.HorizontalAlignment = HorizontalAlignment.Left;
            markerToggle.VerticalAlignment = VerticalAlignment.Center;
            markerToggle.Margin = new Thickness(0);
        }

        var button = grid.Children.OfType<Button>().FirstOrDefault();
        if (button is null)
            return;

        Grid.SetColumn(button, 2);
        button.HorizontalAlignment = HorizontalAlignment.Stretch;
        button.HorizontalContentAlignment = HorizontalAlignment.Stretch;
        button.VerticalContentAlignment = VerticalAlignment.Center;
        button.Padding = new Thickness(0);
        button.Margin = new Thickness(0);

        if (button.Content is not StackPanel content)
            return;

        content.HorizontalAlignment = HorizontalAlignment.Stretch;
        content.VerticalAlignment = VerticalAlignment.Center;

        var titleLine = content.Children.OfType<StackPanel>()
            .FirstOrDefault(panel => panel.Orientation == Orientation.Horizontal);
        if (titleLine is not null)
        {
            MoveMarkerBadgeIntoFixedLane(grid, titleLine);
            titleLine.Orientation = Orientation.Vertical;
            titleLine.HorizontalAlignment = HorizontalAlignment.Stretch;
            titleLine.VerticalAlignment = VerticalAlignment.Center;
        }

        foreach (var text in FindDescendants<TextBlock>(content))
        {
            text.HorizontalAlignment = HorizontalAlignment.Stretch;
            text.TextAlignment = TextAlignment.Left;
            text.TextWrapping = TextWrapping.NoWrap;
            text.TextTrimming = TextTrimming.CharacterEllipsis;
        }
    }

    private static void EnsureThreeColumnLayout(Grid grid)
    {
        if (grid.ColumnDefinitions.Count == 3 &&
            Math.Abs(grid.ColumnDefinitions[0].Width.Value - CheckBoxLaneWidth) < 0.01 &&
            Math.Abs(grid.ColumnDefinitions[1].Width.Value - MarkerBadgeLaneWidth) < 0.01 &&
            grid.ColumnDefinitions[2].Width.IsStar)
        {
            return;
        }

        grid.ColumnDefinitions.Clear();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(CheckBoxLaneWidth) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(MarkerBadgeLaneWidth) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
    }

    private static void MoveMarkerBadgeIntoFixedLane(Grid grid, StackPanel titleLine)
    {
        var leadingBorder = titleLine.Children.OfType<Border>().FirstOrDefault();
        if (leadingBorder is null)
            return;

        titleLine.Children.Remove(leadingBorder);
        if (leadingBorder.Child is not TextBlock codeText || string.IsNullOrWhiteSpace(codeText.Text))
            return;

        leadingBorder.HorizontalAlignment = HorizontalAlignment.Left;
        leadingBorder.VerticalAlignment = VerticalAlignment.Center;
        leadingBorder.Margin = new Thickness(0);
        leadingBorder.IsHitTestVisible = false;
        Grid.SetColumn(leadingBorder, 1);
        if (!grid.Children.Contains(leadingBorder))
            grid.Children.Add(leadingBorder);
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
