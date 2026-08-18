using JunhyunHelper.Infrastructure.Storage;

namespace JunhyunHelper.Desktop.Services;

public sealed class AmmoFavoriteStore
{
    private readonly AtomicJsonFileStore _store;

    public AmmoFavoriteStore(string rootDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootDirectory);
        _store = new AtomicJsonFileStore(Path.Combine(
            Path.GetFullPath(rootDirectory),
            "ammo-favorites.json"));
    }

    public IReadOnlySet<string> Load()
    {
        var values = _store.LoadOrDefault(() => Array.Empty<string>());
        return values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .ToHashSet(StringComparer.Ordinal);
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

        try
        {
            _store.Save(values);
        }
        catch (Exception exception)
        {
            // Favorites are presentation preferences. A transient disk/permission
            // failure must not escalate through a WPF click handler and terminate the app.
            App.WriteDiagnostic("Failed to save ammo favorites", exception);
        }
    }
}
