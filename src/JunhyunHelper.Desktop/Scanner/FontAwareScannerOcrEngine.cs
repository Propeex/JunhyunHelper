using System.Windows.Media.Imaging;
using JunhyunHelper.Infrastructure.Scanner;

namespace JunhyunHelper.Desktop.Scanner;

/// <summary>
/// Title-only OCR decorator. Windows OCR remains the primary recognizer, but a semantic
/// success now receives bounded Tarkov-font visual corroboration before it is returned.
/// Failed/corrupted OCR retains the existing targeted/full-catalog recovery path.
/// Font extraction/rendering is optional hardening: if local Tarkov font evidence is
/// unavailable or inconclusive, the already accepted OCR result is preserved.
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
            var text = await _inner.ReadTextAsync(titleImage, cancellationToken);
            return CorroborateAcceptedText(titleImage, text, cancellationToken);
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

            var existing = _catalog.ResolveOcrText(text);
            if (existing.Success)
                return CorroborateAcceptedText(titleImage, text, cancellationToken, existing);

            return RecoverFailedText(titleImage, text, cancellationToken);
        }
        finally
        {
            ExitOperation();
        }
    }

    private string CorroborateAcceptedText(
        BitmapSource titleImage,
        string text,
        CancellationToken cancellationToken,
        ScannerRecognition? resolved = null)
    {
        var existing = resolved ?? _catalog.ResolveOcrText(text);
        if (!existing.Success || string.IsNullOrWhiteSpace(existing.ItemId))
            return text;

        var assessment = _catalog.AssessOcrText(text);
        var catalog = _catalog.GetItemsSnapshot();
        if (catalog.Count == 0)
            return text;

        try
        {
            FontVerificationResult? corroborated = null;
            if (assessment.HasPlausibleVariant)
            {
                corroborated = _fontVerifier.TryRecover(
                    titleImage,
                    assessment.FilteredText,
                    catalog,
                    cancellationToken);
            }

            if (corroborated is not null &&
                corroborated.Recognition.Success &&
                !string.IsNullOrWhiteSpace(corroborated.Recognition.ItemId) &&
                !string.IsNullOrWhiteSpace(corroborated.Recognition.OfficialName))
            {
                return ApplyCorroboration(existing, text, corroborated, "TARGETED");
            }

            // An exact-but-wrong OCR result can make the targeted semantic shortlist
            // point only at the wrong item. If that rendering does not verify, run the
            // existing strict full-catalog visual path once. It is aspect-pruned and
            // bounded; OCR contributes only weak supporting evidence.
            corroborated = _fullVisualMatcher.TryRecover(
                titleImage,
                assessment.FilteredText,
                catalog,
                cancellationToken);
            if (corroborated is null ||
                !corroborated.Recognition.Success ||
                string.IsNullOrWhiteSpace(corroborated.Recognition.ItemId) ||
                string.IsNullOrWhiteSpace(corroborated.Recognition.OfficialName))
            {
                return text;
            }

            return ApplyCorroboration(existing, text, corroborated, "FULL_CATALOG");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            // Visual corroboration must never turn a healthy OCR path into a fatal
            // Scanner failure when the local game font cache cannot be prepared.
            ScannerDiagnosticLog.Write(
                "title-font-corroboration-error",
                null,
                ("type", exception.GetType().Name),
                ("message", exception.Message));
            return text;
        }
    }

    private static string ApplyCorroboration(
        ScannerRecognition existing,
        string originalText,
        FontVerificationResult corroborated,
        string pass)
    {
        var verified = corroborated.Recognition;
        if (string.Equals(existing.ItemId, verified.ItemId, StringComparison.Ordinal))
        {
            ScannerDiagnosticLog.Write(
                "title-font-corroborated",
                null,
                ("pass", pass),
                ("itemId", existing.ItemId),
                ("officialName", existing.OfficialName),
                ("ocrConfidence", existing.Confidence),
                ("visualConfidence", verified.Confidence),
                ("visualScore", corroborated.VisualScore),
                ("fontVariant", corroborated.FontVariant));
            return originalText;
        }

        ScannerDiagnosticLog.Write(
            "title-font-corrected",
            null,
            ("pass", pass),
            ("ocrItemId", existing.ItemId),
            ("ocrName", existing.OfficialName),
            ("visualItemId", verified.ItemId),
            ("visualName", verified.OfficialName),
            ("ocrConfidence", existing.Confidence),
            ("visualConfidence", verified.Confidence),
            ("visualScore", corroborated.VisualScore),
            ("fontVariant", corroborated.FontVariant));

        // Return only the visually verified official name on a strict disagreement.
        // Keeping the exact-but-wrong OCR line alongside it could produce an exact/exact
        // tie in the semantic matcher and defeat the correction.
        return verified.OfficialName ?? originalText;
    }

    private string RecoverFailedText(
        BitmapSource titleImage,
        string text,
        CancellationToken cancellationToken)
    {
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
            if (assessment.HasPlausibleVariant)
            {
                recovered = _fontVerifier.TryRecover(
                    titleImage,
                    assessment.FilteredText,
                    catalog,
                    cancellationToken);
            }

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
        return string.IsNullOrWhiteSpace(text)
            ? official
            : $"{text}\n{official}";
    }

    private void EnterOperation()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        Interlocked.Increment(ref _activeOperations);
        if (!_disposed)
            return;

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
