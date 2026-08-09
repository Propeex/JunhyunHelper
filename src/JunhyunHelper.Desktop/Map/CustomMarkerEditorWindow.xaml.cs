using System.Windows;
using System.Windows.Media;

namespace JunhyunHelper.Desktop.Map;

public partial class CustomMarkerEditorWindow : Window
{
    public CustomMarkerEditorWindow(string name, string color)
    {
        InitializeComponent();
        NameTextBox.Text = name;
        ColorTextBox.Text = color;
    }

    public string MarkerName { get; private set; } = string.Empty;
    public string MarkerColor { get; private set; } = "#FFD700";

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        var name = NameTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            MessageBox.Show(this, "마커 이름을 입력해주세요.", "사용자 마커", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var color = ColorTextBox.Text.Trim();
        try
        {
            _ = (Brush)new BrushConverter().ConvertFromString(color)!;
        }
        catch
        {
            MessageBox.Show(this, "색상은 #RRGGBB 형식으로 입력해주세요.", "사용자 마커", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        MarkerName = name;
        MarkerColor = color;
        DialogResult = true;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
