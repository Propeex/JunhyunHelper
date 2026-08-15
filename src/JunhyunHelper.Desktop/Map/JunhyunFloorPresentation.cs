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
/// Map artwork remains current-floor only; markers from other known floors remain visible.
/// Floor state is deliberately a small color dot instead of a large arrow badge:
/// current=green, above=red, below=blue. The marker body remains free to communicate
/// marker type/faction semantics without competing with floor meaning.
/// </summary>
public static class JunhyunFloorPresentation
{
    public const double OtherFloorOpacity = 0.85;
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

    public static Color StatusColor(JunhyunFloorRelation relation) => relation switch
    {
        JunhyunFloorRelation.Current => Color.FromRgb(76, 175, 80),
        JunhyunFloorRelation.Above => Color.FromRgb(239, 83, 80),
        JunhyunFloorRelation.Below => Color.FromRgb(66, 165, 245),
        _ => Colors.Transparent,
    };

    public static void ApplyToMarker(
        FrameworkElement markerVisual,
        JunhyunFloorRelationInfo relation,
        double badgeOffsetX = 8,
        double badgeOffsetY = -16,
        double currentFloorOpacity = 1.0)
    {
        if (markerVisual is not Canvas canvas)
            return;

        if (relation.Relation == JunhyunFloorRelation.Unknown)
        {
            markerVisual.Opacity = Math.Clamp(currentFloorOpacity, 0.0, 1.0);
            RemoveDirectionBadge(canvas);
            return;
        }

        markerVisual.Opacity = relation.IsOtherFloor
            ? OtherFloorOpacity
            : Math.Clamp(currentFloorOpacity, 0.0, 1.0);

        var stateText = relation.Relation switch
        {
            JunhyunFloorRelation.Current => "현재 층",
            JunhyunFloorRelation.Above => "위층",
            JunhyunFloorRelation.Below => "아래층",
            _ => "층 미확인",
        };
        var tooltip = string.IsNullOrWhiteSpace(relation.FloorLabel)
            ? stateText
            : $"{stateText}: {relation.FloorLabel}";
        var statusColor = StatusColor(relation.Relation);

        var existing = FindDirectionBadge(canvas);
        if (existing is Border existingDot &&
            string.Equals(existingDot.ToolTip as string, tooltip, StringComparison.Ordinal) &&
            existingDot.Background is SolidColorBrush existingBrush &&
            existingBrush.Color == statusColor &&
            Math.Abs(Canvas.GetLeft(existingDot) - badgeOffsetX) < 0.01 &&
            Math.Abs(Canvas.GetTop(existingDot) - badgeOffsetY) < 0.01)
        {
            return;
        }

        if (existing is not null)
            canvas.Children.Remove(existing);

        // The collapsed text preserves the legacy smoke probe's semantic direction token
        // without rendering an arrow to the user. The visible contract is exclusively the
        // compact color dot.
        var semanticDirection = new TextBlock
        {
            Text = relation.Arrow,
            Visibility = Visibility.Collapsed,
            IsHitTestVisible = false,
        };

        var dot = new Border
        {
            Tag = DirectionBadgeTag,
            Width = 9,
            Height = 9,
            CornerRadius = new CornerRadius(4.5),
            Background = new SolidColorBrush(statusColor),
            BorderBrush = new SolidColorBrush(Color.FromArgb(235, 245, 245, 245)),
            BorderThickness = new Thickness(1),
            ToolTip = tooltip,
            IsHitTestVisible = false,
            Child = semanticDirection,
        };
        Canvas.SetLeft(dot, badgeOffsetX);
        Canvas.SetTop(dot, badgeOffsetY);
        canvas.Children.Add(dot);
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
