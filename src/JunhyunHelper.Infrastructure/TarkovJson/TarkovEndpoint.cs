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
    public static string GameModeSegment(GameMode gameMode) => gameMode switch
    {
        GameMode.Regular => "regular",
        GameMode.Pve => "pve",
        GameMode.PvpSeason => "pvp-season",
        _ => throw new ArgumentOutOfRangeException(nameof(gameMode), gameMode, null),
    };

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
