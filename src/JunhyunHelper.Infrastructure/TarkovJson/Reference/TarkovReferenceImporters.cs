using System.Text.Json;
using JunhyunHelper.Core.Reference;

namespace JunhyunHelper.Infrastructure.TarkovJson.Reference;

public sealed class TarkovTraderImporter
{
    public IReadOnlyList<TraderDefinition> Import(
        TarkovJsonDocument baseDocument,
        TarkovLocalization localization)
    {
        ArgumentNullException.ThrowIfNull(baseDocument);
        ArgumentNullException.ThrowIfNull(localization);

        var traders = TarkovJsonReader.ReadCollectionValue(baseDocument.Data, "traders");
        var ids = new HashSet<string>(StringComparer.Ordinal);
        var result = new List<TraderDefinition>(traders.Count);

        foreach (var raw in traders)
        {
            if (raw.ValueKind != JsonValueKind.Object)
                throw new InvalidDataException("Trader entries must be objects.");

            var id = TarkovJsonReader.RequiredString(raw, "id", "Trader");
            if (!ids.Add(id))
                throw new InvalidDataException($"Duplicate trader id '{id}'.");

            var name = localization.Resolve(TarkovJsonReader.OptionalString(raw, "name"));
            result.Add(new TraderDefinition(
                id,
                name.Korean,
                name.English,
                TarkovJsonReader.OptionalString(raw, "resetTime")));
        }

        return result;
    }
}

public sealed class TarkovMapReferenceImporter
{
    public IReadOnlyList<MapReference> Import(
        TarkovJsonDocument baseDocument,
        TarkovLocalization localization)
    {
        ArgumentNullException.ThrowIfNull(baseDocument);
        ArgumentNullException.ThrowIfNull(localization);

        var maps = TarkovJsonReader.ReadCollection(baseDocument.Data, "maps");
        var ids = new HashSet<string>(StringComparer.Ordinal);
        var result = new List<MapReference>(maps.Count);

        foreach (var raw in maps)
        {
            if (raw.ValueKind != JsonValueKind.Object)
                throw new InvalidDataException("Map entries must be objects.");

            var id = TarkovJsonReader.RequiredString(raw, "id", "Map");
            if (!ids.Add(id))
                throw new InvalidDataException($"Duplicate map id '{id}'.");

            var name = localization.Resolve(TarkovJsonReader.OptionalString(raw, "name"));
            result.Add(new MapReference(
                id,
                name.Korean,
                name.English,
                TarkovJsonReader.OptionalString(raw, "normalizedName")));
        }

        return result;
    }
}
