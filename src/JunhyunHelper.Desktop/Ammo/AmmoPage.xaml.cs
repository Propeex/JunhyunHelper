using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using JunhyunHelper.Core.Ammo;
using JunhyunHelper.Core.Content;
using JunhyunHelper.Desktop.Services;

namespace JunhyunHelper.Desktop.Ammo;

public sealed record AmmoQuestNavigationRequestedEventArgs(string QuestId);

public partial class AmmoPage : UserControl
{
    private static readonly Brush UnknownEffectivenessBackground = CreateFrozenBrush(0x3A, 0x3A, 0x3A);
    private static readonly Brush UnknownEffectivenessForeground = CreateFrozenBrush(0xB8, 0xB8, 0xB8);
    private static readonly Brush[] EffectivenessBackgrounds =
    [
        CreateFrozenBrush(0x5A, 0x2A, 0x2A),
        CreateFrozenBrush(0x8B, 0x37, 0x37),
        CreateFrozenBrush(0xA8, 0x5A, 0x2A),
        CreateFrozenBrush(0x9C, 0x7B, 0x2E),
        CreateFrozenBrush(0x76, 0x8B, 0x32),
        CreateFrozenBrush(0x3E, 0x7C, 0x39),
        CreateFrozenBrush(0x2C, 0x66, 0x3B),
    ];

    private GameContentCatalog? _content;
    private ImageCacheService? _imageCache;
    private AmmoFavoriteStore? _favoriteStore;
    private HashSet<string> _favoriteCalibers = new(StringComparer.Ordinal);
    private IReadOnlyList<AmmoRow> _allRows = [];
    private AmmoRow? _selectedRow;
    private CancellationTokenSource? _iconLoadCts;
    private bool _usingWikiBallisticsFilter;

    public AmmoPage()
    {
        InitializeComponent();
    }

    public event EventHandler<AmmoQuestNavigationRequestedEventArgs>? QuestNavigationRequested;

    public void SetImageCache(ImageCacheService imageCache) =>
        _imageCache = imageCache ?? throw new ArgumentNullException(nameof(imageCache));

    public void SetFavoriteStore(AmmoFavoriteStore favoriteStore)
    {
        _favoriteStore = favoriteStore ?? throw new ArgumentNullException(nameof(favoriteStore));
        _favoriteCalibers = favoriteStore.Load().ToHashSet(StringComparer.Ordinal);
        RefreshFavoriteChoices();
        UpdateFavoriteButton();
    }

    public void SetData(GameContentCatalog content)
    {
        _content = content ?? throw new ArgumentNullException(nameof(content));
        var selectedCaliber = (CaliberComboBox.SelectedItem as CaliberChoice)?.RawCaliber;
        _usingWikiBallisticsFilter = content.Ammunition.Any(ammo => ammo.IsWikiBallisticsListed is not null);
        _allRows = BuildRows(content, _usingWikiBallisticsFilter);

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

        RefreshFavoriteChoices();
        ApplyFilter();

        _iconLoadCts?.Cancel();
        _iconLoadCts?.Dispose();
        _iconLoadCts = new CancellationTokenSource();
        _ = LoadIconsAsync(_allRows, _iconLoadCts.Token);
    }

    public void SetBusy(bool busy)
    {
        CaliberComboBox.IsEnabled = !busy;
        FavoriteCaliberMenuButton.IsEnabled = !busy;
        FavoriteCaliberButton.IsEnabled = !busy &&
                                          (CaliberComboBox.SelectedItem as CaliberChoice)?.RawCaliber is not null;
        ColumnMenuButton.IsEnabled = !busy;
        AmmoGrid.IsEnabled = !busy;
        if (busy)
        {
            FavoriteCaliberPopup.IsOpen = false;
            ColumnMenuPopup.IsOpen = false;
        }
    }

    private static IReadOnlyList<AmmoRow> BuildRows(GameContentCatalog content, bool useWikiBallisticsFilter)
    {
        var itemsById = content.Items.ToDictionary(item => item.Id, StringComparer.Ordinal);
        var comparisonAmmo = useWikiBallisticsFilter
            ? content.Ammunition.Where(ammo => ammo.IsWikiBallisticsListed == true)
            : content.Ammunition;

        return comparisonAmmo
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
                    item?.IconUrl,
                    ammo.Caliber,
                    caliberLabel,
                    ammo.Damage,
                    ammo.ProjectileCount > 1
                        ? $"{ammo.Damage} × {ammo.ProjectileCount}"
                        : ammo.Damage.ToString(CultureInfo.InvariantCulture),
                    ammo.PenetrationPower,
                    $"{ammo.ArmorDamage}%",
                    BuildArmorEffectivenessCells(ammo.ArmorEffectiveness),
                    $"{ammo.InitialSpeed:0} m/s",
                    FormatPercentage(ammo.FragmentationChance),
                    FormatSignedPercentage(ammo.RecoilModifier),
                    FormatSignedPercentage(ammo.AccuracyModifier),
                    BuildCompactAcquisitionText(ammo, content));
            })
            .OrderBy(row => row.PenetrationPower)
            .ThenBy(row => row.Damage)
            .ThenBy(row => row.Name, StringComparer.CurrentCulture)
            .ToArray();
    }

    private async Task LoadIconsAsync(IReadOnlyList<AmmoRow> rows, CancellationToken cancellationToken)
    {
        if (_imageCache is null)
            return;

        try
        {
            foreach (var row in rows)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (string.IsNullOrWhiteSpace(row.IconUrl))
                    continue;

                var image = await _imageCache.LoadAsync(
                    $"item-{row.Ammo.ItemId}",
                    row.IconUrl,
                    cancellationToken);
                if (image is null || cancellationToken.IsCancellationRequested)
                    continue;

                row.Icon = image;
                if (ReferenceEquals(row, _selectedRow))
                    DetailIcon.Source = image;
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private void ApplyFilter()
    {
        var selectedCaliber = (CaliberComboBox.SelectedItem as CaliberChoice)?.RawCaliber;
        var selectedItemId = _selectedRow?.Ammo.ItemId;

        var filtered = _allRows
            .Where(row => selectedCaliber is null ||
                          string.Equals(row.RawCaliber, selectedCaliber, StringComparison.Ordinal))
            .ToArray();

        AmmoGrid.ItemsSource = filtered;
        AmmoGrid.SelectedItem = filtered.FirstOrDefault(row =>
                                    string.Equals(row.Ammo.ItemId, selectedItemId, StringComparison.Ordinal))
                                ?? filtered.FirstOrDefault();

        UpdateFavoriteButton();
        if (filtered.Length == 0)
            ShowDetail(null);
    }

    private void RefreshFavoriteChoices()
    {
        if (FavoriteCaliberItems is null)
            return;

        var available = _allRows
            .Select(row => new CaliberChoice(row.RawCaliber, row.CaliberLabel))
            .DistinctBy(choice => choice.RawCaliber, StringComparer.Ordinal)
            .Where(choice => choice.RawCaliber is not null && _favoriteCalibers.Contains(choice.RawCaliber))
            .OrderBy(choice => choice.Label, StringComparer.CurrentCulture)
            .ToArray();

        FavoriteCaliberItems.ItemsSource = available;
        FavoriteCaliberEmptyText.Visibility = available.Length == 0
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void UpdateFavoriteButton()
    {
        if (FavoriteCaliberButton is null)
            return;

        var caliber = (CaliberComboBox.SelectedItem as CaliberChoice)?.RawCaliber;
        var isFavorite = caliber is not null && _favoriteCalibers.Contains(caliber);
        FavoriteCaliberButton.IsEnabled = caliber is not null;
        FavoriteCaliberButton.Content = isFavorite ? "★" : "☆";
        FavoriteCaliberButton.ToolTip = isFavorite ? "즐겨찾기 해제" : "즐겨찾기 추가";
    }

    private void ShowDetail(AmmoRow? row)
    {
        _selectedRow = row;
        if (row is null || _content is null)
        {
            EmptyDetailText.Visibility = Visibility.Visible;
            DetailGrid.Visibility = Visibility.Collapsed;
            DetailIcon.Source = null;
            DetailArmorEffectivenessItems.ItemsSource = null;
            return;
        }

        EmptyDetailText.Visibility = Visibility.Collapsed;
        DetailGrid.Visibility = Visibility.Visible;

        var ammo = row.Ammo;
        DetailIcon.Source = row.Icon;
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
        DetailArmorEffectivenessItems.ItemsSource = row.ArmorEffectivenessCells;

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

    private static IReadOnlyList<ArmorEffectivenessCell> BuildArmorEffectivenessCells(
        AmmoArmorEffectiveness? effectiveness)
    {
        var values = effectiveness?.IsValid == true
            ? effectiveness.Values.Cast<int?>().ToArray()
            : Enumerable.Repeat<int?>(null, 6).ToArray();

        return values
            .Select((value, index) =>
            {
                var armorClass = index + 1;
                if (value is null)
                {
                    return new ArmorEffectivenessCell(
                        armorClass,
                        "?",
                        UnknownEffectivenessBackground,
                        UnknownEffectivenessForeground,
                        $"방탄 등급 {armorClass} · 현재 Tarkov Wiki 효율값을 안전하게 매칭하지 못했습니다.");
                }

                return new ArmorEffectivenessCell(
                    armorClass,
                    value.Value.ToString(CultureInfo.InvariantCulture),
                    EffectivenessBackgrounds[value.Value],
                    Brushes.White,
                    $"방탄 등급 {armorClass} · Tarkov Wiki 효율 {value.Value}/6 · 값이 높을수록 효과적");
            })
            .ToArray();
    }

    private static Brush CreateFrozenBrush(byte red, byte green, byte blue)
    {
        var brush = new SolidColorBrush(Color.FromRgb(red, green, blue));
        brush.Freeze();
        return brush;
    }

    private static string BuildCompactAcquisitionText(AmmoDefinition ammo, GameContentCatalog content)
    {
        if (ammo.Acquisitions.Count == 0)
            return "레이드 획득";

        var labels = ammo.Acquisitions
            .Select(acquisition => new
            {
                Order = AcquisitionOrder(acquisition.Kind),
                acquisition.RequiredLevel,
                Label = CompactAcquisitionLabel(acquisition, content),
            })
            .OrderBy(entry => entry.Order)
            .ThenBy(entry => entry.RequiredLevel)
            .ThenBy(entry => entry.Label, StringComparer.CurrentCulture)
            .Select(entry => entry.Label)
            .Distinct(StringComparer.CurrentCulture)
            .ToArray();

        return labels.Length <= 2
            ? string.Join(" · ", labels)
            : $"{labels[0]} · {labels[1]} · +{labels.Length - 2}";
    }

    private static string CompactAcquisitionLabel(AmmoAcquisition acquisition, GameContentCatalog content)
    {
        var traderName = TraderName(acquisition.TraderId, content);
        var stationName = StationName(acquisition.StationId, content);
        var level = acquisition.RequiredLevel > 0
            ? acquisition.RequiredLevel.ToString(CultureInfo.InvariantCulture)
            : null;

        return acquisition.Kind switch
        {
            AmmoAcquisitionKind.TraderPurchase => level is null ? traderName : $"{traderName} LL{level}",
            AmmoAcquisitionKind.TraderBarter => level is null ? $"{traderName} 교환" : $"{traderName} LL{level} 교환",
            AmmoAcquisitionKind.HideoutCraft => level is null ? $"{stationName} 제작" : $"{stationName} Lv.{level}",
            _ => acquisition.Kind.ToString(),
        };
    }

    private static int AcquisitionOrder(AmmoAcquisitionKind kind) => kind switch
    {
        AmmoAcquisitionKind.TraderPurchase => 0,
        AmmoAcquisitionKind.TraderBarter => 1,
        AmmoAcquisitionKind.HideoutCraft => 2,
        _ => 9,
    };

    private static IReadOnlyList<AcquisitionRow> BuildAcquisitionRows(AmmoDefinition ammo, GameContentCatalog content)
    {
        if (ammo.Acquisitions.Count == 0)
        {
            return
            [
                new AcquisitionRow(
                    "레이드 획득",
                    string.Empty,
                    "현재 데이터에 확인된 상인 구매·교환·은신처 제작 경로가 없습니다.",
                    string.Empty,
                    null),
            ];
        }

        return ammo.Acquisitions
            .Select(acquisition => BuildAcquisitionRow(acquisition, content))
            .OrderBy(row => row.Title, StringComparer.CurrentCulture)
            .ToArray();
    }

    private static AcquisitionRow BuildAcquisitionRow(AmmoAcquisition acquisition, GameContentCatalog content)
    {
        var traderName = TraderName(acquisition.TraderId, content);
        var stationName = StationName(acquisition.StationId, content);

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

        var unlockQuestId = string.IsNullOrWhiteSpace(acquisition.TaskUnlockQuestId)
            ? null
            : acquisition.TaskUnlockQuestId;
        var unlock = unlockQuestId is null
            ? string.Empty
            : $"해금 퀘스트 · {QuestName(unlockQuestId, content)}";

        return new AcquisitionRow(
            title,
            string.Join(" · ", conditionParts),
            string.Join(Environment.NewLine, detailParts),
            unlock,
            unlockQuestId);
    }

    private static string TraderName(string? traderId, GameContentCatalog content)
    {
        if (string.IsNullOrWhiteSpace(traderId))
            return "상인";
        var trader = content.Traders.FirstOrDefault(candidate => candidate.Id == traderId);
        return trader is null ? traderId : DisplayName(trader.NameKo, trader.NameEn, trader.Id);
    }

    private static string StationName(string? stationId, GameContentCatalog content)
    {
        if (string.IsNullOrWhiteSpace(stationId))
            return "은신처";
        var station = content.HideoutStations.FirstOrDefault(candidate => candidate.Id == stationId);
        return station is null ? stationId : DisplayName(station.NameKo, station.NameEn, station.Id);
    }

    private static string ItemName(string? itemId, GameContentCatalog content)
    {
        if (string.IsNullOrWhiteSpace(itemId))
            return string.Empty;
        var item = content.Items.FirstOrDefault(candidate => candidate.Id == itemId);
        return item is null ? itemId : DisplayName(item.NameKo, item.NameEn, item.Id);
    }

    private static string QuestName(string questId, GameContentCatalog content)
    {
        var quest = content.Quests.FirstOrDefault(candidate => candidate.Id == questId);
        return quest is null ? questId : DisplayName(quest.NameKo, quest.NameEn, quest.Id);
    }

    private static string AmmoTypeText(string? ammoType) => ammoType?.ToLowerInvariant() switch
    {
        "bullet" => "탄환",
        "buckshot" => "산탄",
        "grenade" => "유탄",
        null or "" => "유형 미표기",
        _ => ammoType,
    };

    internal static string CaliberText(string caliber)
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

    private static string FormatPercentage(decimal value) => $"{value * 100m:0.##}%";

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

    private void CaliberComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (IsLoaded)
            ApplyFilter();
    }

    private void FavoriteCaliberButton_Click(object sender, RoutedEventArgs e)
    {
        var caliber = (CaliberComboBox.SelectedItem as CaliberChoice)?.RawCaliber;
        if (caliber is null)
            return;

        if (!_favoriteCalibers.Add(caliber))
            _favoriteCalibers.Remove(caliber);
        _favoriteStore?.Save(_favoriteCalibers);
        RefreshFavoriteChoices();
        UpdateFavoriteButton();
    }

    private void FavoriteCaliberMenuButton_Click(object sender, RoutedEventArgs e) =>
        FavoriteCaliberPopup.IsOpen = !FavoriteCaliberPopup.IsOpen;

    private void FavoriteCaliberShortcutButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string caliber } || string.IsNullOrWhiteSpace(caliber))
            return;

        var target = CaliberComboBox.Items.Cast<CaliberChoice>()
            .FirstOrDefault(choice => string.Equals(choice.RawCaliber, caliber, StringComparison.Ordinal));
        if (target is not null)
        {
            if (ReferenceEquals(CaliberComboBox.SelectedItem, target))
                ApplyFilter();
            else
                CaliberComboBox.SelectedItem = target;
        }

        FavoriteCaliberPopup.IsOpen = false;
    }

    private void UnlockQuestButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string questId } && !string.IsNullOrWhiteSpace(questId))
            QuestNavigationRequested?.Invoke(this, new AmmoQuestNavigationRequestedEventArgs(questId));
    }

    private void ColumnMenuButton_Click(object sender, RoutedEventArgs e) =>
        ColumnMenuPopup.IsOpen = !ColumnMenuPopup.IsOpen;

    private void ColumnVisibility_Changed(object sender, RoutedEventArgs e)
    {
        if (CaliberColumn is null)
            return;

        CaliberColumn.Visibility = VisibilityFor(CaliberColumnCheckBox);
        DamageColumn.Visibility = VisibilityFor(DamageColumnCheckBox);
        PenetrationColumn.Visibility = VisibilityFor(PenetrationColumnCheckBox);
        ArmorDamageColumn.Visibility = VisibilityFor(ArmorDamageColumnCheckBox);
        ArmorEffectivenessColumn.Visibility = VisibilityFor(ArmorEffectivenessColumnCheckBox);
        SpeedColumn.Visibility = VisibilityFor(SpeedColumnCheckBox);
        FragmentationColumn.Visibility = VisibilityFor(FragmentationColumnCheckBox);
        RecoilColumn.Visibility = VisibilityFor(RecoilColumnCheckBox);
        AccuracyColumn.Visibility = VisibilityFor(AccuracyColumnCheckBox);
        AcquisitionColumn.Visibility = VisibilityFor(AcquisitionColumnCheckBox);
    }

    private static Visibility VisibilityFor(CheckBox checkBox) =>
        checkBox.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;

    private void AmmoGrid_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
        ShowDetail(AmmoGrid.SelectedItem as AmmoRow);

    private sealed class AmmoRow : INotifyPropertyChanged
    {
        private ImageSource? _icon;

        public AmmoRow(
            AmmoDefinition ammo,
            string name,
            string? iconUrl,
            string rawCaliber,
            string caliberLabel,
            int damage,
            string damageText,
            int penetrationPower,
            string armorDamageText,
            IReadOnlyList<ArmorEffectivenessCell> armorEffectivenessCells,
            string speedText,
            string fragmentationText,
            string recoilText,
            string accuracyText,
            string acquisitionText)
        {
            Ammo = ammo;
            Name = name;
            IconUrl = iconUrl;
            RawCaliber = rawCaliber;
            CaliberLabel = caliberLabel;
            Damage = damage;
            DamageText = damageText;
            PenetrationPower = penetrationPower;
            ArmorDamageText = armorDamageText;
            ArmorEffectivenessCells = armorEffectivenessCells;
            SpeedText = speedText;
            FragmentationText = fragmentationText;
            RecoilText = recoilText;
            AccuracyText = accuracyText;
            AcquisitionText = acquisitionText;
        }

        public AmmoDefinition Ammo { get; }
        public string Name { get; }
        public string? IconUrl { get; }
        public string RawCaliber { get; }
        public string CaliberLabel { get; }
        public int Damage { get; }
        public string DamageText { get; }
        public int PenetrationPower { get; }
        public string ArmorDamageText { get; }
        public IReadOnlyList<ArmorEffectivenessCell> ArmorEffectivenessCells { get; }
        public string SpeedText { get; }
        public string FragmentationText { get; }
        public string RecoilText { get; }
        public string AccuracyText { get; }
        public string AcquisitionText { get; }

        public ImageSource? Icon
        {
            get => _icon;
            set
            {
                if (ReferenceEquals(_icon, value))
                    return;
                _icon = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Icon)));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }

    private sealed record ArmorEffectivenessCell(
        int ArmorClass,
        string DisplayValue,
        Brush Background,
        Brush Foreground,
        string ToolTip);

    private sealed record AcquisitionRow(
        string Title,
        string Conditions,
        string Details,
        string Unlock,
        string? UnlockQuestId)
    {
        public Visibility UnlockVisibility => string.IsNullOrWhiteSpace(UnlockQuestId)
            ? Visibility.Collapsed
            : Visibility.Visible;
    }

    private sealed record CaliberChoice(string? RawCaliber, string Label)
    {
        public override string ToString() => Label;
    }
}
