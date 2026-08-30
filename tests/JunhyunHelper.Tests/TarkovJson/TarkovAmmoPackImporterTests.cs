using System.Text.Json;
using JunhyunHelper.Core.Ammo;
using JunhyunHelper.Core.Items;
using JunhyunHelper.Infrastructure.TarkovJson;
using JunhyunHelper.Infrastructure.TarkovJson.Ammo;
using Xunit;

namespace JunhyunHelper.Tests.TarkovJson;

public sealed class TarkovAmmoPackImporterTests
{
    [Fact]
    public void AuthoritativeContainsItemsMapsPackToCanonicalAmmo()
    {
        var document = Document("""
            {
              "data": {
                "items": {
                  "ammo": { "id": "ammo" },
                  "pack": {
                    "id": "pack",
                    "containsItems": [
                      { "item": { "id": "ammo" }, "count": 50 }
                    ]
                  }
                }
              }
            }
            """);
        var items = new[]
        {
            Item("ammo", "5.56x45mm M855 ammo"),
            Item("pack", "misleading name ammo pack (99 pcs)"),
        };

        var mapping = Assert.Single(new TarkovAmmoPackImporter().Import(
            document,
            items,
            [Ammo("ammo")]));

        Assert.Equal("pack", mapping.PackItemId);
        Assert.Equal("ammo", mapping.AmmoItemId);
        Assert.Equal(50m, mapping.Count);
        Assert.False(mapping.IsNameFallback);
    }

    [Fact]
    public void ExplicitAmmoPackNameFallsBackOnlyWhenRelationshipIsAbsent()
    {
        var document = Document("""
            {
              "data": {
                "items": {
                  "ammo": { "id": "ammo" },
                  "pack": { "id": "pack" }
                }
              }
            }
            """);
        var items = new[]
        {
            Item("ammo", "5.56x45mm M855 ammo"),
            Item("pack", "5.56x45mm M855 ammo pack (100 pcs)"),
        };

        var mapping = Assert.Single(new TarkovAmmoPackImporter().Import(
            document,
            items,
            [Ammo("ammo")]));

        Assert.Equal("ammo", mapping.AmmoItemId);
        Assert.Equal(100m, mapping.Count);
        Assert.True(mapping.IsNameFallback);
    }

    [Fact]
    public void ExistingMixedContainsItemsNeverFallsBackByName()
    {
        var document = Document("""
            {
              "data": {
                "items": {
                  "ammo": { "id": "ammo" },
                  "pack": {
                    "id": "pack",
                    "containsItems": [
                      { "item": "ammo", "count": 50 },
                      { "item": "other", "count": 1 }
                    ]
                  }
                }
              }
            }
            """);
        var items = new[]
        {
            Item("ammo", "5.56x45mm M855 ammo"),
            Item("pack", "5.56x45mm M855 ammo pack (50 pcs)"),
            Item("other", "Other item"),
        };

        var mappings = new TarkovAmmoPackImporter().Import(
            document,
            items,
            [Ammo("ammo")]);

        Assert.Empty(mappings);
    }

    [Fact]
    public void AmbiguousAmmoNameDoesNotProduceFallbackGuess()
    {
        var document = Document("""
            {
              "data": {
                "items": {
                  "ammo-a": { "id": "ammo-a" },
                  "ammo-b": { "id": "ammo-b" },
                  "pack": { "id": "pack" }
                }
              }
            }
            """);
        var items = new[]
        {
            Item("ammo-a", "Test ammo"),
            Item("ammo-b", "Test ammo"),
            Item("pack", "Test ammo pack (20 pcs)"),
        };

        var mappings = new TarkovAmmoPackImporter().Import(
            document,
            items,
            [Ammo("ammo-a"), Ammo("ammo-b")]);

        Assert.Empty(mappings);
    }

    private static GameItem Item(string id, string nameEn) => new(
        Id: id,
        NameKo: null,
        NameEn: nameEn,
        ShortNameKo: null,
        ShortNameEn: null,
        IconUrl: null,
        WikiUrl: null,
        CategoryIds: []);

    private static AmmoDefinition Ammo(string id) => new(
        ItemId: id,
        Caliber: "Caliber556x45NATO",
        AmmoType: null,
        ProjectileCount: 1,
        Damage: 0,
        ArmorDamage: 0,
        PenetrationPower: 30,
        FragmentationChance: 0,
        RicochetChance: 0,
        AccuracyModifier: 0,
        RecoilModifier: 0,
        InitialSpeed: 0,
        HeavyBleedModifier: 0,
        LightBleedModifier: 0,
        Tracer: false,
        TracerColor: null,
        Acquisitions: []);

    private static TarkovJsonDocument Document(string json)
    {
        using var document = JsonDocument.Parse(json);
        return TarkovJsonDocument.Parse(document.RootElement);
    }
}
