using System.Windows;
using JunhyunHelper.Core.Profiles;

namespace JunhyunHelper.Desktop.Profiles;

public partial class ProfileModeWindow : Window, IInAppOverlayDialog
{
    private Action<bool?>? _inAppCloseRequested;

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

    void IInAppOverlayDialog.AttachInAppOverlay(Action<bool?> closeRequested) =>
        _inAppCloseRequested = closeRequested;

    bool IInAppOverlayDialog.TryDismissInAppOverlay()
    {
        _inAppCloseRequested?.Invoke(false);
        return true;
    }

    private void NextButton_Click(object sender, RoutedEventArgs e)
    {
        if (ModeList.SelectedItem is not ModeChoice choice)
        {
            MessageBox.Show(
                Window.GetWindow(ModeList) ?? System.Windows.Application.Current.MainWindow,
                "생성할 게임 모드를 선택해주세요.",
                "새 프로필",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        SelectedMode = choice.Mode;
        if (_inAppCloseRequested is not null)
            _inAppCloseRequested(true);
        else
            DialogResult = true;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        if (_inAppCloseRequested is not null)
            _inAppCloseRequested(false);
        else
            DialogResult = false;
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
