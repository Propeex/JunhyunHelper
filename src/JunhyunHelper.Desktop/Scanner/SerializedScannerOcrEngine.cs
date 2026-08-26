using System.Diagnostics;
using System.Security.Cryptography;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using JunhyunHelper.Core.Scanner;

namespace JunhyunHelper.Desktop.Scanner;

/// <summary>
/// Windows OCR is shared by title recognition and the inventory/stash context gate.
/// Keep access serialized so a second probe cannot race the active item recognition
/// pipeline or create a second OCR runtime solely for overlay visibility decisions.
/// Exact title bitmaps are also reused only inside the current Scanner latency cycle;
/// no OCR result is cached across frames or one-shot invocations.
///
/// Public-distribution hardening: the proven OCR result is always attempted first. If
/// the input luminance profile looks lifted/washed or unusually low-contrast, an
/// adaptive grayscale-normalized retry is added without lowering semantic/catalog
/// acceptance thresholds. Healthy reference SDR inputs keep the historical path only.
/// </summary>
internal sealed class SerializedScannerOcrEngine : IScannerDeepOcrEngine
{
    private readonly IScannerOcrEngine _inner;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly Dictionary<ExactImageKey, string> _normalCycleCache = [];
    private readonly Dictionary<ExactImageKey, string> _deepCycleCache = [];
    private long _cacheCycleId = long.MinValue;

    public SerializedScannerOcrEngine(IScannerOcrEngine inner)
    {
        ArgumentNullException.ThrowIfNull(inner);
        IScannerOcrEngine effectiveInner = inner is ScannerLab38OcrEngine lab38
            ? new DiagnosticScannerLab38OcrEngine(lab38)
            : inner;
        _inner = effectiveInner is EnvironmentGuardedScannerOcrEngine
            ? effectiveInner
            : new EnvironmentGuardedScannerOcrEngine(effectiveInner);
    }

    public bool IsAvailable => _inner.IsAvailable;
    public string AvailabilityMessage => _inner.AvailabilityMessage;

    public Task<string> ReadTextAsync(BitmapSource titleImage, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(titleImage);
        return ReadSerializedAsync(titleImage, deep: false, cancellationToken);
    }

    public Task<string> ReadDeepTextAsync(BitmapSource titleImage, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(titleImage);
        return ReadSerializedAsync(titleImage, deep: true, cancellationToken);
    }

    private async Task<string> ReadSerializedAsync(
        BitmapSource titleImage,
        bool deep,
        CancellationToken cancellationToken)
    {
        var pass = deep ? "deep" : "normal";
        var gateStarted = Stopwatch.GetTimestamp();
        ScannerPerformanceTrace.Mark(
            "ocr-serialized-wait-start",
            ("pass", pass),
            ("width", titleImage.PixelWidth),
            ("height", titleImage.PixelHeight));
        await _gate.WaitAsync(cancellationToken);
        ScannerPerformanceTrace.Mark(
            "ocr-serialized-wait-end",
            ("pass", pass),
            ("elapsedMs", ScannerPerformanceTrace.ElapsedMilliseconds(gateStarted).ToString("F2", System.Globalization.CultureInfo.InvariantCulture)));

        try
        {
            var cycleId = ScannerLatencyTelemetry.CurrentCycleId;
            ExactImageKey? imageKey = null;
            Dictionary<ExactImageKey, string>? cycleCache = null;

            if (cycleId is { } activeCycle)
            {
                ResetCycleCacheIfNeeded(activeCycle);
                var keyStarted = Stopwatch.GetTimestamp();
                ScannerPerformanceTrace.Mark(
                    "ocr-image-key-start",
                    ("pass", pass),
                    ("width", titleImage.PixelWidth),
                    ("height", titleImage.PixelHeight));
                imageKey = CreateExactImageKey(titleImage);
                ScannerPerformanceTrace.Mark(
                    "ocr-image-key-end",
                    ("pass", pass),
                    ("elapsedMs", ScannerPerformanceTrace.ElapsedMilliseconds(keyStarted).ToString("F2", System.Globalization.CultureInfo.InvariantCulture)),
                    ("bitsPerPixel", imageKey.Value.BitsPerPixel));

                cycleCache = deep ? _deepCycleCache : _normalCycleCache;
                if (cycleCache.TryGetValue(imageKey.Value, out var cached))
                {
                    ScannerPerformanceTrace.Mark("ocr-cycle-cache-hit", ("pass", pass));
                    ScannerDiagnosticLog.Write(
                        "ocr-cycle-reuse",
                        null,
                        ("cycleId", activeCycle),
                        ("pass", pass),
                        ("width", imageKey.Value.Width),
                        ("height", imageKey.Value.Height));
                    return cached;
                }
            }

            var operationStarted = Stopwatch.GetTimestamp();
            ScannerPerformanceTrace.Mark(
                "ocr-operation-start",
                ("pass", pass),
                ("width", titleImage.PixelWidth),
                ("height", titleImage.PixelHeight));
            var result = (deep && _inner is IScannerDeepOcrEngine deepEngine
                ? await deepEngine.ReadDeepTextAsync(titleImage, cancellationToken)
                : await _inner.ReadTextAsync(titleImage, cancellationToken)) ?? string.Empty;

            if (TryCreateEnvironmentNormalizedImage(titleImage, out var normalizedImage, out var luminanceProfile) &&
                (deep || string.IsNullOrWhiteSpace(result)))
            {
                var adaptiveStarted = Stopwatch.GetTimestamp();
                var adaptiveText = await _inner.ReadTextAsync(normalizedImage, cancellationToken) ?? string.Empty;
                result = deep
                    ? MergeOcrEvidence(result, adaptiveText)
                    : string.IsNullOrWhiteSpace(result)
                        ? adaptiveText
                        : result;

                ScannerPerformanceTrace.Mark(
                    "ocr-environment-normalization",
                    ("pass", pass),
                    ("elapsedMs", ScannerPerformanceTrace.ElapsedMilliseconds(adaptiveStarted).ToString("F2", System.Globalization.CultureInfo.InvariantCulture)),
                    ("background", luminanceProfile.BackgroundLuminance),
                    ("foreground", luminanceProfile.ForegroundLuminance),
                    ("contrast", luminanceProfile.ContrastSpan),
                    ("adaptiveThreshold", luminanceProfile.AdaptiveThreshold),
                    ("adaptiveHasText", !string.IsNullOrWhiteSpace(adaptiveText)));
            }

            ScannerPerformanceTrace.Mark(
                "ocr-operation-end",
                ("pass", pass),
                ("elapsedMs", ScannerPerformanceTrace.ElapsedMilliseconds(operationStarted).ToString("F2", System.Globalization.CultureInfo.InvariantCulture)),
                ("hasText", !string.IsNullOrWhiteSpace(result)),
                ("textLength", result.Length));

            // A fire-and-forget overlay probe can inherit an ExecutionContext briefly.
            // Store only if this exact Scanner cycle is still active after OCR completes.
            if (cycleId is { } completedCycle &&
                ScannerLatencyTelemetry.CurrentCycleId == completedCycle &&
                imageKey is { } completedKey &&
                cycleCache is not null)
            {
                cycleCache[completedKey] = result;
            }

            return result;
        }
        finally
        {
            _gate.Release();
        }
    }

    private static bool TryCreateEnvironmentNormalizedImage(
        BitmapSource source,
        out BitmapSource normalized,
        out ScannerTitleLuminanceProfile profile)
    {
        BitmapSource bgra = source.Format == PixelFormats.Bgra32
            ? source
            : new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0);
        if (!bgra.IsFrozen && bgra.CanFreeze)
            bgra.Freeze();

        if (bgra.PixelWidth <= 0 || bgra.PixelHeight <= 0)
        {
            profile = default;
            normalized = null!;
            return false;
        }

        var stride = checked(bgra.PixelWidth * 4);
        var pixels = new byte[checked(stride * bgra.PixelHeight)];
        bgra.CopyPixels(pixels, stride, 0);
        profile = ScannerTitleEnvironmentNormalizer.AnalyzeBgra(
            pixels,
            bgra.PixelWidth,
            bgra.PixelHeight,
            stride);
        if (!profile.UseAdaptiveNormalization)
        {
            normalized = null!;
            return false;
        }

        for (var offset = 0; offset < pixels.Length; offset += 4)
        {
            var gray = ScannerTitleEnvironmentNormalizer.ToGray(
                pixels[offset + 2],
                pixels[offset + 1],
                pixels[offset]);
            var output = (byte)ScannerTitleEnvironmentNormalizer.TransformGray(gray, 1, profile);
            pixels[offset] = output;
            pixels[offset + 1] = output;
            pixels[offset + 2] = output;
            pixels[offset + 3] = 255;
        }

        normalized = BitmapSource.Create(
            bgra.PixelWidth,
            bgra.PixelHeight,
            96,
            96,
            PixelFormats.Bgra32,
            null,
            pixels,
            stride);
        normalized.Freeze();
        return true;
    }

    private static string MergeOcrEvidence(string primary, string adaptive)
    {
        if (string.IsNullOrWhiteSpace(primary))
            return adaptive.Trim();
        if (string.IsNullOrWhiteSpace(adaptive))
            return primary.Trim();

        var candidates = primary
            .Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Concat(adaptive.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Where(candidate => !string.IsNullOrWhiteSpace(candidate))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        return string.Join(" | ", candidates);
    }

    private void ResetCycleCacheIfNeeded(long cycleId)
    {
        if (_cacheCycleId == cycleId)
            return;

        _cacheCycleId = cycleId;
        _normalCycleCache.Clear();
        _deepCycleCache.Clear();
    }

    private static ExactImageKey CreateExactImageKey(BitmapSource image)
    {
        var bitsPerPixel = image.Format.BitsPerPixel;
        if (image.PixelWidth <= 0 || image.PixelHeight <= 0 || bitsPerPixel <= 0)
            return new ExactImageKey(image.PixelWidth, image.PixelHeight, bitsPerPixel, string.Empty);

        var stride = checked((image.PixelWidth * bitsPerPixel + 7) / 8);
        var pixels = new byte[checked(stride * image.PixelHeight)];
        image.CopyPixels(pixels, stride, 0);
        var hash = Convert.ToHexString(SHA256.HashData(pixels));
        return new ExactImageKey(image.PixelWidth, image.PixelHeight, bitsPerPixel, hash);
    }

    private readonly record struct ExactImageKey(
        int Width,
        int Height,
        int BitsPerPixel,
        string Sha256);
}
