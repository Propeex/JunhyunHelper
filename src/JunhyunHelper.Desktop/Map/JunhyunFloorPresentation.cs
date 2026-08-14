using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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
/// Map artwork remains current-floor only; markers from other known floors remain visible
/// and use a compact direction badge so the user can distinguish above from below.
/// </summary>
public static class JunhyunFloorPresentation
{
    public const double OtherFloorOpacity = 0.50;
    private const string DirectionBadgeTag = "JunhyunFloorDirectionBadge";

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

        if (!relation.IsOtherFloor)
        {
            markerVisual.Opacity = Math.Clamp(currentFloorOpacity, 0.0, 1.0);
            RemoveDirectionBadge(canvas);
            return;
        }

        markerVisual.Opacity = OtherFloorOpacity;
        var tooltip = $"{relation.Arrow} {relation.FloorLabel}";
        var existing = FindDirectionBadge(canvas);
        if (existing is not null &&
            string.Equals(existing.ToolTip as string, tooltip, StringComparison.Ordinal) &&
            Math.Abs(Canvas.GetLeft(existing) - badgeOffsetX) < 0.01 &&
            Math.Abs(Canvas.GetTop(existing) - badgeOffsetY) < 0.01)
        {
            return;
        }

        if (existing is not null)
            canvas.Children.Remove(existing);

        var background = relation.Relation == JunhyunFloorRelation.Above
            ? Color.FromRgb(65, 145, 210)
            : Color.FromRgb(220, 132, 38);
        var badge = new Border
        {
            Tag = DirectionBadgeTag,
            Width = 16,
            Height = 16,
            CornerRadius = new CornerRadius(8),
            Background = new SolidColorBrush(Color.FromArgb(245, background.R, background.G, background.B)),
            BorderBrush = new SolidColorBrush(Color.FromArgb(245, 245, 245, 245)),
            BorderThickness = new Thickness(1),
            ToolTip = tooltip,
            IsHitTestVisible = false,
            Child = new TextBlock
            {
                Text = relation.Arrow,
                Foreground = Brushes.White,
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center,
            },
        };
        Canvas.SetLeft(badge, badgeOffsetX);
        Canvas.SetTop(badge, badgeOffsetY);
        canvas.Children.Add(badge);
    }

    public static void RemoveDirectionBadge(Canvas canvas)
    {
        var badge = FindDirectionBadge(canvas);
        if (badge is not null)
            canvas.Children.Remove(badge);
    }

    private static FrameworkElement? FindDirectionBadge(Canvas canvas) =>
        canvas.Children
            .OfType<FrameworkElement>()
            .FirstOrDefault(element =>
                string.Equals(element.Tag as string, DirectionBadgeTag, StringComparison.Ordinal));

    private static JunhyunFloorRelationInfo Unknown() =>
        new(JunhyunFloorRelation.Unknown, string.Empty, string.Empty);
}
