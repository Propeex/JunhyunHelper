using JunhyunHelper.Core.Content;
using JunhyunHelper.Core.Quests;
using JunhyunHelper.Core.Reference;
using JunhyunHelper.Infrastructure.Validation;
using Xunit;

namespace JunhyunHelper.Tests.Validation;

public sealed class QuestPrerequisiteGraphValidationTests
{
    [Fact]
    public void DuplicatePrerequisiteTargetIsFatal()
    {
        var content = Catalog(
            Quest("a",
                Requirement("b", QuestRequiredStatus.Complete),
                Requirement("b", QuestRequiredStatus.Active)),
            Quest("b"));

        var result = new GameContentValidator().Validate(content);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, issue => issue.Code == "quest.prerequisite.duplicate");
    }

    [Fact]
    public void DependencyCycleIsFatal()
    {
        var content = Catalog(
            Quest("a", Requirement("b", QuestRequiredStatus.Complete)),
            Quest("b", Requirement("a", QuestRequiredStatus.Complete)));

        var result = new GameContentValidator().Validate(content);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, issue => issue.Code == "quest.prerequisite.cycle");
    }

    [Fact]
    public void EmptyAcceptedStatusSetIsFatal()
    {
        var content = Catalog(
            Quest("a", new QuestTaskRequirement("b", new HashSet<QuestRequiredStatus>())),
            Quest("b"));

        var result = new GameContentValidator().Validate(content);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, issue => issue.Code == "quest.prerequisite.status-empty");
    }

    [Fact]
    public void SpecialAccessCannotDuplicateOrdinaryUnlockGate()
    {
        var unlock = Quest("unlock");
        var gated = Quest(
            "gated",
            Requirement("unlock", QuestRequiredStatus.Complete)) with
        {
            TraderId = "special-trader",
            SpecialTraderAccessRequirement = new QuestSpecialTraderAccessRequirement(
                "special-trader",
                "unlock",
                new HashSet<QuestRequiredStatus> { QuestRequiredStatus.Complete },
                AllowManualOverride: true),
        };
        var content = Catalog(gated, unlock) with
        {
            Traders = [new TraderDefinition("special-trader", "특수 상인", "Special Trader")],
        };

        var result = new GameContentValidator().Validate(content);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, issue => issue.Code == "quest.special-trader-access.duplicate-gate");
    }

    private static GameContentCatalog Catalog(params QuestDefinition[] quests) => new(
        Items: [],
        Traders: [],
        Maps: [],
        Quests: quests,
        QuestObjectives: [],
        QuestItemRequirements: [],
        HideoutStations: [],
        Ammo: [],
        EditionData: []);

    private static QuestTaskRequirement Requirement(
        string questId,
        params QuestRequiredStatus[] statuses) =>
        new(questId, new HashSet<QuestRequiredStatus>(statuses));

    private static QuestDefinition Quest(
        string id,
        params QuestTaskRequirement[] requirements) => new(
        Id: id,
        NameKo: id,
        NameEn: id,
        TraderId: null,
        MapId: null,
        WikiUrl: null,
        Experience: null,
        KappaRequired: false,
        LightkeeperRequired: false,
        Disabled: false,
        MinimumPlayerLevel: 1,
        RequiredFaction: null,
        RequiredPrestigeLevel: null,
        TaskRequirements: requirements,
        TraderStandingRequirements: [],
        TraderLoyaltyRequirements: []);
}
