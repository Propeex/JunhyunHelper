namespace JunhyunHelper.Core.Editions;

public sealed record EditionDefinition(
    string Id,
    string Title,
    IReadOnlySet<string> ExclusiveQuestIds,
    IReadOnlySet<string> ExcludedQuestIds);
