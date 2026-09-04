using System.Collections.Concurrent;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using JunhyunHelper.Core.Content;
using SkiaSharp;

namespace JunhyunHelper.Desktop.Services;

public sealed record ImagePrefetchProgress(int Completed, int Total);

public sealed class ImageCacheService
{
    private const int MaxImageBytes = 8 * 1024 * 1024;
    private const int MaxImageDimension = 4096;

    private readonly HttpClient _httpClient;
    private readonly string _cacheDirectory;
    private readonly SemaphoreSlim _downloads = new(6, 6);
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _cachePathGates =
        new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, WeakReference<ImageSource>> _decodedImages =
        new(StringComparer.Ordinal);

    public ImageCacheService(HttpClient httpClient, string rootDirectory)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        ArgumentException.ThrowIfNullOrWhiteSpace(rootDirectory);

        _cacheDirectory = Path.Combine(rootDirectory, "image-cache");
        Directory.CreateDirectory(_cacheDirectory);
    }

    public async Task<ImageSource?> LoadAsync(
        string stableId,
        string? sourceUrl,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetSource(stableId, sourceUrl, out var sourceUri, out var path))
            return null;

        try
        {
            if (TryGetDecodedImage(path, out var memoryCached))
                return memoryCached;

            var cached = TryLoadLocalImage(path);
            if (cached is not null)
            {
                RememberDecodedImage(path, cached);
                return cached;
            }

            await EnsureCachedAsync(stableId, sourceUrl, cancellationToken);
            cached = TryLoadLocalImage(path);
            if (cached is not null)
                RememberDecodedImage(path, cached);
            return cached;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return null;
        }
    }

    public async Task PrefetchAsync(
        GameContentCatalog content,
        IProgress<ImagePrefetchProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        // Scanner can match any item in the official catalog, not only items currently
        // referenced by quests/hideout/ammo. Cache every canonical item icon during the
        // explicit Game Content update so scan-time presentation remains network-free.
        var entries = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var item in content.Items)
        {
            if (!string.IsNullOrWhiteSpace(item.Id) && !string.IsNullOrWhiteSpace(item.IconUrl))
                entries[$"item-{item.Id}"] = item.IconUrl;
        }

        foreach (var station in content.HideoutStations)
        {
            if (!string.IsNullOrWhiteSpace(station.ImageUrl))
                entries[$"hideout-{station.Id}"] = station.ImageUrl;
        }

        var total = entries.Count;
        if (total == 0)
        {
            progress?.Report(new ImagePrefetchProgress(0, 0));
            return;
        }

        var completed = 0;
        await Parallel.ForEachAsync(
            entries,
            new ParallelOptions
            {
                MaxDegreeOfParallelism = 6,
                CancellationToken = cancellationToken,
            },
            async (entry, token) =>
            {
                try
                {
                    await EnsureCachedAsync(entry.Key, entry.Value, token);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch
                {
                    // Presentation assets are optional. One failed image cannot invalidate Game Content.
                }
                finally
                {
                    var current = Interlocked.Increment(ref completed);
                    progress?.Report(new ImagePrefetchProgress(current, total));
                }
            });
    }

    private async Task EnsureCachedAsync(
        string stableId,
        string? sourceUrl,
        CancellationToken cancellationToken)
    {
        if (!TryGetSource(stableId, sourceUrl, out var sourceUri, out var path))
            return;
        if (File.Exists(path))
            return;

        // Several product pages can request the same canonical item icon at once.
        // Serialize by the final URL-hashed cache path before consuming a global
        // download slot so duplicates wait for the first request instead of issuing
        // redundant network/PNG normalization work.
        var pathGate = _cachePathGates.GetOrAdd(
            path,
            static _ => new SemaphoreSlim(1, 1));
        await pathGate.WaitAsync(cancellationToken);
        try
        {
            if (File.Exists(path))
                return;

            await _downloads.WaitAsync(cancellationToken);
            try
            {
                if (File.Exists(path))
                    return;
                await DownloadAndNormalizeAsync(sourceUri, path, cancellationToken);
            }
            finally
            {
                _downloads.Release();
            }
        }
        finally
        {
            pathGate.Release();
            _cachePathGates.TryRemove(path, out _);
        }
    }

    private bool TryGetDecodedImage(string path, out ImageSource image)
    {
        image = null!;
        if (!_decodedImages.TryGetValue(path, out var reference))
            return false;
        if (reference.TryGetTarget(out image))
            return true;

        _decodedImages.TryRemove(path, out _);
        return false;
    }

    private void RememberDecodedImage(string path, ImageSource image) =>
        _decodedImages[path] = new WeakReference<ImageSource>(image);

    private bool TryGetSource(
        string stableId,
        string? sourceUrl,
        out Uri sourceUri,
        out string path)
    {
        sourceUri = null!;
        path = string.Empty;
        if (string.IsNullOrWhiteSpace(stableId) ||
            string.IsNullOrWhiteSpace(sourceUrl) ||
            !Uri.TryCreate(sourceUrl, UriKind.Absolute, out var parsed) ||
            parsed.Scheme is not ("http" or "https"))
        {
            return false;
        }

        sourceUri = parsed;
        path = CachePath(stableId, sourceUrl);
        return true;
    }

    private async Task DownloadAndNormalizeAsync(
        Uri sourceUri,
        string destination,
        CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync(
            sourceUri,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        response.EnsureSuccessStatusCode();

        if (response.Content.Headers.ContentLength is > MaxImageBytes)
            throw new InvalidDataException("Image is larger than the JunhyunHelper cache limit.");

        var rawTemporary = $"{destination}.{Guid.NewGuid():N}.raw";
        var pngTemporary = $"{destination}.{Guid.NewGuid():N}.png.tmp";
        try
        {
            await using (var input = await response.Content.ReadAsStreamAsync(cancellationToken))
            await using (var output = new FileStream(
                             rawTemporary,
                             FileMode.CreateNew,
                             FileAccess.Write,
                             FileShare.None,
                             bufferSize: 81920,
                             useAsync: true))
            {
                var buffer = new byte[81920];
                var total = 0;
                while (true)
                {
                    var read = await input.ReadAsync(buffer, cancellationToken);
                    if (read == 0)
                        break;

                    total += read;
                    if (total > MaxImageBytes)
                        throw new InvalidDataException("Image is larger than the JunhyunHelper cache limit.");

                    await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                }

                await output.FlushAsync(cancellationToken);
            }

            cancellationToken.ThrowIfCancellationRequested();
            NormalizeToPng(rawTemporary, pngTemporary);
            cancellationToken.ThrowIfCancellationRequested();
            File.Move(pngTemporary, destination, overwrite: true);
        }
        finally
        {
            TryDelete(rawTemporary);
            TryDelete(pngTemporary);
        }
    }

    private static void NormalizeToPng(string source, string destination)
    {
        using var stream = File.OpenRead(source);
        using var codec = SKCodec.Create(stream)
                          ?? throw new InvalidDataException("Downloaded payload is not a supported image.");

        if (codec.Info.Width <= 0 || codec.Info.Height <= 0 ||
            codec.Info.Width > MaxImageDimension || codec.Info.Height > MaxImageDimension)
        {
            throw new InvalidDataException("Image dimensions are outside the JunhyunHelper cache limits.");
        }

        var imageInfo = new SKImageInfo(
            codec.Info.Width,
            codec.Info.Height,
            SKColorType.Bgra8888,
            SKAlphaType.Premul);
        using var bitmap = new SKBitmap(imageInfo);
        var result = codec.GetPixels(imageInfo, bitmap.GetPixels());
        if (result is not (SKCodecResult.Success or SKCodecResult.IncompleteInput))
            throw new InvalidDataException($"Image decode failed: {result}");

        using var image = SKImage.FromBitmap(bitmap);
        using var encoded = image.Encode(SKEncodedImageFormat.Png, 100)
                            ?? throw new InvalidDataException("Image PNG encoding failed.");
        using var output = File.Create(destination);
        encoded.SaveTo(output);
    }

    private static ImageSource? TryLoadLocalImage(string path)
    {
        if (!File.Exists(path))
            return null;

        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = new Uri(path, UriKind.Absolute);
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }
        catch
        {
            TryDelete(path);
            return null;
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
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
