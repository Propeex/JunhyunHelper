using JunhyunHelper.Infrastructure.Storage;

namespace JunhyunHelper.Desktop;

public partial class MainWindow
{
    private readonly SemaphoreSlim _contentOperationGate = new(1, 1);
    private bool _contentSchemaRefreshStarted;
    private bool _contentSchemaRefreshAttached;

    private void AttachContentSchemaRefreshTrigger()
    {
        if (_contentSchemaRefreshAttached)
            return;

        _contentSchemaRefreshAttached = true;
        LayoutUpdated += MainWindow_ContentSchemaLayoutUpdated;
    }

    private void DetachContentSchemaRefreshTrigger()
    {
        if (!_contentSchemaRefreshAttached)
            return;

        LayoutUpdated -= MainWindow_ContentSchemaLayoutUpdated;
        _contentSchemaRefreshAttached = false;
    }

    private void MainWindow_ContentSchemaLayoutUpdated(object? sender, EventArgs e)
    {
        if (_contentSchemaRefreshStarted || IsProductSmokeRun())
        {
            DetachContentSchemaRefreshTrigger();
            return;
        }

        if (_activeProfile is null || _activeContent is null)
            return;

        _contentSchemaRefreshStarted = true;
        DetachContentSchemaRefreshTrigger();
        _ = TryRefreshLegacyContentSchemaAsync();
    }

    private async Task TryRefreshLegacyContentSchemaAsync()
    {
        if (_activeProfile is null || _activeContent is null)
            return;

        var targetProfileId = _activeProfile.ProfileId;
        var gameMode = _activeProfile.GameMode;
        var gateEntered = false;
        var ownsBusyState = false;

        bool TargetIsStillCurrent() =>
            _activeProfile is { } activeProfile &&
            string.Equals(activeProfile.ProfileId, targetProfileId, StringComparison.Ordinal) &&
            activeProfile.GameMode == gameMode;

        try
        {
            var snapshot = await _services.Content.ReadActiveOrRecoverAsync(gameMode);
            if (!TargetIsStillCurrent() || !ContentSnapshotStore.RequiresCurrentSchemaRefresh(snapshot))
                return;

            // Manual Data Update and this opportunistic migration share one product-level
            // UI operation gate. The infrastructure update service already serializes disk
            // activation, but without this outer gate one caller could release SetBusy(false)
            // while the other was still waiting/running.
            await _contentOperationGate.WaitAsync();
            gateEntered = true;

            // The user may have started a manual update while the initial legacy-schema
            // read was in flight. Re-read after acquiring the gate so a completed manual
            // update does not trigger a redundant second network refresh.
            if (!TargetIsStillCurrent())
                return;

            snapshot = await _services.Content.ReadActiveOrRecoverAsync(gameMode);
            if (!ContentSnapshotStore.RequiresCurrentSchemaRefresh(snapshot))
                return;

            SetBusy(true);
            ownsBusyState = true;

            var result = await RunContentUpdateAsync(gameMode);
            if (!result.Applied || !TargetIsStillCurrent())
                return;

            var refreshed = await _services.Content.ReadActiveOrRecoverAsync(gameMode);
            if (!TargetIsStillCurrent() || ContentSnapshotStore.RequiresCurrentSchemaRefresh(refreshed))
                return;

            _activeContent = refreshed.Content;
            AmmoPage.SetData(_activeContent);
            await RefreshActiveWorkspacesAsync(detectCleanupChanges: false);
            if (TargetIsStillCurrent())
                ShowActiveSection();
        }
        catch (OperationCanceledException)
        {
            // Window/application shutdown can cancel opportunistic migration. The readable
            // last-known-good snapshot remains intact and migration is retried next launch.
        }
        catch (Exception exception)
        {
            // A readable older snapshot remains the intentional offline fallback. Schema
            // refresh is opportunistic: network/update failure must not prevent startup.
            App.WriteDiagnostic("Opportunistic content schema refresh failed", exception);
        }
        finally
        {
            if (ownsBusyState && TargetIsStillCurrent())
                SetBusy(false);
            if (gateEntered)
                _contentOperationGate.Release();
        }
    }

    private static bool IsProductSmokeRun() =>
        string.Equals(
            Environment.GetEnvironmentVariable("JUNHYUNHELPER_MAP_SMOKE"),
            "1",
            StringComparison.Ordinal);
}
