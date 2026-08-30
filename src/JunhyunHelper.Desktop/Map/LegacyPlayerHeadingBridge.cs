using System.Windows.Threading;
using JunhyunHelper.Core.Maps;
using TarkovHelper.Models.Map;
using TarkovHelper.Services.Map;

namespace JunhyunHelper.Desktop.Map;

/// <summary>
/// Reconciles donor player-heading rendering with the affine transform already used for
/// player position. The pinned donor copies raw screenshot yaw into ScreenPosition.Angle;
/// this bridge applies the map orientation after donor rendering on both Map and MiniMap.
/// </summary>
internal sealed class LegacyPlayerHeadingBridge : IDisposable
{
    private readonly TarkovHelper.Pages.Map.MapPage _page;
    private readonly MapTrackerService _tracker = MapTrackerService.Instance;
    private bool _disposed;

    public LegacyPlayerHeadingBridge(TarkovHelper.Pages.Map.MapPage page)
    {
        _page = page ?? throw new ArgumentNullException(nameof(page));
        _tracker.PositionUpdated += PositionUpdated;
    }

    private void PositionUpdated(object? sender, ScreenPosition position)
    {
        if (_disposed || !position.Angle.HasValue)
            return;

        var projectedAngle = Project(position);

        // Donor MapPage may apply its old Factory/Labs special-case during the same event.
        // ContextIdle guarantees the generic affine result is the final presentation.
        _page.Dispatcher.BeginInvoke(
            () =>
            {
                if (!_disposed)
                    _page.ApplyJunhyunPlayerHeading(projectedAngle);
            },
            DispatcherPriority.ContextIdle);

        // The donor MiniMap always writes raw ScreenPosition.Angle. Registry dispatch uses
        // the same post-donor priority so an active overlay ends on the identical heading.
        JunhyunMiniMapProductRegistry.ApplyPlayerHeadingAfterDonor(projectedAngle);
    }

    private double Project(ScreenPosition position)
    {
        var rawAngle = position.Angle!.Value;
        var transform = _tracker.GetMapConfig(position.MapKey)?.PlayerMarkerTransform;
        if (transform is not { Length: >= 4 })
            return rawAngle;

        return PlayerHeadingProjection.Project(
            rawAngle,
            transform[0],
            transform[1],
            transform[2],
            transform[3]);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _tracker.PositionUpdated -= PositionUpdated;
    }
}
