using JunhyunHelper.Core.Content;
using JunhyunHelper.Core.Items;
using JunhyunHelper.Core.Profiles;
using JunhyunHelper.Infrastructure.Storage;

namespace JunhyunHelper.Application.Items;

public sealed record ItemsWorkspace(
    GameProfileSnapshot Profile,
    FutureNeededItemsPlan Plan,
    IReadOnlyList<FlexibleQuestItemProgress> FlexibleQuestItemProgresses);

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
        lock (_workspaceCacheGate)
        {
            _cachedContent = content;
            _cachedProfile = profile;
            _cachedWorkspace = workspace;
        }
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
        return BuildFromProfile(content, updated);
    }

    private static ItemsWorkspace Build(
        GameContentCatalog content,
        GameProfileSnapshot profile)
    {
        var plan = FutureNeededItemsPlanner.Calculate(content, profile);
        var flexibleProgresses = plan.AlternativeQuestRequirements
            .Select(requirement => FlexibleQuestItemRequirementCalculator.Calculate(requirement, profile.Inventory))
            .OrderBy(progress => progress.QuestId, StringComparer.Ordinal)
            .ThenBy(progress => progress.ObjectiveId, StringComparer.Ordinal)
            .ToArray();

        return new ItemsWorkspace(profile, plan, flexibleProgresses);
    }
}