using System.Text.Json.Serialization;
using JunhyunHelper.Core.Scanner;

namespace JunhyunHelper.Desktop.Scanner;

public sealed class ScannerDisplaySettings
{
    public const int CurrentSchemaVersion = 10;

    public const string TraderSellPriceField = "trader_sell_price";
    public const string FleaAveragePriceField = "flea_average_price";
    public const string TraderPricePerSlotField = "trader_price_per_slot";
    public const string FleaPricePerSlotField = "flea_price_per_slot";
    public const string CurrentNeededField = "current_needed";
    public const string AmmoPickupField = "ammo_pickup";

    private static readonly string[] DefaultMiniScannerInfoOrder =
    [
        TraderSellPriceField,
        FleaAveragePriceField,
        TraderPricePerSlotField,
        FleaPricePerSlotField,
        CurrentNeededField,
        AmmoPickupField,
    ];

    private static readonly ScannerHotkeyGesture DefaultAddCorrectionData =
        new(true, false, true, System.Windows.Input.Key.F9);

    public int SchemaVersion { get; set; }
    public bool Enabled { get; set; }
    public bool ShowTraderSellPrice { get; set; } = true;
    public bool ShowFleaAveragePrice { get; set; } = true;
    public bool ShowTraderPricePerSlot { get; set; } = true;
    public bool ShowFleaPricePerSlot { get; set; }
    public bool ShowCurrentNeeded { get; set; } = true;
    public bool ShowAmmoPickup { get; set; } = true;
    public List<string> MiniScannerInfoOrder { get; set; } = [.. DefaultMiniScannerInfoOrder];
    public double? PositionX { get; set; }
    public double? PositionY { get; set; }
    public double FontSize { get; set; } = 18;
    public string OneShotTarkovHotkey { get; set; } = ScannerHotkeyGesture.DefaultOneShotTarkov.ToString();
    public string OneShotTestHotkey { get; set; } = ScannerHotkeyGesture.DefaultOneShotTest.ToString();
    public string ScannerToggleHotkey { get; set; } = ScannerHotkeyGesture.DefaultScannerToggle.ToString();
    public string AddCorrectionDataHotkey { get; set; } = DefaultAddCorrectionData.ToString();

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
        ShowTraderSellPrice = ShowTraderSellPrice,
        ShowFleaAveragePrice = ShowFleaAveragePrice,
        ShowTraderPricePerSlot = ShowTraderPricePerSlot,
        ShowFleaPricePerSlot = ShowFleaPricePerSlot,
        ShowCurrentNeeded = ShowCurrentNeeded,
        ShowAmmoPickup = ShowAmmoPickup,
        MiniScannerInfoOrder = MiniScannerInfoOrder?.ToList() ?? [],
        PositionX = PositionX,
        PositionY = PositionY,
        FontSize = FontSize,
        OneShotTarkovHotkey = OneShotTarkovHotkey,
        OneShotTestHotkey = OneShotTestHotkey,
        ScannerToggleHotkey = ScannerToggleHotkey,
        AddCorrectionDataHotkey = AddCorrectionDataHotkey,
        OcrSubstitutions = OcrSubstitutions?.Select(rule => rule.Clone()).ToList() ?? [],
        OneShotHotkey = OneShotHotkey,
    };

    public void Normalize()
    {
        if (SchemaVersion < 2)
        {
            ShowTraderSellPrice = true;
            ShowTraderPricePerSlot = true;
        }

        if (SchemaVersion < 4)
        {
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

        OneShotHotkey = null;

        if (SchemaVersion < 9)
            ShowAmmoPickup = true;

        MiniScannerInfoOrder = ScannerInfoOrderPolicy.Normalize(
            MiniScannerInfoOrder,
            DefaultMiniScannerInfoOrder);

        OneShotTarkovHotkey = NormalizeHotkey(
            OneShotTarkovHotkey,
            ScannerHotkeyGesture.DefaultOneShotTarkov);
        OneShotTestHotkey = NormalizeHotkey(
            OneShotTestHotkey,
            ScannerHotkeyGesture.DefaultOneShotTest);
        ScannerToggleHotkey = NormalizeHotkey(
            ScannerToggleHotkey,
            ScannerHotkeyGesture.DefaultScannerToggle);
        AddCorrectionDataHotkey = NormalizeHotkey(
            AddCorrectionDataHotkey,
            DefaultAddCorrectionData);

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
        AddCorrectionDataHotkey = EnsureUnique(
            AddCorrectionDataHotkey,
            used,
            DefaultAddCorrectionData,
            new ScannerHotkeyGesture(true, false, true, System.Windows.Input.Key.F8),
            new ScannerHotkeyGesture(true, false, true, System.Windows.Input.Key.F7));

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
        AmmoPickupField => ShowAmmoPickup,
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
            case AmmoPickupField:
                ShowAmmoPickup = visible;
                break;
        }
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
