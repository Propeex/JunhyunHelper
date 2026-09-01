namespace JunhyunHelper.Core.FarmingGuide;

/// <summary>
/// Converts Tarkov's raw slot identifiers into user-facing Korean labels. Source-provided
/// Korean names remain authoritative; raw/mod-style identifiers are normalized here so
/// the Farming Guide never exposes implementation-oriented labels such as mod_scope.
/// </summary>
public static class FarmingGuideSlotLabelPolicy
{
    public static string Attachment(FarmingGuideAttachmentSlotDefinition slot)
    {
        ArgumentNullException.ThrowIfNull(slot);
        var sourceName = MeaningfulLocalizedName(slot.Name);
        return sourceName ?? Translate(slot.NameId, slot.Id, armorPlate: false);
    }

    public static string ArmorPlate(FarmingGuideArmorSlotDefinition slot)
    {
        ArgumentNullException.ThrowIfNull(slot);
        var sourceName = MeaningfulLocalizedName(slot.Name);
        return sourceName ?? Translate(slot.NameId, slot.Id, armorPlate: true);
    }

    private static string? MeaningfulLocalizedName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var trimmed = value.Trim();
        if (!trimmed.Any(ch => ch is >= '\uac00' and <= '\ud7a3'))
            return null;
        return LooksRaw(trimmed) ? null : trimmed;
    }

    private static string Translate(string? primary, string? fallback, bool armorPlate)
    {
        var raw = !string.IsNullOrWhiteSpace(primary) ? primary! : fallback ?? string.Empty;
        var key = Normalize(raw);

        if (armorPlate)
        {
            if (ContainsAny(key, "frontplate", "frontarmorplate")) return "전면 방탄판";
            if (ContainsAny(key, "backplate", "rearplate", "backarmorplate")) return "후면 방탄판";
            if (ContainsAny(key, "leftsideplate", "leftplate")) return "왼쪽 측면 방탄판";
            if (ContainsAny(key, "rightsideplate", "rightplate")) return "오른쪽 측면 방탄판";
            if (ContainsAny(key, "sideplate")) return "측면 방탄판";
            return "방탄판";
        }

        if (ContainsAny(key, "sightfront", "frontsight")) return "가늠쇠";
        if (ContainsAny(key, "sightrear", "rearsight")) return "가늠자";
        if (ContainsAny(key, "scope", "optic", "sight")) return "조준경";
        if (ContainsAny(key, "muzzle", "muzzledevice", "suppressor")) return "총구";
        if (ContainsAny(key, "stock", "buttstock")) return "개머리판";
        if (ContainsAny(key, "pistolgrip")) return "권총 손잡이";
        if (ContainsAny(key, "foregrip", "foregrip2")) return "전방 손잡이";
        if (ContainsAny(key, "handguard")) return "핸드가드";
        if (ContainsAny(key, "magazine", "mag")) return "탄창";
        if (ContainsAny(key, "receiver", "reciever")) return "리시버";
        if (ContainsAny(key, "barrel")) return "총열";
        if (ContainsAny(key, "charge", "charginghandle")) return "장전 손잡이";
        if (ContainsAny(key, "gasblock")) return "가스 블록";
        if (ContainsAny(key, "mount")) return "마운트";
        if (ContainsAny(key, "nvg", "nightvision")) return "야간투시경";
        if (ContainsAny(key, "faceshield", "visor")) return "안면 보호구";
        if (ContainsAny(key, "ear", "earpiece")) return "귀 보호구";
        if (ContainsAny(key, "helmet", "headwear")) return "헬멧 부품";
        if (ContainsAny(key, "tactical", "flashlight", "laser", "device")) return "전술 장치";
        if (ContainsAny(key, "launcher")) return "유탄발사기";
        if (ContainsAny(key, "bipod")) return "양각대";
        if (ContainsAny(key, "rail")) return "레일";

        return Humanize(raw);
    }

    private static bool LooksRaw(string value) =>
        value.Contains('_') || value.StartsWith("mod", StringComparison.OrdinalIgnoreCase);

    private static string Normalize(string value) =>
        new(value.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());

    private static bool ContainsAny(string key, params string[] candidates) =>
        candidates.Any(candidate => key.Contains(Normalize(candidate), StringComparison.Ordinal));

    private static string Humanize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "부착 부위";
        var trimmed = value.Trim();
        if (trimmed.StartsWith("mod_", StringComparison.OrdinalIgnoreCase))
            trimmed = trimmed[4..];
        trimmed = trimmed.Replace('_', ' ').Replace('-', ' ').Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? "부착 부위" : trimmed;
    }
}
