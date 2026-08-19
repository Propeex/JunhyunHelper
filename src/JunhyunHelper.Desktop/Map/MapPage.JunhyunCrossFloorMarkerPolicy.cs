using System.Windows;
using System.Windows.Threading;

namespace TarkovHelper.Pages.Map;

/// <summary>
/// JunhyunHelper product compatibility for the pinned donor's legacy current-floor-only
/// marker filter. The donor records only elements that were visible immediately before it
/// collapses them in <c>_sharedFloorHiddenMarkers</c>. Restore exactly those elements after
/// each donor filter tick so category/faction visibility remains donor-owned while floor is
/// presentation-only as required by JunhyunHelper.
/// </summary>
public partial class MapPage
{
    private Action? _junhyunCrossFloorPresentationRefresh;
    private bool _junhyunCrossFloorMarkerPolicyAttached;

    internal void JunhyunAttachCrossFloorMarkerPolicy(Action reapplyPresentation)
    {
        ArgumentNullException.ThrowIfNull(reapplyPresentation);

        _junhyunCrossFloorPresentationRefresh = reapplyPresentation;
        if (_junhyunCrossFloorMarkerPolicyAttached)
            return;

        _junhyunCrossFloorMarkerPolicyAttached = true;

        // The donor lazily creates this same bounded timer. Initializing it here does
        // not start polling; it merely guarantees that our post-filter callback remains
        // attached whenever the donor later schedules one of its 12 settle ticks.
        _sharedMarkerFilterTimer ??= new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(200),
        };
        _sharedMarkerFilterTimer.Tick -= JunhyunAfterLegacySharedMarkerFilterTick;
        _sharedMarkerFilterTimer.Tick += JunhyunAfterLegacySharedMarkerFilterTick;

        // If the compatibility layer attaches after an early donor tick, normalize the
        // current tree once. This is an asynchronous one-shot, not a persistent poll.
        Dispatcher.BeginInvoke(
            new Action(JunhyunRestoreLegacyFloorSuppressionAndRefresh),
            DispatcherPriority.Send);
    }

    internal void JunhyunDetachCrossFloorMarkerPolicy(Action reapplyPresentation)
    {
        if (!_junhyunCrossFloorMarkerPolicyAttached ||
            !Equals(_junhyunCrossFloorPresentationRefresh, reapplyPresentation))
        {
            return;
        }

        _junhyunCrossFloorMarkerPolicyAttached = false;
        if (_sharedMarkerFilterTimer is not null)
            _sharedMarkerFilterTimer.Tick -= JunhyunAfterLegacySharedMarkerFilterTick;

        // Never leave donor floor-only suppression behind when the product bridge is
        // disposed while the page is still alive.
        JunhyunRestoreLegacyFloorSuppression();
        _junhyunCrossFloorPresentationRefresh = null;
    }

    private void JunhyunAfterLegacySharedMarkerFilterTick(object? sender, EventArgs e)
    {
        if (!_junhyunCrossFloorMarkerPolicyAttached)
            return;

        // Queue after the current Tick invocation so this runs after the donor handler
        // regardless of delegate registration order. Send priority makes the correction
        // the next dispatcher work item rather than leaving a visible collapsed interval.
        Dispatcher.BeginInvoke(
            new Action(JunhyunRestoreLegacyFloorSuppressionAndRefresh),
            DispatcherPriority.Send);
    }

    private void JunhyunRestoreLegacyFloorSuppressionAndRefresh()
    {
        if (!_junhyunCrossFloorMarkerPolicyAttached)
            return;

        JunhyunRestoreLegacyFloorSuppression();
        _junhyunCrossFloorPresentationRefresh?.Invoke();
    }

    private void JunhyunRestoreLegacyFloorSuppression()
    {
        if (_sharedFloorHiddenMarkers.Count == 0)
            return;

        // Do not walk every marker or infer category state. The donor's own set is the
        // precise list of elements it changed from Visible to Collapsed solely for floor.
        foreach (var element in _sharedFloorHiddenMarkers.ToArray())
            element.Visibility = Visibility.Visible;

        _sharedFloorHiddenMarkers.Clear();
    }
}
