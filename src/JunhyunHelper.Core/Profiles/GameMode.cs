namespace JunhyunHelper.Core.Profiles;

public enum GameMode
{
    Regular,
    Pve,
    PvpSeason,
}

public static class GameModeExtensions
{
    public static string ToDataKey(this GameMode gameMode) => gameMode switch
    {
        GameMode.Regular => "regular",
        GameMode.Pve => "pve",
        GameMode.PvpSeason => "pvp-season",
        _ => throw new ArgumentOutOfRangeException(nameof(gameMode), gameMode, null),
    };
}
