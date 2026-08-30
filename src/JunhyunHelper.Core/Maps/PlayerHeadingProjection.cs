namespace JunhyunHelper.Core.Maps;

/// <summary>
/// Projects the screenshot-derived player heading through the same affine orientation
/// that places the player marker on a map image.
/// </summary>
public static class PlayerHeadingProjection
{
    private const double DegenerateVectorThreshold = 1e-12;
    private const double FullTurnDegrees = 360.0;

    /// <summary>
    /// Converts the screenshot heading, whose baseline screen convention is
    /// screenX=-worldX and screenY=worldZ, through the linear portion of a player
    /// marker affine transform [a,b;c,d]. WPF angles are clockwise from screen-up.
    /// </summary>
    public static double Project(
        double baselineScreenAngleDegrees,
        double a,
        double b,
        double c,
        double d)
    {
        var normalizedInput = Normalize(baselineScreenAngleDegrees);
        if (!double.IsFinite(a) ||
            !double.IsFinite(b) ||
            !double.IsFinite(c) ||
            !double.IsFinite(d))
        {
            return normalizedInput;
        }

        var radians = normalizedInput * Math.PI / 180.0;

        // WPF: 0° points up and positive rotation is clockwise.
        var baselineScreenX = Math.Sin(radians);
        var baselineScreenY = -Math.Cos(radians);

        // Undo the baseline EFT->screen orientation used by screenshot yaw parsing.
        var worldX = -baselineScreenX;
        var worldZ = baselineScreenY;

        // Apply the same linear map transform that is used for player position.
        var projectedX = (a * worldX) + (b * worldZ);
        var projectedY = (c * worldX) + (d * worldZ);
        if ((projectedX * projectedX) + (projectedY * projectedY) <= DegenerateVectorThreshold)
            return normalizedInput;

        var projectedAngle = Math.Atan2(projectedX, -projectedY) * 180.0 / Math.PI;
        return Normalize(projectedAngle);
    }

    private static double Normalize(double angleDegrees)
    {
        if (!double.IsFinite(angleDegrees))
            return 0.0;

        var normalized = angleDegrees % FullTurnDegrees;
        if (normalized < 0)
            normalized += FullTurnDegrees;

        // Floating-point cardinal rotations can land infinitesimally below 360°.
        if (normalized >= FullTurnDegrees - 1e-10 || Math.Abs(normalized) <= 1e-10)
            return 0.0;

        return normalized;
    }
}
