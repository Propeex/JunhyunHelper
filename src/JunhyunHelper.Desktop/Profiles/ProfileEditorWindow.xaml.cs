using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using JunhyunHelper.Core.Content;
using JunhyunHelper.Core.Profiles;

namespace JunhyunHelper.Desktop.Profiles;

public sealed record ProfileSettingsResult(
    int Level,
    PmcFaction Faction,
    string? EditionId,
    int? PrestigeLevel,
    IReadOnlyDictionary<string, TraderProgress> Traders);

public sealed class TraderEditorRow
{
    public required string TraderId { get; init; }
    public required string Name { get; init; }
    public int? LoyaltyLevel { get; set; }
    public decimal? Standing { get; set; }
    public bool IsFence { get; init; }
    public bool StandingRelevant { get; init; }

    public string LoyaltyDisplay => LoyaltyLevel is { } value ? $"LL{value}" : "미입력";
    public string StandingDisplay => Standing is { } value
        ? value.ToString("0.0#", CultureInfo.InvariantCulture)
        : "미입력";
}

public partial class ProfileEditorWindow : Window
{
    private const string FenceTraderId = "579dc571d53a0658a154fbec";

    private readonly IReadOnlyList<TraderEditorRow> _traderRows;
    private readonly IReadOnlyList<TraderEditorRow> _loyaltyRows;
    private readonly IReadOnlyList<TraderEditorRow> _standingRows;
    private readonly TraderEditorRow? _fenceRow;
    private int _level;
    private int? _prestige;

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

        TitleText.Text = existingProfile is null ? "새 프로필 설정" : "프로필 수정";
        ModeText.Text = $"{GameModeText(gameMode)} 캐릭터";
        DeleteProfileButton.Visibility = existingProfile is null ? Visibility.Collapsed : Visibility.Visible;

        _level = existingProfile?.Level ?? 1;
        _prestige = existingProfile?.PrestigeLevel;
        UpdateProfileStepTexts();

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

        var standingRelevantTraderIds = content.Quests
            .SelectMany(quest => quest.TraderStandingRequirements)
            .Select(requirement => requirement.TraderId)
            .ToHashSet(StringComparer.Ordinal);

        _traderRows = content.Traders
            .Select(trader =>
            {
                TraderProgress progress = default;
                var hasProgress = existingProfile is not null &&
                                  existingProfile.Traders.TryGetValue(trader.Id, out progress);
                var name = DisplayName(trader.NameKo, trader.NameEn, trader.Id);
                var isFence = string.Equals(trader.Id, FenceTraderId, StringComparison.Ordinal) ||
                              string.Equals(trader.NameEn, "Fence", StringComparison.OrdinalIgnoreCase);

                return new TraderEditorRow
                {
                    TraderId = trader.Id,
                    Name = name,
                    LoyaltyLevel = hasProgress ? progress.LoyaltyLevel : null,
                    Standing = hasProgress ? progress.Standing : null,
                    IsFence = isFence,
                    StandingRelevant = standingRelevantTraderIds.Contains(trader.Id),
                };
            })
            .OrderBy(row => row.Name, StringComparer.CurrentCulture)
            .ToArray();

        _fenceRow = _traderRows.FirstOrDefault(row => row.IsFence);
        _loyaltyRows = _traderRows.Where(row => !row.IsFence).ToArray();
        _standingRows = _traderRows
            .Where(row => !row.IsFence && (row.StandingRelevant || row.Standing is not null))
            .ToArray();

        TraderLoyaltyItems.ItemsSource = _loyaltyRows;
        TraderStandingItems.ItemsSource = _standingRows;
        AdvancedStandingExpander.Visibility = _standingRows.Count > 0
            ? Visibility.Visible
            : Visibility.Collapsed;
        FencePanel.Visibility = _fenceRow is null ? Visibility.Collapsed : Visibility.Visible;
        UpdateFenceText();
    }

    public ProfileSettingsResult? Result { get; private set; }
    public bool DeleteRequested { get; private set; }

    private void LevelMinusButton_Click(object sender, RoutedEventArgs e)
    {
        _level = Math.Max(1, _level - 1);
        UpdateProfileStepTexts();
    }

    private void LevelPlusButton_Click(object sender, RoutedEventArgs e)
    {
        _level++;
        UpdateProfileStepTexts();
    }

    private void PrestigeMinusButton_Click(object sender, RoutedEventArgs e)
    {
        _prestige = _prestige switch
        {
            null => null,
            <= 0 => null,
            _ => _prestige.Value - 1,
        };
        UpdateProfileStepTexts();
    }

    private void PrestigePlusButton_Click(object sender, RoutedEventArgs e)
    {
        _prestige = _prestige is null ? 0 : _prestige.Value + 1;
        UpdateProfileStepTexts();
    }

    private void PrestigeValueButton_Click(object sender, RoutedEventArgs e)
    {
        _prestige = _prestige is null ? 0 : null;
        UpdateProfileStepTexts();
    }

    private void TraderLoyaltyMinusButton_Click(object sender, RoutedEventArgs e)
    {
        if (TraderRow(sender) is not { } row)
            return;

        row.LoyaltyLevel = row.LoyaltyLevel switch
        {
            null => null,
            <= 1 => null,
            _ => row.LoyaltyLevel.Value - 1,
        };
        TraderLoyaltyItems.Items.Refresh();
    }

    private void TraderLoyaltyPlusButton_Click(object sender, RoutedEventArgs e)
    {
        if (TraderRow(sender) is not { } row)
            return;

        row.LoyaltyLevel = row.LoyaltyLevel is null
            ? 1
            : Math.Min(4, row.LoyaltyLevel.Value + 1);
        TraderLoyaltyItems.Items.Refresh();
    }

    private void FenceStandingMinusButton_Click(object sender, RoutedEventArgs e)
    {
        if (_fenceRow is null)
            return;
        _fenceRow.Standing = (_fenceRow.Standing ?? 0m) - 0.1m;
        UpdateFenceText();
    }

    private void FenceStandingPlusButton_Click(object sender, RoutedEventArgs e)
    {
        if (_fenceRow is null)
            return;
        _fenceRow.Standing = (_fenceRow.Standing ?? 0m) + 0.1m;
        UpdateFenceText();
    }

    private void FenceStandingValueButton_Click(object sender, RoutedEventArgs e)
    {
        if (_fenceRow is null)
            return;
        _fenceRow.Standing = _fenceRow.Standing is null ? 0m : null;
        UpdateFenceText();
    }

    private void TraderStandingMinusButton_Click(object sender, RoutedEventArgs e) =>
        ChangeTraderStanding(sender, -0.1m);

    private void TraderStandingPlusButton_Click(object sender, RoutedEventArgs e) =>
        ChangeTraderStanding(sender, 0.1m);

    private void TraderStandingValueButton_Click(object sender, RoutedEventArgs e)
    {
        if (TraderRow(sender) is not { } row)
            return;
        row.Standing = row.Standing is null ? 0m : null;
        TraderStandingItems.Items.Refresh();
    }

    private void ChangeTraderStanding(object sender, decimal delta)
    {
        if (TraderRow(sender) is not { } row)
            return;
        row.Standing = (row.Standing ?? 0m) + delta;
        TraderStandingItems.Items.Refresh();
    }

    private static TraderEditorRow? TraderRow(object sender) =>
        sender is FrameworkElement { Tag: TraderEditorRow row } ? row : null;

    private void UpdateProfileStepTexts()
    {
        LevelValueText.Text = $"Lv.{_level}";
        PrestigeValueButton.Content = _prestige?.ToString(CultureInfo.InvariantCulture) ?? "미입력";
    }

    private void UpdateFenceText()
    {
        if (_fenceRow is null)
            return;
        FenceStandingValueButton.Content = _fenceRow.StandingDisplay;
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (FactionComboBox.SelectedItem is not FactionChoice faction)
        {
            ShowValidation("진영을 선택해주세요.");
            return;
        }

        var traders = _traderRows
            .Select(row => new KeyValuePair<string, TraderProgress>(
                row.TraderId,
                new TraderProgress(row.LoyaltyLevel, row.Standing)))
            .Where(pair => pair.Value.HasAnyValue)
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);

        var editionId = (EditionComboBox.SelectedItem as EditionChoice)?.Id;
        Result = new ProfileSettingsResult(
            _level,
            faction.Value,
            editionId,
            _prestige,
            traders);
        DialogResult = true;
    }

    private void DeleteProfileButton_Click(object sender, RoutedEventArgs e)
    {
        var confirmation = MessageBox.Show(
            this,
            "이 프로필의 완료/실패 퀘스트, 은신처 진행, 상인 진행, 보유 아이템을 모두 삭제합니다.\n" +
            "다운로드한 게임 데이터와 다른 게임 모드 프로필은 유지됩니다.\n\n" +
            "삭제 후 되돌릴 수 없습니다. 계속하시겠습니까?",
            "프로필 삭제",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No);

        if (confirmation != MessageBoxResult.Yes)
            return;

        DeleteRequested = true;
        DialogResult = true;
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
