using System.Text.Json;
using JunhyunHelper.Infrastructure.TarkovJson;
using JunhyunHelper.Infrastructure.TarkovJson.Hideout;
using Xunit;

namespace JunhyunHelper.Tests.TarkovJson;

public sealed class TarkovHideoutImporterTests
{
    [Fact]
    public void ImportsStationLevelsAndMaterialRequirements()
    {
        var baseDocument = Document("""
            {
              "data": {
                "station-workbench": {
                  "id": "station-workbench",
                  "name": "workbench Name",
                  "imageLink": "https://example.test/workbench.png",
                  "levels": [
                    {
                      "level": 1,
                      "constructionTime": 60,
                      "itemRequirements": [
                        {
                          "item": "item-wire",
                          "count": 3,
                          "attributes": {
                            "foundInRaid": true
                          }
                        }
                      ]
                    },
                    {
                      "level": 2,
                      "itemRequirements": [
                        {
                          "item": { "id": "item-bolts" },
                          "quantity": 5,
                          "foundInRaid": false
                        }
                      ]
                    }
                  ]
                }
              }
            }
            """);
        var localization = new TarkovLocalization(
            Catalog("{\"data\":{\"workbench Name\":\"작업대\"}}"),
            Catalog("{\"data\":{\"workbench Name\":\"Workbench\"}}"));

        var station = Assert.Single(new TarkovHideoutImporter().Import(baseDocument, localization));

        Assert.Equal("station-workbench", station.Id);
        Assert.Equal("작업대", station.NameKo);
        Assert.Equal(2, station.Levels.Count);

        var levelOneRequirement = Assert.Single(station.Levels[0].ItemRequirements);
        Assert.Equal("item-wire", levelOneRequirement.ItemId);
        Assert.True(levelOneRequirement.FoundInRaid);

        var levelTwoRequirement = Assert.Single(station.Levels[1].ItemRequirements);
        Assert.Equal(5, levelTwoRequirement.Count);
        Assert.False(levelTwoRequirement.FoundInRaid);
    }

    [Fact]
    public void NestedFoundInRaidMetadataTakesPrecedenceOverLegacyRootField()
    {
        var baseDocument = Document("""
            {
              "data": [
                {
                  "id": "station-a",
                  "levels": [
                    {
                      "level": 2,
                      "itemRequirements": [
                        {
                          "item": "item-tape",
                          "count": 1,
                          "foundInRaid": false,
                          "attributes": {
                            "foundInRaid": true
                          }
                        }
                      ]
                    }
                  ]
                }
              ]
            }
            """);

        var station = Assert.Single(new TarkovHideoutImporter().Import(baseDocument, new TarkovLocalization()));
        var requirement = Assert.Single(Assert.Single(station.Levels).ItemRequirements);

        Assert.True(requirement.FoundInRaid);
    }

    [Fact]
    public void InvalidMaterialReferenceIsFatal()
    {
        var baseDocument = Document("""
            {
              "data": [
                {
                  "id": "station-a",
                  "levels": [
                    {
                      "level": 1,
                      "itemRequirements": [
                        { "item": {}, "count": 1 }
                      ]
                    }
                  ]
                }
              ]
            }
            """);

        Assert.Throws<InvalidDataException>(
            () => new TarkovHideoutImporter().Import(baseDocument, new TarkovLocalization()));
    }

    private static TarkovJsonDocument Document(string json)
    {
        using var parsed = JsonDocument.Parse(json);
        return TarkovJsonDocument.Parse(parsed.RootElement);
    }

    private static TarkovTranslationCatalog Catalog(string json) =>
        TarkovTranslationCatalog.FromDocument(Document(json));
}