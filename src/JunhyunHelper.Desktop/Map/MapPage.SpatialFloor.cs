namespace JunhyunHelper.Desktop.Map;

public partial class MapPage
{
    private bool _spatialFloorTrackingConfigured;

    private void EnsureSpatialFloorTracking()
    {
        if (_spatialFloorTrackingConfigured)
            return;

        _spatialFloorTrackingConfigured = true;
        _screenshotTracker.PositionDetected -= ScreenshotTracker_PositionDetected;
        _screenshotTracker.PositionDetected += ScreenshotTracker_PositionDetectedSpatial;
    }

    private void ScreenshotTracker_PositionDetectedSpatial(object? sender, ScreenshotPositionDetected e)
    {
        Dispatcher.BeginInvoke(() =>
        {
            _playerPosition = e.Position;
            _playerHeading = e.HeadingDegrees;
            if (_settings.ShowTrail)
            {
                if (_trail.Count == 0 || DistanceSquared(_trail[^1], e.Position) > 0.25)
                    _trail.Add(e.Position);
                if (_trail.Count > 400)
                    _trail.RemoveAt(0);
            }

            if (_currentChoice is not null && _currentChoice.Layout.Floors.Count > 1)
            {
                var floor = MapCoordinateTransformer.FloorForPosition(
                    _currentChoice.Layout,
                    e.Position);
                if (floor is not null &&
                    !string.Equals(floor.Id, _currentFloor?.Id, StringComparison.Ordinal))
                {
                    FloorComboBox.SelectedItem = floor;
                }
            }

            RenderTrail();
            RenderPlayer();
            UpdateMiniMap();
        });
    }
}
