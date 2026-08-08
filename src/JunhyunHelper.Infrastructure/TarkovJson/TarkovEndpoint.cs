using JunhyunHelper.Core.Profiles;

namespace JunhyunHelper.Infrastructure.TarkovJson;

public enum TarkovEndpoint
{
    Tasks,
    Hideout,
    Items,
    Traders,
    Maps,
    Barters,
    Crafts,
}

internal static class TarkovSourcePath
{
    public static string GameModeSegment(GameMode gameMode) => gameMode.ToDataKey();

    public static string EndpointSegment(TarkovEndpoint endpoint) => endpoint switch
    {
        TarkovEndpoint.Tasks => "tasks",
        TarkovEndpoint.Hideout => "hideout",
        TarkovEndpoint.Items => "items",
        TarkovEndpoint.Traders => "traders",
        TarkovEndpoint.Maps => "maps",
        TarkovEndpoint.Barters => "barters",
        TarkovEndpoint.Crafts => "crafts",
        _ => throw new ArgumentOutOfRangeException(nameof(endpoint), endpoint, null),
    };
}
