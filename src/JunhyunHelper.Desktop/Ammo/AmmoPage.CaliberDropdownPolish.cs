using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;

namespace JunhyunHelper.Desktop.Ammo;

public partial class AmmoPage
{
    private static readonly bool ProductCaliberDropdownHandlerRegistered = RegisterProductCaliberDropdownHandler();
    private static readonly IValueConverter ProductCaliberIconVisibilityConverter = new CaliberIconVisibilityConverter();

    private bool _productCaliberDropdownApplied;
    private ComboBox? _productFavoriteCaliberComboBox;
    private DispatcherTimer? _productCaliberIconTimer;
    private IReadOnlyList<AmmoRow>? _productSubscribedRows;
    private Dictionary<string, AmmoRow[]> _productCaliberRows = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _productCaliberIconIndices = new(StringComparer.Ordinal);
    private bool _productSyncingFavoriteSelection;

    private static bool RegisterProductCaliberDropdownHandler()
    {
        EventManager.RegisterClassHandler(
            typeof(AmmoPage),
            FrameworkElement.LoadedEvent,
            new RoutedEventHandler(OnProductCaliberDropdownLoaded));
        return true;
    }

    private static void OnProductCaliberDropdownLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is not AmmoPage page || !ReferenceEquals(e.OriginalSource, page))
            return;

        page.Dispatcher.BeginInvoke(page.ApplyProductCaliberDropdownPolish, DispatcherPriority.Loaded);
    }

    private void ApplyProductCaliberDropdownPolish()
    {
        if (_productCaliberDropdownApplied)
            return;
        _productCaliberDropdownApplied = true;

        var template = CreateProductCaliberChoiceTemplate();
        CaliberComboBox.ItemTemplate = template;
        CaliberComboBox.DropDownOpened += ProductCaliberComboBox_DropDownOpened;
        CaliberComboBox.DropDownClosed += ProductCaliberComboBox_DropDownClosed;
        CaliberComboBox.SelectionChanged += ProductCaliberComboBox_SelectionChanged;

        if (FavoriteCaliberMenuButton.Parent is Grid toolbar)
        {
            FavoriteCaliberPopup.IsOpen = false;
            FavoriteCaliberMenuButton.Visibility = Visibility.Collapsed;
            FavoriteCaliberMenuButton.IsHitTestVisible = false;

            _productFavoriteCaliberComboBox = new ComboBox
            {
                Width = 170,
                MinHeight = 32,
                VerticalContentAlignment = VerticalAlignment.Center,
                ItemTemplate = template,
                MaxDropDownHeight = 460,
                ToolTip = "즐겨찾기 구경 선택",
            };
            Grid.SetColumn(_productFavoriteCaliberComboBox, Grid.GetColumn(FavoriteCaliberMenuButton));
            Grid.SetRow(_productFavoriteCaliberComboBox, Grid.GetRow(FavoriteCaliberMenuButton));
            _productFavoriteCaliberComboBox.SetBinding(
                IsEnabledProperty,
                new Binding(nameof(IsEnabled)) { Source = FavoriteCaliberMenuButton });
            _productFavoriteCaliberComboBox.SelectionChanged += ProductFavoriteCaliberComboBox_SelectionChanged;
            _productFavoriteCaliberComboBox.DropDownOpened += ProductCaliberComboBox_DropDownOpened;
            _productFavoriteCaliberComboBox.DropDownClosed += ProductCaliberComboBox_DropDownClosed;
            _productFavoriteCaliberComboBox.IsEnabledChanged += ProductFavoriteCaliberComboBox_IsEnabledChanged;
            toolbar.Children.Add(_productFavoriteCaliberComboBox);
        }

        FavoriteCaliberButton.Click += ProductFavoriteCaliberButton_Click;

        _productCaliberIconTimer = new DispatcherTimer(DispatcherPriority.Background, Dispatcher)
        {
            Interval = TimeSpan.FromMilliseconds(1400),
        };
        _productCaliberIconTimer.Tick += ProductCaliberIconTimer_Tick;

        EnsureProductCaliberRows();
        RefreshProductFavoriteChoices();
        SyncProductFavoriteSelection();
        ScheduleProductCaliberIconRefresh();
    }

    private static DataTemplate CreateProductCaliberChoiceTemplate()
    {
        var root = new FrameworkElementFactory(typeof(StackPanel));
        root.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);
        root.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);

        var iconHost = new FrameworkElementFactory(typeof(Border));
        iconHost.SetValue(FrameworkElement.WidthProperty, 30d);
        iconHost.SetValue(FrameworkElement.HeightProperty, 24d);
        iconHost.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 0, 7, 0));
        iconHost.SetBinding(
            UIElement.VisibilityProperty,
            new Binding(nameof(CaliberChoice.RawCaliber))
            {
                Converter = ProductCaliberIconVisibilityConverter,
            });

        var image = new FrameworkElementFactory(typeof(Image));
        image.SetValue(FrameworkElement.WidthProperty, 28d);
        image.SetValue(FrameworkElement.HeightProperty, 22d);
        image.SetValue(Image.StretchProperty, Stretch.Uniform);
        image.SetValue(UIElement.IsHitTestVisibleProperty, false);
        image.SetBinding(FrameworkElement.TagProperty, new Binding(nameof(CaliberChoice.RawCaliber)));
        iconHost.AppendChild(image);
        root.AppendChild(iconHost);

        var label = new FrameworkElementFactory(typeof(TextBlock));
        label.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        label.SetValue(TextBlock.TextTrimmingProperty, TextTrimming.CharacterEllipsis);
        label.SetBinding(TextBlock.TextProperty, new Binding(nameof(CaliberChoice.Label)));
        root.AppendChild(label);

        return new DataTemplate { VisualTree = root };
    }

    private void ProductCaliberComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        EnsureProductCaliberRows();
        RefreshProductFavoriteChoices();
        SyncProductFavoriteSelection();
        ScheduleProductCaliberIconRefresh();
    }

    private void ProductFavoriteCaliberComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_productSyncingFavoriteSelection ||
            _productFavoriteCaliberComboBox?.SelectedItem is not CaliberChoice { RawCaliber: { } caliber })
        {
            return;
        }

        var target = CaliberComboBox.Items
            .Cast<CaliberChoice>()
            .FirstOrDefault(choice => string.Equals(choice.RawCaliber, caliber, StringComparison.Ordinal));
        if (target is null)
            return;

        if (ReferenceEquals(CaliberComboBox.SelectedItem, target))
            ApplyFilter();
        else
            CaliberComboBox.SelectedItem = target;
    }

    private void ProductFavoriteCaliberButton_Click(object sender, RoutedEventArgs e)
    {
        RefreshProductFavoriteChoices();
        SyncProductFavoriteSelection();
        ScheduleProductCaliberIconRefresh();
    }

    private void RefreshProductFavoriteChoices()
    {
        if (_productFavoriteCaliberComboBox is null)
            return;

        var selectedCaliber = (CaliberComboBox.SelectedItem as CaliberChoice)?.RawCaliber;
        var placeholder = new CaliberChoice(null, "즐겨찾기 선택");
        var favorites = CaliberComboBox.Items
            .Cast<CaliberChoice>()
            .Where(choice => choice.RawCaliber is not null && _favoriteCalibers.Contains(choice.RawCaliber))
            .OrderBy(choice => choice.Label, StringComparer.CurrentCulture)
            .ToArray();

        _productSyncingFavoriteSelection = true;
        try
        {
            _productFavoriteCaliberComboBox.ItemsSource = new[] { placeholder }.Concat(favorites).ToArray();
            _productFavoriteCaliberComboBox.SelectedItem = favorites.FirstOrDefault(choice =>
                string.Equals(choice.RawCaliber, selectedCaliber, StringComparison.Ordinal)) ?? placeholder;
        }
        finally
        {
            _productSyncingFavoriteSelection = false;
        }
    }

    private void SyncProductFavoriteSelection()
    {
        if (_productFavoriteCaliberComboBox?.ItemsSource is not IEnumerable<CaliberChoice> choices)
            return;

        var selectedCaliber = (CaliberComboBox.SelectedItem as CaliberChoice)?.RawCaliber;
        var target = choices.FirstOrDefault(choice =>
                         selectedCaliber is not null &&
                         string.Equals(choice.RawCaliber, selectedCaliber, StringComparison.Ordinal))
                     ?? choices.FirstOrDefault(choice => choice.RawCaliber is null);
        if (target is null || ReferenceEquals(_productFavoriteCaliberComboBox.SelectedItem, target))
            return;

        _productSyncingFavoriteSelection = true;
        try
        {
            _productFavoriteCaliberComboBox.SelectedItem = target;
        }
        finally
        {
            _productSyncingFavoriteSelection = false;
        }
    }

    private void EnsureProductCaliberRows()
    {
        if (ReferenceEquals(_productSubscribedRows, _allRows))
            return;

        if (_productSubscribedRows is not null)
        {
            foreach (var row in _productSubscribedRows)
                row.PropertyChanged -= ProductAmmoRow_PropertyChanged;
        }

        _productSubscribedRows = _allRows;
        foreach (var row in _allRows)
            row.PropertyChanged += ProductAmmoRow_PropertyChanged;

        _productCaliberRows = _allRows
            .GroupBy(row => row.RawCaliber, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);
        _productCaliberIconIndices.Clear();
    }

    private void ProductAmmoRow_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (!string.Equals(e.PropertyName, nameof(AmmoRow.Icon), StringComparison.Ordinal))
            return;

        if (sender is AmmoRow row &&
            (!_productCaliberIconIndices.TryGetValue(row.RawCaliber, out var index) ||
             index < 0 ||
             !_productCaliberRows.TryGetValue(row.RawCaliber, out var rows) ||
             index >= rows.Length ||
             rows[index].Icon is null))
        {
            SelectFirstLoadedProductCaliberIcon(row.RawCaliber);
        }

        ScheduleProductCaliberIconRefresh();
    }

    private void ProductCaliberComboBox_DropDownOpened(object? sender, EventArgs e)
    {
        EnsureProductCaliberRows();
        AdvanceProductCaliberIcons();
        _productCaliberIconTimer?.Start();
        ScheduleProductCaliberIconRefresh();
    }

    private void ProductCaliberComboBox_DropDownClosed(object? sender, EventArgs e) =>
        StopProductCaliberIconTimerWhenInactive();

    private void ProductFavoriteCaliberComboBox_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is false && _productFavoriteCaliberComboBox is not null)
            _productFavoriteCaliberComboBox.IsDropDownOpen = false;
    }

    private void ProductCaliberIconTimer_Tick(object? sender, EventArgs e)
    {
        AdvanceProductCaliberIcons();
        RefreshProductCaliberIconVisuals();
    }

    private void StopProductCaliberIconTimerWhenInactive()
    {
        if (!CaliberComboBox.IsDropDownOpen && _productFavoriteCaliberComboBox?.IsDropDownOpen != true)
            _productCaliberIconTimer?.Stop();
    }

    private void AdvanceProductCaliberIcons()
    {
        foreach (var (caliber, rows) in _productCaliberRows)
        {
            var start = _productCaliberIconIndices.GetValueOrDefault(caliber, -1);
            var next = FindNextLoadedProductCaliberIcon(rows, start);
            if (next >= 0)
                _productCaliberIconIndices[caliber] = next;
        }
    }

    private static int FindNextLoadedProductCaliberIcon(IReadOnlyList<AmmoRow> rows, int current)
    {
        if (rows.Count == 0)
            return -1;

        for (var offset = 1; offset <= rows.Count; offset++)
        {
            var index = (current + offset + rows.Count) % rows.Count;
            if (rows[index].Icon is not null)
                return index;
        }

        return -1;
    }

    private void SelectFirstLoadedProductCaliberIcon(string caliber)
    {
        if (!_productCaliberRows.TryGetValue(caliber, out var rows))
            return;

        for (var index = 0; index < rows.Length; index++)
        {
            if (rows[index].Icon is null)
                continue;
            _productCaliberIconIndices[caliber] = index;
            return;
        }
    }

    private ImageSource? CurrentProductCaliberIcon(string caliber)
    {
        if (!_productCaliberRows.TryGetValue(caliber, out var rows) || rows.Length == 0)
            return null;

        if (!_productCaliberIconIndices.TryGetValue(caliber, out var index) ||
            index < 0 || index >= rows.Length || rows[index].Icon is null)
        {
            SelectFirstLoadedProductCaliberIcon(caliber);
            if (!_productCaliberIconIndices.TryGetValue(caliber, out index))
                return null;
        }

        return rows[index].Icon;
    }

    private void ScheduleProductCaliberIconRefresh() =>
        Dispatcher.BeginInvoke(RefreshProductCaliberIconVisuals, DispatcherPriority.Render);

    private void RefreshProductCaliberIconVisuals()
    {
        RefreshProductCaliberIconVisuals(CaliberComboBox);
        if (_productFavoriteCaliberComboBox is not null)
            RefreshProductCaliberIconVisuals(_productFavoriteCaliberComboBox);
    }

    private void RefreshProductCaliberIconVisuals(ComboBox comboBox)
    {
        comboBox.ApplyTemplate();
        foreach (var image in EnumerateProductCaliberImages(comboBox))
        {
            image.Source = image.Tag is string caliber
                ? CurrentProductCaliberIcon(caliber)
                : null;
        }

        if (comboBox.Template.FindName("PART_Popup", comboBox) is Popup { Child: DependencyObject popupChild })
        {
            foreach (var image in EnumerateProductCaliberImages(popupChild))
            {
                image.Source = image.Tag is string caliber
                    ? CurrentProductCaliberIcon(caliber)
                    : null;
            }
        }
    }

    private static IEnumerable<Image> EnumerateProductCaliberImages(DependencyObject root)
    {
        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var index = 0; index < count; index++)
        {
            var child = VisualTreeHelper.GetChild(root, index);
            if (child is Image image)
                yield return image;

            foreach (var descendant in EnumerateProductCaliberImages(child))
                yield return descendant;
        }
    }

    private sealed class CaliberIconVisibilityConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            value is string caliber && !string.IsNullOrWhiteSpace(caliber)
                ? Visibility.Visible
                : Visibility.Collapsed;

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            Binding.DoNothing;
    }
}