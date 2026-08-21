using System.Security.Cryptography;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace JunhyunHelper.Desktop.Scanner;

/// <summary>
/// Read-only adapter for the existing JunhyunHelper image-cache file contract. Scanner
/// never calls the HTTP-backed ImageCacheService while an item is being recognized.
/// Successfully decoded frozen images are kept in a small process-local cache so live
/// presentation refreshes do not repeatedly reopen and decode the same PNG.
/// </summary>
public sealed class ScannerLocalIconService
{
    private readonly string _cacheDirectory;
    private readonly object _memoryGate = new();
    private readonly Dictionary<string, ImageSource> _memoryCache = new(StringComparer.Ordinal);

    public ScannerLocalIconService(string rootDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootDirectory);
        _cacheDirectory = Path.Combine(Path.GetFullPath(rootDirectory), "image-cache");
    }

    public ImageSource? Load(string stableId, string? sourceUrl)
    {
        if (string.IsNullOrWhiteSpace(stableId) || string.IsNullOrWhiteSpace(sourceUrl))
            return null;
        if (!Uri.TryCreate(sourceUrl, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))
            return null;

        var cacheKey = stableId.Trim() + "\n" + sourceUrl.Trim();
        lock (_memoryGate)
        {
            if (_memoryCache.TryGetValue(cacheKey, out var cached))
                return cached;
        }

        var path = CachePath(stableId, sourceUrl);
        if (!File.Exists(path))
            return null;

        try
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.UriSource = new Uri(path, UriKind.Absolute);
            image.EndInit();
            image.Freeze();

            lock (_memoryGate)
                _memoryCache[cacheKey] = image;
            return image;
        }
        catch
        {
            return null;
        }
    }

    private string CachePath(string stableId, string sourceUrl)
    {
        var invalidCharacters = Path.GetInvalidFileNameChars();
        var safeId = new string(stableId
            .Select(character => invalidCharacters.Contains(character) ? '_' : character)
            .ToArray());
        if (safeId.Length > 80)
            safeId = safeId[..80];

        var hash = Convert.ToHexString(
                SHA256.HashData(Encoding.UTF8.GetBytes(sourceUrl)))
            .ToLowerInvariant()[..16];
        return Path.Combine(_cacheDirectory, $"{safeId}-{hash}.png");
    }
}
