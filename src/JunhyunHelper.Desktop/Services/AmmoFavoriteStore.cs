using System.IO;
using System.Text.Json;

namespace JunhyunHelper.Desktop.Services;

public sealed class AmmoFavoriteStore
{
    private readonly string _path;

    public AmmoFavoriteStore(string rootDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootDirectory);
        _path = Path.Combine(Path.GetFullPath(rootDirectory), "ammo-favorites.json");
    }

    public IReadOnlySet<string> Load()
    {
        try
        {
            if (!File.Exists(_path))
                return new HashSet<string>(StringComparer.Ordinal);

            var values = JsonSerializer.Deserialize<string[]>(File.ReadAllText(_path)) ?? [];
            return values
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value.Trim())
                .ToHashSet(StringComparer.Ordinal);
        }
        catch
        {
            return new HashSet<string>(StringComparer.Ordinal);
        }
    }

    public void Save(IEnumerable<string> calibers)
    {
        ArgumentNullException.ThrowIfNull(calibers);
        var values = calibers
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
        var temporary = $"{_path}.{Guid.NewGuid():N}.tmp";
        try
        {
            File.WriteAllText(temporary, JsonSerializer.Serialize(values));
            File.Move(temporary, _path, overwrite: true);
        }
        finally
        {
            try
            {
                if (File.Exists(temporary))
                    File.Delete(temporary);
            }
            catch
            {
            }
        }
    }
}
