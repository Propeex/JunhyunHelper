using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace JunhyunHelper.Desktop.Controls;

internal static class SearchClearButtonInstaller
{
    private const string MarkerTag = "JunhyunHelper.SearchClearButton";

    public static void Install(TextBox searchBox)
    {
        ArgumentNullException.ThrowIfNull(searchBox);
        if (searchBox.Parent is not Grid parent)
            return;

        var row = Grid.GetRow(searchBox);
        var column = Grid.GetColumn(searchBox);
        if (parent.Children
            .OfType<Button>()
            .Any(button => Equals(button.Tag, MarkerTag)
                && Grid.GetRow(button) == row
                && Grid.GetColumn(button) == column))
        {
            return;
        }

        var padding = searchBox.Padding;
        searchBox.Padding = new Thickness(
            padding.Left,
            padding.Top,
            Math.Max(padding.Right, 42),
            padding.Bottom);

        var clearButton = new Button
        {
            Content = "×",
            Tag = MarkerTag,
            ToolTip = "검색어 지우기",
            Width = 36,
            Padding = new Thickness(0),
            Margin = new Thickness(0, searchBox.Margin.Top, searchBox.Margin.Right, searchBox.Margin.Bottom),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Stretch,
            IsTabStop = false,
        };
        clearButton.Click += (_, _) =>
        {
            searchBox.Clear();
            searchBox.Focus();
            Keyboard.Focus(searchBox);
        };

        Grid.SetRow(clearButton, row);
        Grid.SetColumn(clearButton, column);
        Grid.SetRowSpan(clearButton, Grid.GetRowSpan(searchBox));
        Grid.SetColumnSpan(clearButton, Grid.GetColumnSpan(searchBox));
        Panel.SetZIndex(clearButton, Panel.GetZIndex(searchBox) + 1);
        parent.Children.Add(clearButton);
    }
}
