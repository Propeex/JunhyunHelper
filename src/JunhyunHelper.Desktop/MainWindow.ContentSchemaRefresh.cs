using JunhyunHelper.Infrastructure.Storage;

namespace JunhyunHelper.Desktop;

public partial class MainWindow
{
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

        try
        {
            var gameMode = _activeProfile.GameMode;
            var snapshot = await _services.Content.ReadActiveOrRecoverAsync(gameMode);
            if (!ContentSnapshotStore.RequiresCurrentSchemaRefresh(snapshot))
                return;

            SetBusy(true, "게임 데이터 형식을 최신 버전으로 갱신하는 중...");
            var result = await RunContentUpdateAsync(gameMode);
            if (!result.Applied)
                return;

            var refreshed = await _services.Content.ReadActiveOrRecoverAsync(gameMode);
            if (ContentSnapshotStore.RequiresCurrentSchemaRefresh(refreshed))
                return;

            _activeContent = refreshed.Content;
            AmmoPage.SetData(_activeContent);
            await RefreshActiveWorkspacesAsync(detectCleanupChanges: false);
            ShowActiveSection();
        }
        catch (OperationCanceledException)
        {
            // Window/application shutdown can cancel opportunistic migration. The readable
            // last-known-good snapshot remains intact and migration is retried next launch.
        }
        catch (Exception)
        {
            // A readable older snapshot remains the intentional offline fallback. Schema
            // refresh is opportunistic: network/update failure must not prevent startup.
        }
        finally
        {
            if (_activeProfile is not null)
                SetBusy(false, BuildLoadedStatus(_activeProfile.GameMode));
        }
    }

    private static bool IsProductSmokeRun() =>
        string.Equals(
            Environment.GetEnvironmentVariable("JUNHYUNHELPER_MAP_SMOKE"),
            "1",
            StringComparison.Ordinal);
}
