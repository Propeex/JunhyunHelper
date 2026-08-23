using JunhyunHelper.Core.Scanner;

namespace JunhyunHelper.Infrastructure.Scanner;

/// <summary>
/// Catalog-facing matcher adapter. Core ScannerItemMatcher remains the authority for
/// ordinary exact/fuzzy/one-edit identity matching; reviewed Ground Truth recovery is
/// applied only after that matcher fails LOW_CONFIDENCE and can never replace a success.
/// </summary>
internal sealed class ScannerItemMatcher
{
    private readonly JunhyunHelper.Core.Scanner.ScannerItemMatcher _ordinary = new();
    private ScannerCatalogItem[] _catalog = [];

    public int Count => _ordinary.Count;

    public void ReplaceCatalog(IEnumerable<ScannerCatalogItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        _catalog = items.ToArray();
        _ordinary.ReplaceCatalog(_catalog);
    }

    public ScannerRecognition Resolve(
        string noisyText,
        double minimumConfidence = 0.90,
        double minimumMargin = 0.05)
    {
        var ordinary = _ordinary.Resolve(noisyText, minimumConfidence, minimumMargin);
        return ScannerReviewedGroundTruthRecovery.TryRecover(
            noisyText,
            _catalog,
            ordinary,
            minimumMargin);
    }

    public ScannerRecognition ResolveSingleUnknownGlyph(
        string patternText,
        double minimumMargin = 0.10)
    {
        var ordinary = _ordinary.ResolveSingleUnknownGlyph(patternText, minimumMargin);
        return ScannerUnknownGlyphCatalogRecovery.TryRecover(
            patternText,
            _catalog,
            ordinary,
            minimumMargin);
    }
}
