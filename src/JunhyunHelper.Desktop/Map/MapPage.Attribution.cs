using System.Windows;
using System.Windows.Controls;

namespace JunhyunHelper.Desktop.Map;

public partial class MapPage
{
    private bool _attributionAdded;
    private TextBlock? _attributionText;

    protected override void OnInitialized(EventArgs e)
    {
        base.OnInitialized(e);
        Loaded += (_, _) =>
        {
            EnsureAttributionOverlay();
            EnsureSpatialFloorTracking();
            MapComboBox.SelectionChanged -= MapComboBox_AttributionSelectionChanged;
            MapComboBox.SelectionChanged += MapComboBox_AttributionSelectionChanged;
            UpdateAttributionText();
        };
        EnsureAttributionOverlay();
    }

    private void EnsureAttributionOverlay()
    {
        if (_attributionAdded || MarkerInfoPanel?.Parent is not Grid mapHost)
            return;

        _attributionText = new TextBlock
        {
            Text = "지도: tarkov-dev-svg-maps · CC BY-NC-SA 4.0",
            FontSize = 10,
            Foreground = TryFindResource("TextSecondaryBrush") as System.Windows.Media.Brush,
        };
        var attribution = new Border
        {
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(10),
            Padding = new Thickness(7, 3, 7, 3),
            Background = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromArgb(205, 22, 25, 29)),
            BorderBrush = TryFindResource("BorderBrush") as System.Windows.Media.Brush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(5),
            IsHitTestVisible = false,
            Child = _attributionText,
        };
        Panel.SetZIndex(attribution, 50);
        mapHost.Children.Add(attribution);
        _attributionAdded = true;
        UpdateAttributionText();
    }

    private void MapComboBox_AttributionSelectionChanged(object sender, SelectionChangedEventArgs e) =>
        Dispatcher.BeginInvoke(UpdateAttributionText);

    private void UpdateAttributionText()
    {
        if (_attributionText is null)
            return;
        var author = _currentChoice?.Layout.Attribution;
        _attributionText.Text = string.IsNullOrWhiteSpace(author)
            ? "지도: tarkov-dev-svg-maps · CC BY-NC-SA 4.0"
            : $"지도: {author} / tarkov-dev-svg-maps · CC BY-NC-SA 4.0";
    }
}
