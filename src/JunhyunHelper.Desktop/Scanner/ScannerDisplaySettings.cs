using System.Text.Json.Serialization;
using JunhyunHelper.Core.Scanner;

namespace JunhyunHelper.Desktop.Scanner;

public sealed class ScannerDisplaySettings
{
    public const int CurrentSchemaVersion = 6;

    public const string TraderSellPriceField = "trader_sell_price";
    public const string FleaAveragePriceField = "flea_average_price";
    public const string TraderPricePerSlotField = "trader_price_per_slot";
    public const string FleaPricePerSlotField = "flea_price_per_slot";
    public const string CurrentNeededField = "current_needed";

    private static readonly string[] DefaultMiniScannerInfoOrder =
    [
        TraderSellPriceField,
        FleaAveragePriceField,
        TraderPricePerSlotField,
        FleaPricePerSlotField,
        CurrentNeededField,
    ];

    public int SchemaVersion { get; set; }
    public bool Enabled { get; set; }
    public bool ShowItemName { get; set; } = true;
    public bool ShowItemIcon { get; set; } = true;
    public bool ShowTraderSellPrice { get; set; } = true;
    public bool ShowFleaAveragePrice { get; set; } = true;
    public bool ShowTraderPricePerSlot { get; set; } = true;
    public bool ShowFleaPricePerSlot { get; set; }
    public bool ShowCurrentNeeded { get; set; } = true;
    public List<string> MiniScannerInfoOrder { get; set; } = [.. DefaultMiniScannerInfoOrder];
    public double? PositionX { get; set; }
    public double? PositionY { get; set; }
    public double FontSize { get; set; } = 18;
    public string OneShotTarkovHotkey { get; set; } = ScannerHotkeyGesture.DefaultOneShotTarkov.ToString();
    public string OneShotTestHotkey { get; set; } = ScannerHotkeyGesture.DefaultOneShotTest.ToString();
    public string ScannerToggleHotkey { get; set; } = ScannerHotkeyGesture.DefaultScannerToggle.ToString();

    /// <summary>
    /// Optional user-owned exact OCR corrections. The default is deliberately empty;
    /// rules are evidence supplied by the user, not a global product substitution table.
    /// This remains an internal compatibility setting even though the normal Scanner
    /// settings surface no longer exposes a dedicated editor.
    /// </summary>
    public List<ScannerOcrSubstitutionRule> OcrSubstitutions { get; set; } = [];

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? OneShotHotkey { get; set; }

    public static IReadOnlyList<string> DefaultInfoOrder => DefaultMiniScannerInfoOrder;

    public ScannerDisplaySettings Clone() => new()
    {
        SchemaVersion = SchemaVersion,
        Enabled = Enabled,
        ShowItemName = ShowItemName,
        ShowItemIcon = ShowItemIcon,
        ShowTraderSellPrice = ShowTraderSellPrice,
        ShowFleaAveragePrice = ShowFleaAveragePrice,
        ShowTraderPricePerSlot = ShowTraderPricePerSlot,
        ShowFleaPricePerSlot = ShowFleaPricePerSlot,
        ShowCurrentNeeded = ShowCurrentNeeded,
        MiniScannerInfoOrder = MiniScannerInfoOrder?.ToList() ?? [],
        PositionX = PositionX,
        PositionY = PositionY,
        FontSize = FontSize,
        OneShotTarkovHotkey = OneShotTarkovHotkey,
        OneShotTestHotkey = OneShotTestHotkey,
        ScannerToggleHotkey = ScannerToggleHotkey,
        OcrSubstitutions = OcrSubstitutions?.Select(rule => rule.Clone()).ToList() ?? [],
        OneShotHotkey = OneShotHotkey,
    };

    public void Normalize()
    {
        if (SchemaVersion < 2)
        {
            ShowItemIcon = true;
            ShowTraderSellPrice = true;
            ShowTraderPricePerSlot = true;
        }

        if (SchemaVersion < 4)
        {
            // Schema v3 stored the single one-shot gesture in OneShotHotkey. Preserve
            // that exact user choice (including explicit disable) as the in-game
            // one-shot command. The two new commands receive defaults later through
            // the collision-safe assignment below.
            if (SchemaVersion >= 3)
            {
                OneShotTarkovHotkey = string.IsNullOrWhiteSpace(OneShotHotkey)
                    ? string.Empty
                    : ScannerHotkeyGesture.TryParse(OneShotHotkey, out var migrated)
                        ? migrated.ToString()
                        : ScannerHotkeyGesture.DefaultOneShotTarkov.ToString();
            }
            else
            {
                OneShotTarkovHotkey = ScannerHotkeyGesture.DefaultOneShotTarkov.ToString();
            }

            OneShotTestHotkey = ScannerHotkeyGesture.DefaultOneShotTest.ToString();
            ScannerToggleHotkey = ScannerHotkeyGesture.DefaultScannerToggle.ToString();
            OneShotHotkey = null;
        }

        // v6 makes icon and item name part of the fixed Mini Scanner header. Preserve
        // old files, but never allow those two identity fields to be hidden.
        ShowItemName = true;
        ShowItemIcon = true;
        MiniScannerInfoOrder = NormalizeInfoOrder(MiniScannerInfoOrder);

        OneShotTarkovHotkey = NormalizeHotkey(
            OneShotTarkovHotkey,
            ScannerHotkeyGesture.DefaultOneShotTarkov);
        OneShotTestHotkey = NormalizeHotkey(
            OneShotTestHotkey,
            ScannerHotkeyGesture.DefaultOneShotTest);
        ScannerToggleHotkey = NormalizeHotkey(
            ScannerToggleHotkey,
            ScannerHotkeyGesture.DefaultScannerToggle);

        // RegisterHotKey cannot safely give two product commands the same gesture.
        // Give the long-lived in-game one-shot setting priority, then keep the test and
        // toggle gestures if they are distinct. On schema migration only the newly
        // introduced commands are moved to nearby Ctrl+Shift function keys as needed.
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        AddIfConfigured(used, OneShotTarkovHotkey);
        OneShotTestHotkey = EnsureUnique(
            OneShotTestHotkey,
            used,
            ScannerHotkeyGesture.DefaultOneShotTest,
            ScannerHotkeyGesture.DefaultOneShotTarkov,
            ScannerHotkeyGesture.DefaultScannerToggle,
            new ScannerHotkeyGesture(true, false, true, System.Windows.Input.Key.F9),
            new ScannerHotkeyGesture(true, false, true, System.Windows.Input.Key.F8));
        ScannerToggleHotkey = EnsureUnique(
            ScannerToggleHotkey,
            used,
            ScannerHotkeyGesture.DefaultScannerToggle,
            ScannerHotkeyGesture.DefaultOneShotTest,
            ScannerHotkeyGesture.DefaultOneShotTarkov,
            new ScannerHotkeyGesture(true, false, true, System.Windows.Input.Key.F9),
            new ScannerHotkeyGesture(true, false, true, System.Windows.Input.Key.F8));

        OcrSubstitutions = ScannerOcrSubstitutionEngine
            .NormalizeRules(OcrSubstitutions)
            .Select(rule => rule.Clone())
            .ToList();

        if (PositionX is { } x && !double.IsFinite(x))
            PositionX = null;
        if (PositionY is { } y && !double.IsFinite(y))
            PositionY = null;
        if (PositionX.HasValue != PositionY.HasValue)
        {
            PositionX = null;
            PositionY = null;
        }
        FontSize = double.IsFinite(FontSize) ? Math.Clamp(FontSize, 12, 32) : 18;

        SchemaVersion = CurrentSchemaVersion;
    }

    public bool IsInfoVisible(string field) => field switch
    {
        TraderSellPriceField => ShowTraderSellPrice,
        FleaAveragePriceField => ShowFleaAveragePrice,
        TraderPricePerSlotField => ShowTraderPricePerSlot,
        FleaPricePerSlotField => ShowFleaPricePerSlot,
        CurrentNeededField => ShowCurrentNeeded,
        _ => false,
    };

    public void SetInfoVisible(string field, bool visible)
    {
        switch (field)
        {
            case TraderSellPriceField:
                ShowTraderSellPrice = visible;
                break;
            case FleaAveragePriceField:
                ShowFleaAveragePrice = visible;
                break;
            case TraderPricePerSlotField:
                ShowTraderPricePerSlot = visible;
                break;
            case FleaPricePerSlotField:
                ShowFleaPricePerSlot = visible;
                break;
            case CurrentNeededField:
                ShowCurrentNeeded = visible;
                break;
        }
    }

    private static List<string> NormalizeInfoOrder(IEnumerable<string>? values)
    {
        var known = new HashSet<string>(DefaultMiniScannerInfoOrder, StringComparer.Ordinal);
        var result = new List<string>(DefaultMiniScannerInfoOrder.Length);
        foreach (var value in values ?? [])
        {
            if (known.Contains(value) && !result.Contains(value, StringComparer.Ordinal))
                result.Add(value);
        }

        foreach (var value in DefaultMiniScannerInfoOrder)
        {
            if (!result.Contains(value, StringComparer.Ordinal))
                result.Add(value);
        }
        return result;
    }

    private static string NormalizeHotkey(string? value, ScannerHotkeyGesture fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;
        return ScannerHotkeyGesture.TryParse(value, out var gesture)
            ? gesture.ToString()
            : fallback.ToString();
    }

    private static void AddIfConfigured(ISet<string> used, string value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            used.Add(value);
    }

    private static string EnsureUnique(
        string value,
        ISet<string> used,
        params ScannerHotkeyGesture[] fallbacks)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;
        if (used.Add(value))
            return value;

        foreach (var fallback in fallbacks)
        {
            var candidate = fallback.ToString();
            if (used.Add(candidate))
                return candidate;
        }

        return string.Empty;
    }
}
