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
                        "container-instance",
                        FarmingGuideItemState.Create("nested-bag"),
                        FarmingGuideStorageKind.Backpack,
                        GridIndex: 0,
                        X: 0,
                        Y: 0,
                        Rotated: false),
                    new FarmingGuideStoredItemState(
                        "instance-1",
                        FarmingGuideItemState.Create("loot"),
                        FarmingGuideStorageKind.Backpack,
                        GridIndex: 1,
                        X: 2,
                        Y: 3,
                        Rotated: true,
                        ParentInstanceId: "container-instance"),
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
            Assert.Equal(2, reloaded.WorkingSnapshot.StoredItems.Count);
            var stored = Assert.Single(
                reloaded.WorkingSnapshot.StoredItems,
                item => item.InstanceId == "instance-1");
            Assert.Equal(FarmingGuideStorageKind.Backpack, stored.Storage);
            Assert.Equal(1, stored.GridIndex);
            Assert.Equal(2, stored.X);
            Assert.Equal(3, stored.Y);
            Assert.True(stored.Rotated);
            Assert.Equal("container-instance", stored.ParentInstanceId);
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

            // Dogtags are no longer an equipable Farming Guide surface. Keep accepting the
            // legacy schema-v1 field so existing files deserialize, but normalize it away
            // whenever fixed equipment is saved/read.
            store.SaveFixedEquipment(new FarmingGuideFixedEquipmentState(
                FarmingGuideItemState.Create("melee"),
                FarmingGuideItemState.Create("legacy-dogtag")));

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
            Assert.Null(fixedEquipment.Dogtag);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void DeletePreset_RemovesPresetWithoutDiscardingCurrentWorkingState()
    {
        var root = CreateTempDirectory();
        try
        {
            var store = new FarmingGuidePresetStore(root);
            var first = SnapshotWithHelmet("helmet-a");
            var current = SnapshotWithHelmet("helmet-b");
            store.SavePreset("profile-a", "first", first);
            store.SavePreset("profile-a", "current", current);

            var deleted = store.DeletePreset("profile-a", "CURRENT");

            Assert.Null(deleted.SelectedPresetName);
            var remaining = Assert.Single(deleted.Presets);
            Assert.Equal("first", remaining.Name);
            Assert.Equal(
                "helmet-b",
                deleted.WorkingSnapshot.Equipment[FarmingGuideEquipmentSlot.Helmet].ItemId);

            var reloaded = new FarmingGuidePresetStore(root).LoadProfile("profile-a");
            Assert.Null(reloaded.SelectedPresetName);
            Assert.Single(reloaded.Presets);
            Assert.Equal(
                "helmet-b",
                reloaded.WorkingSnapshot.Equipment[FarmingGuideEquipmentSlot.Helmet].ItemId);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void LoadProfile_SemanticallyPartialJson_IsNormalizedAndRemainsWritable()
    {
        var root = CreateTempDirectory();
        try
        {
            File.WriteAllText(
                Path.Combine(root, "farming-guide.json"),
                """
                {
                  "schemaVersion": 3,
                  "profiles": {
                    "profile-a": {
                      "workingSnapshot": {
                        "equipment": {
                          "Helmet": {
                            "itemId": "helmet-a",
                            "attachments": null,
                            "armorPlates": null
                          }
                        },
                        "rig": null,
                        "backpack": null,
                        "secureContainer": null,
                        "storedItems": [
                          null,
                          {
                            "instanceId": "stack-a",
                            "item": {
                              "itemId": "ammo-a",
                              "attachments": null,
                              "armorPlates": null
                            },
                            "storage": "Pockets",
                            "gridIndex": 0,
                            "x": 0,
                            "y": 0,
                            "rotated": false,
                            "quantity": 0
                          }
                        ]
                      },
                      "selectedPresetName": "missing-preset",
                      "presets": null,
                      "locks": {
                        "equipmentSlots": null,
                        "carriers": null,
                        "itemInstanceIds": null,
                        "reservedCells": null
                      },
                      "weightSettings": {
                        "strengthLevel": 999
                      }
                    }
                  },
                  "fixedEquipment": null
                }
                """);

            var store = new FarmingGuidePresetStore(root);
            var profile = store.LoadProfile("profile-a");

            Assert.Null(profile.SelectedPresetName);
            Assert.Empty(profile.Presets);
            Assert.Equal(51, profile.WeightSettings?.StrengthLevel);
            Assert.Empty(profile.Locks?.EquipmentSlots ?? []);
            Assert.Equal("helmet-a", profile.WorkingSnapshot.Equipment[FarmingGuideEquipmentSlot.Helmet].ItemId);
            Assert.Empty(profile.WorkingSnapshot.Equipment[FarmingGuideEquipmentSlot.Helmet].Attachments);
            var stack = Assert.Single(profile.WorkingSnapshot.StoredItems);
            Assert.Equal("ammo-a", stack.Item.ItemId);
            Assert.Empty(stack.Item.Attachments);
            Assert.Equal(1, stack.Quantity);

            var fixedEquipment = store.LoadFixedEquipment();
            Assert.Null(fixedEquipment.Melee);
            Assert.Null(fixedEquipment.Dogtag);

            store.SaveWorking("profile-a", SnapshotWithHelmet("helmet-b"), selectedPresetName: null);
            var reloaded = new FarmingGuidePresetStore(root).LoadProfile("profile-a");
            Assert.Equal("helmet-b", reloaded.WorkingSnapshot.Equipment[FarmingGuideEquipmentSlot.Helmet].ItemId);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static FarmingGuideLoadoutSnapshot SnapshotWithHelmet(string itemId) =>
        new(
            new Dictionary<FarmingGuideEquipmentSlot, FarmingGuideItemState>
            {
                [FarmingGuideEquipmentSlot.Helmet] = FarmingGuideItemState.Create(itemId),
            },
            null,
            null,
            null,
            []);

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "JunhyunHelper.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
