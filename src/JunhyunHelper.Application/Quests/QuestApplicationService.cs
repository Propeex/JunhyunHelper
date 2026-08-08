using JunhyunHelper.Core.Content;
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
        return BuildWorkspace(content, profile);
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
        if (entry.Availability.State != QuestAvailabilityState.Current)
        {
            throw new InvalidOperationException(
                $"Quest '{questId}' can only be completed while it is Current, but it is '{entry.Availability.State}'.");
        }

        var completed = new HashSet<string>(profile.CompletedQuestIds, StringComparer.Ordinal)
        {
            questId,
        };
        var updated = profile with { CompletedQuestIds = completed };

        await _profileStore.SaveAsync(updated, cancellationToken);
        return BuildWorkspace(content, updated);
    }

    public async Task<QuestWorkspace> UndoCompletionAsync(
        GameContentCatalog content,
        string profileId,
        string questId,
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
        var updated = profile with { CompletedQuestIds = completed };

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

    private static QuestCatalogEntry FindQuest(
        GameContentCatalog content,
        GameProfileSnapshot profile,
        string questId)
    {
        return QuestCatalogQuery.Evaluate(content, profile)
            .FirstOrDefault(entry => string.Equals(entry.Quest.Id, questId, StringComparison.Ordinal))
            ?? throw new KeyNotFoundException($"Quest '{questId}' does not exist in the active game content.");
    }

    private static QuestWorkspace BuildWorkspace(
        GameContentCatalog content,
        GameProfileSnapshot profile)
    {
        var quests = QuestCatalogQuery.Evaluate(content, profile);
        var problems = quests
            .Where(entry => entry.Availability.State == QuestAvailabilityState.Indeterminate)
            .ToArray();

        return new QuestWorkspace(profile, quests, problems);
    }
}
