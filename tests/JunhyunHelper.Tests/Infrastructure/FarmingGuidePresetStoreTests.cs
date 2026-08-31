using JunhyunHelper.Core.FarmingGuide;
using JunhyunHelper.Infrastructure.Storage;
using Xunit;

namespace JunhyunHelper.Tests.Infrastructure;

public sealed class FarmingGuidePresetStoreTests
{
    [Fact]
    public void PresetRoundTrip_PreservesCompleteRaidStartState()
    {
        var root = CreateTempDirectory();
        try
        {
            var store = new FarmingGuidePresetStore(root);
            var scope = FarmingGuideItemState.Create("scope");
            var plate = FarmingGuideItemState.Create("front-plate");
            var weapon = new FarmingGuideItemState(
                "weapon",
                new Dictionary<string, FarmingGuideItemState?> { ["scope-slot"] = scope },
                new Dictionary<string, FarmingGuideItemState?>());
            var armor = new FarmingGuideItemState(
                "armor",
                new Dictionary<string, FarmingGuideItemState?>(),
                new Dictionary<string, FarmingGuideItemState?> { ["front"] = plate });
            var snapshot = new FarmingGuideLoadoutSnapshot(
                new Dictionary<FarmingGuideEquipmentSlot, FarmingGuideItemState>
                {
                    [FarmingGuideEquipmentSlot.PrimaryWeapon1] = weapon,
                    [FarmingGuideEquipmentSlot.BodyArmor] = armor,
                },
                FarmingGuideItemState.Create("rig"),
                FarmingGuideItemState.Create("backpack"),
                FarmingGuideItemState.Create("secure"),
                [
                    new FarmingGuideStoredItemState(
                        "instance-1",
                        FarmingGuideItemState.Create("loot"),
                        FarmingGuideStorageKind.Backpack,
                        GridIndex: 1,
                        X: 2,
                        Y: 3,
                        Rotated: true),
                ]);

            var saved = store.SavePreset("profile-a", "start", snapshot);
            Assert.Equal("start", saved.SelectedPresetName);

            var reloaded = new FarmingGuidePresetStore(root).LoadProfile("profile-a");
            Assert.Equal("start", reloaded.SelectedPresetName);
            Assert.Single(reloaded.Presets);
            Assert.Equal("weapon", reloaded.WorkingSnapshot.Equipment[FarmingGuideEquipmentSlot.PrimaryWeapon1].ItemId);
            Assert.Equal(
                "scope",
                reloaded.WorkingSnapshot.Equipment[FarmingGuideEquipmentSlot.PrimaryWeapon1]
                    .Attachments["scope-slot"]?.ItemId);
            Assert.Equal(
                "front-plate",
                reloaded.WorkingSnapshot.Equipment[FarmingGuideEquipmentSlot.BodyArmor]
                    .ArmorPlates["front"]?.ItemId);
            Assert.Equal("rig", reloaded.WorkingSnapshot.Rig?.ItemId);
            Assert.Equal("backpack", reloaded.WorkingSnapshot.Backpack?.ItemId);
            Assert.Equal("secure", reloaded.WorkingSnapshot.SecureContainer?.ItemId);
            var stored = Assert.Single(reloaded.WorkingSnapshot.StoredItems);
            Assert.Equal("instance-1", stored.InstanceId);
            Assert.Equal(FarmingGuideStorageKind.Backpack, stored.Storage);
            Assert.Equal(1, stored.GridIndex);
            Assert.Equal(2, stored.X);
            Assert.Equal(3, stored.Y);
            Assert.True(stored.Rotated);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void WorkingStateAndFixedEquipment_AreIndependentFromSavedPreset()
    {
        var root = CreateTempDirectory();
        try
        {
            var store = new FarmingGuidePresetStore(root);
            var original = new FarmingGuideLoadoutSnapshot(
                new Dictionary<FarmingGuideEquipmentSlot, FarmingGuideItemState>
                {
                    [FarmingGuideEquipmentSlot.Helmet] = FarmingGuideItemState.Create("helmet-a"),
                },
                null,
                null,
                null,
                []);
            store.SavePreset("profile-a", "default", original);
            store.SaveFixedEquipment(new FarmingGuideFixedEquipmentState(
                FarmingGuideItemState.Create("melee"),
                FarmingGuideItemState.Create("dogtag")));

            var changed = original with
            {
                Equipment = new Dictionary<FarmingGuideEquipmentSlot, FarmingGuideItemState>
                {
                    [FarmingGuideEquipmentSlot.Helmet] = FarmingGuideItemState.Create("helmet-b"),
                },
            };
            store.SaveWorking("profile-a", changed, selectedPresetName: null);

            var profile = new FarmingGuidePresetStore(root).LoadProfile("profile-a");
            Assert.Null(profile.SelectedPresetName);
            Assert.Equal("helmet-b", profile.WorkingSnapshot.Equipment[FarmingGuideEquipmentSlot.Helmet].ItemId);
            var preset = Assert.Single(profile.Presets);
            Assert.Equal("helmet-a", preset.Snapshot.Equipment[FarmingGuideEquipmentSlot.Helmet].ItemId);

            var fixedEquipment = new FarmingGuidePresetStore(root).LoadFixedEquipment();
            Assert.Equal("melee", fixedEquipment.Melee?.ItemId);
            Assert.Equal("dogtag", fixedEquipment.Dogtag?.ItemId);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "JunhyunHelper.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
