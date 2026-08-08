using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using JunhyunHelper.Core.Ammo;
using JunhyunHelper.Core.Content;

namespace JunhyunHelper.Desktop.Ammo;

public partial class AmmoPage : UserControl
{
    private GameContentCatalog? _content;
    private IReadOnlyList<AmmoRow> _allRows = [];
    private AmmoRow? _selectedRow;
    private bool _busy;

    public AmmoPage()
    {
        InitializeComponent();
    }

    public void SetData(GameContentCatalog content)
    {
        _content = content ?? throw new ArgumentNullException(nameof(content));
        var selectedCaliber = (CaliberComboBox.SelectedItem as CaliberChoice)?.RawCaliber;
        _allRows = BuildRows(content);

        var choices = new[] { new CaliberChoice(null, "전체 구경") }
            .Concat(_allRows
                .Select(row => new CaliberChoice(row.RawCaliber, row.CaliberLabel))
                .DistinctBy(choice => choice.RawCaliber, StringComparer.Ordinal)
                .OrderBy(choice => choice.Label, StringComparer.CurrentCulture))
            .ToArray();

        CaliberComboBox.ItemsSource = choices;
        CaliberComboBox.SelectedItem = choices.FirstOrDefault(choice =>
            string.Equals(choice.RawCaliber, selectedCaliber, StringComparison.Ordinal))
            ?? choices[0];

        ApplyFilter();
    }

    public void SetBusy(bool busy)
    {
        _busy = busy;
        SearchBox.IsEnabled = !busy;
        CaliberComboBox.IsEnabled = !busy;
        AmmoGrid.IsEnabled = !busy;
    }

    private static IReadOnlyList<AmmoRow> BuildRows(GameContentCatalog content)
    {
        var itemsById = content.Items.ToDictionary(item => item.Id, StringComparer.Ordinal);

        return content.Ammunition
            .Select(ammo =>
            {
                itemsById.TryGetValue(ammo.ItemId, out var item);
                var name = item is null
                    ? ammo.ItemId
                    : DisplayName(item.NameKo, item.NameEn, item.Id);
                var caliberLabel = CaliberText(ammo.Caliber);

                return new AmmoRow(
                    ammo,
                    name,
                    ammo.Caliber,
                    caliberLabel,
                    ammo.ProjectileCount > 1
                        ? $"{ammo.Damage} × {ammo.ProjectileCount}"
                        : ammo.Damage.ToString(CultureInfo.InvariantCulture),
                    ammo.PenetrationPower,
                    $"{ammo.ArmorDamage}%",
                    $"{ammo.InitialSpeed:0} m/s",
                    FormatPercentage(ammo.FragmentationChance),
                    FormatSignedPercentage(ammo.RecoilModifier),
                    FormatSignedPercentage(ammo.AccuracyModifier),
                    ammo.Acquisitions.Count == 0 ? "없음" : $"{ammo.Acquisitions.Count}개");
            })
            .OrderBy(row => row.CaliberLabel, StringComparer.CurrentCulture)
            .ThenBy(row => row.Name, StringComparer.CurrentCulture)
            .ToArray();
    }

    private void ApplyFilter()
    {
        var search = SearchBox.Text?.Trim() ?? string.Empty;
        var selectedCaliber = (CaliberComboBox.SelectedItem as CaliberChoice)?.RawCaliber;
        var selectedItemId = _selectedRow?.Ammo.ItemId;

        var filtered = _allRows
            .Where(row => selectedCaliber is null ||
                          string.Equals(row.RawCaliber, selectedCaliber, StringComparison.Ordinal))
            .Where(row => string.IsNullOrWhiteSpace(search) ||
                          row.Name.Contains(search, StringComparison.CurrentCultureIgnoreCase) ||
                          row.Ammo.ItemId.Contains(search, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        AmmoGrid.ItemsSource = filtered;
        AmmoGrid.SelectedItem = filtered.FirstOrDefault(row =>
                                    string.Equals(row.Ammo.ItemId, selectedItemId, StringComparison.Ordinal))
                                ?? filtered.FirstOrDefault();

        SummaryText.Text = selectedCaliber is null
            ? $"탄약 {filtered.Length}종 · 구경 {Math.Max(0, CaliberComboBox.Items.Count - 1)}개"
            : $"{CaliberText(selectedCaliber)} · 탄약 {filtered.Length}종";

        if (filtered.Length == 0)
            ShowDetail(null);
    }

    private void ShowDetail(AmmoRow? row)
    {
        _selectedRow = row;
        if (row is null || _content is null)
        {
            EmptyDetailText.Visibility = Visibility.Visible;
            DetailGrid.Visibility = Visibility.Collapsed;
            return;
        }

        EmptyDetailText.Visibility = Visibility.Collapsed;
        DetailGrid.Visibility = Visibility.Visible;

        var ammo = row.Ammo;
        DetailName.Text = row.Name;
        DetailIdentity.Text = $"{row.CaliberLabel} · {AmmoTypeText(ammo.AmmoType)} · Item ID {ammo.ItemId}";
        DetailPerformance.Text = string.Join(
            Environment.NewLine,
            $"피해량: {ammo.Damage}{(ammo.ProjectileCount > 1 ? $" × {ammo.ProjectileCount}발사체" : string.Empty)}",
            $"관통력: {ammo.PenetrationPower}",
            $"장갑 피해: {ammo.ArmorDamage}%",
            $"초속: {ammo.InitialSpeed:0} m/s",
            $"파편 확률: {FormatPercentage(ammo.FragmentationChance)}",
            $"도탄 확률: {FormatPercentage(ammo.RicochetChance)}");

        var tracer = ammo.Tracer
            ? string.IsNullOrWhiteSpace(ammo.TracerColor) ? "예" : $"예 · {ammo.TracerColor}"
            : "아니오";
        DetailModifiers.Text = string.Join(
            Environment.NewLine,
            $"명중 보정: {FormatSignedPercentage(ammo.AccuracyModifier)}",
            $"반동 보정: {FormatSignedPercentage(ammo.RecoilModifier)}",
            $"경출혈 보정: {FormatSignedPercentage(ammo.LightBleedModifier)}",
            $"중출혈 보정: {FormatSignedPercentage(ammo.HeavyBleedModifier)}",
            $"예광탄: {tracer}");

        AcquisitionItems.ItemsSource = BuildAcquisitionRows(ammo, _content);
    }

    private static IReadOnlyList<AcquisitionRow> BuildAcquisitionRows(
        AmmoDefinition ammo,
        GameContentCatalog content)
    {
        if (ammo.Acquisitions.Count == 0)
        {
            return
            [
                new AcquisitionRow(
                    "확인된 수급처 없음",
                    string.Empty,
                    "현재 canonical 데이터에 구매·교환·제작 수급처가 없습니다.",
                    string.Empty),
            ];
        }

        return ammo.Acquisitions
            .Select(acquisition => BuildAcquisitionRow(acquisition, content))
            .OrderBy(row => row.Title, StringComparer.CurrentCulture)
            .ToArray();
    }

    private static AcquisitionRow BuildAcquisitionRow(
        AmmoAcquisition acquisition,
        GameContentCatalog content)
    {
        var traderName = DisplayName(
            content.Traders.FirstOrDefault(trader => trader.Id == acquisition.TraderId)?.NameKo,
            content.Traders.FirstOrDefault(trader => trader.Id == acquisition.TraderId)?.NameEn,
            acquisition.TraderId ?? "상인");
        var stationName = DisplayName(
            content.HideoutStations.FirstOrDefault(station => station.Id == acquisition.StationId)?.NameKo,
            content.HideoutStations.FirstOrDefault(station => station.Id == acquisition.StationId)?.NameEn,
            acquisition.StationId ?? "은신처");

        var title = acquisition.Kind switch
        {
            AmmoAcquisitionKind.TraderPurchase => $"상인 구매 · {traderName}",
            AmmoAcquisitionKind.TraderBarter => $"물물교환 · {traderName}",
            AmmoAcquisitionKind.HideoutCraft => $"은신처 제작 · {stationName}",
            _ => acquisition.Kind.ToString(),
        };

        var conditionParts = new List<string>();
        if (acquisition.RequiredLevel > 0)
        {
            conditionParts.Add(acquisition.Kind == AmmoAcquisitionKind.HideoutCraft
                ? $"시설 Lv.{acquisition.RequiredLevel}"
                : $"상인 LL{acquisition.RequiredLevel}");
        }
        if (acquisition.BuyLimit is { } buyLimit)
            conditionParts.Add($"구매 제한 {buyLimit}");
        if (acquisition.DurationSeconds is > 0)
            conditionParts.Add($"제작 시간 {FormatDuration(acquisition.DurationSeconds.Value)}");
        if (acquisition.OutputCount > 0 && acquisition.OutputCount != 1)
            conditionParts.Add($"결과 {FormatNumber(acquisition.OutputCount)}발");

        var detailParts = new List<string>();
        if (acquisition.Price is { } price)
        {
            var currency = !string.IsNullOrWhiteSpace(acquisition.CurrencyCode)
                ? acquisition.CurrencyCode!
                : ItemName(acquisition.CurrencyItemId, content);
            detailParts.Add($"가격: {FormatNumber(price)} {currency}".TrimEnd());
        }

        if (acquisition.Requirements.Count > 0)
        {
            var requirements = acquisition.Requirements.Select(requirement =>
            {
                var suffix = requirement.IsTool ? " (도구)" : string.Empty;
                return $"{ItemName(requirement.ItemId, content)} {FormatNumber(requirement.Count)}개{suffix}";
            });
            detailParts.Add($"재료: {string.Join(" · ", requirements)}");
        }

        if (detailParts.Count == 0)
            detailParts.Add("추가 비용/재료 정보 없음");

        var unlock = string.IsNullOrWhiteSpace(acquisition.TaskUnlockQuestId)
            ? string.Empty
            : $"해금 퀘스트: {QuestName(acquisition.TaskUnlockQuestId, content)}";

        return new AcquisitionRow(
            title,
            string.Join(" · ", conditionParts),
            string.Join(Environment.NewLine, detailParts),
            unlock);
    }

    private static string ItemName(string? itemId, GameContentCatalog content)
    {
        if (string.IsNullOrWhiteSpace(itemId))
            return string.Empty;

        var item = content.Items.FirstOrDefault(candidate => candidate.Id == itemId);
        return item is null
            ? itemId
            : DisplayName(item.NameKo, item.NameEn, item.Id);
    }

    private static string QuestName(string questId, GameContentCatalog content)
    {
        var quest = content.Quests.FirstOrDefault(candidate => candidate.Id == questId);
        return quest is null
            ? questId
            : DisplayName(quest.NameKo, quest.NameEn, quest.Id);
    }

    private static string AmmoTypeText(string? ammoType) => ammoType?.ToLowerInvariant() switch
    {
        "bullet" => "탄환",
        "buckshot" => "산탄",
        "grenade" => "유탄",
        null or "" => "유형 미표기",
        _ => ammoType,
    };

    private static string CaliberText(string caliber)
    {
        if (string.IsNullOrWhiteSpace(caliber))
            return "구경 미표기";

        return caliber switch
        {
            "Caliber9x18PM" => "9×18mm PM",
            "Caliber9x19PARA" => "9×19mm",
            "Caliber9x21" => "9×21mm",
            "Caliber9x33R" => "9×33R",
            "Caliber545x39" => "5.45×39mm",
            "Caliber556x45NATO" => "5.56×45mm NATO",
            "Caliber762x25TT" => "7.62×25mm TT",
            "Caliber762x35" => "7.62×35mm",
            "Caliber762x39" => "7.62×39mm",
            "Caliber762x51" => "7.62×51mm NATO",
            "Caliber762x54R" => "7.62×54mmR",
            "Caliber86x70" => "8.6×70mm",
            "Caliber9x39" => "9×39mm",
            "Caliber366TKM" => ".366 TKM",
            "Caliber127x55" => "12.7×55mm",
            "Caliber12g" => "12 gauge",
            "Caliber20g" => "20 gauge",
            "Caliber23x75" => "23×75mm",
            "Caliber26x75" => "26×75mm",
            "Caliber30x29" => "30×29mm",
            "Caliber40x46" => "40×46mm",
            "Caliber40mmRU" => "40mm RU",
            "Caliber46x30" => "4.6×30mm",
            "Caliber57x28" => "5.7×28mm",
            "Caliber68x51" => "6.8×51mm",
            "Caliber127x99" => "12.7×99mm",
            "Caliber127x108" => "12.7×108mm",
            _ => caliber.StartsWith("Caliber", StringComparison.Ordinal)
                ? caliber["Caliber".Length..]
                : caliber,
        };
    }

    private static string FormatPercentage(decimal value) =>
        $"{value * 100m:0.##}%";

    private static string FormatSignedPercentage(decimal value)
    {
        var percent = value * 100m;
        return percent switch
        {
            > 0 => $"+{percent:0.##}%",
            < 0 => $"{percent:0.##}%",
            _ => "0%",
        };
    }

    private static string FormatNumber(decimal value) =>
        decimal.Truncate(value) == value
            ? value.ToString("0", CultureInfo.InvariantCulture)
            : value.ToString("0.##", CultureInfo.InvariantCulture);

    private static string FormatDuration(int seconds)
    {
        var duration = TimeSpan.FromSeconds(seconds);
        if (duration.TotalHours >= 1)
            return $"{(int)duration.TotalHours}시간 {duration.Minutes}분";
        if (duration.TotalMinutes >= 1)
            return $"{duration.Minutes}분 {duration.Seconds}초";
        return $"{duration.Seconds}초";
    }

    private static string DisplayName(string? korean, string? english, string fallback) =>
        !string.IsNullOrWhiteSpace(korean)
            ? korean
            : !string.IsNullOrWhiteSpace(english)
                ? english
                : fallback;

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilter();

    private void CaliberComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (IsLoaded)
            ApplyFilter();
    }

    private void AmmoGrid_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
        ShowDetail(AmmoGrid.SelectedItem as AmmoRow);

    private sealed record AmmoRow(
        AmmoDefinition Ammo,
        string Name,
        string RawCaliber,
        string CaliberLabel,
        string DamageText,
        int PenetrationPower,
        string ArmorDamageText,
        string SpeedText,
        string FragmentationText,
        string RecoilText,
        string AccuracyText,
        string AcquisitionCountText);

    private sealed record AcquisitionRow(
        string Title,
        string Conditions,
        string Details,
        string Unlock);

    private sealed record CaliberChoice(string? RawCaliber, string Label)
    {
        public override string ToString() => Label;
    }
}
