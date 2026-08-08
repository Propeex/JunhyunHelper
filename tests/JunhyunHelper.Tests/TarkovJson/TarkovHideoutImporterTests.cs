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
                          "foundInRaid": false
                        }
                      ]
                    },
                    {
                      "level": 2,
                      "itemRequirements": [
                        {
                          "item": { "id": "item-bolts" },
                          "quantity": 5
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
        Assert.Equal("item-wire", Assert.Single(station.Levels[0].ItemRequirements).ItemId);
        Assert.Equal(5, Assert.Single(station.Levels[1].ItemRequirements).Count);
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
