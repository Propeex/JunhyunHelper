namespace JunhyunHelper.Core.Scanner;

/// <summary>
/// Produces a conservative identity signature from the bright title-ink mask rather
/// than raw BGRA bytes. Dark background noise and unused trailing title-field width are
/// intentionally ignored, while a changed glyph shape changes the signature. This is
/// used only to stabilize an already verified Scanner item; it does not establish a new
/// item identity or relax any semantic-header/OCR/catalog threshold.
/// </summary>
public static class ScannerTitleIdentitySignature
{
    public static bool TryCompute(
        ReadOnlySpan<byte> bgraPixels,
        int width,
        int height,
        int stride,
        out ulong signature)
    {
        signature = 0;
        if (width < 8 || height < 6 || stride < width * 4 || bgraPixels.Length < stride * height)
            return false;

        Span<int> histogram = stackalloc int[256];
        for (var y = 0; y < height; y++)
        {
            var row = y * stride;
            for (var x = 0; x < width; x++)
            {
                var offset = row + x * 4;
                histogram[Luminance(bgraPixels, offset)]++;
            }
        }

        var background = 0;
        var backgroundCount = -1;
        for (var value = 0; value <= 140; value++)
        {
            if (histogram[value] <= backgroundCount)
                continue;
            background = value;
            backgroundCount = histogram[value];
        }

        if (backgroundCount <= 0 || background > 110)
            return false;

        // Quantize the background estimate so a few capture-level luminance points do
        // not move the foreground threshold between otherwise identical frames.
        var quantizedBackground = Math.Clamp(((background + 4) / 8) * 8, 0, 112);
        var threshold = Math.Clamp(Math.Max(56, quantizedBackground + 32), 56, 192);
        var minimumColumnPixels = Math.Max(2, height / 14);
        var strongColumnPixels = Math.Max(4, minimumColumnPixels * 2);
        var brightPerColumn = new int[width];
        var foregroundPixels = 0;

        for (var y = 0; y < height; y++)
        {
            var row = y * stride;
            for (var x = 0; x < width; x++)
            {
                var offset = row + x * 4;
                if (Luminance(bgraPixels, offset) < threshold)
                    continue;
                brightPerColumn[x]++;
                foregroundPixels++;
            }
        }

        if (foregroundPixels < Math.Max(8, height / 2))
            return false;

        var rightmostInk = -1;
        for (var x = width - 1; x >= 0; x--)
        {
            if (brightPerColumn[x] < minimumColumnPixels)
                continue;

            var supportedNeighbors = 0;
            for (var neighbor = Math.Max(0, x - 2); neighbor <= Math.Min(width - 1, x + 2); neighbor++)
            {
                if (brightPerColumn[neighbor] >= minimumColumnPixels)
                    supportedNeighbors++;
            }

            if (supportedNeighbors >= 2 || brightPerColumn[x] >= strongColumnPixels)
            {
                rightmostInk = x;
                break;
            }
        }

        if (rightmostInk < 2)
            return false;

        // Hash only the meaningful left-to-ink region. Inspect title ROIs can differ in
        // their empty trailing width without changing the displayed item name.
        var identityWidth = Math.Min(width, rightmostInk + 3);
        const ulong offsetBasis = 14695981039346656037UL;
        const ulong prime = 1099511628211UL;
        var hash = offsetBasis;

        static ulong Mix(ulong current, byte value)
        {
            const ulong fnvPrime = 1099511628211UL;
            current ^= value;
            return current * fnvPrime;
        }

        hash = Mix(hash, (byte)(identityWidth & 0xFF));
        hash = Mix(hash, (byte)((identityWidth >> 8) & 0xFF));
        hash = Mix(hash, (byte)(height & 0xFF));
        hash = Mix(hash, (byte)((height >> 8) & 0xFF));

        byte packed = 0;
        var bit = 0;
        for (var y = 0; y < height; y++)
        {
            var row = y * stride;
            for (var x = 0; x < identityWidth; x++)
            {
                var offset = row + x * 4;
                if (Luminance(bgraPixels, offset) >= threshold)
                    packed |= (byte)(1 << bit);

                bit++;
                if (bit != 8)
                    continue;

                hash ^= packed;
                hash *= prime;
                packed = 0;
                bit = 0;
            }
        }

        if (bit > 0)
        {
            hash ^= packed;
            hash *= prime;
        }

        signature = hash;
        return true;
    }

    private static int Luminance(ReadOnlySpan<byte> bgraPixels, int offset) =>
        (77 * bgraPixels[offset + 2] +
         150 * bgraPixels[offset + 1] +
         29 * bgraPixels[offset]) >> 8;
}
