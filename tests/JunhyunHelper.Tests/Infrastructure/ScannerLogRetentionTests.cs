using System.Text;
using JunhyunHelper.Infrastructure.Scanner;
using Xunit;

namespace JunhyunHelper.Tests.Infrastructure;

public sealed class ScannerLogRetentionTests
{
    [Fact]
    public void PruneFiles_RemovesExpiredAndMalformedLinesAcrossBothFiles()
    {
        var root = CreateTemporaryDirectory();
        try
        {
            var primary = Path.Combine(root, "scanner.log");
            var rotated = primary + ".1";
            var now = new DateTimeOffset(2026, 8, 24, 9, 0, 0, TimeSpan.Zero);

            File.WriteAllLines(rotated, new[]
            {
                Line(now.AddDays(-8), "expired"),
                "malformed legacy line",
                Line(now.AddDays(-6), "recent-rotated"),
            }, Encoding.UTF8);
            File.WriteAllLines(primary, new[]
            {
                Line(now.AddHours(-2), "recent-primary"),
            }, Encoding.UTF8);

            var result = ScannerLogRetention.PruneFiles(
                primary,
                rotated,
                TimeSpan.FromDays(7),
                1024 * 1024,
                now);

            Assert.True(result.Success);
            Assert.Equal(2, result.RetainedLines);
            Assert.Equal(2, result.RemovedLines);
            Assert.False(File.Exists(rotated));

            var lines = File.ReadAllLines(primary, Encoding.UTF8);
            Assert.Equal(2, lines.Length);
            Assert.Contains("recent-rotated", lines[0], StringComparison.Ordinal);
            Assert.Contains("recent-primary", lines[1], StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void PruneFiles_EnforcesByteBudgetByKeepingNewestLines()
    {
        var root = CreateTemporaryDirectory();
        try
        {
            var primary = Path.Combine(root, "scanner.log");
            var rotated = primary + ".1";
            var now = new DateTimeOffset(2026, 8, 24, 9, 0, 0, TimeSpan.Zero);
            var oldRecent = Line(now.AddHours(-3), "old-" + new string('a', 120));
            var middleRecent = Line(now.AddHours(-2), "middle-" + new string('b', 120));
            var newest = Line(now.AddHours(-1), "newest-" + new string('c', 120));

            File.WriteAllLines(primary, new[] { oldRecent, middleRecent, newest }, Encoding.UTF8);
            var newestBytes = Encoding.UTF8.GetByteCount(newest) + Encoding.UTF8.GetByteCount(Environment.NewLine);
            var budget = newestBytes + 8;

            var result = ScannerLogRetention.PruneFiles(
                primary,
                rotated,
                TimeSpan.FromDays(7),
                budget,
                now);

            Assert.True(result.Success);
            Assert.Equal(1, result.RetainedLines);
            var lines = File.ReadAllLines(primary, Encoding.UTF8);
            Assert.Single(lines);
            Assert.Contains("newest-", lines[0], StringComparison.Ordinal);
            Assert.True(result.RetainedBytes <= budget);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void PruneFiles_NoExistingFiles_IsSuccessfulNoOp()
    {
        var root = CreateTemporaryDirectory();
        try
        {
            var primary = Path.Combine(root, "scanner.log");
            var result = ScannerLogRetention.PruneFiles(
                primary,
                primary + ".1",
                TimeSpan.FromDays(7),
                1024,
                DateTimeOffset.UtcNow);

            Assert.True(result.Success);
            Assert.Equal(0, result.RetainedLines);
            Assert.Equal(0, result.RemovedLines);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static string Line(DateTimeOffset timestamp, string marker) =>
        $"{timestamp:O} | test | mode=TarkovWindow | marker={marker}";

    private static string CreateTemporaryDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "JunhyunHelper-ScannerLogTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
