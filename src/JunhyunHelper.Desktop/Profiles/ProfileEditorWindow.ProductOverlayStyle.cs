using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace JunhyunHelper.Desktop.Profiles;

public partial class ProfileEditorWindow
{
    protected override void OnInitialized(EventArgs e)
    {
        base.OnInitialized(e);
        ApplyProductOverlayStyle();
    }

    private void ApplyProductOverlayStyle()
    {
        if (Content is not Grid root || root.Parent is not null)
            return;

        Content = null;
        root.Margin = new Thickness(0);
        Content = new Border
        {
            Margin = new Thickness(14),
            Padding = new Thickness(14),
            Background = TryFindResource("BackgroundMediumBrush") as Brush ?? Brushes.DimGray,
            BorderBrush = TryFindResource("BorderBrush") as Brush ?? Brushes.Gray,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Child = root,
        };
    }
}
