using System.Windows.Media;
using System.Windows.Media.Imaging;
using JunhyunHelper.Core.Scanner;

namespace JunhyunHelper.Desktop.Scanner;

public sealed partial class ScannerRuntimeService
{
    private static IReadOnlyList<ScannerInspectCandidate> NormalizeTitleIdentitySignatures(
        IReadOnlyList<ScannerInspectCandidate> candidates)
    {
        if (candidates.Count == 0)
            return candidates;

        var normalized = new ScannerInspectCandidate[candidates.Count];
        for (var index = 0; index < candidates.Count; index++)
            normalized[index] = NormalizeTitleIdentitySignature(candidates[index]);
        return normalized;
    }

    private static ScannerInspectCandidate NormalizeTitleIdentitySignature(
        ScannerInspectCandidate candidate)
    {
        if (candidate.TitleImage is null)
            return candidate;

        try
        {
            BitmapSource source;
            if (candidate.TitleImage.Format == PixelFormats.Bgra32)
            {
                source = candidate.TitleImage;
            }
            else
            {
                var converted = new FormatConvertedBitmap(
                    candidate.TitleImage,
                    PixelFormats.Bgra32,
                    null,
                    0);
                converted.Freeze();
                source = converted;
            }

            var stride = checked(source.PixelWidth * 4);
            var pixels = new byte[checked(stride * source.PixelHeight)];
            source.CopyPixels(pixels, stride, 0);

            if (!ScannerTitleIdentitySignature.TryCompute(
                    pixels,
                    source.PixelWidth,
                    source.PixelHeight,
                    stride,
                    out var identitySignature))
            {
                // Fail closed: retain the detector's exact raw-pixel signature when a
                // stable foreground signature cannot be proven from the title bitmap.
                return candidate;
            }

            return candidate with
            {
                TitleSignature = $"shape:{identitySignature:X16}",
            };
        }
        catch (Exception exception) when (
            exception is ArgumentException or InvalidOperationException or OverflowException)
        {
            // Identity stabilization is optional hardening. It must never block the
            // original detector evidence or turn a scan failure into a false positive.
            return candidate;
        }
    }
}
