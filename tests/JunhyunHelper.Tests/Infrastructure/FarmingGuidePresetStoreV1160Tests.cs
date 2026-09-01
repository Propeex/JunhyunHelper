using JunhyunHelper.Core.FarmingGuide;
using JunhyunHelper.Infrastructure.Storage;
using Xunit;

namespace JunhyunHelper.Tests.Infrastructure;

public sealed class FarmingGuidePresetStoreV1160Tests
{
    [Fact]
    public void SchemaV3RoundTripPreservesStackQuantityAndStrength()
    {
        var root = CreateTempDirectory();
        try
        {
            var store = new FarmingGuidePresetStore(root);
            var snapshot = new FarmingGuideLoadoutSnapshot(
                new Dictionary<FarmingGuideEquipmentSlot, FarmingGuideItemState>(),
                FarmingGuideItemState.Create("rig"),
                null,
                null,
                [
                    new FarmingGuideStoredItemState(
                        "ammo-stack",
                        FarmingGuideItemState.Create("ammo"),
                        FarmingGuideStorageKind.Rig,
                        0,
                        0,
                        0,
                        false,
                        Quantity: 43),
                ]);

            store.SaveWorking("profile-a", snapshot, selectedPresetName: null);
            store.SaveWeightSettings("profile-a", new FarmingGuideWeightSettings(37));
            store.SavePreset("profile-a", "raid", snapshot);

            var reloaded = new FarmingGuidePresetStore(root).LoadProfile("profile-a");
            Assert.Equal(37, reloaded.WeightSettings?.StrengthLevel);
            Assert.Equal(43, Assert.Single(reloaded.WorkingSnapshot.StoredItems).Quantity);
            Assert.Equal(43, Assert.Single(Assert.Single(reloaded.Presets).Snapshot.StoredItems).Quantity);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void LegacyMissingQuantityAndStrengthNormalizeToOneAndZero()
    {
        var root = CreateTempDirectory();
        try
        {
            File.WriteAllText(
                Path.Combine(root, "farming-guide.json"),
                """
                {
                  "schemaVersion": 2,
                  "profiles": {
                    "profile-a": {
                      "workingSnapshot": {
                        "equipment": {},
                        "rig": null,
                        "backpack": null,
                        "secureContainer": null,
                        "storedItems": [
                          {
                            "instanceId": "legacy",
                            "item": { "itemId": "ammo", "attachments": {}, "armorPlates": {} },
                            "storage": "Pockets",
                            "gridIndex": 0,
                            "x": 0,
                            "y": 0,
                            "rotated": false,
                            "parentInstanceId": null
                          }
                        ]
                      },
                      "selectedPresetName": null,
                      "presets": [],
                      "locks": { "equipmentSlots": [], "carriers": [], "itemInstanceIds": [], "reservedCells": [] }
                    }
                  },
                  "fixedEquipment": { "melee": null, "dogtag": null }
                }
                """);

            var loaded = new FarmingGuidePresetStore(root).LoadProfile("profile-a");
            Assert.Equal(1, Assert.Single(loaded.WorkingSnapshot.StoredItems).Quantity);
            Assert.Equal(0, loaded.WeightSettings?.StrengthLevel);
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
