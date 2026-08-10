using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace JunhyunHelper.Desktop.Map;

/// <summary>
/// Adds the JunhyunHelper-owned MiniMap base-opacity control to the Main Map
/// settings panel. Hover/timed-hide still override this value to fully transparent.
/// </summary>
public sealed class LegacyMiniMapOpacitySettingsBridge : IDisposable
{
    private readonly TarkovHelper.Pages.Map.MapPage _page;
    private readonly JunhyunMapProductSettingsStore _store = JunhyunMapProductSettingsStore.Instance;
    private Slider? _slider;
    private TextBlock? _valueText;
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

        var row = new Grid { Margin = new Thickness(0, 3, 0, 8) };
        row.ColumnDefinitions.Add(new ColumnDefinition());
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.Children.Add(new TextBlock
        {
            Text = "미니맵 투명도",
            Foreground = Brush("TextSecondaryBrush", Brushes.LightGray),
            FontSize = 12,
            VerticalAlignment = VerticalAlignment.Center,
        });

        var panel = new StackPanel { Orientation = Orientation.Horizontal };
        _slider = new Slider
        {
            Minimum = 10,
            Maximum = 100,
            TickFrequency = 5,
            IsSnapToTickEnabled = true,
            Width = 100,
            Value = Math.Round(_store.MiniMapOpacity * 100.0),
            VerticalAlignment = VerticalAlignment.Center,
        };
        _slider.ValueChanged += Slider_ValueChanged;
        panel.Children.Add(_slider);

        _valueText = new TextBlock
        {
            Width = 42,
            Margin = new Thickness(8, 0, 0, 0),
            Foreground = Brush("TextPrimaryBrush", Brushes.White),
            VerticalAlignment = VerticalAlignment.Center,
        };
        panel.Children.Add(_valueText);
        Grid.SetColumn(panel, 1);
        row.Children.Add(panel);
        stack.Children.Add(row);
        UpdateText();
    }

    private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_disposed)
            return;

        _store.MiniMapOpacity = e.NewValue / 100.0;
        JunhyunMiniMapProductRegistry.ApplyBaseOpacity(_store.MiniMapOpacity);
        UpdateText();
    }

    private void UpdateText()
    {
        if (_valueText is not null)
            _valueText.Text = $"{_store.MiniMapOpacity * 100.0:0}%";
    }

    private Brush Brush(string key, Brush fallback) =>
        _page.TryFindResource(key) as Brush ?? fallback;

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;

        if (_slider is not null)
            _slider.ValueChanged -= Slider_ValueChanged;
    }
}
