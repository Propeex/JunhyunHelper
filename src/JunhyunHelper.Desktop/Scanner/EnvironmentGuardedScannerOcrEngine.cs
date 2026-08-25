using System.Diagnostics;
using System.Windows.Media.Imaging;
using JunhyunHelper.Core.Scanner;

namespace JunhyunHelper.Desktop.Scanner;

/// <summary>
/// Runtime circuit breaker around the OS OCR backend. Some unpackaged Windows desktop
/// environments can return an empty OCR result only after a long delay. The Scanner has
/// multiple conservative retries, so one degraded backend call can otherwise multiply
/// into many serialized delays. After one slow + empty result, suppress further OS OCR
/// calls for a short cooldown and let the existing strict Tarkov-font recovery path make
/// the next decision. Successful OCR is never suppressed or downgraded.
/// </summary>
internal sealed class EnvironmentGuardedScannerOcrEngine : IScannerDeepOcrEngine
{
    private readonly IScannerOcrEngine _inner;
    private readonly ScannerOcrBackendHealthPolicy _health = new();
    private bool _suppressionLogged;

    public EnvironmentGuardedScannerOcrEngine(IScannerOcrEngine inner)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        ScannerDiagnosticLog.Write(
            "ocr-backend-environment",
            null,
            ("backend", inner.GetType().Name),
            ("available", inner.IsAvailable),
            ("availability", inner.AvailabilityMessage),
            ("os", Environment.OSVersion.VersionString),
            ("processArchitecture", System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture));
    }

    public bool IsAvailable => _inner.IsAvailable;
    public string AvailabilityMessage => _inner.AvailabilityMessage;

    public Task<string> ReadTextAsync(BitmapSource titleImage, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(titleImage);
        return ReadGuardedAsync(titleImage, deep: false, cancellationToken);
    }

    public Task<string> ReadDeepTextAsync(BitmapSource titleImage, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(titleImage);
        return ReadGuardedAsync(titleImage, deep: true, cancellationToken);
    }

    private async Task<string> ReadGuardedAsync(
        BitmapSource titleImage,
        bool deep,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var now = DateTimeOffset.UtcNow;
        if (!_health.ShouldAttempt(now))
        {
            if (!_suppressionLogged)
            {
                _suppressionLogged = true;
                ScannerDiagnosticLog.Write(
                    "ocr-backend-suppressed",
                    null,
                    ("pass", deep ? "deep" : "normal"),
                    ("degradedUntilUtc", _health.DegradedUntilUtc.ToString("O")),
                    ("width", titleImage.PixelWidth),
                    ("height", titleImage.PixelHeight));
            }
            return string.Empty;
        }

        _suppressionLogged = false;
        var started = Stopwatch.GetTimestamp();
        var text = deep && _inner is IScannerDeepOcrEngine deepEngine
            ? await deepEngine.ReadDeepTextAsync(titleImage, cancellationToken)
            : await _inner.ReadTextAsync(titleImage, cancellationToken);
        var elapsed = Stopwatch.GetElapsedTime(started);
        var hasUsableText = !string.IsNullOrWhiteSpace(text);
        var enteredDegraded = _health.RecordResult(DateTimeOffset.UtcNow, elapsed, hasUsableText);

        if (enteredDegraded || elapsed >= ScannerOcrBackendHealthPolicy.DefaultSlowEmptyThreshold)
        {
            ScannerDiagnosticLog.Write(
                enteredDegraded ? "ocr-backend-degraded" : "ocr-backend-slow",
                null,
                ("pass", deep ? "deep" : "normal"),
                ("elapsedMs", elapsed.TotalMilliseconds.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)),
                ("hasText", hasUsableText),
                ("width", titleImage.PixelWidth),
                ("height", titleImage.PixelHeight),
                ("degradedUntilUtc", _health.DegradedUntilUtc == DateTimeOffset.MinValue
                    ? string.Empty
                    : _health.DegradedUntilUtc.ToString("O")));
        }

        return text;
    }
}
