using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using TarkovHelper.Models.Map;

namespace JunhyunHelper.Desktop.Map;

public enum JunhyunFloorRelation
{
    Unknown,
    Current,
    Above,
    Below,
}

public enum JunhyunMissingFloorBehavior
{
    TreatAsMain,
    KeepUnknown,
}

public readonly record struct JunhyunFloorRelationInfo(
    JunhyunFloorRelation Relation,
    string Arrow,
    string FloorLabel)
{
    public bool IsOtherFloor => Relation is JunhyunFloorRelation.Above or JunhyunFloorRelation.Below;
}

/// <summary>
/// Product-owned floor relation semantics shared by Main Map and MiniMap marker renderers.
/// Map artwork remains current-floor only. Marker type/icon colors remain authoritative;
/// floor relation is communicated by a compact colored outline. A very small direction
/// glyph supplements red/blue for accessibility without obscuring the marker.
/// </summary>
public static class JunhyunFloorPresentation
{
    public const double OtherFloorOpacity = 0.75;
    private const string FloorIndicatorTag = "JunhyunFloorRelationIndicator";
    private const string DirectionGlyphTag = "JunhyunFloorDirectionGlyph";
    private const double IndicatorDiameter = 30.0;
    private const double IndicatorStrokeThickness = 2.25;
    private const double GlyphSize = 10.0;

    private static readonly Color CurrentFloorColor = Color.FromRgb(62, 196, 112);
    private static readonly Color AboveFloorColor = Color.FromRgb(238, 88, 88);
    private static readonly Color BelowFloorColor = Color.FromRgb(72, 145, 235);

    public static JunhyunFloorRelationInfo Resolve(
        MapConfig? config,
        string? markerFloorId,
        string? currentFloorId,
        JunhyunMissingFloorBehavior missingFloorBehavior = JunhyunMissingFloorBehavior.TreatAsMain)
    {
        if (config?.Floors is null || config.Floors.Count == 0 || string.IsNullOrWhiteSpace(currentFloorId))
            return Unknown();

        var current = config.Floors.FirstOrDefault(floor =>
            string.Equals(floor.LayerId, currentFloorId, StringComparison.OrdinalIgnoreCase));
        if (current is null)
            return Unknown();

        if (string.IsNullOrWhiteSpace(markerFloorId) &&
            missingFloorBehavior == JunhyunMissingFloorBehavior.KeepUnknown)
        {
            return Unknown();
        }

        var effectiveMarkerFloorId = string.IsNullOrWhiteSpace(markerFloorId) ? "main" : markerFloorId;
        var marker = config.Floors.FirstOrDefault(floor =>
            string.Equals(floor.LayerId, effectiveMarkerFloorId, StringComparison.OrdinalIgnoreCase));
        if (marker is null)
            return Unknown();

        if (marker.Order == current.Order)
        {
            return new JunhyunFloorRelationInfo(
                JunhyunFloorRelation.Current,
                string.Empty,
                marker.DisplayName ?? marker.LayerId);
        }

        var above = marker.Order > current.Order;
        return new JunhyunFloorRelationInfo(
            above ? JunhyunFloorRelation.Above : JunhyunFloorRelation.Below,
            above ? "↑" : "↓",
            marker.DisplayName ?? marker.LayerId);
    }

    public static void ApplyToMarker(
        FrameworkElement markerVisual,
        JunhyunFloorRelationInfo relation,
        double badgeOffsetX = 8,
        double badgeOffsetY = -16,
        double currentFloorOpacity = 1.0)
    {
        if (markerVisual is not Canvas canvas)
            return;

        markerVisual.Opacity = relation.IsOtherFloor
            ? OtherFloorOpacity
            : Math.Clamp(currentFloorOpacity, 0.0, 1.0);

        if (relation.Relation == JunhyunFloorRelation.Unknown)
        {
            RemoveDirectionBadge(canvas);
            return;
        }

        var color = RelationColor(relation.Relation);
        var tooltip = relation.Relation switch
        {
            JunhyunFloorRelation.Current => $"현재 층 · {relation.FloorLabel}",
            JunhyunFloorRelation.Above => $"위층 · {relation.FloorLabel}",
            JunhyunFloorRelation.Below => $"아래층 · {relation.FloorLabel}",
            _ => relation.FloorLabel,
        };

        UpsertRing(canvas, color, tooltip);
        if (relation.IsOtherFloor)
            UpsertDirectionGlyph(canvas, relation.Arrow, color, tooltip, badgeOffsetX, badgeOffsetY);
        else
            RemoveDirectionGlyph(canvas);
    }

    /// <summary>
    /// Backward-compatible name retained for callers from the previous arrow-badge
    /// implementation. It removes both the compact floor ring and the tiny glyph.
    /// </summary>
    public static void RemoveDirectionBadge(Canvas canvas)
    {
        var indicator = FindFloorIndicator(canvas);
        if (indicator is not null)
            canvas.Children.Remove(indicator);
        RemoveDirectionGlyph(canvas);
    }

    public static bool HasFloorIndicator(Canvas canvas, JunhyunFloorRelation relation)
    {
        var indicator = FindFloorIndicator(canvas);
        return indicator?.Stroke is SolidColorBrush stroke &&
               stroke.Color == RelationColor(relation);
    }

    public static bool IsFloorIndicator(Ellipse ellipse) =>
        string.Equals(ellipse.Tag as string, FloorIndicatorTag, StringComparison.Ordinal);

    private static void UpsertRing(Canvas canvas, Color color, string tooltip)
    {
        var existing = FindFloorIndicator(canvas);
        if (existing is not null &&
            existing.Stroke is SolidColorBrush existingStroke &&
            existingStroke.Color == color &&
            string.Equals(existing.ToolTip as string, tooltip, StringComparison.Ordinal))
        {
            return;
        }

        if (existing is not null)
            canvas.Children.Remove(existing);

        var indicator = new Ellipse
        {
            Tag = FloorIndicatorTag,
            Width = IndicatorDiameter,
            Height = IndicatorDiameter,
            Fill = Brushes.Transparent,
            Stroke = new SolidColorBrush(color),
            StrokeThickness = IndicatorStrokeThickness,
            ToolTip = tooltip,
            IsHitTestVisible = false,
        };
        Canvas.SetLeft(indicator, -IndicatorDiameter / 2.0);
        Canvas.SetTop(indicator, -IndicatorDiameter / 2.0);
        Panel.SetZIndex(indicator, 900);
        canvas.Children.Add(indicator);
    }

    private static void UpsertDirectionGlyph(
        Canvas canvas,
        string arrow,
        Color color,
        string tooltip,
        double badgeOffsetX,
        double badgeOffsetY)
    {
        var existing = FindDirectionGlyph(canvas);
        if (existing?.Child is TextBlock existingText &&
            string.Equals(existingText.Text, arrow, StringComparison.Ordinal) &&
            existingText.Foreground is SolidColorBrush existingBrush &&
            existingBrush.Color == color)
        {
            existing.ToolTip = tooltip;
            Canvas.SetLeft(existing, badgeOffsetX - 1.0);
            Canvas.SetTop(existing, badgeOffsetY + 3.0);
            return;
        }

        if (existing is not null)
            canvas.Children.Remove(existing);

        var glyph = new Border
        {
            Tag = DirectionGlyphTag,
            Width = GlyphSize,
            Height = GlyphSize,
            Background = new SolidColorBrush(Color.FromArgb(190, 20, 20, 20)),
            CornerRadius = new CornerRadius(GlyphSize / 2.0),
            ToolTip = tooltip,
            IsHitTestVisible = false,
            Child = new TextBlock
            {
                Text = arrow,
                Foreground = new SolidColorBrush(color),
                FontSize = 7.0,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center,
            },
        };
        Canvas.SetLeft(glyph, badgeOffsetX - 1.0);
        Canvas.SetTop(glyph, badgeOffsetY + 3.0);
        Panel.SetZIndex(glyph, 910);
        canvas.Children.Add(glyph);
    }

    private static void RemoveDirectionGlyph(Canvas canvas)
    {
        var glyph = FindDirectionGlyph(canvas);
        if (glyph is not null)
            canvas.Children.Remove(glyph);
    }

    private static Ellipse? FindFloorIndicator(Canvas canvas) =>
        canvas.Children
            .OfType<Ellipse>()
            .FirstOrDefault(element =>
                string.Equals(element.Tag as string, FloorIndicatorTag, StringComparison.Ordinal));

    private static Border? FindDirectionGlyph(Canvas canvas) =>
        canvas.Children
            .OfType<Border>()
            .FirstOrDefault(element =>
                string.Equals(element.Tag as string, DirectionGlyphTag, StringComparison.Ordinal));

    private static Color RelationColor(JunhyunFloorRelation relation) => relation switch
    {
        JunhyunFloorRelation.Current => CurrentFloorColor,
        JunhyunFloorRelation.Above => AboveFloorColor,
        JunhyunFloorRelation.Below => BelowFloorColor,
        _ => Colors.Transparent,
    };

    private static JunhyunFloorRelationInfo Unknown() =>
        new(JunhyunFloorRelation.Unknown, string.Empty, string.Empty);
}
