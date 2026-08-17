using JunhyunHelper.Core.Quests;

namespace JunhyunHelper.Infrastructure.TarkovJson.Quests;

/// <summary>
/// Resolves the small, audited set of live EFT quests whose current task feed exposes
/// availability only as an opaque trader dialogue gate. The public task payload does not
/// publish what action unlocks each dialogue, so these exact quest IDs are mapped from a
/// 2026-08-17 live-data + current quest-progression audit.
///
/// This compatibility layer is deliberately fail-closed:
/// - it runs only while the quest still has an opaque "dialogue" requirement;
/// - it runs only while upstream still publishes zero ordinary task requirements;
/// - it verifies the expected trader and every referenced prerequisite exists;
/// - every other unsupported requirement is preserved;
/// - any future/new dialogue quest remains unresolved.
///
/// If upstream begins publishing explicit taskRequirements, those source rules win and
/// this compatibility layer automatically stops applying to that quest.
/// </summary>
internal static class TarkovDialogueAvailabilityCompatibility
{
    private const string DialogueRequirementType = "dialogue";

    private const string TherapistTraderId = "54cb57776803fa99248b456e";
    private const string SkierTraderId = "58330581ace78e27b8b10cee";
    private const string MechanicTraderId = "5a7c2eca46aef81a7ca2145d";
    private const string RagmanTraderId = "5ac3b934156ae10c4430e83c";

    private const string FirstInLineQuestId = "657315ddab5a49b71f098853";
    private const string BurningRubberQuestId = "657315e270bb0b8dba00cc48";
    private const string SavingTheMoleQuestId = "657315e4a6af4ab4b50f3459";
    private const string OperationAquariusQuestId = "59689fbd86f7740d137ebfc4";
    private const string SupplyPlansQuestId = "596a0e1686f7741ddf17dbee";
    private const string SupplierQuestId = "596b36c586f77450d6045ad2";
    private const string GunsmithMp133QuestId = "5ac23c6186f7741247042bad";
    private const string MakeUltraGreatAgainQuestId = "5ae448bf86f7744d733e55ee";
    private const string FuelCrisisQuestId = "5ae448f286f77448d73c0131";
    private const string PathfinderQuestId = "5ae449c386f7744bde357697";
    private const string IntroductionQuestId = "5d2495a886f77425cd51e403";
    private const string PassionForErgonomicsQuestId = "675c1570526ff496850895d9";

    private const string ShortageQuestId = "5967733e86f774602332fc84";
    private const string PharmacistQuestId = "5969f9e986f7741dde183a50";
    private const string OnlyBusinessQuestId = "5ae448a386f7744d3730fff0";
    private const string BigSaleQuestId = "5ae448e586f7744dcf0c2a67";
    private const string GratitudeQuestId = "5ae449b386f77446d8741719";
    private const string FarmingPart2QuestId = "5ac3460c86f7742880308185";

    private static readonly IReadOnlyDictionary<string, DialogueQuestRule> Rules =
        new Dictionary<string, DialogueQuestRule>(StringComparer.Ordinal)
        {
            // Ground Zero / introductory trader quests: current public progression data
            // lists no previous quest. Their dialogue is the initial trader/story handoff.
            [FirstInLineQuestId] = new(TherapistTraderId),
            [BurningRubberQuestId] = new(SkierTraderId),
            [SavingTheMoleQuestId] = new(MechanicTraderId),

            // Current feed omits these ordinary progression edges and exposes only a
            // dialogue gate. Restore the audited prerequisite semantics instead of
            // treating the quest as permanently indeterminate.
            [OperationAquariusQuestId] = new(
                TherapistTraderId,
                MinimumPlayerLevel: 6,
                Prerequisites: [Complete(ShortageQuestId)]),
            [SupplyPlansQuestId] = new(
                TherapistTraderId,
                MinimumPlayerLevel: 13,
                Prerequisites: [Complete(PharmacistQuestId)]),
            [SupplierQuestId] = new(
                SkierTraderId,
                MinimumPlayerLevel: 5,
                Prerequisites: [Complete(BurningRubberQuestId)]),
            [GunsmithMp133QuestId] = new(
                MechanicTraderId,
                MinimumPlayerLevel: 2,
                Prerequisites: [Complete(SavingTheMoleQuestId)]),
            [MakeUltraGreatAgainQuestId] = new(
                RagmanTraderId,
                Prerequisites: [Complete(OnlyBusinessQuestId)]),
            // This canonical ID is historically The Blood of War - Part 1; the current
            // live translation feed labels it "Fuel Crisis". The ID is authoritative.
            [FuelCrisisQuestId] = new(
                RagmanTraderId,
                Prerequisites:
                [
                    Complete(BigSaleQuestId),
                    Complete(MakeUltraGreatAgainQuestId),
                ]),
            // This canonical ID is historically Sales Night; the current live translation
            // feed labels it "Pathfinder". Preserve the verified progression edge by ID.
            [PathfinderQuestId] = new(
                RagmanTraderId,
                MinimumPlayerLevel: 30,
                Prerequisites: [Complete(GratitudeQuestId)]),
            [IntroductionQuestId] = new(
                MechanicTraderId,
                MinimumPlayerLevel: 2,
                // Introduction becomes available once the first Gunsmith quest has been
                // accepted; completion is not required.
                Prerequisites: [Active(GunsmithMp133QuestId)]),
            [PassionForErgonomicsQuestId] = new(
                MechanicTraderId,
                Prerequisites: [Complete(FarmingPart2QuestId)]),
        };

    public static IReadOnlyList<QuestDefinition> Apply(IReadOnlyList<QuestDefinition> quests)
    {
        ArgumentNullException.ThrowIfNull(quests);

        var questIds = quests.Select(static quest => quest.Id).ToHashSet(StringComparer.Ordinal);
        return quests.Select(quest => ApplyRule(quest, questIds)).ToArray();
    }

    private static QuestDefinition ApplyRule(
        QuestDefinition quest,
        IReadOnlySet<string> questIds)
    {
        if (!Rules.TryGetValue(quest.Id, out var rule) ||
            !string.Equals(quest.TraderId, rule.TraderId, StringComparison.Ordinal) ||
            quest.TaskRequirements.Count != 0 ||
            !quest.UnsupportedAvailabilityRequirements.Contains(
                DialogueRequirementType,
                StringComparer.Ordinal))
        {
            return quest;
        }

        var prerequisites = rule.Prerequisites ?? Array.Empty<QuestTaskRequirement>();
        if (prerequisites.Any(requirement => !questIds.Contains(requirement.RequiredQuestId)))
            return quest;

        var remainingUnsupported = quest.UnsupportedAvailabilityRequirements
            .Where(type => !string.Equals(type, DialogueRequirementType, StringComparison.Ordinal))
            .ToArray();

        return quest with
        {
            MinimumPlayerLevel = rule.MinimumPlayerLevel is { } minimum
                ? Math.Max(quest.MinimumPlayerLevel, minimum)
                : quest.MinimumPlayerLevel,
            TaskRequirements = prerequisites.ToArray(),
            UnsupportedAvailabilityRequirementTypes = remainingUnsupported.Length == 0
                ? null
                : remainingUnsupported,
        };
    }

    private static QuestTaskRequirement Complete(string questId) =>
        new(questId, new HashSet<QuestRequiredStatus> { QuestRequiredStatus.Complete });

    private static QuestTaskRequirement Active(string questId) =>
        new(questId, new HashSet<QuestRequiredStatus> { QuestRequiredStatus.Active });

    private sealed record DialogueQuestRule(
        string TraderId,
        int? MinimumPlayerLevel = null,
        IReadOnlyList<QuestTaskRequirement>? Prerequisites = null);
}
