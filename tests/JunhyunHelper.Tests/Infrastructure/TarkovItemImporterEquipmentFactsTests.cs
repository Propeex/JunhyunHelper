using System.Text.Json;
using JunhyunHelper.Infrastructure.TarkovJson;
using JunhyunHelper.Infrastructure.TarkovJson.Items;
using Xunit;

namespace JunhyunHelper.Tests.Infrastructure;

public sealed class TarkovItemImporterEquipmentFactsTests
{
    [Fact]
    public void Import_PreservesPositiveArmorClassAsFarmingGuideFact()
    {
        var item = ImportSingle("""
            {
              "items": [
                {
                  "id": "armor",
                  "properties": {
                    "propertiesType": "ItemPropertiesArmor",
                    "class": 5
                  }
                }
              ]
            }
            """);

        Assert.Equal(5, item.FarmingGuideData?.ArmorClass);
    }

    [Fact]
    public void Import_UsesArmorClassToIdentifyArmoredChestRigAndKeepsItsGrids()
    {
        var item = ImportSingle("""
            {
              "items": [
                {
                  "id": "armored-rig",
                  "properties": {
                    "propertiesType": "ItemPropertiesChestRig",
                    "class": 4,
                    "grids": [
                      { "width": 2, "height": 3, "filters": {} }
                    ]
                  }
                }
              ]
            }
            """);

        Assert.True(item.FarmingGuideData?.IsArmoredRig);
        Assert.Equal(4, item.FarmingGuideData?.ArmorClass);
        Assert.Equal(6, item.FarmingGuideData?.StorageCapacity);
    }

    [Fact]
    public void Import_DoesNotInventArmorClassWhenSourceIsMissingOrZero()
    {
        var missing = ImportSingle("""
            { "items": [ { "id": "missing", "properties": { "propertiesType": "ItemPropertiesArmor" } } ] }
            """);
        var zero = ImportSingle("""
            { "items": [ { "id": "zero", "properties": { "propertiesType": "ItemPropertiesArmor", "class": 0 } } ] }
            """);

        Assert.Null(missing.FarmingGuideData?.ArmorClass);
        Assert.Null(zero.FarmingGuideData?.ArmorClass);
    }

    private static JunhyunHelper.Core.Items.GameItem ImportSingle(string json)
    {
        using var document = JsonDocument.Parse(json);
        var source = new TarkovJsonDocument(document.RootElement.Clone(), []);
        return Assert.Single(new TarkovItemImporter().Import(source, new TarkovLocalization()));
    }
}
