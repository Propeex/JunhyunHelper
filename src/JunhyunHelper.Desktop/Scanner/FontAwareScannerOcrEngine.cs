using System.Windows.Media.Imaging;
using JunhyunHelper.Infrastructure.Scanner;

namespace JunhyunHelper.Desktop.Scanner;

/// <summary>
/// Title-only OCR decorator. Regular OCR is passed through unchanged. During the
/// existing deep-OCR fallback, a failed semantic result may be supplemented with an
/// official item name only when Tarkov-font visual verification passes conservative
/// thresholds. This preserves all existing OCR successes and fail-closed behavior.
/// </summary>
public sealed class FontAwareScannerOcrEngine : IScannerDeepOcrEngine, IDisposable
{
    private readonly IScannerOcrEngine _inner;
    private readonly ScannerCatalogService _catalog;
    private readonly TarkovTitleFontProvider _fontProvider;
    private readonly ScannerTitleFontVerifier _fontVerifier;
    private bool _disposed;

    public FontAwareScannerOcrEngine(
        IScannerOcrEngine inner,
        ScannerCatalogService catalog,
        string rootDirectory)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        ArgumentException.ThrowIfNullOrWhiteSpace(rootDirectory);

        _fontProvider = new TarkovTitleFontProvider(rootDirectory);
        _fontVerifier = new ScannerTitleFontVerifier(_fontProvider);
    }

    public bool IsAvailable => _inner.IsAvailable;

    public string AvailabilityMessage => _inner.AvailabilityMessage;

    public Task<string> ReadTextAsync(
        BitmapSource titleImage,
        CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _inner.ReadTextAsync(titleImage, cancellationToken);
    }

    public async Task<string> ReadDeepTextAsync(
        BitmapSource titleImage,
        CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(titleImage);

        var text = _inner is IScannerDeepOcrEngine deepOcr
            ? await deepOcr.ReadDeepTextAsync(titleImage, cancellationToken)
            : await _inner.ReadTextAsync(titleImage, cancellationToken);

        // Do not touch any result that the existing semantic gate already accepts.
        // The new font path is recovery-only, never a replacement for a success.
        var existing = _catalog.ResolveOcrText(text);
        if (existing.Success || string.IsNullOrWhiteSpace(text))
            return text;

        FontVerificationResult? recovered;
        try
        {
            recovered = _fontVerifier.TryRecover(
                titleImage,
                text,
                _catalog.GetItemsSnapshot(),
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            // Font-aware recovery is optional hardening. Any extraction/rendering
            // problem must degrade to the already-proven OCR-only path.
            ScannerDiagnosticLog.Write(
                "title-font-recovery-error",
                null,
                ("type", exception.GetType().Name),
                ("message", exception.Message));
            return text;
        }

        if (recovered is null ||
            !recovered.Recognition.Success ||
            string.IsNullOrWhiteSpace(recovered.Recognition.OfficialName))
        {
            return text;
        }

        // ScannerItemMatcher considers individual OCR lines as independent variants.
        // Keeping the raw deep OCR as the first line preserves diagnostics while the
        // verified official name supplies an exact second-line semantic variant.
        return $"{text}\n{recovered.Recognition.OfficialName}";
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _fontVerifier.Dispose();
        _fontProvider.Dispose();
        GC.SuppressFinalize(this);
    }
}
