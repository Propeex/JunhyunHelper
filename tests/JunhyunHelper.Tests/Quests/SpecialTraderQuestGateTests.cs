using JunhyunHelper.Core.Profiles;
using JunhyunHelper.Core.Quests;
using JunhyunHelper.Infrastructure.TarkovJson;
using Xunit;

namespace JunhyunHelper.Tests.Quests;

public sealed class SpecialTraderQuestGateTests
{
    private const string LightkeeperTrader = "638f541a29ffd1183d187f57";
    private const string LightkeeperUnlock = "625d700cc48e6c62a440fab5";
    private const string BtrTrader = "656f0f98d80a697f855d34b1";
    private const string BtrUnlock = "6752f6d83038f7df520c83e8";
    private const string RefTrader = "6617beeaa9cfa777ca915b7c";
    private const string RefRegularUnlock = "66058cb22cee99303f1ba067";
    private const string RefPveUnlock = "6834145ebc1f443d7603c8a7";

    [Fact]
    public void AddsCompleteGateForCurrentSpecialTraderUnlocks()
    {
        var quests = new[]
        {
            Quest(LightkeeperUnlock),
            Quest(BtrUnlock),
            Quest(RefRegularUnlock),
            Quest("lightkeeper-followup", LightkeeperTrader),
            Quest("btr-followup", BtrTrader),
            Quest("ref-followup", RefTrader),
        };

        var result = TarkovGameContentImporter.ApplySpecialTraderAccessRequirements(
            quests,
            GameMode.Regular);

        AssertCompleteGate(result, "lightkeeper-followup", LightkeeperUnlock);
        AssertCompleteGate(result, "btr-followup", BtrUnlock);
        AssertCompleteGate(result, "ref-followup", RefRegularUnlock);
    }

    [Fact]
    public void UsesPveSpecificRefUnlockQuest()
    {
        var quests = new[]
        {
            Quest(RefPveUnlock),
            Quest("ref-followup", RefTrader),
        };

        var result = TarkovGameContentImporter.ApplySpecialTraderAccessRequirements(
            quests,
            GameMode.Pve);

        AssertCompleteGate(result, "ref-followup", RefPveUnlock);
    }

    [Fact]
    public void DoesNotInjectReferenceToUnlockQuestMissingFromCurrentMode()
    {
        var quests = new[]
        {
            Quest("season-ref-task", RefTrader),
        };

        var result = TarkovGameContentImporter.ApplySpecialTraderAccessRequirements(
            quests,
            GameMode.PvpSeason);

        var quest = Assert.Single(result);
        Assert.Empty(quest.TaskRequirements);
    }

    [Fact]
    public void ExistingActiveGateIsStrengthenedWithoutDuplicate()
    {
        var quests = new[]
        {
            Quest(LightkeeperUnlock),
            Quest(
                "lightkeeper-followup",
                LightkeeperTrader,
                [new QuestTaskRequirement(
                    LightkeeperUnlock,
                    new HashSet<QuestRequiredStatus> { QuestRequiredStatus.Active })]),
        };

        var result = TarkovGameContentImporter.ApplySpecialTraderAccessRequirements(
            quests,
            GameMode.Regular);

        var quest = Assert.Single(result, candidate => candidate.Id == "lightkeeper-followup");
        var gate = Assert.Single(quest.TaskRequirements);
        Assert.Equal(LightkeeperUnlock, gate.RequiredQuestId);
        Assert.Equal([QuestRequiredStatus.Complete], gate.AcceptedStatuses);
    }

    private static void AssertCompleteGate(
        IReadOnlyList<QuestDefinition> quests,
        string questId,
        string unlockQuestId)
    {
        var quest = Assert.Single(quests, candidate => candidate.Id == questId);
        var gate = Assert.Single(
            quest.TaskRequirements,
            requirement => requirement.RequiredQuestId == unlockQuestId);
        Assert.Contains(QuestRequiredStatus.Complete, gate.AcceptedStatuses);
    }

    private static QuestDefinition Quest(
        string id,
        string? traderId = null,
        IReadOnlyList<QuestTaskRequirement>? requirements = null) =>
        new(
            Id: id,
            NameKo: id,
            NameEn: id,
            TraderId: traderId,
            MapId: null,
            WikiUrl: null,
            Experience: null,
            KappaRequired: false,
            LightkeeperRequired: false,
            Disabled: false,
            MinimumPlayerLevel: 1,
            RequiredFaction: null,
            RequiredPrestigeLevel: null,
            TaskRequirements: requirements ?? [],
            TraderStandingRequirements: [],
            TraderLoyaltyRequirements: []);
}
