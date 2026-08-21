using System.Windows.Media.Imaging;
using JunhyunHelper.Infrastructure.Scanner;

namespace JunhyunHelper.Desktop.Scanner;

/// <summary>
/// Title-only OCR decorator. Regular OCR remains the first-stage recognizer. During
/// deep recovery, catalog-driven character validation and Tarkov-font visual matching
/// can supplement failed OCR, including when OCR is empty or corrupted. An already
/// accepted semantic OCR result is never rejected or replaced by this layer.
/// </summary>
public sealed class FontAwareScannerOcrEngine : IScannerDeepOcrEngine, IDisposable
{
    private readonly IScannerOcrEngine _inner;
    private readonly ScannerCatalogService _catalog;
    private readonly TarkovTitleFontProvider _fontProvider;
    private readonly ScannerTitleFontVerifier _fontVerifier;
    private readonly ScannerFullCatalogVisualMatcher _fullVisualMatcher;
    private int _activeOperations;
    private int _resourcesDisposed;
    private volatile bool _disposed;

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
        _fullVisualMatcher = new ScannerFullCatalogVisualMatcher(_fontProvider);
    }

    public bool IsAvailable => _inner.IsAvailable;

    public string AvailabilityMessage => _inner.AvailabilityMessage;

    public async Task<string> ReadTextAsync(
        BitmapSource titleImage,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(titleImage);
        EnterOperation();
        try
        {
            return await _inner.ReadTextAsync(titleImage, cancellationToken);
        }
        finally
        {
            ExitOperation();
        }
    }

    public async Task<string> ReadDeepTextAsync(
        BitmapSource titleImage,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(titleImage);
        EnterOperation();
        try
        {
            var text = _inner is IScannerDeepOcrEngine deepOcr
                ? await deepOcr.ReadDeepTextAsync(titleImage, cancellationToken)
                : await _inner.ReadTextAsync(titleImage, cancellationToken);

            // Do not touch any result that the existing semantic gate already accepts.
            var existing = _catalog.ResolveOcrText(text);
            if (existing.Success)
                return text;

            var assessment = _catalog.AssessOcrText(text);
            if (assessment.IsCorrupted)
            {
                ScannerDiagnosticLog.Write(
                    "ocr-character-quality",
                    null,
                    ("validRatio", assessment.ValidCharacterRatio),
                    ("invalidCharacters", assessment.InvalidCharacterCount),
                    ("hanCharacters", assessment.HanCharacterCount),
                    ("acceptedVariants", assessment.AcceptedVariantCount),
                    ("totalVariants", assessment.TotalVariantCount));
            }

            var catalog = _catalog.GetItemsSnapshot();
            FontVerificationResult? recovered = null;
            try
            {
                // Fast recovery first: when at least one plausible OCR variant survived
                // catalog character validation, use it to narrow the visual shortlist.
                if (assessment.HasPlausibleVariant)
                {
                    recovered = _fontVerifier.TryRecover(
                        titleImage,
                        assessment.FilteredText,
                        catalog,
                        cancellationToken);
                }

                // If OCR could not narrow a trustworthy candidate set, search the complete
                // official catalog by rendered title shape. OCR contributes only weak
                // supporting evidence in this fallback and may be completely empty.
                recovered ??= _fullVisualMatcher.TryRecover(
                    titleImage,
                    assessment.FilteredText,
                    catalog,
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

            var official = recovered.Recognition.OfficialName;
            // ScannerItemMatcher considers individual lines/variants independently. Raw OCR
            // remains available for diagnostics, while the exact visually-verified official
            // name provides the semantic identity variant. If OCR was empty, return only the
            // verified official name so recognition can succeed without OCR text.
            return string.IsNullOrWhiteSpace(text)
                ? official
                : $"{text}\n{official}";
        }
        finally
        {
            ExitOperation();
        }
    }

    private void EnterOperation()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        Interlocked.Increment(ref _activeOperations);
        if (!_disposed)
            return;

        // Dispose may race the tiny window between the first check and increment. Do
        // not let a new operation enter after disposal has been requested.
        ExitOperation();
        throw new ObjectDisposedException(nameof(FontAwareScannerOcrEngine));
    }

    private void ExitOperation()
    {
        if (Interlocked.Decrement(ref _activeOperations) == 0 && _disposed)
            DisposeResources();
    }

    private void DisposeResources()
    {
        if (Interlocked.Exchange(ref _resourcesDisposed, 1) != 0)
            return;
        _fullVisualMatcher.Dispose();
        _fontVerifier.Dispose();
        _fontProvider.Dispose();
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        if (Volatile.Read(ref _activeOperations) == 0)
            DisposeResources();
        GC.SuppressFinalize(this);
    }
}
