using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;

namespace JunhyunHelper.Infrastructure.Updates;

public sealed record ProgramUpdateRelease(
    Version Version,
    string TagName,
    string PackageFileName,
    Uri PackageUri,
    Uri ChecksumUri);

public sealed record PreparedProgramUpdate(
    ProgramUpdateRelease Release,
    string WorkDirectory,
    string StagingDirectory);

public sealed record ProgramUpdateProgress(string Message, double? Fraction = null);

public sealed class GitHubProgramUpdateClient : IDisposable
{
    internal const string StablePackageFileName = "준현 헬퍼.zip";
    internal const string StablePackageRootDirectory = "준현 헬퍼";
    internal static readonly Uri LatestReleaseUri = new("https://api.github.com/repos/Propeex/JunhyunHelper/releases/latest");

    private readonly HttpClient _httpClient;
    private readonly bool _ownsHttpClient;

    public GitHubProgramUpdateClient(HttpClient? httpClient = null)
    {
        _ownsHttpClient = httpClient is null;
        _httpClient = httpClient ?? new HttpClient
        {
            Timeout = Timeout.InfiniteTimeSpan,
        };

        if (_httpClient.DefaultRequestHeaders.UserAgent.Count == 0)
            _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("JunhyunHelper", "1.0"));

        if (!_httpClient.DefaultRequestHeaders.Accept.Any(header => string.Equals(header.MediaType, "application/vnd.github+json", StringComparison.OrdinalIgnoreCase)))
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

        if (!_httpClient.DefaultRequestHeaders.Contains("X-GitHub-Api-Version"))
            _httpClient.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
    }

    public async Task<ProgramUpdateRelease?> GetLatestReleaseAsync(
        Version currentVersion,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(currentVersion);

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(8));

        using var request = new HttpRequestMessage(HttpMethod.Get, LatestReleaseUri);
        using var response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            timeout.Token).ConfigureAwait(false);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();

        await using var responseStream = await response.Content.ReadAsStreamAsync(timeout.Token).ConfigureAwait(false);
        using var document = await JsonDocument.ParseAsync(responseStream, cancellationToken: timeout.Token).ConfigureAwait(false);
        var release = ParseLatestRelease(document.RootElement);

        return release is not null && release.Version.CompareTo(currentVersion) > 0
            ? release
            : null;
    }

    public async Task<PreparedProgramUpdate> PrepareUpdateAsync(
        ProgramUpdateRelease release,
        string localApplicationDataRoot,
        IProgress<ProgramUpdateProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(release);
        ArgumentException.ThrowIfNullOrWhiteSpace(localApplicationDataRoot);

        var pendingRoot = Path.Combine(localApplicationDataRoot, "JunhyunHelper", "updates", "pending");
        var workDirectory = Path.Combine(
            pendingRoot,
            $"{release.Version.ToString(3)}-{Guid.NewGuid():N}");
        var stagingDirectory = Path.Combine(workDirectory, "staging");
        var packagePath = Path.Combine(workDirectory, release.PackageFileName);

        Directory.CreateDirectory(workDirectory);

        try
        {
            progress?.Report(new ProgramUpdateProgress("업데이트 정보를 확인하는 중..."));
            var checksumText = await DownloadStringAsync(release.ChecksumUri, cancellationToken).ConfigureAwait(false);
            var expectedHash = ParseExpectedSha256(checksumText, release.PackageFileName);

            progress?.Report(new ProgramUpdateProgress("업데이트 파일을 다운로드하는 중...", 0));
            await DownloadFileAsync(release.PackageUri, packagePath, progress, cancellationToken).ConfigureAwait(false);

            progress?.Report(new ProgramUpdateProgress("다운로드 파일을 검증하는 중...", 0.9));
            await VerifySha256Async(packagePath, expectedHash, cancellationToken).ConfigureAwait(false);

            progress?.Report(new ProgramUpdateProgress("업데이트 파일을 준비하는 중...", 0.94));
            ExtractAndValidatePackage(packagePath, stagingDirectory);

            progress?.Report(new ProgramUpdateProgress("업데이트 준비가 완료되었습니다.", 1));
            return new PreparedProgramUpdate(release, workDirectory, stagingDirectory);
        }
        catch
        {
            TryDeleteDirectory(workDirectory);
            throw;
        }
    }

    internal static ProgramUpdateRelease? ParseLatestRelease(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object)
            throw new InvalidDataException("GitHub release response is not an object.");

        if (GetOptionalBoolean(root, "draft") || GetOptionalBoolean(root, "prerelease"))
            return null;

        if (!root.TryGetProperty("tag_name", out var tagElement) ||
            tagElement.ValueKind != JsonValueKind.String ||
            !TryParseReleaseVersion(tagElement.GetString(), out var version))
        {
            throw new InvalidDataException("GitHub release tag is missing or invalid.");
        }

        if (!root.TryGetProperty("assets", out var assetsElement) || assetsElement.ValueKind != JsonValueKind.Array)
            throw new InvalidDataException("GitHub release assets are missing.");

        var tagName = tagElement.GetString()!;
        var legacyPackageFileName = $"Junhyun-Helper-v{version.ToString(3)}-win-x64.zip";
        Uri? stablePackageUri = null;
        Uri? legacyPackageUri = null;
        Uri? checksumUri = null;

        foreach (var asset in assetsElement.EnumerateArray())
        {
            if (asset.ValueKind != JsonValueKind.Object ||
                !asset.TryGetProperty("name", out var nameElement) ||
                nameElement.ValueKind != JsonValueKind.String ||
                !asset.TryGetProperty("browser_download_url", out var uriElement) ||
                uriElement.ValueKind != JsonValueKind.String)
            {
                continue;
            }

            var name = nameElement.GetString();
            if (!string.Equals(name, StablePackageFileName, StringComparison.Ordinal) &&
                !string.Equals(name, legacyPackageFileName, StringComparison.Ordinal) &&
                !string.Equals(name, "SHA256SUMS.txt", StringComparison.Ordinal))
            {
                continue;
            }

            var uriText = uriElement.GetString();
            if (string.IsNullOrWhiteSpace(uriText) || !Uri.TryCreate(uriText, UriKind.Absolute, out var uri))
                throw new InvalidDataException($"The release asset URL for {name} is invalid.");

            ValidateReleaseAssetUri(uri);

            if (string.Equals(name, StablePackageFileName, StringComparison.Ordinal))
                stablePackageUri = uri;
            else if (string.Equals(name, legacyPackageFileName, StringComparison.Ordinal))
                legacyPackageUri = uri;
            else
                checksumUri = uri;
        }

        var packageUri = stablePackageUri ?? legacyPackageUri;
        var packageFileName = stablePackageUri is not null ? StablePackageFileName : legacyPackageFileName;
        if (packageUri is null || checksumUri is null)
            throw new InvalidDataException("The latest release does not contain the required Windows package and checksum assets.");

        return new ProgramUpdateRelease(version, tagName, packageFileName, packageUri, checksumUri);
    }

    internal static bool TryParseReleaseVersion(string? tagName, out Version version)
    {
        version = new Version(0, 0, 0);
        if (string.IsNullOrWhiteSpace(tagName))
            return false;

        var text = tagName.Trim();
        if (text.StartsWith('v') || text.StartsWith('V'))
            text = text[1..];

        if (text.Contains('-') || text.Contains('+'))
            return false;

        var components = text.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (components.Length != 3 || components.Any(component => !int.TryParse(component, out var value) || value < 0))
            return false;

        if (!Version.TryParse(text, out var parsed) || parsed is null)
            return false;

        version = parsed;
        return true;
    }

    internal static string ParseExpectedSha256(string checksumText, string packageFileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(checksumText);
        ArgumentException.ThrowIfNullOrWhiteSpace(packageFileName);

        foreach (var rawLine in checksumText.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var tokens = rawLine.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (tokens.Length < 2)
                continue;

            var fileName = tokens[^1].TrimStart('*');
            if (!string.Equals(fileName, packageFileName, StringComparison.Ordinal))
                continue;

            var hash = tokens[0];
            if (hash.Length != 64 || !IsHex(hash))
                throw new InvalidDataException("The release checksum is not a valid SHA-256 value.");

            return hash.ToLowerInvariant();
        }

        throw new InvalidDataException($"No SHA-256 entry was found for {packageFileName}.");
    }

    internal static void ExtractAndValidatePackage(string packagePath, string stagingDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packagePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(stagingDirectory);

        if (Directory.Exists(stagingDirectory))
            Directory.Delete(stagingDirectory, recursive: true);
        Directory.CreateDirectory(stagingDirectory);

        var allowedRoots = new HashSet<string>(StringComparer.Ordinal)
        {
            "준현 헬퍼.exe",
            "FIRST_RUN_KO.txt",
            "Assets",
        };

        using var archive = ZipFile.OpenRead(packagePath);
        var normalizedEntries = archive.Entries
            .Select(entry => (Entry: entry, Name: entry.FullName.Replace('\\', '/').Trim()))
            .Where(item => item.Name.Length > 0)
            .ToArray();

        var stableRootPrefix = StablePackageRootDirectory + "/";
        var hasStableRoot = normalizedEntries.Any(item =>
            string.Equals(item.Name.TrimEnd('/'), StablePackageRootDirectory, StringComparison.Ordinal) ||
            item.Name.StartsWith(stableRootPrefix, StringComparison.Ordinal));
        var hasLegacyRoot = normalizedEntries.Any(item =>
            !string.Equals(item.Name.TrimEnd('/'), StablePackageRootDirectory, StringComparison.Ordinal) &&
            !item.Name.StartsWith(stableRootPrefix, StringComparison.Ordinal));

        if (hasStableRoot && hasLegacyRoot)
            throw new InvalidDataException("The update package mixes the stable product folder with legacy root entries.");

        var stripStableRoot = hasStableRoot;
        var seenEntries = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in normalizedEntries)
        {
            var normalized = item.Name;
            if (normalized.StartsWith("/", StringComparison.Ordinal) || normalized.Contains(':'))
                throw new InvalidDataException($"Unsafe update archive path: {item.Entry.FullName}");

            var sourceSegments = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (sourceSegments.Length == 0 || sourceSegments.Any(segment => segment is "." or ".."))
                throw new InvalidDataException($"Unsafe update archive path: {item.Entry.FullName}");

            if (stripStableRoot)
            {
                if (!string.Equals(sourceSegments[0], StablePackageRootDirectory, StringComparison.Ordinal))
                    throw new InvalidDataException($"Unexpected stable update package root entry: {sourceSegments[0]}");
                sourceSegments = sourceSegments.Skip(1).ToArray();
                if (sourceSegments.Length == 0)
                    continue;
            }

            if (!allowedRoots.Contains(sourceSegments[0]))
                throw new InvalidDataException($"Unexpected update package root entry: {sourceSegments[0]}");

            var relativePath = string.Join('/', sourceSegments);
            if (!seenEntries.Add(relativePath))
                throw new InvalidDataException($"Duplicate update archive entry: {item.Entry.FullName}");

            var unixFileType = (item.Entry.ExternalAttributes >> 16) & 0xF000;
            if (unixFileType == 0xA000)
                throw new InvalidDataException($"Symbolic links are not allowed in the update package: {item.Entry.FullName}");

            if (string.Equals(Path.GetExtension(relativePath), ".pdb", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Debug symbols are not allowed in the update package.");

            var destinationPath = Path.Combine(stagingDirectory, Path.Combine(sourceSegments));
            var isDirectory = normalized.EndsWith("/", StringComparison.Ordinal) || string.IsNullOrEmpty(item.Entry.Name);

            if (isDirectory)
            {
                Directory.CreateDirectory(destinationPath);
                continue;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
            item.Entry.ExtractToFile(destinationPath, overwrite: false);
        }

        ValidateStagingDirectory(stagingDirectory);
    }

    internal static void ValidateStagingDirectory(string stagingDirectory)
    {
        var executablePath = Path.Combine(stagingDirectory, "준현 헬퍼.exe");
        var firstRunPath = Path.Combine(stagingDirectory, "FIRST_RUN_KO.txt");
        var assetsPath = Path.Combine(stagingDirectory, "Assets");

        if (!File.Exists(executablePath) || new FileInfo(executablePath).Length == 0)
            throw new InvalidDataException("The update package does not contain a valid 준현 헬퍼.exe.");

        if (!File.Exists(firstRunPath) || new FileInfo(firstRunPath).Length == 0)
            throw new InvalidDataException("The update package does not contain FIRST_RUN_KO.txt.");

        if (!Directory.Exists(assetsPath) || !Directory.EnumerateFiles(assetsPath, "*", SearchOption.AllDirectories).Any())
            throw new InvalidDataException("The update package does not contain the required Assets directory.");
    }

    private async Task<string> DownloadStringAsync(Uri uri, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task DownloadFileAsync(
        Uri uri,
        string destinationPath,
        IProgress<ProgramUpdateProgress>? progress,
        CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var contentLength = response.Content.Headers.ContentLength;
        await using var source = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        await using var destination = new FileStream(
            destinationPath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 128 * 1024,
            options: FileOptions.Asynchronous | FileOptions.SequentialScan);

        var buffer = new byte[128 * 1024];
        long totalRead = 0;
        while (true)
        {
            var read = await source.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
            if (read == 0)
                break;

            await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
            totalRead += read;

            if (contentLength is > 0)
            {
                var fraction = Math.Clamp((double)totalRead / contentLength.Value, 0, 1);
                progress?.Report(new ProgramUpdateProgress("업데이트 파일을 다운로드하는 중...", fraction * 0.88));
            }
        }

        await destination.FlushAsync(cancellationToken).ConfigureAwait(false);
        destination.Flush(flushToDisk: true);
    }

    private static async Task VerifySha256Async(string path, string expectedHash, CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 128 * 1024,
            options: FileOptions.Asynchronous | FileOptions.SequentialScan);
        var actualHashBytes = await SHA256.HashDataAsync(stream, cancellationToken).ConfigureAwait(false);
        var actualHash = Convert.ToHexString(actualHashBytes).ToLowerInvariant();

        if (!CryptographicOperations.FixedTimeEquals(
                Convert.FromHexString(actualHash),
                Convert.FromHexString(expectedHash)))
        {
            throw new InvalidDataException("The downloaded update package SHA-256 does not match the published checksum.");
        }
    }

    private static void ValidateReleaseAssetUri(Uri uri)
    {
        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(uri.Host, "github.com", StringComparison.OrdinalIgnoreCase) ||
            !uri.AbsolutePath.StartsWith("/Propeex/JunhyunHelper/releases/download/", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("The release asset URL is not an approved JunhyunHelper GitHub Release URL.");
        }
    }

    private static bool GetOptionalBoolean(JsonElement root, string propertyName) =>
        root.TryGetProperty(propertyName, out var value) &&
        value.ValueKind == JsonValueKind.True;

    private static bool IsHex(string value)
    {
        foreach (var character in value)
        {
            if (!Uri.IsHexDigit(character))
                return false;
        }

        return true;
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
        catch
        {
            // Failed update cleanup must not hide the original error.
        }
    }

    public void Dispose()
    {
        if (_ownsHttpClient)
            _httpClient.Dispose();
    }
}
