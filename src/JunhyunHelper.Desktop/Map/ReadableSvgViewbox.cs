using JunhyunHelper.Infrastructure.Maps;
using SharpVectors.Converters;

namespace JunhyunHelper.Desktop.Map;

public sealed class ReadableSvgViewbox : SvgViewbox
{
    private static readonly object RenderGate = new();
    private long _sourceRequestVersion;

    public new Uri? Source
    {
        get => base.Source;
        set
        {
            var version = Interlocked.Increment(ref _sourceRequestVersion);
            _ = ApplyReadableSourceAsync(value, version);
        }
    }

    private async Task ApplyReadableSourceAsync(Uri? source, long version)
    {
        Uri? prepared;
        try
        {
            prepared = await Task.Run(() => PrepareReadableSource(source));
        }
        catch
        {
            prepared = source;
        }

        if (version != Volatile.Read(ref _sourceRequestVersion))
            return;

        try
        {
            await Dispatcher.InvokeAsync(() =>
            {
                if (version != Volatile.Read(ref _sourceRequestVersion))
                    return;

                // Force SharpVectors to reload when an updated asset keeps the same local URI.
                ClearValue(SourceProperty);
                if (prepared is not null)
                    base.Source = prepared;
            });
        }
        catch
        {
            // The owning window/control may already be shutting down. Rendering is non-authoritative.
        }
    }

    private static Uri? PrepareReadableSource(Uri? source)
    {
        if (source is null || !source.IsAbsoluteUri || !source.IsFile)
            return source;

        var sourcePath = source.LocalPath;
        if (!File.Exists(sourcePath))
            return source;

        // Legacy Tarkov-Helper SVGs already contain the exact palette, labels and
        // floor presentation selected by the user. The readable-v1 schematic CSS
        // was created for Tarkov.dev source artwork and must never rewrite these maps.
        if (Path.GetFileName(sourcePath).Contains("legacy-", StringComparison.OrdinalIgnoreCase))
            return source;

        string? temporary = null;
        try
        {
            var sourceDirectory = Path.GetDirectoryName(sourcePath);
            if (string.IsNullOrWhiteSpace(sourceDirectory))
                return source;

            var readableDirectory = Path.Combine(sourceDirectory, "readable");
            var destination = Path.Combine(
                readableDirectory,
                $"{Path.GetFileNameWithoutExtension(sourcePath)}.{MapSvgPresentationTransformer.PresentationRevision}.svg");

            lock (RenderGate)
            {
                if (!File.Exists(destination) ||
                    File.GetLastWriteTimeUtc(destination) < File.GetLastWriteTimeUtc(sourcePath))
                {
                    Directory.CreateDirectory(readableDirectory);
                    temporary = $"{destination}.{Guid.NewGuid():N}.tmp";
                    MapSvgPresentationTransformer.CreateReadableCopy(
                        sourcePath,
                        temporary,
                        Array.Empty<string?>(),
                        selectedFloorLayer: null);
                    File.Move(temporary, destination, overwrite: true);
                    temporary = null;
                }
            }

            return new Uri(destination, UriKind.Absolute);
        }
        catch
        {
            return source;
        }
        finally
        {
            if (!string.IsNullOrWhiteSpace(temporary) && File.Exists(temporary))
            {
                try { File.Delete(temporary); }
                catch { }
            }
        }
    }
}