using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SkiaSharp;

namespace JunhyunHelper.Desktop.Services;

public sealed class ImageCacheService
{
    private const int MaxImageBytes = 8 * 1024 * 1024;
    private const int MaxImageDimension = 4096;

    private readonly HttpClient _httpClient;
    private readonly string _cacheDirectory;
    private readonly SemaphoreSlim _downloads = new(6, 6);

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
        if (string.IsNullOrWhiteSpace(stableId) ||
            string.IsNullOrWhiteSpace(sourceUrl) ||
            !Uri.TryCreate(sourceUrl, UriKind.Absolute, out var sourceUri) ||
            sourceUri.Scheme is not ("http" or "https"))
        {
            return null;
        }

        try
        {
            var path = CachePath(stableId, sourceUrl);
            var cached = TryLoadLocalImage(path);
            if (cached is not null)
                return cached;

            await _downloads.WaitAsync(cancellationToken);
            try
            {
                cached = TryLoadLocalImage(path);
                if (cached is not null)
                    return cached;

                await DownloadAndNormalizeAsync(sourceUri, path, cancellationToken);
            }
            finally
            {
                _downloads.Release();
            }

            return TryLoadLocalImage(path);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            // Images are presentation-only. A failed image must never break Game Content/User Progress.
            return null;
        }
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
            // A later load may retry. Image failures stay non-fatal.
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

        // Canonical source assets may be WebP. Cache a normalized PNG so WPF decoding
        // does not depend on optional Windows WebP codecs.
        return Path.Combine(_cacheDirectory, $"{safeId}-{hash}.png");
    }
}
