using System.Diagnostics;
using System.Security.Cryptography;
using System.Windows.Media.Imaging;

namespace JunhyunHelper.Desktop.Scanner;

/// <summary>
/// Windows OCR is shared by title recognition and the inventory/stash context gate.
/// Keep access serialized so a second probe cannot race the active item recognition
/// pipeline or create a second OCR runtime solely for overlay visibility decisions.
/// Exact title bitmaps are also reused only inside the current Scanner latency cycle;
/// no OCR result is cached across frames or one-shot invocations.
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
        _inner = inner is EnvironmentGuardedScannerOcrEngine
            ? inner
            : new EnvironmentGuardedScannerOcrEngine(inner);
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
            var result = deep && _inner is IScannerDeepOcrEngine deepEngine
                ? await deepEngine.ReadDeepTextAsync(titleImage, cancellationToken)
                : await _inner.ReadTextAsync(titleImage, cancellationToken);
            ScannerPerformanceTrace.Mark(
                "ocr-operation-end",
                ("pass", pass),
                ("elapsedMs", ScannerPerformanceTrace.ElapsedMilliseconds(operationStarted).ToString("F2", System.Globalization.CultureInfo.InvariantCulture)),
                ("hasText", !string.IsNullOrWhiteSpace(result)),
                ("textLength", result?.Length ?? 0));

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
