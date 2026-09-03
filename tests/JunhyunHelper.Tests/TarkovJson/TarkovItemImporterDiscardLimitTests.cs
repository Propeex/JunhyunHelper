using System.Text.Json;
using JunhyunHelper.Infrastructure.TarkovJson;
using JunhyunHelper.Infrastructure.TarkovJson.Items;
using Xunit;

namespace JunhyunHelper.Tests.TarkovJson;

public sealed class TarkovItemImporterDiscardLimitTests
{
    [Fact]
    public void PreservesTopLevelUnlimitedAndLimitedDiscardValuesExactly()
    {
        var document = Document("""
            {
              "data": {
                "items": [
                  { "id": "unlimited", "name": "Unlimited", "discardLimit": -1 },
                  { "id": "limited", "name": "Limited", "discardLimit": 3 }
                ]
              }
            }
            """);

        var items = new TarkovItemImporter().Import(document, new TarkovLocalization())
            .ToDictionary(item => item.Id, StringComparer.Ordinal);

        Assert.Equal(-1, items["unlimited"].DiscardLimit);
        Assert.Equal(3, items["limited"].DiscardLimit);
    }

    [Fact]
    public void ReadsNestedDiscardLimitForCompatibleFutureOrAlternateShape()
    {
        var document = Document("""
            {
              "data": {
                "items": [
                  {
                    "id": "nested",
                    "name": "Nested",
                    "properties": { "discardLimit": 7 }
                  }
                ]
              }
            }
            """);

        var item = Assert.Single(new TarkovItemImporter().Import(document, new TarkovLocalization()));

        Assert.Equal(7, item.DiscardLimit);
    }

    [Fact]
    public void MissingDiscardLimitRemainsUnknown()
    {
        var document = Document("""
            {
              "data": {
                "items": [
                  { "id": "unknown", "name": "Unknown" }
                ]
              }
            }
            """);

        var item = Assert.Single(new TarkovItemImporter().Import(document, new TarkovLocalization()));

        Assert.Null(item.DiscardLimit);
    }

    private static TarkovJsonDocument Document(string json)
    {
        using var parsed = JsonDocument.Parse(json);
        return TarkovJsonDocument.Parse(parsed.RootElement);
    }
}
