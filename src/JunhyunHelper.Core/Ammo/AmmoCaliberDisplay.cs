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
            "Caliber93x64" => "9.3×64mm",
            "Caliber9x18PM" => "9×18mm Makarov",
            "Caliber9x19PARA" => "9×19mm Parabellum",
            "Caliber9x21" => "9×21mm Gyurza",
            "Caliber9x33R" => ".357 Magnum",
            "Caliber545x39" => "5.45×39mm",
            "Caliber556x45NATO" => "5.56×45mm NATO",
            "Caliber762x25TT" => "7.62×25mm Tokarev",
            "Caliber762x35" => ".300 Blackout",
            "Caliber762x39" => "7.62×39mm",
            "Caliber762x51" => "7.62×51mm NATO",
            "Caliber762x54R" => "7.62×54mmR",
            "Caliber86x70" => ".338 Lapua Magnum",
            "Caliber9x39" => "9×39mm",
            "Caliber366TKM" => ".366 TKM",
            "Caliber1143x23ACP" => ".45 ACP",
            "Caliber1143x23" => ".45 ACP",
            "Caliber127x33" => ".50 Action Express",
            "Caliber127x55" => "12.7×55mm",
            "Caliber12g" => "12/70",
            "Caliber20g" => "20/70",
            "Caliber23x75" => "23×75mmR",
            "Caliber26x75" => "26×75mm flare",
            "Caliber30x29" => "30×29mm",
            "Caliber40x46" => "40×46mm",
            "Caliber40mmRU" => "40mm VOG",
            "Caliber46x30" => "4.6×30mm HK",
            "Caliber57x28" => "5.7×28mm FN",
            "Caliber68x51" => "6.8×51mm",
            "Caliber127x99" => ".50 BMG",
            "Caliber127x108" => "12.7×108mm",
            _ => caliber.StartsWith("Caliber", StringComparison.Ordinal)
                ? caliber["Caliber".Length..]
                : caliber,
        };
    }
}
