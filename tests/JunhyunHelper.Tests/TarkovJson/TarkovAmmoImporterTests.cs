using System.Text.Json;
using JunhyunHelper.Core.Ammo;
using JunhyunHelper.Infrastructure.TarkovJson;
using JunhyunHelper.Infrastructure.TarkovJson.Ammo;
using Xunit;

namespace JunhyunHelper.Tests.TarkovJson;

public sealed class TarkovAmmoImporterTests
{
    [Fact]
    public void OnlyItemPropertiesAmmoBecomesTableAmmo()
    {
        var items = Document("""
            {
              "data": {
                "items": {
                  "ammo-a": {
                    "id": "ammo-a",
                    "types": ["ammo", "noFlea"],
                    "properties": {
                      "propertiesType": "ItemPropertiesAmmo",
                      "caliber": "Caliber556x45NATO",
                      "ammoType": "bullet",
                      "projectileCount": 1,
                      "damage": 54,
                      "armorDamage": 37,
                      "penetrationPower": 31,
                      "fragmentationChance": 0.4,
                      "ricochetChance": 0.2,
                      "accuracyModifier": 0,
                      "recoilModifier": -0.05,
                      "initialSpeed": 922,
                      "heavyBleedModifier": 0.1,
                      "lightBleedModifier": 0.2,
                      "tracer": false,
                      "tracerColor": "none"
                    }
                  },
                  "grenade-a": {
                    "id": "grenade-a",
                    "types": ["ammo", "grenade"],
                    "properties": {
                      "propertiesType": "ItemPropertiesGrenade"
                    }
                  },
                  "box-a": {
                    "id": "box-a",
                    "types": ["ammo", "ammoBox"]
                  }
                }
              }
            }
            """);

        var ammo = Assert.Single(new TarkovAmmoImporter().Import(items, EmptyArray(), EmptyArray()));

        Assert.Equal("ammo-a", ammo.ItemId);
        Assert.Equal("Caliber556x45NATO", ammo.Caliber);
        Assert.Equal(54, ammo.Damage);
        Assert.Equal(31, ammo.PenetrationPower);
        Assert.Equal(922m, ammo.InitialSpeed);
    }

    [Fact]
    public void PreservesPurchaseBarterAndCraftAsSeparateAcquisitionKinds()
    {
        var items = Document("""
            {
              "data": {
                "items": {
                  "ammo-a": {
                    "id": "ammo-a",
                    "buyFromTrader": [
                      {
                        "trader": "trader-a",
                        "price": 44,
                        "priceRUB": 44,
                        "currency": "RUB",
                        "currencyItem": "rubles",
                        "minTraderLevel": 1,
                        "taskUnlock": "quest-purchase",
                        "buyLimit": 400
                      }
                    ],
                    "properties": {
                      "propertiesType": "ItemPropertiesAmmo",
                      "caliber": "Caliber9x19PARA",
                      "ammoType": "bullet",
                      "projectileCount": 1,
                      "damage": 50,
                      "armorDamage": 20,
                      "penetrationPower": 10,
                      "fragmentationChance": 0.1,
                      "ricochetChance": 0.05,
                      "accuracyModifier": 0,
                      "recoilModifier": 0,
                      "initialSpeed": 300,
                      "heavyBleedModifier": 0,
                      "lightBleedModifier": 0,
                      "tracer": false
                    }
                  }
                }
              }
            }
            """);

        var barters = Document("""
            {
              "data": [
                {
                  "id": "barter-a",
                  "trader": "trader-b",
                  "taskUnlock": null,
                  "requiredItems": [
                    { "item": "item-barter", "count": 1.25, "attributes": {} }
                  ],
                  "minTraderLevel": 2,
                  "buyLimit": 3,
                  "offeredItem": {
                    "item": "ammo-a",
                    "count": 10,
                    "attributes": {}
                  }
                }
              ]
            }
            """);

        var crafts = Document("""
            {
              "data": [
                {
                  "id": "craft-a",
                  "requiredItems": [
                    { "item": "powder", "count": 2, "attributes": {} },
                    { "item": "pliers", "count": 1, "attributes": { "tool": true } }
                  ],
                  "requiredQuestItems": [],
                  "station": "workbench",
                  "duration": 3300,
                  "gameEditions": [],
                  "level": 2,
                  "taskUnlock": "quest-craft",
                  "productItem": {
                    "item": "ammo-a",
                    "count": 120,
                    "attributes": {}
                  }
                }
              ]
            }
            """);

        var ammo = Assert.Single(new TarkovAmmoImporter().Import(items, barters, crafts));

        Assert.Equal(3, ammo.Acquisitions.Count);

        var purchase = Assert.Single(ammo.Acquisitions, a => a.Kind == AmmoAcquisitionKind.TraderPurchase);
        Assert.Equal("trader-a", purchase.TraderId);
        Assert.Equal(44m, purchase.Price);
        Assert.Equal("RUB", purchase.CurrencyCode);
        Assert.Equal("rubles", purchase.CurrencyItemId);
        Assert.Equal("quest-purchase", purchase.TaskUnlockQuestId);

        var barter = Assert.Single(ammo.Acquisitions, a => a.Kind == AmmoAcquisitionKind.TraderBarter);
        Assert.Equal("barter-a", barter.ReferenceId);
        Assert.Equal(10m, barter.OutputCount);
        Assert.Equal(1.25m, Assert.Single(barter.Requirements).Count);

        var craft = Assert.Single(ammo.Acquisitions, a => a.Kind == AmmoAcquisitionKind.HideoutCraft);
        Assert.Equal("workbench", craft.StationId);
        Assert.Equal(120m, craft.OutputCount);
        Assert.Equal(3300, craft.DurationSeconds);
        Assert.Contains(craft.Requirements, requirement =>
            requirement.ItemId == "pliers" && requirement.IsTool);
    }

    [Fact]
    public void UnknownAmmoPropertyShapeFailsInsteadOfBeingGuessed()
    {
        var items = Document("""
            {
              "data": {
                "items": {
                  "ammo-a": {
                    "id": "ammo-a",
                    "properties": {
                      "propertiesType": "ItemPropertiesAmmo",
                      "caliber": "Caliber9x19PARA",
                      "projectileCount": 1,
                      "damage": "fifty",
                      "armorDamage": 20,
                      "penetrationPower": 10,
                      "fragmentationChance": 0,
                      "ricochetChance": 0,
                      "accuracyModifier": 0,
                      "recoilModifier": 0,
                      "initialSpeed": 300,
                      "heavyBleedModifier": 0,
                      "lightBleedModifier": 0
                    }
                  }
                }
              }
            }
            """);

        Assert.Throws<InvalidDataException>(() =>
            new TarkovAmmoImporter().Import(items, EmptyArray(), EmptyArray()));
    }

    private static TarkovJsonDocument EmptyArray() => Document("{\"data\":[]}");

    private static TarkovJsonDocument Document(string json)
    {
        using var document = JsonDocument.Parse(json);
        return TarkovJsonDocument.Parse(document.RootElement);
    }
}
