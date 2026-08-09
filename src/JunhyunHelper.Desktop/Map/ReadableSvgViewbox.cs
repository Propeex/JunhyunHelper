using JunhyunHelper.Infrastructure.Maps;
using SharpVectors.Converters;

namespace JunhyunHelper.Desktop.Map;

public sealed class ReadableSvgViewbox : SvgViewbox
{
    private static readonly object RenderGate = new();

    public void SetReadableSource(Uri? source)
    {
        var prepared = PrepareReadableSource(source);

        // Force SharpVectors to reload when a source file is refreshed in place but keeps the same URI.
        ClearValue(SourceProperty);
        if (prepared is not null)
            base.Source = prepared;
    }

    private static Uri? PrepareReadableSource(Uri? source)
    {
        if (source is null || !source.IsAbsoluteUri || !source.IsFile)
            return source;

        var sourcePath = source.LocalPath;
        if (!File.Exists(sourcePath))
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
