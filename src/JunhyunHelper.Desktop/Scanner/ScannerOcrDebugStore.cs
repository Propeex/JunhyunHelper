using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace JunhyunHelper.Desktop.Scanner;

/// <summary>
/// Bounded in-memory evidence store for the exact images supplied to Windows OCR.
/// Nothing is written to disk here. ScannerDiagnosticDataset persists these images only
/// when the observation is retained or the user explicitly confirms/corrects a case.
/// </summary>
internal static class ScannerOcrDebugStore
{
    private const int MaximumEntries = 16;
    private static readonly object Gate = new();
    private static readonly Dictionary<string, ScannerOcrDebugSnapshot> Entries = new(StringComparer.Ordinal);
    private static readonly Queue<string> Order = new();

    public static string ComputeTitleSignature(BitmapSource source)
    {
        ArgumentNullException.ThrowIfNull(source);
        var converted = source.Format == PixelFormats.Bgra32
            ? source
            : new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0);
        if (converted.CanFreeze && !converted.IsFrozen)
            converted.Freeze();

        var stride = converted.PixelWidth * 4;
        var pixels = new byte[Math.Max(0, stride * converted.PixelHeight)];
        if (pixels.Length > 0)
            converted.CopyPixels(pixels, stride, 0);

        const ulong offset = 14695981039346656037;
        const ulong prime = 1099511628211;
        var hash = offset;
        foreach (var value in pixels)
        {
            hash ^= value;
            hash *= prime;
        }
        return $"{hash:X16}";
    }

    public static void Publish(BitmapSource source, string pass, IReadOnlyList<BitmapSource> processedImages)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(processedImages);
        var signature = ComputeTitleSignature(source);
        var frozen = processedImages
            .Where(image => image is not null)
            .Select(FreezeClone)
            .ToArray();
        if (frozen.Length == 0)
            return;

        lock (Gate)
        {
            if (!Entries.ContainsKey(signature))
                Order.Enqueue(signature);
            Entries[signature] = new ScannerOcrDebugSnapshot(signature, pass, frozen, DateTimeOffset.Now);
            while (Order.Count > MaximumEntries)
            {
                var oldest = Order.Dequeue();
                if (!Order.Contains(oldest))
                    Entries.Remove(oldest);
            }
        }
    }

    public static ScannerOcrDebugSnapshot? Get(string? titleSignature)
    {
        if (string.IsNullOrWhiteSpace(titleSignature))
            return null;
        lock (Gate)
            return Entries.GetValueOrDefault(titleSignature.Trim());
    }

    public static void Clear()
    {
        lock (Gate)
        {
            Entries.Clear();
            Order.Clear();
        }
    }

    private static BitmapSource FreezeClone(BitmapSource source)
    {
        if (source.IsFrozen)
            return source;
        var clone = source.CloneCurrentValue();
        clone.Freeze();
        return clone;
    }
}

internal sealed record ScannerOcrDebugSnapshot(
    string TitleSignature,
    string Pass,
    IReadOnlyList<BitmapSource> ProcessedImages,
    DateTimeOffset Timestamp);