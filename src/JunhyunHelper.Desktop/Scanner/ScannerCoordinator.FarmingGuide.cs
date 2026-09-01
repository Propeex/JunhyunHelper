namespace JunhyunHelper.Desktop.Scanner;

public sealed partial class ScannerCoordinator
{
    public ScannerItemSnapshot? CreateFarmingGuideSnapshot(string itemId)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (string.IsNullOrWhiteSpace(itemId))
            return null;
        return Presentation.CreateSnapshot(itemId.Trim());
    }

    /// <summary>
    /// Simulated Farming Guide scans are a product test path, not a Scanner-runtime
    /// capture mode. Load the current mode's verified local Scanner catalog on demand so
    /// hover + T keeps working after an application restart even when Scanner itself is
    /// disabled and has not initialized its in-memory catalog yet.
    /// </summary>
    public async Task<ScannerItemSnapshot?> CreateFarmingGuideSnapshotAsync(
        string itemId,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (string.IsNullOrWhiteSpace(itemId))
            return null;

        var context = GetContext();
        if (context is null)
            return null;
        if (!await _catalog.EnsureLoadedAsync(context.GameMode, cancellationToken))
            return null;

        return Presentation.CreateSnapshot(itemId.Trim());
    }

    public void SetFarmingGuideInstruction(string? instruction)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _overlay.SetFarmingGuideInstruction(
            _settings.Current.ShowFarmingGuide ? instruction : null);
    }

    public void ShowFarmingGuideTestSnapshot(ScannerItemSnapshot snapshot)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(snapshot);
        _overlay.ShowTemporaryPreview(snapshot, TimeSpan.FromSeconds(3));
    }

    /// <summary>
    /// Short acknowledgement remains transient; the active recommendation itself is held
    /// by SetFarmingGuideInstruction until acceptance, replacement by a new scan, or raid end.
    /// </summary>
    public void ShowFarmingGuideStatus(string message)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!_settings.Current.ShowFarmingGuide || string.IsNullOrWhiteSpace(message))
            return;
        _overlay.ShowTransientStatus(message.Trim());
    }
}
