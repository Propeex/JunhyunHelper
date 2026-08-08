using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using JunhyunHelper.Core.Content;
using JunhyunHelper.Core.Profiles;
using JunhyunHelper.Desktop.Services;

namespace JunhyunHelper.Desktop.Profiles;

public sealed record ProfileSettingsResult(
    int Level,
    PmcFaction Faction,
    string? EditionId,
    int? PrestigeLevel,
    IReadOnlyDictionary<string, TraderProgress> Traders);

public sealed class TraderEditorRow : INotifyPropertyChanged
{
    private int? _loyaltyLevel;
    private decimal? _standing;

    public required string TraderId { get; init; }
    public required string Name { get; init; }
    public required bool IsFence { get; init; }
    public required bool NeedsAdvancedStanding { get; init; }
    public required int DisplayRank { get; init; }

    public int? LoyaltyLevel
    {
        get => _loyaltyLevel;
        set
        {
            if (_loyaltyLevel == value)
                return;
            _loyaltyLevel = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(LoyaltyDisplay));
        }
    }

    public decimal? Standing
    {
        get => _standing;
        set
        {
            if (_standing == value)
                return;
            _standing = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(StandingDisplay));
            OnPropertyChanged(nameof(IsStandingKnown));
        }
    }

    public bool IsStandingKnown
    {
        get => Standing is not null;
        set
        {
            if (value == IsStandingKnown)
                return;
            Standing = value ? 0m : null;
        }
    }

    public string LoyaltyDisplay => LoyaltyLevel is null ? "미입력" : $"LL{LoyaltyLevel.Value}";
    public string StandingDisplay => Standing?.ToString("0.##", CultureInfo.CurrentCulture) ?? "미입력";

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public partial class ProfileEditorWindow : Window
{
    private const string FenceTraderId = "579dc571d53a0658a154fbec";
    private const int LastCoreTraderRank = 8; // Ref. Lightkeeper/BTR Driver follow as special traders.

    private readonly IReadOnlyList<TraderEditorRow> _traderRows;
    private readonly bool _editingExistingProfile;
    private int _level;
    private int? _prestigeLevel;

    public ProfileEditorWindow(
        GameMode gameMode,
        GameContentCatalog content,
        GameProfileSnapshot? existingProfile = null)
    {
        ArgumentNullException.ThrowIfNull(content);
        if (existingProfile is not null && existingProfile.GameMode != gameMode)
        {
            throw new ArgumentException(
                "Existing profile game mode does not match the editor game mode.",
                nameof(existingProfile));
        }

        InitializeComponent();

        _editingExistingProfile = existingProfile is not null;
        _level = existingProfile?.Level ?? 1;
        _prestigeLevel = existingProfile?.PrestigeLevel;

        TitleText.Text = existingProfile is null ? "새 프로필 설정" : "프로필 수정";
        ModeText.Text = $"{GameModeText(gameMode)} 캐릭터";
        DeleteProfileButton.Visibility = _editingExistingProfile
            ? Visibility.Visible
            : Visibility.Collapsed;
        UpdateTopValues();

        var factions = new[]
        {
            new FactionChoice(PmcFaction.Usec, "USEC"),
            new FactionChoice(PmcFaction.Bear, "BEAR"),
        };
        FactionComboBox.ItemsSource = factions;
        FactionComboBox.SelectedItem = factions.First(choice =>
            choice.Value == (existingProfile?.Faction ?? PmcFaction.Usec));

        var editions = new[] { new EditionChoice(null, "미입력") }
            .Concat(content.Editions
                .OrderBy(edition => edition.Title, StringComparer.CurrentCulture)
                .Select(edition => new EditionChoice(edition.Id, edition.Title)))
            .ToArray();
        EditionComboBox.ItemsSource = editions;
        EditionComboBox.SelectedItem = editions.FirstOrDefault(choice =>
            string.Equals(choice.Id, existingProfile?.EditionId, StringComparison.Ordinal)) ?? editions[0];

        var standingRequiredTraderIds = content.Quests
            .SelectMany(quest => quest.TraderStandingRequirements)
            .Select(requirement => requirement.TraderId)
            .ToHashSet(StringComparer.Ordinal);

        _traderRows = content.Traders
            .Select(trader =>
            {
                TraderProgress progress = default;
                var hasProgress = existingProfile is not null &&
                                  existingProfile.Traders.TryGetValue(trader.Id, out progress);
                var isFence = string.Equals(trader.Id, FenceTraderId, StringComparison.Ordinal) ||
                              string.Equals(trader.NameEn, "Fence", StringComparison.OrdinalIgnoreCase);

                return new TraderEditorRow
                {
                    TraderId = trader.Id,
                    Name = DisplayName(trader.NameKo, trader.NameEn, trader.Id),
                    IsFence = isFence,
                    NeedsAdvancedStanding = !isFence &&
                                            (standingRequiredTraderIds.Contains(trader.Id) ||
                                             (hasProgress && progress.Standing is not null)),
                    DisplayRank = UiReferenceOrder.TraderRank(trader),
                    LoyaltyLevel = isFence
                        ? hasProgress ? progress.LoyaltyLevel : null
                        : hasProgress ? progress.LoyaltyLevel ?? 1 : 1,
                    Standing = isFence
                        ? hasProgress ? progress.Standing ?? 0m : 0m
                        : hasProgress ? progress.Standing : null,
                };
            })
            .ToArray();

        var fenceRow = _traderRows.FirstOrDefault(row => row.IsFence);
        FencePanel.DataContext = fenceRow;
        FencePanel.Visibility = fenceRow is null ? Visibility.Collapsed : Visibility.Visible;

        var coreRows = _traderRows
            .Where(row => !row.IsFence && row.DisplayRank <= LastCoreTraderRank)
            .OrderBy(row => row.DisplayRank)
            .ThenBy(row => row.Name, StringComparer.CurrentCulture)
            .ToArray();
        TraderItems.ItemsSource = coreRows;

        var specialRows = _traderRows
            .Where(row => !row.IsFence && row.DisplayRank > LastCoreTraderRank)
            .OrderBy(row => row.DisplayRank)
            .ThenBy(row => row.Name, StringComparer.CurrentCulture)
            .ToArray();
        SpecialTraderItems.ItemsSource = specialRows;
        SpecialTraderPanel.Visibility = specialRows.Length > 0
            ? Visibility.Visible
            : Visibility.Collapsed;

        var advancedRows = _traderRows.Where(row => row.NeedsAdvancedStanding).ToArray();
        AdvancedStandingItems.ItemsSource = advancedRows;
        AdvancedStandingExpander.Visibility = advancedRows.Length > 0
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    public ProfileSettingsResult? Result { get; private set; }
    public bool DeleteRequested { get; private set; }

    private void LevelMinusButton_Click(object sender, RoutedEventArgs e)
    {
        _level = Math.Max(1, _level - 1);
        UpdateTopValues();
    }

    private void LevelPlusButton_Click(object sender, RoutedEventArgs e)
    {
        _level++;
        UpdateTopValues();
    }

    private void PrestigeMinusButton_Click(object sender, RoutedEventArgs e)
    {
        _prestigeLevel = _prestigeLevel switch
        {
            null => null,
            <= 0 => null,
            _ => _prestigeLevel.Value - 1,
        };
        UpdateTopValues();
    }

    private void PrestigePlusButton_Click(object sender, RoutedEventArgs e)
    {
        _prestigeLevel = _prestigeLevel is null ? 0 : _prestigeLevel.Value + 1;
        UpdateTopValues();
    }

    private static TraderEditorRow? RowFrom(object sender) =>
        (sender as FrameworkElement)?.DataContext as TraderEditorRow;

    private void TraderLoyaltyMinusButton_Click(object sender, RoutedEventArgs e)
    {
        if (RowFrom(sender) is not { IsFence: false } row)
            return;
        row.LoyaltyLevel = Math.Max(1, (row.LoyaltyLevel ?? 1) - 1);
    }

    private void TraderLoyaltyPlusButton_Click(object sender, RoutedEventArgs e)
    {
        if (RowFrom(sender) is not { IsFence: false } row)
            return;
        row.LoyaltyLevel = Math.Min(4, (row.LoyaltyLevel ?? 1) + 1);
    }

    private void TraderStandingMinusButton_Click(object sender, RoutedEventArgs e) =>
        AdjustStanding(sender, -0.1m);

    private void TraderStandingPlusButton_Click(object sender, RoutedEventArgs e) =>
        AdjustStanding(sender, 0.1m);

    private static void AdjustStanding(object sender, decimal delta)
    {
        if (RowFrom(sender) is not { } row || row.Standing is null)
            return;

        row.Standing = Math.Round(row.Standing.Value + delta, 2, MidpointRounding.AwayFromZero);
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (FactionComboBox.SelectedItem is not FactionChoice faction)
        {
            ShowValidation("진영을 선택해주세요.");
            return;
        }

        var traders = new Dictionary<string, TraderProgress>(StringComparer.Ordinal);
        foreach (var row in _traderRows)
        {
            int? loyalty = row.IsFence ? row.LoyaltyLevel : row.LoyaltyLevel ?? 1;
            decimal? standing = row.IsFence || row.IsStandingKnown ? row.Standing : null;
            if (loyalty is null && standing is null)
                continue;

            traders[row.TraderId] = new TraderProgress(loyalty, standing);
        }

        var editionId = (EditionComboBox.SelectedItem as EditionChoice)?.Id;
        Result = new ProfileSettingsResult(
            _level,
            faction.Value,
            editionId,
            _prestigeLevel,
            traders);
        DialogResult = true;
    }

    private void DeleteProfileButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_editingExistingProfile)
            return;

        var result = MessageBox.Show(
            this,
            "이 프로필의 퀘스트 진행, 은신처 레벨, 상인 진행, 보유 아이템 기록을 모두 삭제합니다. 다운로드된 게임 데이터와 다른 프로필은 유지됩니다.\n\n삭제하시겠습니까?",
            "프로필 삭제",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No);
        if (result != MessageBoxResult.Yes)
            return;

        DeleteRequested = true;
        Result = null;
        DialogResult = true;
    }

    private void UpdateTopValues()
    {
        LevelValueText.Text = $"Lv.{_level}";
        PrestigeValueText.Text = _prestigeLevel is null
            ? "미입력"
            : _prestigeLevel.Value.ToString(CultureInfo.CurrentCulture);
    }

    private void ShowValidation(string message)
    {
        MessageBox.Show(
            this,
            message,
            "프로필 설정",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private static string DisplayName(string? korean, string? english, string fallback) =>
        !string.IsNullOrWhiteSpace(korean)
            ? korean
            : !string.IsNullOrWhiteSpace(english)
                ? english
                : fallback;

    private static string GameModeText(GameMode gameMode) => gameMode switch
    {
        GameMode.Regular => "PvP",
        GameMode.Pve => "PvE",
        GameMode.PvpSeason => "시즌",
        _ => gameMode.ToString(),
    };

    private sealed record FactionChoice(PmcFaction Value, string Label)
    {
        public override string ToString() => Label;
    }

    private sealed record EditionChoice(string? Id, string Label)
    {
        public override string ToString() => Label;
    }
}
