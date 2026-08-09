using System.Text.Json;
using JunhyunHelper.Core.Maps;
using JunhyunHelper.Core.Quests;
using JunhyunHelper.Infrastructure.TarkovJson;
using JunhyunHelper.Infrastructure.TarkovJson.Maps;
using JunhyunHelper.Infrastructure.TarkovJson.Quests;
using Xunit;

namespace JunhyunHelper.Tests.Infrastructure;

public sealed class MapContentImporterTests
{
    [Fact]
    public void MapMarkerImporter_ClassifiesDynamicMapMarkers()
    {
        var source = Source(
            """
            {
              "maps": {
                "map-1": {
                  "id": "map-1",
                  "extracts": [
                    { "id": "pmc-exit", "name": "PMC Exit", "faction": "pmc", "position": { "x": 10, "y": 2, "z": 20 } },
                    { "id": "scav-exit", "name": "Scav Exit", "faction": "scav", "position": { "x": 30, "y": 3, "z": 40 } }
                  ],
                  "transits": [
                    { "id": "transit-1", "name": "Transit", "position": { "x": 50, "y": 4, "z": 60 } }
                  ],
                  "spawns": [
                    { "id": "pmc-spawn", "categories": ["player"], "sides": ["pmc"], "position": { "x": 70, "y": 5, "z": 80 } },
                    { "id": "sniper-spawn", "categories": ["sniper"], "sides": ["scav"], "position": { "x": 90, "y": 6, "z": 100 } }
                  ],
                  "hazards": [],
                  "locks": [],
                  "switches": [],
                  "stationaryWeapons": [],
                  "btrStops": [],
                  "lootContainers": [],
                  "lootLoose": []
                }
              }
            }
            """);

        var markers = new TarkovMapMarkerImporter().Import(source, new TarkovLocalization());

        Assert.Collection(
            markers.OrderBy(marker => marker.Id, StringComparer.Ordinal),
            marker => Assert.Equal(MapMarkerKind.PmcExtract, marker.Kind),
            marker => Assert.Equal(MapMarkerKind.PmcSpawn, marker.Kind),
            marker => Assert.Equal(MapMarkerKind.ScavExtract, marker.Kind),
            marker => Assert.Equal(MapMarkerKind.SniperScav, marker.Kind),
            marker => Assert.Equal(MapMarkerKind.Transit, marker.Kind));
        Assert.All(markers, marker => Assert.Equal("map-1", marker.MapId));
    }

    [Fact]
    public void QuestObjectiveImporter_PreservesPossibleLocationsAndZones()
    {
        var source = Source(
            """
            {
              "tasks": {
                "quest-1": {
                  "id": "quest-1",
                  "objectives": [
                    {
                      "id": "objective-1",
                      "type": "visit",
                      "description": "Visit the marked location",
                      "optional": false,
                      "maps": [],
                      "possibleLocations": [
                        {
                          "map": { "id": "map-1" },
                          "positions": [
                            { "x": 100, "y": 7, "z": 200 },
                            { "x": 110, "y": 8, "z": 210 }
                          ]
                        }
                      ],
                      "zones": [
                        {
                          "map": { "id": "map-1" },
                          "position": { "x": 120, "y": 9, "z": 220 },
                          "top": 12,
                          "bottom": 5,
                          "outline": [
                            { "x": 115, "z": 215 },
                            { "x": 125, "z": 215 },
                            { "x": 125, "z": 225 }
                          ]
                        }
                      ]
                    }
                  ]
                }
              }
            }
            """);

        var imported = new TarkovQuestObjectiveImporter().Import(source, new TarkovLocalization());
        var objective = Assert.Single(imported.Objectives);

        Assert.Equal("quest-1", objective.QuestId);
        Assert.Equal(["map-1"], objective.MapIds);
        Assert.Equal(3, objective.MapLocations.Count);
        Assert.Equal(2, objective.MapLocations.Count(location => location.Kind == QuestMapLocationKind.PossibleLocation));
        var zone = Assert.Single(objective.MapLocations.Where(location => location.Kind == QuestMapLocationKind.Zone));
        Assert.Equal(new MapWorldPosition(120, 9, 220), zone.Position);
        Assert.Equal(3, zone.Outline.Count);
        Assert.Equal(12, zone.Top);
        Assert.Equal(5, zone.Bottom);
    }

    private static TarkovJsonDocument Source(string dataJson)
    {
        using var document = JsonDocument.Parse(dataJson);
        return new TarkovJsonDocument(document.RootElement.Clone(), Array.Empty<string>());
    }
}
