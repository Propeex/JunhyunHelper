using System.Text.Json;
using JunhyunHelper.Infrastructure.Storage;
using Xunit;

namespace JunhyunHelper.Tests.Storage;

public sealed class AtomicJsonFileStoreTests
{
    [Fact]
    public void SaveCommitsPrimaryAndRetainsPreviousValueAsBackup()
    {
        var directory = CreateTempDirectory();
        try
        {
            var path = Path.Combine(directory, "preferences.json");
            var store = new AtomicJsonFileStore(path);

            store.Save(new PreferenceDocument { Value = 1 });
            store.Save(new PreferenceDocument { Value = 2 });

            Assert.Equal(2, Read(path).Value);
            Assert.Equal(1, Read(store.BackupPath).Value);
            Assert.Equal(2, store.LoadOrDefault(() => new PreferenceDocument()).Value);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void CorruptedPrimaryFallsBackToPreviousSuccessfulValue()
    {
        var directory = CreateTempDirectory();
        try
        {
            var path = Path.Combine(directory, "preferences.json");
            var store = new AtomicJsonFileStore(path);

            store.Save(new PreferenceDocument { Value = 10 });
            store.Save(new PreferenceDocument { Value = 20 });
            File.WriteAllText(path, "{ definitely-not-json");

            var loaded = store.LoadOrDefault(() => new PreferenceDocument { Value = -1 });

            Assert.Equal(10, loaded.Value);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void SaveCreatesMissingParentDirectory()
    {
        var directory = CreateTempDirectory();
        try
        {
            var path = Path.Combine(directory, "nested", "preferences.json");
            var store = new AtomicJsonFileStore(path);

            store.Save(new PreferenceDocument { Value = 7 });

            Assert.True(File.Exists(path));
            Assert.Equal(7, store.LoadOrDefault(() => new PreferenceDocument()).Value);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static PreferenceDocument Read(string path) =>
        JsonSerializer.Deserialize<PreferenceDocument>(File.ReadAllText(path))
        ?? throw new InvalidDataException($"Preference test document at '{path}' was empty.");

    private static string CreateTempDirectory()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            "JunhyunHelper.Tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }

    private sealed class PreferenceDocument
    {
        public int Value { get; init; }
    }
}
