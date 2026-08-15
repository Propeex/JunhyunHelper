using JunhyunHelper.Application.Quests;
using JunhyunHelper.Core.Content;
using JunhyunHelper.Core.Profiles;
using JunhyunHelper.Core.Quests;
using JunhyunHelper.Core.Reference;
using JunhyunHelper.Infrastructure.Storage;
using Xunit;

namespace JunhyunHelper.Tests.Application;

public sealed class SpecialTraderAccessApplicationTests
{
    private const string TraderId = "lightkeeper";
    private const string UnlockId = "getting-acquainted";

    [Fact]
    public async Task CannotSynchronizeAccessBeforeInitialUnlockResolves()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var directory = CreateTempDirectory();
        try
        {
            var store = new UserProfileStore(Path.Combine(directory, "user.db"));
            await store.SaveAsync(Profile(), cancellationToken);
            var service = new QuestApplicationService(store);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.SetSpecialTraderAccessAsync(
                    Content(),
                    "profile",
                    TraderId,
                    accessAvailable: true,
                    cancellationToken));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task AccessLossAfterInitialCompletionPersistsAsSparseOverride()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var directory = CreateTempDirectory();
        try
        {
            var store = new UserProfileStore(Path.Combine(directory, "user.db"));
            await store.SaveAsync(Profile() with
            {
                CompletedQuestIds = new HashSet<string>([UnlockId], StringComparer.Ordinal),
            }, cancellationToken);
            var service = new QuestApplicationService(store);

            var workspace = await service.SetSpecialTraderAccessAsync(
                Content(),
                "profile",
                TraderId,
                accessAvailable: false,
                cancellationToken);

            Assert.False(workspace.Profile.SpecialTraderAccessOverrides[TraderId]);
            Assert.Equal(
                QuestAvailabilityState.Locked,
                workspace.Quests.Single(entry => entry.Quest.Id == "followup").Availability.State);

            var coldStore = new UserProfileStore(Path.Combine(directory, "user.db"));
            var persisted = await coldStore.LoadAsync("profile", cancellationToken);
            Assert.NotNull(persisted);
            Assert.False(persisted.SpecialTraderAccessOverrides[TraderId]);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task RecoveryAfterExplicitUnlockFailureAllowsFollowup()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var directory = CreateTempDirectory();
        try
        {
            var store = new UserProfileStore(Path.Combine(directory, "user.db"));
            await store.SaveAsync(Profile() with
            {
                FailedQuestIds = new HashSet<string>([UnlockId], StringComparer.Ordinal),
            }, cancellationToken);
            var service = new QuestApplicationService(store);

            var workspace = await service.SetSpecialTraderAccessAsync(
                Content(),
                "profile",
                TraderId,
                accessAvailable: true,
                cancellationToken);

            Assert.True(workspace.Profile.SpecialTraderAccessOverrides[TraderId]);
            Assert.Equal(
                QuestAvailabilityState.Current,
                workspace.Quests.Single(entry => entry.Quest.Id == "followup").Availability.State);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static GameContentCatalog Content() => new(
        Items: [],
        Traders: [new TraderDefinition(TraderId, "라이트키퍼", "Lightkeeper")],
        Maps: [],
        Quests: [UnlockQuest(), FollowupQuest()],
        QuestObjectives: [],
        QuestItemRequirements: [],
        HideoutStations: [],
        Ammo: [],
        EditionData: []);

    private static QuestDefinition UnlockQuest() => new(
        Id: UnlockId,
        NameKo: UnlockId,
        NameEn: UnlockId,
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
        TaskRequirements: [],
        TraderStandingRequirements: [],
        TraderLoyaltyRequirements: [],
        Restartable: false,
        UnsupportedFailureConditionTypes: ["traderStanding"]);

    private static QuestDefinition FollowupQuest() => new(
        Id: "followup",
        NameKo: "후속",
        NameEn: "Followup",
        TraderId: TraderId,
        MapId: null,
        WikiUrl: null,
        Experience: null,
        KappaRequired: false,
        LightkeeperRequired: true,
        Disabled: false,
        MinimumPlayerLevel: 1,
        RequiredFaction: null,
        RequiredPrestigeLevel: null,
        TaskRequirements: [],
        TraderStandingRequirements: [],
        TraderLoyaltyRequirements: [],
        SpecialTraderAccessRequirement: new QuestSpecialTraderAccessRequirement(
            TraderId,
            UnlockId,
            new HashSet<QuestRequiredStatus> { QuestRequiredStatus.Complete },
            AllowManualOverride: true));

    private static GameProfileSnapshot Profile() => new()
    {
        ProfileId = "profile",
        GameMode = GameMode.Regular,
        Level = 60,
        Faction = PmcFaction.Usec,
    };

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"JunhyunHelperTests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }
}
