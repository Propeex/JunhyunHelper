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

        var maximumLevel = MaximumLevel(station);
        if (level is < 0 || level > maximumLevel)
        {
            throw new ArgumentOutOfRangeException(
                nameof(level),
                level,
                $"Hideout station '{stationId}' level must be between 0 and {maximumLevel}, or null when unentered.");
        }

        var profile = await LoadRequiredProfileAsync(profileId, cancellationToken);
        var levels = new Dictionary<string, int>(profile.HideoutLevels, StringComparer.Ordinal);
        if (level.HasValue)
            levels[stationId] = level.Value;
        else
            levels.Remove(stationId);

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
                    : (int?)null;
                var maximumLevel = MaximumLevel(station);
                var nextLevel = currentLevel.HasValue
                    ? station.Levels
                        .Where(level => level.Level > currentLevel.Value)
                        .OrderBy(level => level.Level)
                        .FirstOrDefault()
                    : null;

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
