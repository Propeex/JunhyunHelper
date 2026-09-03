using JunhyunHelper.Core.FarmingGuide;

namespace JunhyunHelper.Desktop.Scanner;

public sealed partial class ScannerRuntimeService
{
    /// <summary>
    /// Returns the exact currently verified live Scanner presentation with positive FIR
    /// evidence attached. Catalog-only and display-test lookups deliberately do not acquire
    /// provenance: only a real Tarkov-window observation of the same currently shown item may
    /// promote Unknown to FoundInRaid.
    ///
    /// Absence of the visual marker remains Unknown. Scanner never manufactures an explicit
    /// NotFoundInRaid result from a failed visual detection.
    /// </summary>
    internal ScannerItemSnapshot? CreateLiveFarmingGuideSnapshot(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId) ||
            Status.State != ScannerRuntimeState.ShowingItem ||
            Status.CaptureMode != ScannerCaptureMode.TarkovWindow ||
            !string.Equals(Status.ItemId, itemId, StringComparison.Ordinal))
        {
            return null;
        }

        var snapshot = _currentSnapshot;
        var candidate = _verifiedCandidate;
        if (snapshot is null ||
            !string.Equals(snapshot.ItemId, itemId, StringComparison.Ordinal))
        {
            return null;
        }

        return snapshot with
        {
            FirStatus = candidate?.HasFoundInRaidMarkerEvidence == true
                ? FarmingGuideFirStatus.FoundInRaid
                : FarmingGuideFirStatus.Unknown,
        };
    }
}
