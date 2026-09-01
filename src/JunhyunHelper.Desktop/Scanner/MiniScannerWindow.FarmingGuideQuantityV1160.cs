using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

namespace JunhyunHelper.Desktop.Scanner;

public partial class MiniScannerWindow
{
    private Border? _farmingGuideQuantityHost;
    private TextBox? _farmingGuideQuantityInput;
    private bool _farmingGuideQuantityActive;

    public event Action<int>? FarmingGuideQuantitySubmitted;

    public bool IsFarmingGuideQuantityActive => _farmingGuideQuantityActive;

    public void BeginFarmingGuideQuantityInput(ScannerDisplaySettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        _farmingGuideQuantityActive = true;
        _farmingGuideInstruction = null;
        FarmingGuideText.Visibility = Visibility.Collapsed;
        FarmingGuideText.Text = string.Empty;

        EnsureFarmingGuideQuantityHost(settings);
        var index = InfoStackPanel.Children.IndexOf(FarmingGuideText);
        if (InfoStackPanel.Children.Contains(_farmingGuideQuantityHost!))
            InfoStackPanel.Children.Remove(_farmingGuideQuantityHost!);
        InfoStackPanel.Children.Insert(index < 0 ? InfoStackPanel.Children.Count : index, _farmingGuideQuantityHost!);
        _farmingGuideQuantityHost!.Visibility = Visibility.Visible;

        // Mini Scanner normally never activates. Quantity entry is the deliberate narrow
        // exception: temporarily allow activation only while the user types the stack size.
        var handle = new WindowInteropHelper(this).Handle;
        if (handle != IntPtr.Zero)
        {
            var styles = GetWindowLongPtr(handle, GwlExStyle).ToInt64();
            styles &= ~WsExNoActivate;
            SetWindowLongPtr(handle, GwlExStyle, new IntPtr(styles));
        }

        ShowAndPosition(settings);
        Activate();
        _farmingGuideQuantityInput!.Focus();
        _farmingGuideQuantityInput.SelectAll();
    }

    public void CancelFarmingGuideQuantityInput(ScannerDisplaySettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (!_farmingGuideQuantityActive && _farmingGuideQuantityHost is null)
            return;

        _farmingGuideQuantityActive = false;
        if (_farmingGuideQuantityHost is not null)
            _farmingGuideQuantityHost.Visibility = Visibility.Collapsed;
        ApplyExtendedStyles();
        EnforceTopmost();
    }

    private void EnsureFarmingGuideQuantityHost(ScannerDisplaySettings settings)
    {
        if (_farmingGuideQuantityHost is not null)
        {
            if (_farmingGuideQuantityInput is not null)
                _farmingGuideQuantityInput.FontSize = settings.FontSize;
            return;
        }

        _farmingGuideQuantityInput = new TextBox
        {
            Width = 110,
            MinHeight = 28,
            FontSize = settings.FontSize,
            Text = "1",
            VerticalContentAlignment = VerticalAlignment.Center,
            HorizontalContentAlignment = HorizontalAlignment.Right,
            Padding = new Thickness(7, 3, 7, 3),
        };
        _farmingGuideQuantityInput.PreviewTextInput += (_, e) =>
        {
            e.Handled = e.Text.Any(ch => !char.IsDigit(ch));
        };
        DataObject.AddPastingHandler(_farmingGuideQuantityInput, (_, e) =>
        {
            if (!e.DataObject.GetDataPresent(DataFormats.Text) ||
                e.DataObject.GetData(DataFormats.Text) is not string text ||
                text.Any(ch => !char.IsDigit(ch)))
            {
                e.CancelCommand();
            }
        });
        _farmingGuideQuantityInput.KeyDown += (_, e) =>
        {
            if (e.Key != Key.Enter)
                return;
            if (!int.TryParse(
                    _farmingGuideQuantityInput.Text,
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out var quantity) || quantity <= 0)
            {
                _farmingGuideQuantityInput.SelectAll();
                e.Handled = true;
                return;
            }

            _farmingGuideQuantityActive = false;
            _farmingGuideQuantityHost!.Visibility = Visibility.Collapsed;
            ApplyExtendedStyles();
            EnforceTopmost();
            FarmingGuideQuantitySubmitted?.Invoke(quantity);
            e.Handled = true;
        };

        var row = new StackPanel { Orientation = Orientation.Horizontal };
        row.Children.Add(new TextBlock
        {
            Text = "개수",
            Foreground = (Brush)FindResource("AccentBrush"),
            FontWeight = FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0),
        });
        row.Children.Add(_farmingGuideQuantityInput);
        _farmingGuideQuantityHost = new Border
        {
            BorderBrush = (Brush)FindResource("AccentBrush"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(5),
            Padding = new Thickness(7, 5, 7, 5),
            Margin = new Thickness(0, 2, 0, 0),
            Background = (Brush)FindResource("BackgroundMediumBrush"),
            Child = row,
        };
    }
}
