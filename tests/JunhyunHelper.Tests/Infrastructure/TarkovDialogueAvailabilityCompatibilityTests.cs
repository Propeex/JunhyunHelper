using JunhyunHelper.Core.Profiles;
using JunhyunHelper.Core.Quests;
using JunhyunHelper.Infrastructure.TarkovJson.Quests;
using Xunit;

namespace JunhyunHelper.Tests.Infrastructure;

public sealed class TarkovDialogueAvailabilityCompatibilityTests
{
    private const string TherapistTraderId = "54cb57776803fa99248b456e";
    private const string SkierTraderId = "58330581ace78e27b8b10cee";
    private const string MechanicTraderId = "5a7c2eca46aef81a7ca2145d";

    [Fact]
    public void AuditedRootDialogueQuest_BecomesDeterministic()
    {
        var quest = Quest(
            "657315ddab5a49b71f098853",
            TherapistTraderId,
            minimumLevel: 1,
            unsupported: ["dialogue"]);

        var result = TarkovDialogueAvailabilityCompatibility.Apply([quest]).Single();

        Assert.Empty(result.UnsupportedAvailabilityRequirements);
        Assert.Empty(result.TaskRequirements);
        Assert.Equal(1, result.MinimumPlayerLevel);
    }

    [Fact]
    public void SupplierDialogueQuest_RestoresBurningRubberCompletionAndLevelGate()
    {
        var burningRubber = Quest("657315e270bb0b8dba00cc48", SkierTraderId);
        var supplier = Quest(
            "596b36c586f77450d6045ad2",
            SkierTraderId,
            unsupported: ["dialogue"]);

        var result = TarkovDialogueAvailabilityCompatibility.Apply([burningRubber, supplier]);
        var mapped = result.Single(quest => quest.Id == supplier.Id);

        Assert.Empty(mapped.UnsupportedAvailabilityRequirements);
        Assert.Equal(5, mapped.MinimumPlayerLevel);
        var requirement = Assert.Single(mapped.TaskRequirements);
        Assert.Equal(burningRubber.Id, requirement.RequiredQuestId);
        Assert.Equal(QuestRequiredStatus.Complete, Assert.Single(requirement.AcceptedStatuses));

        var locked = QuestAvailabilityEvaluator.Evaluate(
            result,
            Profile(level: 10))[supplier.Id];
        Assert.Equal(QuestAvailabilityState.Locked, locked.State);

        var current = QuestAvailabilityEvaluator.Evaluate(
            result,
            Profile(
                level: 10,
                completedQuestIds: new HashSet<string>([burningRubber.Id], StringComparer.Ordinal)))[supplier.Id];
        Assert.Equal(QuestAvailabilityState.Current, current.State);
    }

    [Fact]
    public void Introduction_RestoresAcceptedGunsmithRequirement()
    {
        var gunsmith = Quest("5ac23c6186f7741247042bad", MechanicTraderId);
        var introduction = Quest(
            "5d2495a886f77425cd51e403",
            MechanicTraderId,
            unsupported: ["dialogue"]);

        var result = TarkovDialogueAvailabilityCompatibility.Apply([gunsmith, introduction]);
        var mapped = result.Single(quest => quest.Id == introduction.Id);

        Assert.Equal(2, mapped.MinimumPlayerLevel);
        var requirement = Assert.Single(mapped.TaskRequirements);
        Assert.Equal(gunsmith.Id, requirement.RequiredQuestId);
        Assert.Equal(QuestRequiredStatus.Active, Assert.Single(requirement.AcceptedStatuses));
    }

    [Fact]
    public void UnknownDialogueQuest_RemainsIndeterminate()
    {
        var quest = Quest("future-dialogue", MechanicTraderId, unsupported: ["dialogue"]);

        var result = TarkovDialogueAvailabilityCompatibility.Apply([quest]).Single();

        Assert.Equal(["dialogue"], result.UnsupportedAvailabilityRequirements);
        var availability = QuestAvailabilityEvaluator.Evaluate([result], Profile(level: 50))[result.Id];
        Assert.Equal(QuestAvailabilityState.Indeterminate, availability.State);
    }

    [Fact]
    public void AuditedQuest_WithFutureUpstreamTaskRequirement_IsNotOverridden()
    {
        var existing = new QuestTaskRequirement(
            "upstream-prerequisite",
            new HashSet<QuestRequiredStatus> { QuestRequiredStatus.Complete });
        var quest = Quest(
            "596b36c586f77450d6045ad2",
            SkierTraderId,
            requirements: [existing],
            unsupported: ["dialogue"]);

        var result = TarkovDialogueAvailabilityCompatibility.Apply([quest]).Single();

        Assert.Equal(["dialogue"], result.UnsupportedAvailabilityRequirements);
        Assert.Same(existing, Assert.Single(result.TaskRequirements));
    }

    [Fact]
    public void MissingAuditedPrerequisite_KeepsDialogueFailClosed()
    {
        var quest = Quest(
            "596b36c586f77450d6045ad2",
            SkierTraderId,
            unsupported: ["dialogue", "futureCondition"]);

        var result = TarkovDialogueAvailabilityCompatibility.Apply([quest]).Single();

        Assert.Contains("dialogue", result.UnsupportedAvailabilityRequirements);
        Assert.Contains("futureCondition", result.UnsupportedAvailabilityRequirements);
        Assert.Empty(result.TaskRequirements);
    }

    [Fact]
    public void OtherUnsupportedRequirements_ArePreservedWhenDialogueIsResolved()
    {
        var root = Quest(
            "657315ddab5a49b71f098853",
            TherapistTraderId,
            unsupported: ["dialogue", "futureCondition"]);

        var result = TarkovDialogueAvailabilityCompatibility.Apply([root]).Single();

        Assert.DoesNotContain("dialogue", result.UnsupportedAvailabilityRequirements);
        Assert.Equal(["futureCondition"], result.UnsupportedAvailabilityRequirements);
    }

    private static QuestDefinition Quest(
        string id,
        string traderId,
        int minimumLevel = 0,
        IReadOnlyList<QuestTaskRequirement>? requirements = null,
        IReadOnlyList<string>? unsupported = null) =>
        new(
            id,
            null,
            null,
            traderId,
            null,
            null,
            null,
            false,
            false,
            false,
            minimumLevel,
            null,
            null,
            requirements ?? Array.Empty<QuestTaskRequirement>(),
            Array.Empty<QuestTraderStandingRequirement>(),
            Array.Empty<QuestTraderLoyaltyRequirement>(),
            unsupported);

    private static GameProfileSnapshot Profile(
        int level,
        IReadOnlySet<string>? completedQuestIds = null) =>
        new()
        {
            ProfileId = "profile",
            GameMode = GameMode.Regular,
            Level = level,
            Faction = PmcFaction.Usec,
            CompletedQuestIds = completedQuestIds ?? new HashSet<string>(StringComparer.Ordinal),
        };
}
