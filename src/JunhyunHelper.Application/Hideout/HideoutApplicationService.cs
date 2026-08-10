using JunhyunHelper.Application.Items;
using JunhyunHelper.Core.Content;
using JunhyunHelper.Core.Hideout;
using JunhyunHelper.Core.Items;
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
        return BuildFromProfile(content, profile);
    }

    /// <summary>
    /// Rebuilds the derived Hideout workspace from an already loaded authoritative
    /// profile snapshot without another user.db read.
    /// </summary>
    public HideoutWorkspace BuildFromProfile(
        GameContentCatalog content,
        GameProfileSnapshot profile)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(profile);
        return BuildWorkspace(content, profile);
    }

    public Task<HideoutWorkspace> SetLevelAsync(
        GameContentCatalog content,
        string profileId,
        string stationId,
        int? level,
        CancellationToken cancellationToken = default) =>
        SetLevelAsync(
            content,
            profileId,
            stationId,
            level,
            restoreInventoryOnRollback: false,
            cancellationToken);

    public async Task<HideoutWorkspace> SetLevelAsync(
        GameContentCatalog content,
        string profileId,
        string stationId,
        int? level,
        bool restoreInventoryOnRollback,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(profileId);
        ArgumentException.ThrowIfNullOrWhiteSpace(stationId);

        var station = content.HideoutStations.FirstOrDefault(candidate =>
            string.Equals(candidate.Id, stationId, StringComparison.Ordinal))
            ?? throw new KeyNotFoundException($"Hideout station '{stationId}' does not exist in active content.");

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
        var currentLevel = profile.HideoutLevels.TryGetValue(stationId, out var savedLevel)
            ? savedLevel
            : 0;
        if (normalizedLevel == currentLevel)
            return BuildWorkspace(content, profile);

        IReadOnlyDictionary<string, InventoryQuantity> inventory = profile.Inventory;
        var consumptions = new Dictionary<string, InventoryConsumption>(
            profile.HideoutUpgradeConsumptions,
            StringComparer.Ordinal);

        if (normalizedLevel > currentLevel)
        {
            foreach (var targetLevel in Enumerable.Range(currentLevel + 1, normalizedLevel - currentLevel))
            {
                var key = UpgradeConsumptionKey(stationId, targetLevel);
                if (consumptions.TryGetValue(key, out var existingConsumption) && !existingConsumption.IsEmpty)
                {
                    // The level was previously rolled back without restoring inventory. Those
                    // materials remain spent, so a later re-upgrade must not deduct them twice.
                    continue;
                }

                var upgrade = station.Levels.FirstOrDefault(candidate => candidate.Level == targetLevel);
                if (upgrade is null)
                    continue;

                var result = FixedInventoryConsumptionPolicy.Consume(
                    inventory,
                    upgrade.ItemRequirements.Select(requirement => new FixedItemConsumptionRequirement(
                        requirement.ItemId,
                        requirement.Count,
                        requirement.FoundInRaid)));
                inventory = result.Inventory;

                if (result.Consumption.IsEmpty)
                    consumptions.Remove(key);
                else
                    consumptions[key] = result.Consumption;
            }
        }
        else
        {
            foreach (var rolledBackLevel in Enumerable.Range(normalizedLevel + 1, currentLevel - normalizedLevel).Reverse())
            {
                var key = UpgradeConsumptionKey(stationId, rolledBackLevel);
                if (consumptions.TryGetValue(key, out var consumption) && restoreInventoryOnRollback)
                {
                    inventory = FixedInventoryConsumptionPolicy.Restore(inventory, consumption);
                    consumptions.Remove(key);
                }
                // Choosing not to restore deliberately keeps the ledger. If the user raises
                // the level again, the same already-spent materials are not consumed twice.
            }
        }

        var levels = new Dictionary<string, int>(profile.HideoutLevels, StringComparer.Ordinal);
        if (normalizedLevel == 0)
            levels.Remove(stationId);
        else
            levels[stationId] = normalizedLevel;

        var updated = profile with
        {
            HideoutLevels = levels,
            Inventory = inventory,
            HideoutUpgradeConsumptions = consumptions,
        };
        await _profileStore.SaveAsync(updated, cancellationToken);
        return BuildWorkspace(content, updated);
    }

    public static string UpgradeConsumptionKey(string stationId, int targetLevel)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stationId);
        if (targetLevel < 1)
            throw new ArgumentOutOfRangeException(nameof(targetLevel));
        return $"{stationId.Trim()}:{targetLevel}";
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