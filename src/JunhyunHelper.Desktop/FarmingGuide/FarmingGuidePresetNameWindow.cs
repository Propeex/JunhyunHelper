using System.Windows;
using System.Windows.Controls;

namespace JunhyunHelper.Desktop.FarmingGuide;

public sealed class FarmingGuidePresetNameWindow : Window
{
    private readonly TextBox _nameBox;

    public FarmingGuidePresetNameWindow()
    {
        Title = "파밍 가이드 프리셋 저장";
        Width = 390;
        Height = 170;
        ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ShowInTaskbar = false;

        var root = new Grid { Margin = new Thickness(16) };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        root.Children.Add(new TextBlock
        {
            Text = "프리셋 이름",
            Margin = new Thickness(0, 0, 0, 7),
            FontWeight = FontWeights.SemiBold,
        });

        _nameBox = new TextBox { MinHeight = 34 };
        _nameBox.KeyDown += (_, e) =>
        {
            if (e.Key != System.Windows.Input.Key.Enter)
                return;
            Accept();
            e.Handled = true;
        };
        Grid.SetRow(_nameBox, 1);
        root.Children.Add(_nameBox);

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 12, 0, 0),
        };
        var cancel = new Button { Content = "취소", MinWidth = 80, Margin = new Thickness(0, 0, 8, 0) };
        cancel.Click += (_, _) => { DialogResult = false; };
        var save = new Button { Content = "저장", MinWidth = 80, IsDefault = true };
        save.Click += (_, _) => Accept();
        buttons.Children.Add(cancel);
        buttons.Children.Add(save);
        Grid.SetRow(buttons, 2);
        root.Children.Add(buttons);

        Content = root;
        Loaded += (_, _) => _nameBox.Focus();
    }

    public string? PresetName { get; private set; }

    private void Accept()
    {
        var value = _nameBox.Text.Trim();
        if (value.Length == 0)
            return;
        PresetName = value;
        DialogResult = true;
    }
}
