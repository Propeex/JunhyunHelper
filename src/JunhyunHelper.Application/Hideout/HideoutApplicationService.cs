using JunhyunHelper.Core.Content;
using JunhyunHelper.Core.Hideout;
using JunhyunHelper.Core.Profiles;
using JunhyunHelper.Infrastructure.Storage;

namespace JunhyunHelper.Application.Hideout;

public sealed record HideoutStationEntry(
    HideoutStation Station,
    int? CurrentLevel,
    int MaximumLevel,
    HideoutLevel? NextLevel);

public sealed record HideoutWorkspace(
    GameProfileSnapshot Profile,
    IReadOnlyList<HideoutStationEntry> Stations);

public sealed class HideoutApplicationService
{
    private readonly UserProfileStore _profileStore;

    public HideoutApplicationService(UserProfileStore profileStore)
    {
        _profileStore = profileStore ?? throw new ArgumentNullException(nameof(profileStore));
    }

    public async Task<HideoutWorkspace> LoadAsync(
        GameContentCatalog content,
        string profileId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(profileId);

        var profile = await LoadRequiredProfileAsync(profileId, cancellationToken);
        return BuildWorkspace(content, profile);
    }

    public async Task<HideoutWorkspace> SetLevelAsync(
        GameContentCatalog content,
        string profileId,
        string stationId,
        int? level,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(profileId);
        ArgumentException.ThrowIfNullOrWhiteSpace(stationId);

        var station = content.HideoutStations.FirstOrDefault(candidate =>
            string.Equals(candidate.Id, stationId, StringComparison.Ordinal))
            ?? throw new KeyNotFoundException($"Hideout station '{stationId}' does not exist in active content.");

        // Product rule: an absent station value and Lv.0 mean the same thing.
        // Keep accepting null at this application boundary so older callers remain safe,
        // but normalize it immediately to the single Lv.0 meaning.
        var normalizedLevel = level ?? 0;
        var maximumLevel = MaximumLevel(station);
        if (normalizedLevel < 0 || normalizedLevel > maximumLevel)
        {
            throw new ArgumentOutOfRangeException(
                nameof(level),
                normalizedLevel,
                $"Hideout station '{stationId}' level must be between 0 and {maximumLevel}.");
        }

        var profile = await LoadRequiredProfileAsync(profileId, cancellationToken);
        var levels = new Dictionary<string, int>(profile.HideoutLevels, StringComparer.Ordinal);
        if (normalizedLevel == 0)
            levels.Remove(stationId);
        else
            levels[stationId] = normalizedLevel;

        var updated = profile with { HideoutLevels = levels };
        await _profileStore.SaveAsync(updated, cancellationToken);
        return BuildWorkspace(content, updated);
    }

    private async Task<GameProfileSnapshot> LoadRequiredProfileAsync(
        string profileId,
        CancellationToken cancellationToken)
    {
        return await _profileStore.LoadAsync(profileId, cancellationToken)
            ?? throw new KeyNotFoundException($"Profile '{profileId}' does not exist.");
    }

    private static HideoutWorkspace BuildWorkspace(
        GameContentCatalog content,
        GameProfileSnapshot profile)
    {
        var stations = content.HideoutStations
            .Select(station =>
            {
                var currentLevel = profile.HideoutLevels.TryGetValue(station.Id, out var savedLevel)
                    ? savedLevel
                    : 0;
                var maximumLevel = MaximumLevel(station);
                var nextLevel = station.Levels
                    .Where(level => level.Level > currentLevel)
                    .OrderBy(level => level.Level)
                    .FirstOrDefault();

                return new HideoutStationEntry(
                    station,
                    currentLevel,
                    maximumLevel,
                    nextLevel);
            })
            .ToArray();

        return new HideoutWorkspace(profile, stations);
    }

    private static int MaximumLevel(HideoutStation station) =>
        station.Levels.Count == 0 ? 0 : station.Levels.Max(level => level.Level);
}
