namespace JunhyunHelper.Core.Editions;

public sealed record EditionDefinition(
    string Id,
    string Title,
    HashSet<string> ExclusiveQuestIds,
    HashSet<string> ExcludedQuestIds);
