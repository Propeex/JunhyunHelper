using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using JunhyunHelper.Infrastructure.Updates;
using Xunit;

namespace JunhyunHelper.Tests.Infrastructure;

public sealed class ProgramUpdateTests
{
    private const string StableReleaseJson = """
        {
          "tag_name": "v0.1.14",
          "draft": false,
          "prerelease": false,
          "assets": [
            {
              "name": "Junhyun-Helper-v0.1.14-win-x64.zip",
              "browser_download_url": "https://github.com/Propeex/JunhyunHelper/releases/download/v0.1.14/Junhyun-Helper-v0.1.14-win-x64.zip"
            },
            {
              "name": "SHA256SUMS.txt",
              "browser_download_url": "https://github.com/Propeex/JunhyunHelper/releases/download/v0.1.14/SHA256SUMS.txt"
            }
          ]
        }
        """;

    [Fact]
    public void LatestReleaseParserRequiresStableExactWindowsAssets()
    {
        using var document = JsonDocument.Parse(StableReleaseJson);

        var release = GitHubProgramUpdateClient.ParseLatestRelease(document.RootElement);

        Assert.NotNull(release);
        Assert.Equal(new Version(0, 1, 14), release.Version);
        Assert.Equal("v0.1.14", release.TagName);
        Assert.Equal("Junhyun-Helper-v0.1.14-win-x64.zip", release.PackageFileName);
    }

    [Theory]
    [InlineData("0.1.13", true)]
    [InlineData("0.1.14", false)]
    [InlineData("0.1.15", false)]
    public async Task LatestReleaseCheckOnlyReturnsStrictlyNewerStableVersion(string currentVersion, bool expectedUpdate)
    {
        using var handler = new StubHttpMessageHandler(StableReleaseJson);
        using var httpClient = new HttpClient(handler);
        using var client = new GitHubProgramUpdateClient(httpClient);

        var release = await client.GetLatestReleaseAsync(
            Version.Parse(currentVersion),
            TestContext.Current.CancellationToken);

        Assert.Equal(expectedUpdate, release is not null);
    }

    [Theory]
    [InlineData("v0.1.14", true)]
    [InlineData("0.2.0", true)]
    [InlineData("V1.0.0", true)]
    [InlineData("v0.1.14-beta", false)]
    [InlineData("v0.1", false)]
    [InlineData("latest", false)]
    public void ReleaseVersionParserOnlyAcceptsStableThreePartVersions(string value, bool expected)
    {
        var parsed = GitHubProgramUpdateClient.TryParseReleaseVersion(value, out _);

        Assert.Equal(expected, parsed);
    }

    [Fact]
    public void ChecksumParserUsesTheExactPackageEntry()
    {
        const string packageName = "Junhyun-Helper-v0.1.14-win-x64.zip";
        const string expected = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";
        var checksumText = $"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa  other.zip\n{expected}  {packageName}\n";

        var actual = GitHubProgramUpdateClient.ParseExpectedSha256(checksumText, packageName);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void PackageExtractionRejectsPathTraversal()
    {
        var root = CreateTempDirectory();
        try
        {
            var package = Path.Combine(root, "update.zip");
            using (var archive = ZipFile.Open(package, ZipArchiveMode.Create))
            {
                var entry = archive.CreateEntry("../outside.txt");
                using var writer = new StreamWriter(entry.Open(), Encoding.UTF8);
                writer.Write("bad");
            }

            Assert.Throws<InvalidDataException>(() =>
                GitHubProgramUpdateClient.ExtractAndValidatePackage(package, Path.Combine(root, "staging")));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void PackageExtractionAcceptsOnlyThePortableProductRoot()
    {
        var root = CreateTempDirectory();
        try
        {
            var package = Path.Combine(root, "update.zip");
            using (var archive = ZipFile.Open(package, ZipArchiveMode.Create))
            {
                WriteEntry(archive, "준현 헬퍼.exe", "new executable");
                WriteEntry(archive, "FIRST_RUN_KO.txt", "first run");
                WriteEntry(archive, "Assets/DB/Data/example.json", "{}");
            }

            var staging = Path.Combine(root, "staging");
            GitHubProgramUpdateClient.ExtractAndValidatePackage(package, staging);

            Assert.Equal("new executable", File.ReadAllText(Path.Combine(staging, "준현 헬퍼.exe")));
            Assert.True(File.Exists(Path.Combine(staging, "Assets", "DB", "Data", "example.json")));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task ProductReplacementReplacesOwnedFilesAndPreservesUnrelatedFiles()
    {
        var root = CreateTempDirectory();
        try
        {
            var staging = Path.Combine(root, "staging");
            var target = Path.Combine(root, "target");
            CreateProductTree(staging, "new");
            CreateProductTree(target, "old");
            File.WriteAllText(Path.Combine(target, "user-note.txt"), "keep me");
            File.WriteAllText(Path.Combine(target, "Assets", "stale.asset"), "old only");

            await ProgramUpdateApplier.ReplaceProductFilesAsync(
                staging,
                target,
                TestContext.Current.CancellationToken);

            Assert.Equal("new executable", File.ReadAllText(Path.Combine(target, "준현 헬퍼.exe")));
            Assert.Equal("new first run", File.ReadAllText(Path.Combine(target, "FIRST_RUN_KO.txt")));
            Assert.Equal("new asset", File.ReadAllText(Path.Combine(target, "Assets", "current.asset")));
            Assert.False(File.Exists(Path.Combine(target, "Assets", "stale.asset")));
            Assert.Equal("keep me", File.ReadAllText(Path.Combine(target, "user-note.txt")));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task ProductReplacementRollsBackEarlierFilesWhenExecutableSwapFails()
    {
        if (!OperatingSystem.IsWindows())
            return;

        var root = CreateTempDirectory();
        try
        {
            var staging = Path.Combine(root, "staging");
            var target = Path.Combine(root, "target");
            CreateProductTree(staging, "new");
            CreateProductTree(target, "old");
            File.WriteAllText(Path.Combine(target, "Assets", "old-only.asset"), "old only");

            var executablePath = Path.Combine(target, "준현 헬퍼.exe");
            using (var lockStream = new FileStream(
                       executablePath,
                       FileMode.Open,
                       FileAccess.Read,
                       FileShare.Read))
            {
                await Assert.ThrowsAnyAsync<IOException>(() =>
                    ProgramUpdateApplier.ReplaceProductFilesAsync(
                        staging,
                        target,
                        TestContext.Current.CancellationToken));
            }

            Assert.Equal("old executable", File.ReadAllText(executablePath));
            Assert.Equal("old first run", File.ReadAllText(Path.Combine(target, "FIRST_RUN_KO.txt")));
            Assert.Equal("old asset", File.ReadAllText(Path.Combine(target, "Assets", "current.asset")));
            Assert.Equal("old only", File.ReadAllText(Path.Combine(target, "Assets", "old-only.asset")));
            Assert.DoesNotContain(
                Directory.EnumerateFileSystemEntries(target),
                path => Path.GetFileName(path).StartsWith(".__junhyun_", StringComparison.Ordinal));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static void CreateProductTree(string root, string value)
    {
        Directory.CreateDirectory(Path.Combine(root, "Assets"));
        File.WriteAllText(Path.Combine(root, "준현 헬퍼.exe"), $"{value} executable");
        File.WriteAllText(Path.Combine(root, "FIRST_RUN_KO.txt"), $"{value} first run");
        File.WriteAllText(Path.Combine(root, "Assets", "current.asset"), $"{value} asset");
    }

    private static void WriteEntry(ZipArchive archive, string path, string value)
    {
        var entry = archive.CreateEntry(path);
        using var writer = new StreamWriter(entry.Open(), Encoding.UTF8);
        writer.Write(value);
    }

    private static string CreateTempDirectory()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            "JunhyunHelper.Tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }

    private sealed class StubHttpMessageHandler(string responseJson) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Assert.Equal(GitHubProgramUpdateClient.LatestReleaseUri, request.RequestUri);
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json"),
            });
        }
    }
}
