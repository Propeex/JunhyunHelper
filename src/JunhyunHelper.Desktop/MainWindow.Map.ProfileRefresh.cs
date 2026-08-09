using System.Windows.Controls;
using System.Windows.Threading;

namespace JunhyunHelper.Desktop;

public partial class MainWindow
{
    private bool _mapProfileRefreshHooked;

    private void EnsureMapProfileRefreshHook()
    {
        if (_mapProfileRefreshHooked)
            return;

        ProfileComboBox.SelectionChanged += ProfileComboBox_MapRefreshSelectionChanged;
        _mapProfileRefreshHooked = true;
    }

    private void RemoveMapProfileRefreshHook()
    {
        if (!_mapProfileRefreshHooked)
            return;

        ProfileComboBox.SelectionChanged -= ProfileComboBox_MapRefreshSelectionChanged;
        _mapProfileRefreshHooked = false;
    }

    private void ProfileComboBox_MapRefreshSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _ = Dispatcher.BeginInvoke(
            async () =>
            {
                if (MapPlaceholder.IsVisible)
                    await RefreshMapPageFromActiveProfileAsync();
            },
            DispatcherPriority.ContextIdle);
    }
}
