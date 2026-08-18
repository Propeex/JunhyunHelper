using System.Text.Json;

namespace JunhyunHelper.Infrastructure.Storage;

/// <summary>
/// Small-file JSON persistence for non-authoritative local product preferences.
/// Writes are committed from a same-directory temporary file and the previous
/// successful primary file is retained as a best-effort recovery copy.
/// </summary>
public sealed class AtomicJsonFileStore
{
    private readonly string _path;

    public AtomicJsonFileStore(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        _path = System.IO.Path.GetFullPath(path);
    }

    public string Path => _path;

    public string BackupPath => _path + ".bak";

    public T LoadOrDefault<T>(
        Func<T> defaultFactory,
        JsonSerializerOptions? options = null)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(defaultFactory);

        if (TryRead(_path, options, out T? primary) && primary is not null)
            return primary;

        if (TryRead(BackupPath, options, out T? backup) && backup is not null)
            return backup;

        return defaultFactory();
    }

    public void Save<T>(T value, JsonSerializerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(value);

        var directory = System.IO.Path.GetDirectoryName(_path);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        var temporary = $"{_path}.{Guid.NewGuid():N}.tmp";
        var replacementBackup = $"{_path}.{Guid.NewGuid():N}.bak.tmp";

        try
        {
            using (var stream = new FileStream(
                       temporary,
                       FileMode.CreateNew,
                       FileAccess.Write,
                       FileShare.None,
                       bufferSize: 16 * 1024,
                       options: FileOptions.WriteThrough))
            {
                JsonSerializer.Serialize(stream, value, options);
                stream.Flush(flushToDisk: true);
            }

            if (File.Exists(_path))
            {
                File.Replace(
                    temporary,
                    _path,
                    replacementBackup,
                    ignoreMetadataErrors: true);

                // Only promote the replaced primary to the recovery slot when it is
                // still readable as the same document type. If the primary had already
                // become corrupt, preserve the older known-good .bak instead of
                // replacing it with the corrupt bytes.
                if (TryRead(replacementBackup, options, out T? previous) && previous is not null)
                {
                    try
                    {
                        File.Move(replacementBackup, BackupPath, overwrite: true);
                    }
                    catch
                    {
                        // The new primary is already committed. Losing a newer recovery
                        // copy must not turn a successful preference save into a product error.
                        TryDelete(replacementBackup);
                    }
                }
                else
                {
                    TryDelete(replacementBackup);
                }
            }
            else
            {
                File.Move(temporary, _path);
            }
        }
        finally
        {
            TryDelete(temporary);
            TryDelete(replacementBackup);
        }
    }

    private static bool TryRead<T>(
        string path,
        JsonSerializerOptions? options,
        out T? value)
        where T : class
    {
        value = null;
        try
        {
            if (!File.Exists(path))
                return false;

            using var stream = File.OpenRead(path);
            value = JsonSerializer.Deserialize<T>(stream, options);
            return value is not null;
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or JsonException or NotSupportedException)
        {
            value = null;
            return false;
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
}
