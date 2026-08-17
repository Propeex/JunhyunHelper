using JunhyunHelper.Core.Content;
using JunhyunHelper.Core.Items;
using JunhyunHelper.Core.Profiles;
using JunhyunHelper.Infrastructure.Storage;

namespace JunhyunHelper.Application.Items;

public sealed record ItemsWorkspace(
    GameProfileSnapshot Profile,
    FutureNeededItemsPlan Plan,
    IReadOnlyList<FlexibleQuestItemProgress> FlexibleQuestItemProgresses,
    FutureNeededItemsBasis? PlanningBasis = null);

public sealed class ItemsApplicationService
{
    private readonly UserProfileStore _profileStore;
    private readonly object _workspaceCacheGate = new();
    private GameContentCatalog? _cachedContent;
    private GameProfileSnapshot? _cachedProfile;
    private ItemsWorkspace? _cachedWorkspace;

    public ItemsApplicationService(UserProfileStore profileStore)
    {
        _profileStore = profileStore ?? throw new ArgumentNullException(nameof(profileStore));
    }

    public async Task<ItemsWorkspace> LoadAsync(
        GameContentCatalog content,
        string profileId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(profileId);

        var profile = await _profileStore.LoadAsync(profileId, cancellationToken)
            ?? throw new KeyNotFoundException($"Profile '{profileId}' does not exist.");

        return BuildFromProfile(content, profile);
    }

    public ItemsWorkspace BuildFromProfile(
        GameContentCatalog content,
        GameProfileSnapshot profile)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(profile);

        lock (_workspaceCacheGate)
        {
            if (ReferenceEquals(_cachedContent, content) &&
                ReferenceEquals(_cachedProfile, profile) &&
                _cachedWorkspace is not null)
            {
                return _cachedWorkspace;
            }
        }

        var workspace = Build(content, profile);
        Cache(content, profile, workspace);
        return workspace;
    }

    public async Task<ItemsWorkspace> SetInventoryAsync(
        GameContentCatalog content,
        string profileId,
        string itemId,
        int fir,
        int nonFir,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(profileId);
        ArgumentException.ThrowIfNullOrWhiteSpace(itemId);
        if (fir < 0)
            throw new ArgumentOutOfRangeException(nameof(fir), fir, "FIR quantity cannot be negative.");
        if (nonFir < 0)
            throw new ArgumentOutOfRangeException(nameof(nonFir), nonFir, "Non-FIR quantity cannot be negative.");

        var profile = await _profileStore.LoadAsync(profileId, cancellationToken)
            ?? throw new KeyNotFoundException($"Profile '{profileId}' does not exist.");

        var inventory = new Dictionary<string, InventoryQuantity>(profile.Inventory, StringComparer.Ordinal);
        var normalizedItemId = itemId.Trim();
        if (fir == 0 && nonFir == 0)
            inventory.Remove(normalizedItemId);
        else
            inventory[normalizedItemId] = new InventoryQuantity(fir, nonFir);

        var updated = profile with { Inventory = inventory };
        await _profileStore.SaveAsync(updated, cancellationToken);

        FutureNeededItemsBasis? reusableBasis = null;
        lock (_workspaceCacheGate)
        {
            if (ReferenceEquals(_cachedContent, content) &&
                _cachedProfile is not null &&
                _cachedWorkspace?.PlanningBasis is { } cachedBasis &&
                PlanningStateEquals(_cachedProfile, profile))
            {
                reusableBasis = cachedBasis;
            }
        }

        var workspace = reusableBasis is null
            ? Build(content, updated)
            : BuildFromBasis(updated, reusableBasis);
        Cache(content, updated, workspace);
        return workspace;
    }

    private static ItemsWorkspace Build(
        GameContentCatalog content,
        GameProfileSnapshot profile)
    {
        var basis = FutureNeededItemsPlanner.BuildBasis(content, profile);
        return BuildFromBasis(profile, basis);
    }

    private static ItemsWorkspace BuildFromBasis(
        GameProfileSnapshot profile,
        FutureNeededItemsBasis basis)
    {
        var plan = FutureNeededItemsPlanner.Calculate(basis, profile.Inventory);
        var flexibleProgresses = basis.AlternativeQuestRequirements
            .Select(requirement => FlexibleQuestItemRequirementCalculator.Calculate(requirement, profile.Inventory))
            .OrderBy(progress => progress.QuestId, StringComparer.Ordinal)
            .ThenBy(progress => progress.ObjectiveId, StringComparer.Ordinal)
            .ToArray();

        return new ItemsWorkspace(profile, plan, flexibleProgresses, basis);
    }

    private void Cache(GameContentCatalog content, GameProfileSnapshot profile, ItemsWorkspace workspace)
    {
        lock (_workspaceCacheGate)
        {
            _cachedContent = content;
            _cachedProfile = profile;
            _cachedWorkspace = workspace;
        }
    }

    private static bool PlanningStateEquals(GameProfileSnapshot left, GameProfileSnapshot right) =>
        left.ProfileId == right.ProfileId &&
        left.GameMode == right.GameMode &&
        left.Level == right.Level &&
        left.Faction == right.Faction &&
        left.EditionId == right.EditionId &&
        left.PrestigeLevel == right.PrestigeLevel &&
        DictionaryEquals(left.Traders, right.Traders) &&
        SetEquals(left.CompletedQuestIds, right.CompletedQuestIds) &&
        SetEquals(left.FailedQuestIds, right.FailedQuestIds) &&
        DictionaryEquals(left.SpecialTraderAccessOverrides, right.SpecialTraderAccessOverrides) &&
        DictionaryEquals(left.ProfileVariables, right.ProfileVariables) &&
        DictionaryEquals(left.HideoutLevels, right.HideoutLevels);

    private static bool SetEquals<T>(IReadOnlySet<T> left, IReadOnlySet<T> right) where T : notnull =>
        left.Count == right.Count && left.All(right.Contains);

    private static bool DictionaryEquals<TKey, TValue>(
        IReadOnlyDictionary<TKey, TValue> left,
        IReadOnlyDictionary<TKey, TValue> right)
        where TKey : notnull
    {
        if (left.Count != right.Count)
            return false;

        var comparer = EqualityComparer<TValue>.Default;
        foreach (var (key, value) in left)
        {
            if (!right.TryGetValue(key, out var other) || !comparer.Equals(value, other))
                return false;
        }
        return true;
    }
}
