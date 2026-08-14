using JunhyunHelper.Application.Items;
using JunhyunHelper.Core.Content;
using JunhyunHelper.Core.Items;
using JunhyunHelper.Core.Profiles;
using JunhyunHelper.Core.Quests;
using JunhyunHelper.Infrastructure.Storage;

namespace JunhyunHelper.Application.Quests;

public sealed record QuestWorkspace(
    GameProfileSnapshot Profile,
    IReadOnlyList<QuestCatalogEntry> Quests,
    IReadOnlyList<QuestCatalogEntry> Problems);

public sealed class QuestApplicationService
{
    private readonly UserProfileStore _profileStore;
    private readonly object _workspaceCacheGate = new();
    private GameContentCatalog? _cachedContent;
    private GameProfileSnapshot? _cachedProfile;
    private QuestWorkspace? _cachedWorkspace;

    public QuestApplicationService(UserProfileStore profileStore)
    {
        _profileStore = profileStore ?? throw new ArgumentNullException(nameof(profileStore));
    }

    public async Task<QuestWorkspace> LoadAsync(
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
    /// Rebuilds the derived Quest workspace from an already loaded authoritative
    /// profile snapshot. Repeated refreshes of the exact same immutable snapshot
    /// reuse the previous evaluation.
    /// </summary>
    public QuestWorkspace BuildFromProfile(
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

        var workspace = BuildWorkspace(content, profile);
        lock (_workspaceCacheGate)
        {
            _cachedContent = content;
            _cachedProfile = profile;
            _cachedWorkspace = workspace;
        }
        return workspace;
    }

    public async Task<QuestWorkspace> CompleteAsync(
        GameContentCatalog content,
        string profileId,
        string questId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(profileId);
        ArgumentException.ThrowIfNullOrWhiteSpace(questId);

        var profile = await LoadRequiredProfileAsync(profileId, cancellationToken);
        var entry = FindQuest(content, profile, questId);
        if (entry.Availability.State is not (QuestAvailabilityState.Current or QuestAvailabilityState.Indeterminate))
        {
            throw new InvalidOperationException(
                $"Quest '{questId}' can only be completed while it is Current or Indeterminate, but it is '{entry.Availability.State}'.");
        }

        var questConsumptions = new Dictionary<string, InventoryConsumption>(
            profile.QuestConsumptions,
            StringComparer.Ordinal);
        IReadOnlyDictionary<string, InventoryQuantity> inventory = profile.Inventory;

        // A ledger can survive an undo when the user explicitly chose not to restore
        // inventory. In that case the materials are already considered spent and a later
        // re-completion must not deduct them a second time.
        if (!questConsumptions.TryGetValue(questId, out var existingConsumption) || existingConsumption.IsEmpty)
        {
            var fixedRequirements = content.QuestItemRequirements
                .Where(requirement => string.Equals(requirement.QuestId, questId, StringComparison.Ordinal))
                // Flexible hand-ins are deliberately excluded: the helper cannot know which
                // candidate the user actually submitted, so guessing would corrupt inventory truth.
                .Where(requirement => requirement.AcceptedItemIds.Count == 1)
                .Select(requirement => new FixedItemConsumptionRequirement(
                    requirement.AcceptedItemIds[0],
                    requirement.Count,
                    requirement.FoundInRaid))
                .ToArray();
            var consumption = FixedInventoryConsumptionPolicy.Consume(profile.Inventory, fixedRequirements);
            inventory = consumption.Inventory;
            if (consumption.Consumption.IsEmpty)
                questConsumptions.Remove(questId);
            else
                questConsumptions[questId] = consumption.Consumption;
        }

        var completed = new HashSet<string>(profile.CompletedQuestIds, StringComparer.Ordinal)
        {
            questId,
        };
        var failed = new HashSet<string>(profile.FailedQuestIds, StringComparer.Ordinal);
        failed.Remove(questId);

        var updated = profile with
        {
            CompletedQuestIds = completed,
            FailedQuestIds = failed,
            Inventory = inventory,
            QuestConsumptions = questConsumptions,
        };

        await _profileStore.SaveAsync(updated, cancellationToken);
        return BuildFromProfile(content, updated);
    }

    public async Task<QuestWorkspace> FailAsync(
        GameContentCatalog content,
        string profileId,
        string questId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(profileId);
        ArgumentException.ThrowIfNullOrWhiteSpace(questId);

        var profile = await LoadRequiredProfileAsync(profileId, cancellationToken);
        var entry = FindQuest(content, profile, questId);
        if (entry.Availability.State != QuestAvailabilityState.Current)
        {
            throw new InvalidOperationException(
                $"Quest '{questId}' can only be failed while it is Current, but it is '{entry.Availability.State}'.");
        }

        if (!entry.Quest.RequiresExplicitFailureInput)
        {
            throw new InvalidOperationException(
                $"Quest '{questId}' does not require manual permanent-failure input.");
        }

        var failed = new HashSet<string>(profile.FailedQuestIds, StringComparer.Ordinal)
        {
            questId,
        };
        var updated = profile with { FailedQuestIds = failed };

        await _profileStore.SaveAsync(updated, cancellationToken);
        return BuildFromProfile(content, updated);
    }

    public async Task<QuestWorkspace> UndoFailureAsync(
        GameContentCatalog content,
        string profileId,
        string questId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(profileId);
        ArgumentException.ThrowIfNullOrWhiteSpace(questId);

        var profile = await LoadRequiredProfileAsync(profileId, cancellationToken);
        if (!profile.FailedQuestIds.Contains(questId))
        {
            throw new InvalidOperationException(
                $"Quest '{questId}' is not explicitly failed in profile '{profileId}'.");
        }

        if (!content.Quests.Any(quest => string.Equals(quest.Id, questId, StringComparison.Ordinal)))
            throw new KeyNotFoundException($"Quest '{questId}' does not exist in the active game content.");

        var failed = new HashSet<string>(profile.FailedQuestIds, StringComparer.Ordinal);
        failed.Remove(questId);
        var updated = profile with { FailedQuestIds = failed };

        await _profileStore.SaveAsync(updated, cancellationToken);
        return BuildFromProfile(content, updated);
    }

    public Task<QuestWorkspace> UndoCompletionAsync(
        GameContentCatalog content,
        string profileId,
        string questId,
        CancellationToken cancellationToken = default) =>
        UndoCompletionAsync(content, profileId, questId, restoreInventory: false, cancellationToken);

    public async Task<QuestWorkspace> UndoCompletionAsync(
        GameContentCatalog content,
        string profileId,
        string questId,
        bool restoreInventory,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(profileId);
        ArgumentException.ThrowIfNullOrWhiteSpace(questId);

        var profile = await LoadRequiredProfileAsync(profileId, cancellationToken);
        if (!profile.CompletedQuestIds.Contains(questId))
        {
            throw new InvalidOperationException(
                $"Quest '{questId}' is not completed in profile '{profileId}'.");
        }

        if (!content.Quests.Any(quest => string.Equals(quest.Id, questId, StringComparison.Ordinal)))
            throw new KeyNotFoundException($"Quest '{questId}' does not exist in the active game content.");

        var completed = new HashSet<string>(profile.CompletedQuestIds, StringComparer.Ordinal);
        completed.Remove(questId);
        var questConsumptions = new Dictionary<string, InventoryConsumption>(
            profile.QuestConsumptions,
            StringComparer.Ordinal);
        var inventory = profile.Inventory;
        if (questConsumptions.TryGetValue(questId, out var consumption) && restoreInventory)
        {
            inventory = FixedInventoryConsumptionPolicy.Restore(inventory, consumption);
            questConsumptions.Remove(questId);
        }
        // Choosing not to restore keeps the ledger so a later re-completion does not
        // automatically consume the same materials again.

        var updated = profile with
        {
            CompletedQuestIds = completed,
            Inventory = inventory,
            QuestConsumptions = questConsumptions,
        };

        await _profileStore.SaveAsync(updated, cancellationToken);
        return BuildFromProfile(content, updated);
    }

    private async Task<GameProfileSnapshot> LoadRequiredProfileAsync(
        string profileId,
        CancellationToken cancellationToken)
    {
        return await _profileStore.LoadAsync(profileId, cancellationToken)
            ?? throw new KeyNotFoundException($"Profile '{profileId}' does not exist.");
    }

    private QuestCatalogEntry FindQuest(
        GameContentCatalog content,
        GameProfileSnapshot profile,
        string questId)
    {
        return BuildFromProfile(content, profile).Quests
            .FirstOrDefault(candidate => string.Equals(candidate.Quest.Id, questId, StringComparison.Ordinal))
            ?? throw new KeyNotFoundException($"Quest '{questId}' does not exist in the active game content.");
    }

    private static QuestWorkspace BuildWorkspace(
        GameContentCatalog content,
        GameProfileSnapshot profile)
    {
        var quests = QuestCatalogQuery.Evaluate(content, profile).ToArray();
        var problems = quests
            .Where(static entry => entry.Availability.State == QuestAvailabilityState.Indeterminate)
            .ToArray();

        // Do not convert opaque upstream conditions into Current. Indeterminate means the
        // program cannot prove the condition from its own authoritative User Progress.
        // Keep it explicit so Current remains a meaningful statement while the user can
        // still manually complete a quest they know is active/completed in the game.
        return new QuestWorkspace(profile, quests, problems);
    }
}
