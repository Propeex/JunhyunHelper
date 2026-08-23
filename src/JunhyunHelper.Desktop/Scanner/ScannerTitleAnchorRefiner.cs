using JunhyunHelper.Core.Scanner;

namespace JunhyunHelper.Desktop.Scanner;

/// <summary>
/// Converts a coarse Scanner Lab inspect candidate into the authoritative live title ROI.
/// v1.3.3 deliberately delegates horizontal title ownership to the real inspect-header
/// frame lock. First-glyph connected components are no longer permitted to move the OCR
/// crop start, because Korean glyph fragmentation caused the v1.3.2 Strike Cigarettes
/// regression.
/// </summary>
internal static class ScannerTitleAnchorRefiner
{
    public static ScannerTitleAnchorRefinement Refine(
        ReadOnlySpan<byte> bgra,
        int width,
        int height,
        int stride,
        ScannerDetectedCandidate candidate)
    {
        var result = ScannerInspectHeaderLock.Refine(
            bgra,
            width,
            height,
            stride,
            candidate);

        if (string.Equals(result.Reason, "HEADER_FRAME_LOCKED", StringComparison.Ordinal))
            return result;

        // Keep the proven v1.4.0 lock authoritative. The live Ground Truth path is a
        // fail-closed recovery path only for headers the older synthetic template rejects.
        var recovered = ScannerLiveHeaderGroundTruthRefiner.TryRefine(
            bgra,
            width,
            height,
            stride,
            candidate);
        if (recovered is { } liveGroundTruthLock)
            return liveGroundTruthLock;

        // Every partial/failed lock stays below the runtime's trusted-anchor floor so OCR
        // cannot run merely because some subset of close/icon/field evidence scored well.
        return result with { Score = Math.Min(result.Score, 0.47) };
    }
}

internal readonly record struct ScannerTitleAnchorRefinement(
    ScannerDetectedRegion Title,
    ScannerDetectedRegion Magnifier,
    ScannerDetectedRegion CloseButton,
    double Score,
    string Reason);
