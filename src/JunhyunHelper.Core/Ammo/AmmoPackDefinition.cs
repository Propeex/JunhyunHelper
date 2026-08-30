namespace JunhyunHelper.Core.Ammo;

/// <summary>
/// Canonical mapping from a Scanner-visible ammunition package to the ammunition item
/// whose pickup value should be evaluated. Authoritative source relationships are always
/// preferred; the fallback flag is retained for diagnostics and future data-quality work.
/// </summary>
public sealed record AmmoPackDefinition(
    string PackItemId,
    string AmmoItemId,
    decimal? Count,
    bool IsNameFallback = false);
