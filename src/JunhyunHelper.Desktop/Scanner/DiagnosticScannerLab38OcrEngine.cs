using System.Diagnostics;
using System.Reflection;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using JunhyunHelper.Core.Scanner;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Security.Cryptography;

namespace JunhyunHelper.Desktop.Scanner;

/// <summary>
/// Diagnostic-only mirror of the current Scanner Lab 3.8 OCR algorithm. It reuses the
/// already-created OcrEngine from ScannerLab38OcrEngine and preserves the same scaling,
/// variant, line/pair and WinRT recognition semantics while exposing the exact blocking
/// phase to the bounded in-memory performance trace.
///
/// The v1.7.5 environment guard sits outside a whole normal/deep operation. Deep OCR can
/// contain four actual WinRT calls, so a slow-empty first call could still be amplified
/// before the outer guard observed the result. This diagnostic candidate therefore also
/// applies the same slow-empty health policy at the actual RecognizeAsync boundary. It
/// does not suppress successful OCR or relax any recognition acceptance rule.
/// </summary>
internal sealed class DiagnosticScannerLab38OcrEngine : IScannerDeepOcrEngine
{
    private static readonly FieldInfo? EngineField = typeof(ScannerLab38OcrEngine)
        .GetField("_engine", BindingFlags.Instance | BindingFlags.NonPublic);

    private readonly ScannerLab38OcrEngine _fallback;
    private readonly OcrEngine? _engine;
    private readonly ScannerOcrBackendHealthPolicy _actualBackendHealth = new();
    private readonly bool _canTraceExactWinRtBoundary;
    private long _nextBackendCallId;
    private bool _actualSuppressionLogged;

    public DiagnosticScannerLab38OcrEngine(ScannerLab38OcrEngine source)
    {
        _fallback = source ?? throw new ArgumentNullException(nameof(source));
        _engine = EngineField?.GetValue(source) as OcrEngine;
        _canTraceExactWinRtBoundary = !source.IsAvailable || _engine is not null;
        ScannerPerformanceTrace.Mark(
            "ocr-diagnostic-adapter",
            ("sourceAvailable", source.IsAvailable),
            ("exactBoundaryAvailable", _canTraceExactWinRtBoundary),
            ("engineFieldFound", EngineField is not null));
    }

    public bool IsAvailable => _fallback.IsAvailable;
    public string AvailabilityMessage => _fallback.AvailabilityMessage;

    public async Task<string> ReadTextAsync(BitmapSource titleImage, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(titleImage);
        if (!_canTraceExactWinRtBoundary)
            return await _fallback.ReadTextAsync(titleImage, cancellationToken) ?? string.Empty;
        if (_engine is null)
            return string.Empty;

        using var timing = ScannerLatencyTelemetry.Measure(ScannerLatencyTelemetry.OcrNormal);
        var enlargeStarted = Stopwatch.GetTimestamp();
        ScannerPerformanceTrace.Mark(
            "ocr-enlarge-start",
            ("pass", "normal"),
            ("width", titleImage.PixelWidth),
            ("height", titleImage.PixelHeight));
        var enlarged = EnlargeTitle(titleImage);
        ScannerPerformanceTrace.Mark(
            "ocr-enlarge-end",
            ("pass", "normal"),
            ("elapsedMs", FormatMs(enlargeStarted)),
            ("width", enlarged.PixelWidth),
            ("height", enlarged.PixelHeight));
        return await RecognizeLinesAndPairsAsync(enlarged, "normal", 0, cancellationToken);
    }

    public async Task<string> ReadDeepTextAsync(BitmapSource titleImage, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(titleImage);
        if (!_canTraceExactWinRtBoundary)
            return await _fallback.ReadDeepTextAsync(titleImage, cancellationToken) ?? string.Empty;
        if (_engine is null)
            return string.Empty;

        using var timing = ScannerLatencyTelemetry.Measure(ScannerLatencyTelemetry.OcrDeep);
        var enlargeStarted = Stopwatch.GetTimestamp();
        ScannerPerformanceTrace.Mark(
            "ocr-enlarge-start",
            ("pass", "deep"),
            ("width", titleImage.PixelWidth),
            ("height", titleImage.PixelHeight));
        var enlarged = EnlargeTitle(titleImage);
        ScannerPerformanceTrace.Mark(
            "ocr-enlarge-end",
            ("pass", "deep"),
            ("elapsedMs", FormatMs(enlargeStarted)),
            ("width", enlarged.PixelWidth),
            ("height", enlarged.PixelHeight));

        var variants = new BitmapSource[4];
        variants[0] = enlarged;
        for (var mode = 1; mode <= 3; mode++)
        {
            var variantStarted = Stopwatch.GetTimestamp();
            ScannerPerformanceTrace.Mark("ocr-variant-start", ("variant", mode));
            variants[mode] = CreateVariant(enlarged, mode);
            ScannerPerformanceTrace.Mark(
                "ocr-variant-end",
                ("variant", mode),
                ("elapsedMs", FormatMs(variantStarted)),
                ("width", variants[mode].PixelWidth),
                ("height", variants[mode].PixelHeight));
        }

        var results = new List<string>();
        for (var variant = 0; variant < variants.Length; variant++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var text = await RecognizeLinesAndPairsAsync(variants[variant], "deep", variant, cancellationToken);
            if (!string.IsNullOrWhiteSpace(text))
                results.AddRange(text.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        }

        return string.Join(" | ", results.Distinct(StringComparer.Ordinal));
    }

    private static BitmapSource EnlargeTitle(BitmapSource source)
    {
        var requested = source.PixelHeight <= 14
            ? 8.0
            : source.PixelHeight <= 20
                ? 6.0
                : 4.0;
        var maximumDimension = Math.Max(source.PixelWidth, source.PixelHeight);
        var allowed = maximumDimension <= 0
            ? 1.0
            : Math.Max(1.0, Math.Floor(OcrEngine.MaxImageDimension / (double)maximumDimension));
        var scale = Math.Max(1.0, Math.Min(requested, allowed));
        if (scale <= 1.0)
            return source;

        var transformed = new TransformedBitmap(source, new ScaleTransform(scale, scale));
        transformed.Freeze();
        return transformed;
    }

    private static BitmapSource CreateVariant(BitmapSource source, int mode)
    {
        var converted = new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0);
        converted.Freeze();
        var stride = converted.PixelWidth * 4;
        var pixels = new byte[stride * converted.PixelHeight];
        converted.CopyPixels(pixels, stride, 0);

        for (var offset = 0; offset < pixels.Length; offset += 4)
        {
            var b = pixels[offset];
            var g = pixels[offset + 1];
            var r = pixels[offset + 2];
            var gray = (77 * r + 150 * g + 29 * b) >> 8;
            int output = mode switch
            {
                1 => Math.Clamp((int)((gray - 55) * 1.8), 0, 255),
                2 => gray >= 105 ? 255 : 0,
                3 => gray >= 105 ? 0 : 255,
                _ => gray,
            };
            pixels[offset] = (byte)output;
            pixels[offset + 1] = (byte)output;
            pixels[offset + 2] = (byte)output;
            pixels[offset + 3] = 255;
        }

        var result = BitmapSource.Create(
            converted.PixelWidth,
            converted.PixelHeight,
            96,
            96,
            PixelFormats.Bgra32,
            null,
            pixels,
            stride);
        result.Freeze();
        return result;
    }

    private async Task<string> RecognizeLinesAndPairsAsync(
        BitmapSource image,
        string pass,
        int variant,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var now = DateTimeOffset.UtcNow;
        if (!_actualBackendHealth.ShouldAttempt(now))
        {
            if (!_actualSuppressionLogged)
            {
                _actualSuppressionLogged = true;
                ScannerPerformanceTrace.Mark(
                    "ocr-winrt-suppressed",
                    ("pass", pass),
                    ("variant", variant),
                    ("degradedUntilUtc", _actualBackendHealth.DegradedUntilUtc.ToString("O")),
                    ("width", image.PixelWidth),
                    ("height", image.PixelHeight));
            }
            return string.Empty;
        }

        _actualSuppressionLogged = false;
        var backend = await RecognizeAsync(image, pass, variant, cancellationToken);
        var hasUsableText = !string.IsNullOrWhiteSpace(backend.Result.Text);
        var enteredDegraded = _actualBackendHealth.RecordResult(
            DateTimeOffset.UtcNow,
            backend.WinRtElapsed,
            hasUsableText);
        ScannerPerformanceTrace.Mark(
            "ocr-winrt-health-result",
            ("pass", pass),
            ("variant", variant),
            ("elapsedMs", backend.WinRtElapsed.TotalMilliseconds.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)),
            ("hasText", hasUsableText),
            ("enteredDegraded", enteredDegraded),
            ("degradedUntilUtc", enteredDegraded ? _actualBackendHealth.DegradedUntilUtc.ToString("O") : string.Empty));

        var lines = backend.Result.Lines
            .Select(line => line.Text?.Trim() ?? string.Empty)
            .Where(line => line.Length >= 2)
            .ToArray();
        if (lines.Length == 0)
            return string.Empty;

        var candidates = new List<string>(lines.Length * 2);
        candidates.AddRange(lines);
        for (var index = 0; index < lines.Length - 1; index++)
        {
            var pair = $"{lines[index]} {lines[index + 1]}".Trim();
            if (pair.Length >= 3)
                candidates.Add(pair);
        }
        return string.Join(" | ", candidates.Distinct(StringComparer.Ordinal));
    }

    private async Task<BackendRecognition> RecognizeAsync(
        BitmapSource image,
        string pass,
        int variant,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var callId = Interlocked.Increment(ref _nextBackendCallId);

        var conversionStarted = Stopwatch.GetTimestamp();
        ScannerPerformanceTrace.Mark(
            "ocr-bgra-convert-start",
            ("callId", callId),
            ("pass", pass),
            ("variant", variant),
            ("width", image.PixelWidth),
            ("height", image.PixelHeight),
            ("format", image.Format));
        BitmapSource bgra = image.Format == PixelFormats.Bgra32
            ? image
            : new FormatConvertedBitmap(image, PixelFormats.Bgra32, null, 0);
        if (!bgra.IsFrozen && bgra.CanFreeze)
            bgra.Freeze();
        ScannerPerformanceTrace.Mark(
            "ocr-bgra-convert-end",
            ("callId", callId),
            ("elapsedMs", FormatMs(conversionStarted)),
            ("converted", !ReferenceEquals(bgra, image)));

        var copyStarted = Stopwatch.GetTimestamp();
        ScannerPerformanceTrace.Mark("ocr-copy-pixels-start", ("callId", callId));
        var stride = checked(bgra.PixelWidth * 4);
        var pixels = new byte[checked(stride * bgra.PixelHeight)];
        bgra.CopyPixels(pixels, stride, 0);
        ScannerPerformanceTrace.Mark(
            "ocr-copy-pixels-end",
            ("callId", callId),
            ("elapsedMs", FormatMs(copyStarted)),
            ("bytes", pixels.Length));

        var bitmapStarted = Stopwatch.GetTimestamp();
        ScannerPerformanceTrace.Mark("ocr-software-bitmap-start", ("callId", callId));
        var buffer = CryptographicBuffer.CreateFromByteArray(pixels);
        using var softwareBitmap = SoftwareBitmap.CreateCopyFromBuffer(
            buffer,
            BitmapPixelFormat.Bgra8,
            bgra.PixelWidth,
            bgra.PixelHeight,
            BitmapAlphaMode.Premultiplied);
        ScannerPerformanceTrace.Mark(
            "ocr-software-bitmap-end",
            ("callId", callId),
            ("elapsedMs", FormatMs(bitmapStarted)));

        cancellationToken.ThrowIfCancellationRequested();
        var winRtStarted = Stopwatch.GetTimestamp();
        ScannerPerformanceTrace.Mark(
            "ocr-winrt-recognize-start",
            ("callId", callId),
            ("pass", pass),
            ("variant", variant),
            ("width", bgra.PixelWidth),
            ("height", bgra.PixelHeight));
        try
        {
            var result = await _engine!.RecognizeAsync(softwareBitmap);
            var winRtElapsed = Stopwatch.GetElapsedTime(winRtStarted);
            ScannerPerformanceTrace.Mark(
                "ocr-winrt-recognize-end",
                ("callId", callId),
                ("pass", pass),
                ("variant", variant),
                ("elapsedMs", winRtElapsed.TotalMilliseconds.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)),
                ("hasText", !string.IsNullOrWhiteSpace(result.Text)),
                ("textLength", result.Text?.Length ?? 0),
                ("lineCount", result.Lines?.Count ?? 0));
            cancellationToken.ThrowIfCancellationRequested();
            return new BackendRecognition(result, winRtElapsed);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            ScannerPerformanceTrace.Mark(
                "ocr-winrt-recognize-error",
                ("callId", callId),
                ("pass", pass),
                ("variant", variant),
                ("elapsedMs", FormatMs(winRtStarted)),
                ("type", exception.GetType().Name),
                ("message", exception.Message));
            throw;
        }
    }

    private static string FormatMs(long startedTimestamp) =>
        ScannerPerformanceTrace.ElapsedMilliseconds(startedTimestamp)
            .ToString("F2", System.Globalization.CultureInfo.InvariantCulture);

    private readonly record struct BackendRecognition(OcrResult Result, TimeSpan WinRtElapsed);
}
