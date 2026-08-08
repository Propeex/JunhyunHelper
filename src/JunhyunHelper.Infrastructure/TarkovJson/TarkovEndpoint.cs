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
    public static string GameMode(GameMode gameMode) => gameMode switch
    {
        Profiles.GameMode.Regular => "regular",
        Profiles.GameMode.Pve => "pve",
        Profiles.GameMode.PvpSeason => "pvp-season",
        _ => throw new ArgumentOutOfRangeException(nameof(gameMode), gameMode, null),
    };

    public static string Endpoint(TarkovEndpoint endpoint) => endpoint switch
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
