using System.Text.Json;
using JunhyunHelper.Infrastructure.TarkovJson;
using JunhyunHelper.Infrastructure.TarkovJson.Items;
using Xunit;

namespace JunhyunHelper.Tests.Infrastructure;

public sealed class TarkovItemImporterStorageLayoutTests
{
    [Fact]
    public void Import_PrefersGridLayoutNameOverRigLayoutName()
    {
        var item = ImportSingle("""
            {
              "items": [
                {
                  "id": "layout-item",
                  "properties": {
                    "GridLayoutName": "grid-layout",
                    "RigLayoutName": "rig-layout"
                  }
                }
              ]
            }
            """);

        Assert.Equal("grid-layout", item.FarmingGuideData?.StorageLayoutName);
    }

    [Fact]
    public void Import_UsesRigLayoutNameWhenGridLayoutNameMissing()
    {
        var item = ImportSingle("""
            {
              "items": [
                {
                  "id": "layout-item",
                  "properties": {
                    "rigLayoutName": "mbss_rig"
                  }
                }
              ]
            }
            """);

        Assert.Equal("mbss_rig", item.FarmingGuideData?.StorageLayoutName);
    }

    [Fact]
    public void Import_PreservesLayoutIdentityWithoutOtherFarmingGuideData()
    {
        var item = ImportSingle("""
            {
              "items": [
                {
                  "id": "layout-item",
                  "properties": {
                    "gridLayoutName": "A18"
                  }
                }
              ]
            }
            """);

        Assert.NotNull(item.FarmingGuideData);
        Assert.Equal("A18", item.FarmingGuideData.StorageLayoutName);
        Assert.Empty(item.FarmingGuideData.StorageGrids);
    }

    private static Core.Items.GameItem ImportSingle(string json)
    {
        using var document = JsonDocument.Parse(json);
        var source = new TarkovJsonDocument(document.RootElement.Clone(), []);
        return Assert.Single(new TarkovItemImporter().Import(source, new TarkovLocalization()));
    }
}
