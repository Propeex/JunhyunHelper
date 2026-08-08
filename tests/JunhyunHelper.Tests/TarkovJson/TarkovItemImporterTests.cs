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
