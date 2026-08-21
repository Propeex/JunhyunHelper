using System.Buffers.Binary;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using SkiaSharp;

namespace JunhyunHelper.Desktop.Scanner;

/// <summary>
/// Locates the exact UI fonts shipped with the user's Tarkov installation without
/// redistributing game font binaries. The resources.assets file is read-only and only
/// the embedded SFNT font payloads required by Scanner are copied into the app-local
/// scanner cache.
/// </summary>
public sealed class TarkovTitleFontProvider : IDisposable
{
    private const long MaxResourcesAssetBytes = 1_073_741_824;
    private const int MaxFontPayloadBytes = 67_108_864;
    private const int ScanBufferBytes = 1024 * 1024;
    private const int FontCacheSchemaVersion = 1;
    private static readonly TimeSpan RetryInterval = TimeSpan.FromSeconds(5);

    private readonly object _gate = new();
    private readonly string _cacheDirectory;
    private readonly string _manifestPath;

    private TarkovTitleFonts? _fonts;
    private SourceStamp? _loadedSourceStamp;
    private DateTimeOffset _nextAttemptUtc = DateTimeOffset.MinValue;
    private bool _disposed;

    public TarkovTitleFontProvider(string rootDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootDirectory);
        _cacheDirectory = Path.Combine(Path.GetFullPath(rootDirectory), "scanner", "fonts");
        _manifestPath = Path.Combine(_cacheDirectory, "font-cache.json");
    }

    public bool TryGetFonts(out TarkovTitleFonts fonts)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        lock (_gate)
        {
            var resourcesPath = FindResourcesAssets();
            var sourceStamp = TryGetSourceStamp(resourcesPath);
            if (_fonts is not null && IsLoadedGenerationCurrent(sourceStamp))
            {
                fonts = _fonts;
                return true;
            }

            if (_fonts is not null)
            {
                _fonts.Dispose();
                _fonts = null;
                _loadedSourceStamp = null;
                ScannerDiagnosticLog.Write("title-font-source-changed");
            }

            if (DateTimeOffset.UtcNow < _nextAttemptUtc)
            {
                fonts = null!;
                return false;
            }
            _nextAttemptUtc = DateTimeOffset.UtcNow + RetryInterval;

            if (TryLoadCache(resourcesPath, sourceStamp, out var cached))
            {
                _fonts = cached;
                _loadedSourceStamp = sourceStamp;
                fonts = cached;
                return true;
            }

            if (resourcesPath is null || sourceStamp is null || !TryExtractRequiredFonts(resourcesPath, sourceStamp.Value))
            {
                fonts = null!;
                return false;
            }

            if (!TryLoadCache(resourcesPath, sourceStamp, out cached))
            {
                fonts = null!;
                return false;
            }

            _fonts = cached;
            _loadedSourceStamp = sourceStamp;
            fonts = cached;
            return true;
        }
    }

    private bool IsLoadedGenerationCurrent(SourceStamp? currentSource)
    {
        if (_fonts is null)
            return false;

        // A cached generation remains useful when Tarkov is not currently running.
        // Once a live resources.assets path is visible, however, its identity must match
        // the generation that was loaded into this process before we keep using it.
        if (currentSource is null)
            return true;
        if (_loadedSourceStamp is { } loaded)
            return loaded.Equals(currentSource.Value);

        return CacheMatchesSource(currentSource.Value);
    }

    private bool TryLoadCache(
        string? resourcesPath,
        SourceStamp? sourceStamp,
        out TarkovTitleFonts fonts)
    {
        fonts = null!;
        try
        {
            var regularPath = Path.Combine(_cacheDirectory, "Bender-Regular.otf");
            var boldPath = Path.Combine(_cacheDirectory, "Bender-Bold.otf");
            var koreanPath = Path.Combine(_cacheDirectory, "NotoSans-CJK-KR-Regular.otf");

            if (!File.Exists(koreanPath) || (!File.Exists(regularPath) && !File.Exists(boldPath)))
                return false;

            if (sourceStamp is { } liveSource && !CacheMatchesSource(liveSource))
            {
                // Legacy v1.2.0 caches have no manifest. Keep them readable when their
                // files are at least as new as the current source, but force extraction
                // whenever Tarkov's resources.assets is newer.
                var manifest = TryReadManifest();
                if (manifest is not null || SourceIsNewerThanCache(liveSource, regularPath, boldPath, koreanPath))
                    return false;
            }
            else if (resourcesPath is not null && sourceStamp is null && File.Exists(resourcesPath))
            {
                return false;
            }

            var regular = File.Exists(regularPath) ? SKTypeface.FromFile(regularPath) : null;
            var bold = File.Exists(boldPath) ? SKTypeface.FromFile(boldPath) : null;
            var korean = SKTypeface.FromFile(koreanPath);
            if (korean is null || (regular is null && bold is null))
            {
                regular?.Dispose();
                bold?.Dispose();
                korean?.Dispose();
                return false;
            }

            if (!IsBender(regular) && !IsBender(bold))
            {
                regular?.Dispose();
                bold?.Dispose();
                korean.Dispose();
                return false;
            }

            if (!IsKoreanNoto(korean))
            {
                regular?.Dispose();
                bold?.Dispose();
                korean.Dispose();
                return false;
            }

            var generationKey = ComputeCacheGenerationKey(regularPath, boldPath, koreanPath);
            if (string.IsNullOrWhiteSpace(generationKey))
            {
                regular?.Dispose();
                bold?.Dispose();
                korean.Dispose();
                return false;
            }

            var manifest = TryReadManifest();
            if (manifest is not null &&
                (!string.Equals(manifest.GenerationKey, generationKey, StringComparison.Ordinal) ||
                 manifest.SchemaVersion != FontCacheSchemaVersion))
            {
                regular?.Dispose();
                bold?.Dispose();
                korean.Dispose();
                return false;
            }

            fonts = new TarkovTitleFonts(regular, bold, korean, generationKey);
            return true;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            ScannerDiagnosticLog.Write(
                "title-font-cache-load-failed",
                null,
                ("type", exception.GetType().Name),
                ("message", exception.Message));
            return false;
        }
    }

    private bool TryExtractRequiredFonts(string resourcesPath, SourceStamp sourceStamp)
    {
        try
        {
            if (sourceStamp.Length is <= 0 or > MaxResourcesAssetBytes)
                return false;

            byte[]? regular = null;
            byte[]? bold = null;
            byte[]? korean = null;

            foreach (var payload in EnumerateSfntPayloads(resourcesPath, sourceStamp.Length))
            {
                using var data = SKData.CreateCopy(payload);
                using var typeface = SKTypeface.FromData(data);
                if (typeface is null)
                    continue;

                if (IsBender(typeface))
                {
                    if (typeface.IsBold)
                        bold ??= payload;
                    else if (!typeface.IsItalic)
                        regular ??= payload;
                }
                else if (IsKoreanNoto(typeface) && !typeface.IsBold && !typeface.IsItalic)
                {
                    korean ??= payload;
                }

                if (korean is not null && regular is not null && bold is not null)
                    break;
            }

            if (korean is null || (regular is null && bold is null))
            {
                ScannerDiagnosticLog.Write(
                    "title-font-extract-missing",
                    null,
                    ("resources", resourcesPath),
                    ("benderRegular", regular is not null),
                    ("benderBold", bold is not null),
                    ("notoKorean", korean is not null));
                return false;
            }

            Directory.CreateDirectory(_cacheDirectory);
            var regularPath = Path.Combine(_cacheDirectory, "Bender-Regular.otf");
            var boldPath = Path.Combine(_cacheDirectory, "Bender-Bold.otf");
            var koreanPath = Path.Combine(_cacheDirectory, "NotoSans-CJK-KR-Regular.otf");

            // Do not leave an old variant from an earlier Tarkov generation next to a
            // newly extracted set. The manifest is committed last, so an interrupted
            // update is rejected on the next load rather than silently mixing fonts.
            if (regular is not null)
                WriteAtomically(regularPath, regular);
            else
                TryDelete(regularPath);
            if (bold is not null)
                WriteAtomically(boldPath, bold);
            else
                TryDelete(boldPath);
            WriteAtomically(koreanPath, korean);

            var generationKey = ComputeCacheGenerationKey(regularPath, boldPath, koreanPath);
            if (string.IsNullOrWhiteSpace(generationKey))
                return false;

            WriteManifestAtomically(new FontCacheManifest(
                FontCacheSchemaVersion,
                sourceStamp.Path,
                sourceStamp.Length,
                sourceStamp.LastWriteUtcTicks,
                generationKey));

            ScannerDiagnosticLog.Write(
                "title-font-extract-ready",
                null,
                ("resources", resourcesPath),
                ("benderRegular", regular is not null),
                ("benderBold", bold is not null),
                ("notoKorean", true),
                ("generation", generationKey[..Math.Min(12, generationKey.Length)]));
            return true;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            ScannerDiagnosticLog.Write(
                "title-font-extract-failed",
                null,
                ("type", exception.GetType().Name),
                ("message", exception.Message));
            return false;
        }
    }

    /// <summary>
    /// Scans resources.assets in a bounded streaming buffer. v1.2.0 loaded the whole
    /// file into one managed byte array; streaming keeps the visual-recovery fallback
    /// from creating a large transient allocation when Tarkov's asset file grows.
    /// </summary>
    private static IEnumerable<byte[]> EnumerateSfntPayloads(string path, long fileLength)
    {
        var buffer = new byte[ScanBufferBytes + 11];
        using var scan = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete,
            ScanBufferBytes,
            FileOptions.SequentialScan);
        using var random = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete,
            64 * 1024,
            FileOptions.RandomAccess);

        var carry = 0;
        long nextReadOffset = 0;
        while (nextReadOffset < fileLength)
        {
            var requested = (int)Math.Min(ScanBufferBytes, fileLength - nextReadOffset);
            var read = scan.Read(buffer, carry, requested);
            if (read <= 0)
                yield break;

            var total = carry + read;
            var bufferStart = nextReadOffset - carry;
            for (var index = 0; index <= total - 12; index++)
            {
                if (!LooksLikeSfnt(buffer, index))
                    continue;
                var numTables = BinaryPrimitives.ReadUInt16BigEndian(buffer.AsSpan(index + 4, 2));
                if (numTables is < 1 or > 128)
                    continue;

                var absoluteOffset = bufferStart + index;
                if (absoluteOffset < 0 || !TryReadSfntPayload(random, absoluteOffset, fileLength, out var payload))
                    continue;
                yield return payload;
            }

            nextReadOffset += read;
            carry = Math.Min(11, total);
            if (carry > 0)
                Buffer.BlockCopy(buffer, total - carry, buffer, 0, carry);
        }
    }

    private static bool TryReadSfntPayload(
        FileStream stream,
        long start,
        long fileLength,
        out byte[] payload)
    {
        payload = [];
        try
        {
            if (start < 0 || start > fileLength - 12)
                return false;

            Span<byte> header = stackalloc byte[12];
            stream.Position = start;
            stream.ReadExactly(header);
            if (!LooksLikeSfnt(header, 0))
                return false;

            var numTables = BinaryPrimitives.ReadUInt16BigEndian(header.Slice(4, 2));
            if (numTables is < 1 or > 128)
                return false;
            var directoryLength = 12 + numTables * 16;
            if (start > fileLength - directoryLength)
                return false;

            var directory = new byte[directoryLength];
            stream.Position = start;
            stream.ReadExactly(directory);
            if (!TryGetSfntLength(directory, start: 0, fileLength - start, out var length) ||
                length > MaxFontPayloadBytes)
            {
                return false;
            }

            payload = new byte[length];
            stream.Position = start;
            stream.ReadExactly(payload);
            return true;
        }
        catch (Exception exception) when (
            exception is IOException or EndOfStreamException or ArgumentOutOfRangeException)
        {
            payload = [];
            return false;
        }
    }

    internal static bool TryGetSfntLength(ReadOnlySpan<byte> source, int start, out int length) =>
        TryGetSfntLength(source, start, source.Length - start, out length);

    private static bool TryGetSfntLength(
        ReadOnlySpan<byte> source,
        int start,
        long availableLength,
        out int length)
    {
        length = 0;
        if (start < 0 || start > source.Length - 12 || !LooksLikeSfnt(source, start))
            return false;

        var numTables = BinaryPrimitives.ReadUInt16BigEndian(source.Slice(start + 4, 2));
        if (numTables is < 1 or > 128)
            return false;

        var directoryLength = 12 + numTables * 16;
        if (start > source.Length - directoryLength)
            return false;

        long maximumEnd = directoryLength;
        for (var index = 0; index < numTables; index++)
        {
            var record = start + 12 + index * 16;
            var tableOffset = BinaryPrimitives.ReadUInt32BigEndian(source.Slice(record + 8, 4));
            var tableLength = BinaryPrimitives.ReadUInt32BigEndian(source.Slice(record + 12, 4));
            var tableEnd = (long)tableOffset + tableLength;
            if (tableOffset < directoryLength || tableLength == 0 || tableEnd > availableLength)
                return false;
            maximumEnd = Math.Max(maximumEnd, tableEnd);
        }

        if (maximumEnd is <= 12 or > int.MaxValue || maximumEnd > MaxFontPayloadBytes)
            return false;

        var alignedEnd = (maximumEnd + 3) & ~3L;
        if (alignedEnd > availableLength)
            alignedEnd = maximumEnd;

        length = (int)alignedEnd;
        return true;
    }

    private bool CacheMatchesSource(SourceStamp source)
    {
        var manifest = TryReadManifest();
        return manifest is not null &&
               manifest.SchemaVersion == FontCacheSchemaVersion &&
               string.Equals(
                   Path.GetFullPath(manifest.SourcePath),
                   source.Path,
                   StringComparison.OrdinalIgnoreCase) &&
               manifest.SourceLength == source.Length &&
               manifest.SourceLastWriteUtcTicks == source.LastWriteUtcTicks;
    }

    private FontCacheManifest? TryReadManifest()
    {
        try
        {
            if (!File.Exists(_manifestPath))
                return null;
            using var stream = File.OpenRead(_manifestPath);
            return JsonSerializer.Deserialize<FontCacheManifest>(stream);
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or JsonException or NotSupportedException)
        {
            return null;
        }
    }

    private static bool SourceIsNewerThanCache(
        SourceStamp source,
        string regularPath,
        string boldPath,
        string koreanPath)
    {
        var benderPath = File.Exists(regularPath) ? regularPath : boldPath;
        if (!File.Exists(benderPath) || !File.Exists(koreanPath))
            return true;
        var cacheStamp = new[] { benderPath, koreanPath }
            .Select(File.GetLastWriteTimeUtc)
            .Min();
        return source.LastWriteUtcTicks > cacheStamp.Ticks;
    }

    private static string ComputeCacheGenerationKey(
        string regularPath,
        string boldPath,
        string koreanPath)
    {
        var rows = new List<string>(3);
        if (File.Exists(regularPath))
            rows.Add("regular:" + HashFile(regularPath));
        if (File.Exists(boldPath))
            rows.Add("bold:" + HashFile(boldPath));
        if (!File.Exists(koreanPath))
            return string.Empty;
        rows.Add("korean:" + HashFile(koreanPath));
        var joined = Encoding.UTF8.GetBytes(string.Join('|', rows));
        return Convert.ToHexString(SHA256.HashData(joined));
    }

    private static string HashFile(string path)
    {
        using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            128 * 1024,
            FileOptions.SequentialScan);
        return Convert.ToHexString(SHA256.HashData(stream));
    }

    private void WriteManifestAtomically(FontCacheManifest manifest)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(manifest);
        WriteAtomically(_manifestPath, bytes);
    }

    private static bool LooksLikeSfnt(ReadOnlySpan<byte> source, int offset)
    {
        if (offset < 0 || offset > source.Length - 4)
            return false;

        return source[offset] == (byte)'O' &&
               source[offset + 1] == (byte)'T' &&
               source[offset + 2] == (byte)'T' &&
               source[offset + 3] == (byte)'O' ||
               source[offset] == 0x00 &&
               source[offset + 1] == 0x01 &&
               source[offset + 2] == 0x00 &&
               source[offset + 3] == 0x00;
    }

    private static bool IsBender(SKTypeface? typeface) =>
        typeface is not null &&
        typeface.FamilyName.Contains("Bender", StringComparison.OrdinalIgnoreCase);

    private static bool IsKoreanNoto(SKTypeface typeface) =>
        typeface.FamilyName.Contains("Noto Sans CJK KR", StringComparison.OrdinalIgnoreCase) ||
        typeface.PostScriptName?.Contains("NotoSansCJKkr", StringComparison.OrdinalIgnoreCase) == true;

    private static string? FindResourcesAssets()
    {
        try
        {
            foreach (var process in Process.GetProcessesByName("EscapeFromTarkov"))
            {
                using (process)
                {
                    string? executable;
                    try
                    {
                        executable = process.MainModule?.FileName;
                    }
                    catch (Exception exception) when (
                        exception is InvalidOperationException or
                        System.ComponentModel.Win32Exception or
                        NotSupportedException)
                    {
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(executable))
                        continue;
                    var directory = Path.GetDirectoryName(executable);
                    if (string.IsNullOrWhiteSpace(directory))
                        continue;

                    var resources = Path.Combine(directory, "EscapeFromTarkov_Data", "resources.assets");
                    if (File.Exists(resources))
                        return Path.GetFullPath(resources);
                }
            }
        }
        catch (InvalidOperationException)
        {
        }
        catch (System.ComponentModel.Win32Exception)
        {
        }

        return null;
    }

    private static SourceStamp? TryGetSourceStamp(string? resourcesPath)
    {
        if (string.IsNullOrWhiteSpace(resourcesPath))
            return null;
        try
        {
            var info = new FileInfo(resourcesPath);
            if (!info.Exists)
                return null;
            return new SourceStamp(
                Path.GetFullPath(resourcesPath),
                info.Length,
                info.LastWriteTimeUtc.Ticks);
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            return null;
        }
    }

    private static void WriteAtomically(string path, byte[] bytes)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);
        var temp = $"{path}.{Guid.NewGuid():N}.tmp";
        try
        {
            using (var stream = new FileStream(
                       temp,
                       FileMode.CreateNew,
                       FileAccess.Write,
                       FileShare.None,
                       64 * 1024,
                       FileOptions.WriteThrough))
            {
                stream.Write(bytes);
                stream.Flush(flushToDisk: true);
            }
            File.Move(temp, path, overwrite: true);
        }
        finally
        {
            TryDelete(temp);
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

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        lock (_gate)
        {
            _fonts?.Dispose();
            _fonts = null;
            _loadedSourceStamp = null;
        }
        GC.SuppressFinalize(this);
    }

    private readonly record struct SourceStamp(string Path, long Length, long LastWriteUtcTicks);

    private sealed record FontCacheManifest(
        int SchemaVersion,
        string SourcePath,
        long SourceLength,
        long SourceLastWriteUtcTicks,
        string GenerationKey);
}

public sealed class TarkovTitleFonts : IDisposable
{
    private bool _disposed;

    public TarkovTitleFonts(
        SKTypeface? benderRegular,
        SKTypeface? benderBold,
        SKTypeface notoKorean,
        string generationKey)
    {
        BenderRegular = benderRegular;
        BenderBold = benderBold;
        NotoKorean = notoKorean ?? throw new ArgumentNullException(nameof(notoKorean));
        GenerationKey = string.IsNullOrWhiteSpace(generationKey)
            ? throw new ArgumentException("Font generation key is required.", nameof(generationKey))
            : generationKey;
    }

    public SKTypeface? BenderRegular { get; }
    public SKTypeface? BenderBold { get; }
    public SKTypeface NotoKorean { get; }
    public string GenerationKey { get; }

    public IEnumerable<SKTypeface> BenderVariants
    {
        get
        {
            if (BenderRegular is not null)
                yield return BenderRegular;
            if (BenderBold is not null)
                yield return BenderBold;
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        BenderRegular?.Dispose();
        BenderBold?.Dispose();
        NotoKorean.Dispose();
        GC.SuppressFinalize(this);
    }
}
