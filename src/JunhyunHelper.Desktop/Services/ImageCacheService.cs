using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace JunhyunHelper.Desktop.Services;

public sealed class ImageCacheService
{
    private const int MaxImageBytes = 8 * 1024 * 1024;

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
            if (!File.Exists(path))
            {
                await _downloads.WaitAsync(cancellationToken);
                try
                {
                    if (!File.Exists(path))
                        await DownloadAsync(sourceUri, path, cancellationToken);
                }
                finally
                {
                    _downloads.Release();
                }
            }

            return LoadLocalImage(path);
        }
        catch (Exception exception) when (
            exception is HttpRequestException or IOException or UnauthorizedAccessException or
            NotSupportedException or System.Runtime.InteropServices.ExternalException)
        {
            return null;
        }
    }

    private async Task DownloadAsync(Uri sourceUri, string destination, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync(
            sourceUri,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        response.EnsureSuccessStatusCode();

        if (response.Content.Headers.ContentLength is > MaxImageBytes)
            throw new InvalidDataException("Image is larger than the JunhyunHelper cache limit.");

        var temporary = $"{destination}.{Guid.NewGuid():N}.tmp";
        try
        {
            await using var input = await response.Content.ReadAsStreamAsync(cancellationToken);
            await using var output = new FileStream(
                temporary,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 81920,
                useAsync: true);

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
            File.Move(temporary, destination, overwrite: false);
        }
        finally
        {
            if (File.Exists(temporary))
                File.Delete(temporary);
        }
    }

    private static ImageSource LoadLocalImage(string path)
    {
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.UriSource = new Uri(path, UriKind.Absolute);
        bitmap.EndInit();
        bitmap.Freeze();
        return bitmap;
    }

    private string CachePath(string stableId, string sourceUrl)
    {
        var safeId = new string(stableId
            .Select(character => Path.GetInvalidFileNameChars().Contains(character) ? '_' : character)
            .ToArray());
        if (safeId.Length > 80)
            safeId = safeId[..80];

        var hash = Convert.ToHexString(
                SHA256.HashData(Encoding.UTF8.GetBytes(sourceUrl)))
            .ToLowerInvariant()[..16];

        return Path.Combine(_cacheDirectory, $"{safeId}-{hash}.img");
    }
}
