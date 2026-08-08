using System.Windows;
using JunhyunHelper.Core.Profiles;

namespace JunhyunHelper.Desktop.Profiles;

public partial class ProfileModeWindow : Window
{
    public ProfileModeWindow(IReadOnlyCollection<GameMode> availableModes)
    {
        ArgumentNullException.ThrowIfNull(availableModes);
        InitializeComponent();

        ModeList.ItemsSource = availableModes
            .Select(mode => new ModeChoice(mode, GameModeText(mode)))
            .ToArray();
        ModeList.SelectedIndex = ModeList.Items.Count > 0 ? 0 : -1;
    }

    public GameMode? SelectedMode { get; private set; }

    private void NextButton_Click(object sender, RoutedEventArgs e)
    {
        if (ModeList.SelectedItem is not ModeChoice choice)
        {
            MessageBox.Show(
                this,
                "생성할 게임 모드를 선택해주세요.",
                "새 프로필",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        SelectedMode = choice.Mode;
        DialogResult = true;
    }

    private static string GameModeText(GameMode gameMode) => gameMode switch
    {
        GameMode.Regular => "PvP 캐릭터",
        GameMode.Pve => "PvE 캐릭터",
        GameMode.PvpSeason => "시즌 캐릭터",
        _ => gameMode.ToString(),
    };

    private sealed record ModeChoice(GameMode Mode, string Label)
    {
        public override string ToString() => Label;
    }
}
