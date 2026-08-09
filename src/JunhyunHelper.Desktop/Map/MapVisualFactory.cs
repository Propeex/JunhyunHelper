using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using JunhyunHelper.Core.Maps;

namespace JunhyunHelper.Desktop.Map;

internal static class MapVisualFactory
{
    public static FrameworkElement CreateMarker(
        MapMarkerKind kind,
        string toolTip,
        string? iconPath = null,
        double size = 28) =>
        TryCreateIcon(iconPath, toolTip, size)
        ?? CreateBadge(SymbolFor(kind), BrushFor(kind), toolTip, size);

    public static FrameworkElement CreateQuestMarker(
        string toolTip,
        string? iconPath = null,
        double size = 30) =>
        TryCreateIcon(iconPath, toolTip, size)
        ?? CreateBadge("!", Brushes.Gold, toolTip, size);

    public static FrameworkElement CreateUserMarker(string color, string toolTip, double size = 28)
    {
        Brush brush;
        try { brush = (Brush)new BrushConverter().ConvertFromString(color)!; }
        catch { brush = Brushes.Gold; }
        return CreateBadge("●", brush, toolTip, size);
    }

    public static FrameworkElement CreatePlayerMarker(
        double headingDegrees,
        string? iconPath = null,
        double size = 32)
    {
        var icon = TryCreateIcon(iconPath, "현재 위치", size);
        if (icon is not null)
        {
            icon.IsHitTestVisible = false;
            icon.RenderTransformOrigin = new Point(0.5, 0.5);
            icon.RenderTransform = new RotateTransform(headingDegrees);
            return icon;
        }

        var grid = new Grid
        {
            Width = size,
            Height = size,
            IsHitTestVisible = false,
            RenderTransformOrigin = new Point(0.5, 0.5),
            RenderTransform = new RotateTransform(headingDegrees),
        };
        grid.Children.Add(new Ellipse
        {
            Fill = Brushes.DodgerBlue,
            Stroke = Brushes.White,
            StrokeThickness = 1.5,
            Opacity = 0.9,
        });
        grid.Children.Add(new Polygon
        {
            Points = new PointCollection
            {
                new(size * 0.5, size * 0.15),
                new(size * 0.72, size * 0.72),
                new(size * 0.5, size * 0.58),
                new(size * 0.28, size * 0.72),
            },
            Fill = Brushes.White,
        });
        return grid;
    }

    public static FrameworkElement CreateBadge(
        string symbol,
        Brush background,
        string toolTip,
        double size)
    {
        var border = new Border
        {
            Width = size,
            Height = size,
            CornerRadius = new CornerRadius(size / 2),
            Background = background,
            BorderBrush = Brushes.White,
            BorderThickness = new Thickness(1.25),
            ToolTip = toolTip,
            Cursor = System.Windows.Input.Cursors.Hand,
            Child = new TextBlock
            {
                Text = symbol,
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                FontSize = Math.Max(10, size * 0.45),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center,
            },
        };
        border.Effect = new System.Windows.Media.Effects.DropShadowEffect
        {
            BlurRadius = 3,
            ShadowDepth = 1,
            Opacity = 0.55,
        };
        return border;
    }

    private static FrameworkElement? TryCreateIcon(string? iconPath, string toolTip, double size)
    {
        if (string.IsNullOrWhiteSpace(iconPath) || !File.Exists(iconPath))
            return null;

        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = new Uri(iconPath, UriKind.Absolute);
            bitmap.EndInit();
            bitmap.Freeze();

            var grid = new Grid
            {
                Width = size,
                Height = size,
                ToolTip = toolTip,
                Cursor = System.Windows.Input.Cursors.Hand,
            };
            grid.Children.Add(new Image
            {
                Source = bitmap,
                Stretch = Stretch.Uniform,
                SnapsToDevicePixels = true,
                IsHitTestVisible = false,
            });
            grid.Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                BlurRadius = 3,
                ShadowDepth = 1,
                Opacity = 0.55,
            };
            return grid;
        }
        catch
        {
            return null;
        }
    }

    private static string SymbolFor(MapMarkerKind kind) => kind switch
    {
        MapMarkerKind.PmcExtract => "↗",
        MapMarkerKind.ScavExtract => "↘",
        MapMarkerKind.SharedExtract => "⇄",
        MapMarkerKind.Transit => "⇥",
        MapMarkerKind.PmcSpawn => "P",
        MapMarkerKind.ScavSpawn => "S",
        MapMarkerKind.SniperScav => "⌾",
        MapMarkerKind.Boss => "★",
        MapMarkerKind.SpecialAi => "◆",
        MapMarkerKind.Hazard => "!",
        MapMarkerKind.Lock => "▣",
        MapMarkerKind.Switch => "⏻",
        MapMarkerKind.StationaryWeapon => "✚",
        MapMarkerKind.BtrStop => "▰",
        MapMarkerKind.LootContainer => "□",
        MapMarkerKind.LooseLoot => "◇",
        _ => "•",
    };

    private static Brush BrushFor(MapMarkerKind kind) => kind switch
    {
        MapMarkerKind.PmcExtract => Brushes.SeaGreen,
        MapMarkerKind.ScavExtract => Brushes.OliveDrab,
        MapMarkerKind.SharedExtract => Brushes.MediumSeaGreen,
        MapMarkerKind.Transit => Brushes.Teal,
        MapMarkerKind.PmcSpawn => Brushes.SteelBlue,
        MapMarkerKind.ScavSpawn => Brushes.SlateGray,
        MapMarkerKind.SniperScav => Brushes.IndianRed,
        MapMarkerKind.Boss => Brushes.DarkRed,
        MapMarkerKind.SpecialAi => Brushes.Purple,
        MapMarkerKind.Hazard => Brushes.OrangeRed,
        MapMarkerKind.Lock => Brushes.SaddleBrown,
        MapMarkerKind.Switch => Brushes.DarkGoldenrod,
        MapMarkerKind.StationaryWeapon => Brushes.DimGray,
        MapMarkerKind.BtrStop => Brushes.DarkOliveGreen,
        MapMarkerKind.LootContainer => Brushes.DarkCyan,
        MapMarkerKind.LooseLoot => Brushes.CadetBlue,
        _ => Brushes.Gray,
    };
}
