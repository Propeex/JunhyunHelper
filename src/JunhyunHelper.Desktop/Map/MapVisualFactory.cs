using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using JunhyunHelper.Core.Maps;

namespace JunhyunHelper.Desktop.Map;

internal static class MapVisualFactory
{
    private static readonly IReadOnlyDictionary<MapMarkerKind, string> MarkerIconFiles =
        new Dictionary<MapMarkerKind, string>
        {
            [MapMarkerKind.PmcExtract] = "extract_pmc.png",
            [MapMarkerKind.ScavExtract] = "extract_scav.png",
            [MapMarkerKind.SharedExtract] = "extract_shared.png",
            [MapMarkerKind.Transit] = "extract_transit.png",
            [MapMarkerKind.PmcSpawn] = "spawn_pmc.png",
            [MapMarkerKind.ScavSpawn] = "spawn_scav.png",
            [MapMarkerKind.SniperScav] = "spawn_sniper_scav.png",
            [MapMarkerKind.Boss] = "spawn_boss.png",
            [MapMarkerKind.SpecialAi] = "spawn_rogue.png",
            [MapMarkerKind.Hazard] = "hazard.png",
            [MapMarkerKind.Lock] = "lock.png",
            [MapMarkerKind.Switch] = "switch.png",
            [MapMarkerKind.StationaryWeapon] = "stationarygun.png",
            [MapMarkerKind.BtrStop] = "btr_stop.png",
            [MapMarkerKind.LootContainer] = "container_crate.png",
            [MapMarkerKind.LooseLoot] = "loose_loot.png",
        };

    private static string? _iconDirectory;

    public static void ConfigureIconDirectory(string iconDirectory) =>
        _iconDirectory = string.IsNullOrWhiteSpace(iconDirectory)
            ? null
            : Path.GetFullPath(iconDirectory);

    public static FrameworkElement CreateMarker(
        MapMarkerKind kind,
        string toolTip,
        double size = 28,
        string? iconPath = null) =>
        TryCreateIcon(iconPath ?? ResolveMarkerIcon(kind), toolTip, size)
        ?? CreateBadge(SymbolFor(kind), BrushFor(kind), toolTip, size);

    public static FrameworkElement CreateQuestMarker(
        string toolTip,
        double size = 30,
        string? iconPath = null) =>
        TryCreateIcon(iconPath ?? ResolveIcon("quest_objective.png"), toolTip, size)
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
        double size = 32,
        string? iconPath = null)
    {
        var icon = TryCreateIcon(iconPath ?? ResolveIcon("player-position.png"), "현재 위치", size);
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

    private static string? ResolveMarkerIcon(MapMarkerKind kind) =>
        MarkerIconFiles.TryGetValue(kind, out var fileName)
            ? ResolveIcon(fileName)
            : null;

    private static string? ResolveIcon(string fileName)
    {
        if (string.IsNullOrWhiteSpace(_iconDirectory))
            return null;
        var path = Path.Combine(_iconDirectory, fileName);
        return File.Exists(path) ? path : null;
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
