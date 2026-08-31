using System.Text.Json;
using JunhyunHelper.Infrastructure.TarkovJson;
using JunhyunHelper.Infrastructure.TarkovJson.Items;
using Xunit;

namespace JunhyunHelper.Tests.TarkovJson;

public sealed class TarkovItemImporterTests
{
    [Fact]
    public void ImportsStableIdAndLocalizedDisplayFields()
    {
        var baseDocument = Document("""
            {
              "data": {
                "items": {
                  "item-a": {
                    "id": "item-a",
                    "name": "item-a Name",
                    "shortName": "item-a ShortName",
                    "iconLink": "https://example.test/item-a.png",
                    "wikiLink": "https://example.test/item-a",
                    "categories": ["category-a", { "id": "category-b" }]
                  }
                }
              }
            }
            """);
        var localization = new TarkovLocalization(
            Catalog("{\"data\":{\"item-a Name\":\"아이템 A\",\"item-a ShortName\":\"A\"}}"),
            Catalog("{\"data\":{\"item-a Name\":\"Item A\",\"item-a ShortName\":\"A\"}}"));

        var item = Assert.Single(new TarkovItemImporter().Import(baseDocument, localization));

        Assert.Equal("item-a", item.Id);
        Assert.Equal("아이템 A", item.NameKo);
        Assert.Equal("Item A", item.NameEn);
        Assert.Equal(2, item.CategoryIds.Count);
        Assert.Contains("category-a", item.CategoryIds);
        Assert.Contains("category-b", item.CategoryIds);
    }

    [Fact]
    public void ImportsFarmingGuideStorageSlotsArmorAndConflictContracts()
    {
        var baseDocument = Document("""
            {
              "data": {
                "items": [
                  {
                    "id": "rig-a",
                    "name": "rig-a Name",
                    "categories": ["rig-category"],
                    "types": ["rig"],
                    "blocksHeadphones": true,
                    "conflictingItems": [{ "id": "conflict-a" }],
                    "conflictingSlotIds": ["slot-conflict"],
                    "properties": {
                      "propertiesType": "ItemPropertiesChestRig",
                      "class": 3,
                      "grids": [
                        {
                          "width": 2,
                          "height": 1,
                          "filters": {
                            "allowedCategories": [{ "id": "allowed-category" }],
                            "allowedItems": ["allowed-item"],
                            "excludedCategories": ["excluded-category"],
                            "excludedItems": [{ "id": "excluded-item" }]
                          }
                        }
                      ],
                      "slots": [
                        {
                          "id": "mod-slot",
                          "nameId": "mod_scope",
                          "name": "Scope",
                          "required": true,
                          "filters": {
                            "allowedItems": [{ "id": "scope-a" }]
                          }
                        }
                      ],
                      "armorSlots": [
                        {
                          "id": "front-slot",
                          "nameId": "front",
                          "name": "Front",
                          "allowedPlates": [{ "id": "plate-a" }]
                        },
                        {
                          "id": "back-slot",
                          "nameId": "back",
                          "name": "Back"
                        }
                      ]
                    }
                  }
                ]
              }
            }
            """);

        var item = Assert.Single(
            new TarkovItemImporter().Import(baseDocument, new TarkovLocalization()));
        var layout = Assert.NotNull(item.FarmingGuideData);

        Assert.Equal("ItemPropertiesChestRig", layout.PropertiesType);
        Assert.True(layout.IsArmoredRig);
        Assert.True(layout.BlocksHeadphones);
        Assert.Contains("conflict-a", layout.ConflictingItemIds);
        Assert.Contains("slot-conflict", layout.ConflictingSlotIds);

        var grid = Assert.Single(layout.StorageGrids);
        Assert.Equal(2, grid.Width);
        Assert.Equal(1, grid.Height);
        Assert.Contains("allowed-category", grid.Filters.AllowedCategoryIds);
        Assert.Contains("allowed-item", grid.Filters.AllowedItemIds);
        Assert.Contains("excluded-category", grid.Filters.ExcludedCategoryIds);
        Assert.Contains("excluded-item", grid.Filters.ExcludedItemIds);

        var attachment = Assert.Single(layout.AttachmentSlots);
        Assert.Equal("mod-slot", attachment.Id);
        Assert.Equal("mod_scope", attachment.NameId);
        Assert.True(attachment.Required);
        Assert.Contains("scope-a", attachment.Filters.AllowedItemIds);

        Assert.Equal(2, layout.ArmorSlots.Count);
        var openPlate = layout.ArmorSlots[0];
        Assert.False(openPlate.Locked);
        Assert.Contains("plate-a", openPlate.AllowedPlateIds);
        var lockedPlate = layout.ArmorSlots[1];
        Assert.True(lockedPlate.Locked);
        Assert.Empty(lockedPlate.AllowedPlateIds);
    }

    [Fact]
    public void SupportsArrayCollectionsWithoutChangingCanonicalResult()
    {
        var baseDocument = Document("""
            {
              "data": {
                "items": [
                  { "id": "item-a", "name": "item-a Name" }
                ]
              }
            }
            """);

        var item = Assert.Single(
            new TarkovItemImporter().Import(baseDocument, new TarkovLocalization()));

        Assert.Equal("item-a", item.Id);
        Assert.Equal("item-a Name", item.NameEn);
    }

    [Fact]
    public void UnknownCollectionShapeIsFatalInsteadOfSilentlyBecomingEmpty()
    {
        var baseDocument = Document("""
            {
              "data": {
                "items": "future-shape"
              }
            }
            """);

        var error = Assert.Throws<InvalidDataException>(
            () => new TarkovItemImporter().Import(baseDocument, new TarkovLocalization()));

        Assert.Contains("must be an array or object", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void MissingStableItemIdIsFatal()
    {
        var baseDocument = Document("""
            {
              "data": {
                "items": [
                  { "name": "item-a Name" }
                ]
              }
            }
            """);

        Assert.Throws<InvalidDataException>(
            () => new TarkovItemImporter().Import(baseDocument, new TarkovLocalization()));
    }

    private static TarkovJsonDocument Document(string json)
    {
        using var parsed = JsonDocument.Parse(json);
        return TarkovJsonDocument.Parse(parsed.RootElement);
    }

    private static TarkovTranslationCatalog Catalog(string json) =>
        TarkovTranslationCatalog.FromDocument(Document(json));
}
