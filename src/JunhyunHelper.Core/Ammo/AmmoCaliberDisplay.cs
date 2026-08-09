namespace JunhyunHelper.Core.Ammo;

public static class AmmoCaliberDisplay
{
    public static string GetLabel(string? caliber)
    {
        if (string.IsNullOrWhiteSpace(caliber))
            return "구경 미표기";

        return caliber switch
        {
            "Caliber784x49" => ".308 Marlin Express",
            "Caliber93x64" => "9.3x64mm",
            "Caliber9x18PM" => "9x18mm Makarov",
            "Caliber9x19PARA" => "9x19mm Parabellum",
            "Caliber9x21" => "9x21mm Gyurza",
            "Caliber9x33R" => ".357 Magnum",
            "Caliber545x39" => "5.45x39mm",
            "Caliber556x45NATO" => "5.56x45mm NATO",
            "Caliber762x25TT" => "7.62x25mm Tokarev",
            "Caliber762x35" => ".300 Blackout",
            "Caliber762x39" => "7.62x39mm",
            "Caliber762x51" => "7.62x51mm NATO",
            "Caliber762x54R" => "7.62x54mmR",
            "Caliber86x70" => ".338 Lapua Magnum",
            "Caliber9x39" => "9x39mm",
            "Caliber366TKM" => ".366 TKM",
            "Caliber1143x23ACP" => ".45 ACP",
            "Caliber1143x23" => ".45 ACP",
            "Caliber127x33" => ".50 Action Express",
            "Caliber127x55" => "12.7x55mm",
            "Caliber12g" => "12/70",
            "Caliber20g" => "20/70",
            "Caliber23x75" => "23x75mmR",
            "Caliber26x75" => "26x75mm flare",
            "Caliber30x29" => "30x29mm",
            "Caliber40x46" => "40x46mm",
            "Caliber40mmRU" => "40mm VOG",
            "Caliber46x30" => "4.6x30mm HK",
            "Caliber57x28" => "5.7x28mm FN",
            "Caliber68x51" => "6.8x51mm",
            "Caliber127x99" => ".50 BMG",
            "Caliber127x108" => "12.7x108mm",
            _ => caliber.StartsWith("Caliber", StringComparison.Ordinal)
                ? caliber["Caliber".Length..]
                : caliber,
        };
    }
}
