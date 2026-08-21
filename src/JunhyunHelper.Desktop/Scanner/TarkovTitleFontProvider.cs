using System.Buffers.Binary;
using System.Diagnostics;
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
    private static readonly TimeSpan RetryInterval = TimeSpan.FromSeconds(5);
    private readonly object _gate = new();
    private readonly string _cacheDirectory;

    private TarkovTitleFonts? _fonts;
    private DateTimeOffset _nextAttemptUtc = DateTimeOffset.MinValue;
    private bool _disposed;

    public TarkovTitleFontProvider(string rootDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootDirectory);
        _cacheDirectory = Path.Combine(Path.GetFullPath(rootDirectory), "scanner", "fonts");
    }

    public bool TryGetFonts(out TarkovTitleFonts fonts)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        lock (_gate)
        {
            if (_fonts is not null)
            {
                fonts = _fonts;
                return true;
            }

            if (DateTimeOffset.UtcNow < _nextAttemptUtc)
            {
                fonts = null!;
                return false;
            }
            _nextAttemptUtc = DateTimeOffset.UtcNow + RetryInterval;

            var resourcesPath = FindResourcesAssets();
            if (TryLoadCache(resourcesPath, out var cached))
            {
                _fonts = cached;
                fonts = cached;
                return true;
            }

            if (resourcesPath is null || !TryExtractRequiredFonts(resourcesPath))
            {
                fonts = null!;
                return false;
            }

            if (!TryLoadCache(resourcesPath, out cached))
            {
                fonts = null!;
                return false;
            }

            _fonts = cached;
            fonts = cached;
            return true;
        }
    }

    private bool TryLoadCache(string? resourcesPath, out TarkovTitleFonts fonts)
    {
        fonts = null!;
        try
        {
            var regularPath = Path.Combine(_cacheDirectory, "Bender-Regular.otf");
            var boldPath = Path.Combine(_cacheDirectory, "Bender-Bold.otf");
            var koreanPath = Path.Combine(_cacheDirectory, "NotoSans-CJK-KR-Regular.otf");

            if (!File.Exists(koreanPath) || (!File.Exists(regularPath) && !File.Exists(boldPath)))
                return false;

            if (resourcesPath is not null && File.Exists(resourcesPath))
            {
                var sourceStamp = File.GetLastWriteTimeUtc(resourcesPath);
                var cacheStamp = new[]
                    {
                        koreanPath,
                        File.Exists(regularPath) ? regularPath : boldPath,
                    }
                    .Select(File.GetLastWriteTimeUtc)
                    .Min();
                if (sourceStamp > cacheStamp)
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

            fonts = new TarkovTitleFonts(regular, bold, korean);
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

    private bool TryExtractRequiredFonts(string resourcesPath)
    {
        try
        {
            var info = new FileInfo(resourcesPath);
            if (info.Length is <= 0 or > 134_217_728)
                return false;

            var bytes = File.ReadAllBytes(resourcesPath);
            byte[]? regular = null;
            byte[]? bold = null;
            byte[]? korean = null;

            foreach (var payload in EnumerateSfntPayloads(bytes))
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
            if (regular is not null)
                WriteAtomically(Path.Combine(_cacheDirectory, "Bender-Regular.otf"), regular);
            if (bold is not null)
                WriteAtomically(Path.Combine(_cacheDirectory, "Bender-Bold.otf"), bold);
            WriteAtomically(Path.Combine(_cacheDirectory, "NotoSans-CJK-KR-Regular.otf"), korean);

            ScannerDiagnosticLog.Write(
                "title-font-extract-ready",
                null,
                ("resources", resourcesPath),
                ("benderRegular", regular is not null),
                ("benderBold", bold is not null),
                ("notoKorean", true));
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

    private static IEnumerable<byte[]> EnumerateSfntPayloads(byte[] source)
    {
        for (var offset = 0; offset <= source.Length - 12; offset++)
        {
            if (!LooksLikeSfnt(source, offset))
                continue;
            if (!TryGetSfntLength(source, offset, out var length))
                continue;

            var payload = new byte[length];
            Buffer.BlockCopy(source, offset, payload, 0, length);
            yield return payload;
            offset += Math.Max(0, length - 1);
        }
    }

    internal static bool TryGetSfntLength(ReadOnlySpan<byte> source, int start, out int length)
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
            if (tableOffset < directoryLength || tableLength == 0 || tableEnd > source.Length - start)
                return false;
            maximumEnd = Math.Max(maximumEnd, tableEnd);
        }

        if (maximumEnd is <= 12 or > int.MaxValue)
            return false;

        var alignedEnd = (maximumEnd + 3) & ~3L;
        if (alignedEnd > source.Length - start)
            alignedEnd = maximumEnd;

        length = (int)alignedEnd;
        return true;
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
                        return resources;
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

    private static void WriteAtomically(string path, byte[] bytes)
    {
        var temp = path + ".tmp";
        File.WriteAllBytes(temp, bytes);
        File.Move(temp, path, overwrite: true);
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
        }
        GC.SuppressFinalize(this);
    }
}

public sealed class TarkovTitleFonts : IDisposable
{
    private bool _disposed;

    public TarkovTitleFonts(SKTypeface? benderRegular, SKTypeface? benderBold, SKTypeface notoKorean)
    {
        BenderRegular = benderRegular;
        BenderBold = benderBold;
        NotoKorean = notoKorean ?? throw new ArgumentNullException(nameof(notoKorean));
    }

    public SKTypeface? BenderRegular { get; }
    public SKTypeface? BenderBold { get; }
    public SKTypeface NotoKorean { get; }

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
