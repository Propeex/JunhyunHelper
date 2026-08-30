using System.Windows.Threading;
using JunhyunHelper.Desktop.Controls;

namespace JunhyunHelper.Desktop.Hideout;

public partial class HideoutPage
{
    protected override void OnInitialized(EventArgs e)
    {
        base.OnInitialized(e);
        _ = Dispatcher.BeginInvoke(
            DispatcherPriority.Loaded,
            new Action(() => SearchClearButtonInstaller.Install(SearchBox)));
    }
}
