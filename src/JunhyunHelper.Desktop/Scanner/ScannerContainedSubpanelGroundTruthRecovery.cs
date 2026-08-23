using JunhyunHelper.Core.Scanner;

namespace JunhyunHelper.Desktop.Scanner;

/// <summary>
/// Third-stage recovery for reviewed live cases where an oversized stash/inventory
/// rectangle horizontally contains the real inspect panel. Neutral header lines are
/// proposals only; every proposal must still pass the existing live header refiner.
/// </summary>
internal static class ScannerContainedSubpanelGroundTruthRecovery
{
    private const double ProposalDensityFloor = 0.86;
    private const int ProposalGapAllowance = 2;

    public static ScannerTitleAnchorRefinement? TryRefine(
        ReadOnlySpan<byte> bgra,
        int width,
        int height,
        int stride,
        ScannerDetectedCandidate candidate)
    {
        if (width < 120 || height < 80 || stride < width * 4 || bgra.Length < stride * height)
            return null;

        var panel = candidate.Window;
        if (panel.Width < 650 || panel.Height < 420)
            return null;

        var left = Math.Clamp(panel.X - 8, 0, width - 1);
        var right = Math.Clamp(panel.X + panel.Width + 8, left, width - 1);
        var top = Math.Clamp(panel.Y + 20, 0, height - 1);
        var bottom = Math.Clamp(
            panel.Y + Math.Min(panel.Height - 1, (int)Math.Round(panel.Height * 0.65)),
            top,
            height - 1);
        var minimumLength = Math.Max(360, (int)Math.Round(panel.Width * 0.42));

        var proposals = new List<HeaderProposal>();
        HeaderProposal? previous = null;
        for (var y = top; y <= bottom; y++)
        {
            foreach (var run in FindRuns(bgra, stride, left, right, y))
            {
                if (run.Length < minimumLength || run.Density < ProposalDensityFloor)
                    continue;
                if (!HasRedEvidenceNearRight(bgra, width, height, stride, run.Left, y, run.Length))
                    continue;

                var proposal = new HeaderProposal(run.Left, y, run.Length, run.Density);
                if (previous is { } prior &&
                    Math.Abs(prior.Y - proposal.Y) <= 6 &&
                    Math.Abs(prior.Left - proposal.Left) <= 24 &&
                    Math.Abs(prior.Width - proposal.Width) <= 48)
                {
                    if (proposal.Density > prior.Density)
                    {
                        proposals[^1] = proposal;
                        previous = proposal;
                    }
                    continue;
                }

                proposals.Add(proposal);
                previous = proposal;
            }
        }

        ScannerTitleAnchorRefinement? best = null;
        foreach (var proposal in proposals
                     .OrderByDescending(value => value.Density)
                     .ThenBy(value => value.Y)
                     .Take(10))
        {
            var provisionalHeight = Math.Clamp(
                panel.Y + panel.Height - proposal.Y,
                160,
                height - proposal.Y);
            var provisional = new ScannerDetectedRegion(
                proposal.Left,
                proposal.Y,
                proposal.Width,
                provisionalHeight,
                Math.Max(candidate.Window.Score, proposal.Density));
            var provisionalCandidate = new ScannerDetectedCandidate(
                provisional,
                ScannerDetailGeometryDetector.GetTitleRegion(provisional),
                default,
                "CONTAINED_HEADER_PROPOSAL");

            var refined = ScannerLiveHeaderGroundTruthRefiner.TryRefine(
                bgra,
                width,
                height,
                stride,
                provisionalCandidate);
            if (refined is not { } locked ||
                !string.Equals(locked.Reason, "HEADER_FRAME_LOCKED", StringComparison.Ordinal) ||
                locked.Score < 0.68)
            {
                continue;
            }

            var originalRight = panel.X + panel.Width;
            var lockedRight = locked.CloseButton.X + locked.CloseButton.Width;
            if (Math.Abs(locked.Title.Y - proposal.Y) > 34 ||
                locked.Magnifier.X < panel.X - 24 ||
                lockedRight > originalRight + 32)
            {
                continue;
            }

            if (best is null || locked.Score > best.Value.Score)
                best = locked;
        }

        return best;
    }

    private static IReadOnlyList<NeutralRun> FindRuns(
        ReadOnlySpan<byte> bgra,
        int stride,
        int left,
        int right,
        int y)
    {
        var result = new List<NeutralRun>();
        var start = -1;
        var lastGood = -1;
        var good = 0;
        var gap = 0;

        void Complete()
        {
            if (start < 0 || lastGood < start)
                return;
            var length = lastGood - start + 1;
            result.Add(new NeutralRun(start, length, good / (double)Math.Max(1, length)));
        }

        for (var x = left; x <= right; x++)
        {
            if (IsHeaderTopBorderPixel(bgra, stride, x, y))
            {
                if (start < 0)
                {
                    start = x;
                    good = 0;
                }
                lastGood = x;
                good++;
                gap = 0;
                continue;
            }

            if (start < 0)
                continue;
            gap++;
            if (gap <= ProposalGapAllowance)
                continue;

            Complete();
            start = -1;
            lastGood = -1;
            good = 0;
            gap = 0;
        }

        Complete();
        return result;
    }

    private static bool HasRedEvidenceNearRight(
        ReadOnlySpan<byte> bgra,
        int width,
        int height,
        int stride,
        int runLeft,
        int runY,
        int runLength)
    {
        var runRight = runLeft + runLength - 1;
        var left = Math.Clamp(runRight - 52, 0, width - 1);
        var right = Math.Clamp(runRight + 42, left, width - 1);
        var top = Math.Clamp(runY - 4, 0, height - 1);
        var bottom = Math.Clamp(runY + 34, top, height - 1);
        var red = 0;

        for (var y = top; y <= bottom; y++)
        for (var x = left; x <= right; x++)
        {
            ReadBgr(bgra, stride, x, y, out var b, out var g, out var r);
            if (r >= 52 && r - g >= 20 && r - b >= 20)
                red++;
        }

        return red >= 16;
    }

    private static bool IsHeaderTopBorderPixel(ReadOnlySpan<byte> bgra, int stride, int x, int y)
    {
        ReadBgr(bgra, stride, x, y, out var b, out var g, out var r);
        var max = Math.Max(r, Math.Max(g, b));
        var min = Math.Min(r, Math.Min(g, b));
        var gray = (77 * r + 150 * g + 29 * b) >> 8;
        return gray is >= 38 and <= 108 && max - min <= 36;
    }

    private static void ReadBgr(
        ReadOnlySpan<byte> bgra,
        int stride,
        int x,
        int y,
        out int b,
        out int g,
        out int r)
    {
        var offset = y * stride + x * 4;
        b = bgra[offset];
        g = bgra[offset + 1];
        r = bgra[offset + 2];
    }

    private readonly record struct NeutralRun(int Left, int Length, double Density);
    private readonly record struct HeaderProposal(int Left, int Y, int Width, double Density);
}
