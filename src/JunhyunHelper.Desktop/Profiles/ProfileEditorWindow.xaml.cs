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
    public bool IsIncluded { get; set; }
    public string LoyaltyText { get; set; } = "1";
    public string StandingText { get; set; } = "0";
}

public partial class ProfileEditorWindow : Window
{
    private readonly IReadOnlyList<TraderEditorRow> _traderRows;

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
        LevelTextBox.Text = (existingProfile?.Level ?? 1).ToString(CultureInfo.InvariantCulture);
        PrestigeTextBox.Text = (existingProfile?.PrestigeLevel ?? 0).ToString(CultureInfo.InvariantCulture);

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

        _traderRows = content.Traders
            .OrderBy(trader => DisplayName(trader.NameKo, trader.NameEn, trader.Id), StringComparer.CurrentCulture)
            .Select(trader =>
            {
                TraderProgress progress = default;
                var hasProgress = existingProfile is not null &&
                                  existingProfile.Traders.TryGetValue(trader.Id, out progress);
                return new TraderEditorRow
                {
                    TraderId = trader.Id,
                    Name = DisplayName(trader.NameKo, trader.NameEn, trader.Id),
                    IsIncluded = hasProgress,
                    LoyaltyText = hasProgress
                        ? progress.LoyaltyLevel.ToString(CultureInfo.InvariantCulture)
                        : "1",
                    StandingText = hasProgress
                        ? progress.Standing.ToString(CultureInfo.InvariantCulture)
                        : "0",
                };
            })
            .ToArray();
        TraderGrid.ItemsSource = _traderRows;
    }

    public ProfileSettingsResult? Result { get; private set; }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        TraderGrid.CommitEdit(DataGridEditingUnit.Cell, true);
        TraderGrid.CommitEdit(DataGridEditingUnit.Row, true);

        if (!int.TryParse(LevelTextBox.Text?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var level) ||
            level < 1)
        {
            ShowValidation("레벨은 1 이상의 정수로 입력해주세요.");
            return;
        }

        int? prestige = null;
        var prestigeText = PrestigeTextBox.Text?.Trim();
        if (!string.IsNullOrWhiteSpace(prestigeText))
        {
            if (!int.TryParse(prestigeText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedPrestige) ||
                parsedPrestige < 0)
            {
                ShowValidation("프레스티지는 0 이상의 정수로 입력하거나 비워두세요.");
                return;
            }
            prestige = parsedPrestige;
        }

        if (FactionComboBox.SelectedItem is not FactionChoice faction)
        {
            ShowValidation("진영을 선택해주세요.");
            return;
        }

        var traders = new Dictionary<string, TraderProgress>(StringComparer.Ordinal);
        foreach (var row in _traderRows.Where(row => row.IsIncluded))
        {
            if (!int.TryParse(row.LoyaltyText?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var loyalty) ||
                loyalty < 0 || loyalty > 4)
            {
                ShowValidation($"{row.Name}의 LL은 0~4 사이 정수로 입력해주세요.");
                return;
            }

            if (!TryParseDecimal(row.StandingText, out var standing))
            {
                ShowValidation($"{row.Name}의 평판을 숫자로 입력해주세요.");
                return;
            }

            traders[row.TraderId] = new TraderProgress(loyalty, standing);
        }

        var editionId = (EditionComboBox.SelectedItem as EditionChoice)?.Id;
        Result = new ProfileSettingsResult(
            level,
            faction.Value,
            editionId,
            prestige,
            traders);
        DialogResult = true;
    }

    private static bool TryParseDecimal(string? text, out decimal value)
    {
        var trimmed = text?.Trim();
        return decimal.TryParse(trimmed, NumberStyles.Number, CultureInfo.CurrentCulture, out value) ||
               decimal.TryParse(trimmed, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
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
