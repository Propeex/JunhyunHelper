using System.Xml.Linq;
using Xunit;

namespace JunhyunHelper.Tests.Desktop;

public sealed class MapAssetFileLifecycleTests
{
    [Fact]
    public void Exclusive_writer_must_be_disposed_before_a_downloaded_asset_is_reopened()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"junhyunhelper-map-lock-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "map.svg");

        try
        {
            using (var writer = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                using var textWriter = new StreamWriter(writer, leaveOpen: true);
                textWriter.Write("<svg xmlns=\"http://www.w3.org/2000/svg\"></svg>");
                textWriter.Flush();

                Assert.ThrowsAny<IOException>(() =>
                {
                    using var _ = File.OpenRead(path);
                });
            }

            var document = XDocument.Load(path);
            Assert.Equal("svg", document.Root?.Name.LocalName);
        }
        finally
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void Map_asset_download_source_validates_only_after_exclusive_writer_scope()
    {
        var repositoryRoot = FindRepositoryRoot();
        var path = Path.Combine(
            repositoryRoot,
            "src",
            "JunhyunHelper.Desktop",
            "Services",
            "MapAssetCacheService.cs");
        var source = File.ReadAllText(path);

        AssertLifecycle(source, "DownloadSvgCoreAsync", "ValidateSvg(destination);");
        AssertLifecycle(source, "DownloadPngAsync", "ValidatePng(destination);");
    }

    private static void AssertLifecycle(string source, string methodName, string validationCall)
    {
        var methodStart = source.IndexOf($"private async Task {methodName}", StringComparison.Ordinal);
        Assert.True(methodStart >= 0, $"{methodName} was not found.");

        var nextMethod = source.IndexOf("\n    private ", methodStart + 1, StringComparison.Ordinal);
        var method = nextMethod >= 0
            ? source[methodStart..nextMethod]
            : source[methodStart..];

        var writerStart = method.IndexOf("await using (var output = new FileStream(", StringComparison.Ordinal);
        var flush = method.IndexOf("await output.FlushAsync(cancellationToken);", StringComparison.Ordinal);
        var validation = method.LastIndexOf(validationCall, StringComparison.Ordinal);

        Assert.True(writerStart >= 0, $"{methodName} must scope the exclusive writer explicitly.");
        Assert.True(flush > writerStart, $"{methodName} must flush inside the writer scope.");
        Assert.True(validation > flush, $"{methodName} must validate after writing.");

        var betweenFlushAndValidation = method[(flush + "await output.FlushAsync(cancellationToken);".Length)..validation];
        Assert.Contains("}", betweenFlushAndValidation, StringComparison.Ordinal);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "JunhyunHelper.slnx")) ||
                Directory.Exists(Path.Combine(directory.FullName, ".git")))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Repository root was not found from the test output directory.");
    }
}
