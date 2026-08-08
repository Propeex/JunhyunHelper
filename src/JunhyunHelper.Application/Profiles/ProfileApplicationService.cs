using JunhyunHelper.Core.Profiles;
using JunhyunHelper.Infrastructure.Storage;

namespace JunhyunHelper.Application.Profiles;

public sealed class ProfileApplicationService
{
    private readonly UserProfileStore _profileStore;

    public ProfileApplicationService(UserProfileStore profileStore)
    {
        _profileStore = profileStore ?? throw new ArgumentNullException(nameof(profileStore));
    }

    public Task<IReadOnlyList<GameProfileSnapshot>> LoadAllAsync(
        CancellationToken cancellationToken = default) =>
        _profileStore.LoadAllAsync(cancellationToken);

    public async Task<GameProfileSnapshot> CreateAsync(
        GameMode gameMode,
        int level,
        PmcFaction faction,
        string? editionId,
        int? prestigeLevel,
        IReadOnlyDictionary<string, TraderProgress> traders,
        CancellationToken cancellationToken = default)
    {
        ValidateSettings(level, prestigeLevel, traders);

        var profiles = await _profileStore.LoadAllAsync(cancellationToken);
        if (profiles.Any(profile => profile.GameMode == gameMode))
        {
            throw new InvalidOperationException(
                $"A profile for game mode '{gameMode}' already exists.");
        }

        var profile = new GameProfileSnapshot
        {
            ProfileId = gameMode.ToDataKey(),
            GameMode = gameMode,
            Level = level,
            Faction = faction,
            EditionId = NormalizeOptional(editionId),
            PrestigeLevel = prestigeLevel,
            Traders = CopyTraders(traders),
        };

        await _profileStore.SaveAsync(profile, cancellationToken);
        return profile;
    }

    public async Task<GameProfileSnapshot> UpdateSettingsAsync(
        string profileId,
        int level,
        PmcFaction faction,
        string? editionId,
        int? prestigeLevel,
        IReadOnlyDictionary<string, TraderProgress> traders,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(profileId);
        ValidateSettings(level, prestigeLevel, traders);

        var profile = await _profileStore.LoadAsync(profileId, cancellationToken)
            ?? throw new KeyNotFoundException($"Profile '{profileId}' does not exist.");

        var updated = profile with
        {
            Level = level,
            Faction = faction,
            EditionId = NormalizeOptional(editionId),
            PrestigeLevel = prestigeLevel,
            Traders = CopyTraders(traders),
        };

        await _profileStore.SaveAsync(updated, cancellationToken);
        return updated;
    }

    public async Task DeleteAsync(
        string profileId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(profileId);
        if (!await _profileStore.DeleteAsync(profileId, cancellationToken))
            throw new KeyNotFoundException($"Profile '{profileId}' does not exist.");
    }

    private static void ValidateSettings(
        int level,
        int? prestigeLevel,
        IReadOnlyDictionary<string, TraderProgress> traders)
    {
        ArgumentNullException.ThrowIfNull(traders);

        if (level < 1)
            throw new ArgumentOutOfRangeException(nameof(level), level, "Player level must be at least 1.");
        if (prestigeLevel is < 0)
            throw new ArgumentOutOfRangeException(nameof(prestigeLevel), prestigeLevel, "Prestige level cannot be negative.");

        foreach (var (traderId, progress) in traders)
        {
            if (string.IsNullOrWhiteSpace(traderId))
                throw new ArgumentException("Trader id cannot be empty.", nameof(traders));
            if (progress.LoyaltyLevel is < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(traders),
                    progress.LoyaltyLevel,
                    $"Trader '{traderId}' loyalty level cannot be negative.");
            }
            if (progress.LoyaltyLevel is null && progress.Standing is null)
            {
                throw new ArgumentException(
                    $"Trader '{traderId}' must contain at least one known progress value.",
                    nameof(traders));
            }
        }
    }

    private static Dictionary<string, TraderProgress> CopyTraders(
        IReadOnlyDictionary<string, TraderProgress> traders) =>
        traders
            .Where(pair => pair.Value.LoyaltyLevel is not null || pair.Value.Standing is not null)
            .ToDictionary(pair => pair.Key, pair => pair.Value.Normalize(), StringComparer.Ordinal);

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
