using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace JunhyunHelper.Desktop.Map;

/// <summary>
/// Adds JunhyunHelper-owned MiniMap display controls to the Main Map settings panel.
/// Hover/timed-hide still override base opacity to fully transparent, and marker scale
/// applies only to non-player MiniMap markers.
/// </summary>
public sealed class LegacyMiniMapOpacitySettingsBridge : IDisposable
{
    private readonly TarkovHelper.Pages.Map.MapPage _page;
    private readonly JunhyunMapProductSettingsStore _store = JunhyunMapProductSettingsStore.Instance;
    private Slider? _opacitySlider;
    private TextBlock? _opacityValueText;
    private Slider? _markerScaleSlider;
    private TextBlock? _markerScaleValueText;
    private bool _disposed;

    public LegacyMiniMapOpacitySettingsBridge(TarkovHelper.Pages.Map.MapPage page)
    {
        _page = page ?? throw new ArgumentNullException(nameof(page));
        Inject();
    }

    private void Inject()
    {
        if (_page.FindName("SettingsPanel") is not Border settingsPanel ||
            settingsPanel.Child is not ScrollViewer scrollViewer ||
            scrollViewer.Content is not StackPanel stack)
        {
            return;
        }

        stack.Children.Add(new Border
        {
            Height = 1,
            Background = Brush("BorderBrush", Brushes.DimGray),
            Margin = new Thickness(0, 10, 0, 12),
        });

        stack.Children.Add(new TextBlock
        {
            Text = "미니맵 표시",
            FontSize = 14,
            FontWeight = FontWeights.SemiBold,
            Foreground = Brush("TextPrimaryBrush", Brushes.White),
            Margin = new Thickness(0, 0, 0, 8),
        });

        AddOpacityRow(stack);
        AddMarkerScaleRow(stack);
    }

    private void AddOpacityRow(StackPanel stack)
    {
        var row = CreateRow("미니맵 투명도");
        var panel = new StackPanel { Orientation = Orientation.Horizontal };

        _opacitySlider = new Slider
        {
            Minimum = 10,
            Maximum = 100,
            TickFrequency = 5,
            IsSnapToTickEnabled = true,
            Width = 100,
            Value = Math.Round(_store.MiniMapOpacity * 100.0),
            VerticalAlignment = VerticalAlignment.Center,
        };
        _opacitySlider.ValueChanged += OpacitySlider_ValueChanged;
        panel.Children.Add(_opacitySlider);

        _opacityValueText = CreateValueText();
        panel.Children.Add(_opacityValueText);
        Grid.SetColumn(panel, 1);
        row.Children.Add(panel);
        stack.Children.Add(row);
        UpdateOpacityText();
    }

    private void AddMarkerScaleRow(StackPanel stack)
    {
        var row = CreateRow("미니맵 마커 크기");
        var panel = new StackPanel { Orientation = Orientation.Horizontal };

        _markerScaleSlider = new Slider
        {
            Minimum = 25,
            Maximum = 150,
            TickFrequency = 5,
            IsSnapToTickEnabled = true,
            Width = 100,
            Value = Math.Round(_store.MiniMapMarkerScale * 100.0),
            VerticalAlignment = VerticalAlignment.Center,
        };
        _markerScaleSlider.ValueChanged += MarkerScaleSlider_ValueChanged;
        panel.Children.Add(_markerScaleSlider);

        _markerScaleValueText = CreateValueText();
        panel.Children.Add(_markerScaleValueText);
        Grid.SetColumn(panel, 1);
        row.Children.Add(panel);
        stack.Children.Add(row);
        UpdateMarkerScaleText();
    }

    private Grid CreateRow(string label)
    {
        var row = new Grid { Margin = new Thickness(0, 3, 0, 8) };
        row.ColumnDefinitions.Add(new ColumnDefinition());
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.Children.Add(new TextBlock
        {
            Text = label,
            Foreground = Brush("TextSecondaryBrush", Brushes.LightGray),
            FontSize = 12,
            VerticalAlignment = VerticalAlignment.Center,
        });
        return row;
    }

    private TextBlock CreateValueText() => new()
    {
        Width = 42,
        Margin = new Thickness(8, 0, 0, 0),
        Foreground = Brush("TextPrimaryBrush", Brushes.White),
        VerticalAlignment = VerticalAlignment.Center,
    };

    private void OpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_disposed)
            return;

        _store.MiniMapOpacity = e.NewValue / 100.0;
        JunhyunMiniMapProductRegistry.ApplyBaseOpacity(_store.MiniMapOpacity);
        UpdateOpacityText();
    }

    private void MarkerScaleSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_disposed)
            return;

        _store.MiniMapMarkerScale = e.NewValue / 100.0;
        JunhyunMiniMapProductRegistry.ApplyMarkerScale(_store.MiniMapMarkerScale);
        UpdateMarkerScaleText();
    }

    private void UpdateOpacityText()
    {
        if (_opacityValueText is not null)
            _opacityValueText.Text = $"{_store.MiniMapOpacity * 100.0:0}%";
    }

    private void UpdateMarkerScaleText()
    {
        if (_markerScaleValueText is not null)
            _markerScaleValueText.Text = $"{_store.MiniMapMarkerScale * 100.0:0}%";
    }

    private Brush Brush(string key, Brush fallback) =>
        _page.TryFindResource(key) as Brush ?? fallback;

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;

        if (_opacitySlider is not null)
            _opacitySlider.ValueChanged -= OpacitySlider_ValueChanged;
        if (_markerScaleSlider is not null)
            _markerScaleSlider.ValueChanged -= MarkerScaleSlider_ValueChanged;
    }
}
