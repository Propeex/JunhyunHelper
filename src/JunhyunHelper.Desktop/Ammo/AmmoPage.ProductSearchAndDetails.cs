using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace JunhyunHelper.Desktop.Ammo;

public partial class AmmoPage
{
    private TextBox? _productSearchBox;
    private Popup? _productSearchPopup;
    private ListBox? _productSearchResults;
    private bool _productDetailsPrepared;

    static AmmoPage()
    {
        EventManager.RegisterClassHandler(
            typeof(AmmoPage),
            LoadedEvent,
            new RoutedEventHandler(ProductLoaded));
    }

    private static void ProductLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is AmmoPage page)
            page.InitializeProductSearchAndDetails();
    }

    private void InitializeProductSearchAndDetails()
    {
        if (_productSearchBox is null && Content is Grid root)
        {
            var header = root.Children
                .OfType<Grid>()
                .FirstOrDefault(element => Grid.GetRow(element) == 0);
            if (header is not null)
                CreateProductSearch(header);
        }

        if (!_productDetailsPrepared)
        {
            _productDetailsPrepared = true;
            WrapDetailSections();
        }
    }

    private void CreateProductSearch(Grid header)
    {
        _productSearchBox = new TextBox
        {
            Width = 270,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(12, 0, 0, 0),
            ToolTip = "탄약 이름 또는 구경 검색",
        };
        _productSearchBox.TextChanged += ProductSearchBox_TextChanged;
        _productSearchBox.PreviewKeyDown += ProductSearchBox_PreviewKeyDown;
        Grid.SetColumn(_productSearchBox, Math.Max(0, header.ColumnDefinitions.Count - 1));
        header.Children.Add(_productSearchBox);

        _productSearchResults = new ListBox
        {
            MinWidth = 270,
            MaxHeight = 340,
            DisplayMemberPath = nameof(AmmoSearchHit.Label),
            Background = TryFindResource("BackgroundMediumBrush") as Brush ?? Brushes.Black,
            Foreground = TryFindResource("TextPrimaryBrush") as Brush ?? Brushes.White,
            BorderThickness = new Thickness(0),
        };
        _productSearchResults.PreviewMouseLeftButtonUp += ProductSearchResults_PreviewMouseLeftButtonUp;

        var border = new Border
        {
            MinWidth = 270,
            Background = TryFindResource("BackgroundMediumBrush") as Brush ?? Brushes.Black,
            BorderBrush = TryFindResource("BorderBrush") as Brush ?? Brushes.DimGray,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(6),
            Child = _productSearchResults,
        };

        _productSearchPopup = new Popup
        {
            PlacementTarget = _productSearchBox,
            Placement = PlacementMode.Bottom,
            StaysOpen = false,
            AllowsTransparency = true,
            Child = border,
        };
        header.Children.Add(_productSearchPopup);
    }

    private void ProductSearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_productSearchBox is null || _productSearchPopup is null || _productSearchResults is null)
            return;

        var query = _productSearchBox.Text.Trim();
        if (query.Length == 0)
        {
            _productSearchPopup.IsOpen = false;
            _productSearchResults.ItemsSource = null;
            return;
        }

        var hits = _allRows
            .Where(row =>
                row.Name.Contains(query, StringComparison.CurrentCultureIgnoreCase) ||
                row.CaliberLabel.Contains(query, StringComparison.CurrentCultureIgnoreCase) ||
                row.RawCaliber.Contains(query, StringComparison.OrdinalIgnoreCase))
            .Take(20)
            .Select(row => new AmmoSearchHit(row, $"{row.Name} · {row.CaliberLabel}"))
            .ToArray();

        _productSearchResults.ItemsSource = hits;
        _productSearchPopup.IsOpen = hits.Length > 0;
        if (hits.Length > 0)
            _productSearchResults.SelectedIndex = 0;
    }

    private void ProductSearchBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            if (_productSearchPopup is not null)
                _productSearchPopup.IsOpen = false;
            return;
        }

        if (e.Key == Key.Enter && _productSearchResults?.SelectedItem is AmmoSearchHit hit)
        {
            NavigateToSearchHit(hit.Row);
            e.Handled = true;
        }
    }

    private void ProductSearchResults_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_productSearchResults?.SelectedItem is AmmoSearchHit hit)
            NavigateToSearchHit(hit.Row);
    }

    private void NavigateToSearchHit(AmmoRow row)
    {
        if (CaliberComboBox.ItemsSource is IEnumerable<CaliberChoice> choices)
        {
            var caliber = choices.FirstOrDefault(choice =>
                string.Equals(choice.RawCaliber, row.RawCaliber, StringComparison.Ordinal));
            if (caliber is not null)
                CaliberComboBox.SelectedItem = caliber;
        }

        ApplyFilter();
        _selectedRow = row;
        AmmoGrid.SelectedItem = row;
        AmmoGrid.ScrollIntoView(row);
        ShowDetail(row);

        if (_productSearchPopup is not null)
            _productSearchPopup.IsOpen = false;
    }

    private void WrapDetailSections()
    {
        if (DetailGrid is null)
            return;

        var left = DetailGrid.Children
            .OfType<ScrollViewer>()
            .FirstOrDefault(viewer => Grid.GetColumn(viewer) == 0);
        var right = DetailGrid.Children
            .OfType<ScrollViewer>()
            .FirstOrDefault(viewer => Grid.GetColumn(viewer) == 2);

        if (left is not null)
            WrapDetailSection(left, "탄약 상세정보", 0);
        if (right is not null)
            WrapDetailSection(right, "수급 경로 상세정보", 2);
    }

    private void WrapDetailSection(ScrollViewer content, string header, int column)
    {
        DetailGrid.Children.Remove(content);
        var expander = new Expander
        {
            Header = header,
            IsExpanded = true,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            Foreground = TryFindResource("TextPrimaryBrush") as Brush ?? Brushes.White,
            Content = content,
        };
        Grid.SetColumn(expander, column);
        DetailGrid.Children.Add(expander);
    }

    private sealed record AmmoSearchHit(AmmoRow Row, string Label);
}
