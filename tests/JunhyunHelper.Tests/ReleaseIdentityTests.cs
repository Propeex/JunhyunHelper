using System.Xml.Linq;
using Xunit;

namespace JunhyunHelper.Tests;

public sealed class ReleaseIdentityTests
{
    [Fact]
    public void ProjectFirstRunAndReleaseNotesUseTheSameVersion()
    {
        var root = FindRepositoryRoot();
        var projectPath = Path.Combine(
            root,
            "src",
            "JunhyunHelper.Desktop",
            "JunhyunHelper.Desktop.csproj");
        var firstRunPath = Path.Combine(root, "packaging", "FIRST_RUN_KO.txt");

        var project = XDocument.Load(projectPath);
        var version = project
            .Descendants("Version")
            .Select(static element => element.Value.Trim())
            .FirstOrDefault(static value => value.Length > 0);
        Assert.False(string.IsNullOrWhiteSpace(version));

        var expectedFirstLine = $"준현 헬퍼 v{version} — Windows x64";
        var firstLine = File.ReadLines(firstRunPath).FirstOrDefault();
        Assert.Equal(expectedFirstLine, firstLine);

        var releaseNotesPath = Path.Combine(root, "docs", $"RELEASE_NOTES_V{version}.md");
        Assert.True(
            File.Exists(releaseNotesPath),
            $"Release notes for source version {version} were not found at '{releaseNotesPath}'.");
        var releaseHeading = File.ReadLines(releaseNotesPath).FirstOrDefault();
        Assert.Equal($"# 준현 헬퍼 v{version}", releaseHeading);
    }

    private static string FindRepositoryRoot()
    {
        for (var current = new DirectoryInfo(AppContext.BaseDirectory);
             current is not null;
             current = current.Parent)
        {
            if (File.Exists(Path.Combine(
                    current.FullName,
                    "src",
                    "JunhyunHelper.Desktop",
                    "JunhyunHelper.Desktop.csproj")) &&
                File.Exists(Path.Combine(current.FullName, "packaging", "FIRST_RUN_KO.txt")))
            {
                return current.FullName;
            }
        }

        throw new DirectoryNotFoundException(
            "Could not locate the JunhyunHelper repository root for release identity validation.");
    }
}
