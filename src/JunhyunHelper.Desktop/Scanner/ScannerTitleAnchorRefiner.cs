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
        ScannerDetectedCandidate candidate) =>
        ScannerInspectHeaderLock.Refine(
            bgra,
            width,
            height,
            stride,
            candidate);
}

internal readonly record struct ScannerTitleAnchorRefinement(
    ScannerDetectedRegion Title,
    ScannerDetectedRegion Magnifier,
    ScannerDetectedRegion CloseButton,
    double Score,
    string Reason);
