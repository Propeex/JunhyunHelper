using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using JunhyunHelper.Core.Maps;

namespace JunhyunHelper.Desktop.Map;

internal static class MapBulkPersistenceBootstrap
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        EventManager.RegisterClassHandler(
            typeof(MapPage),
            FrameworkElement.LoadedEvent,
            new RoutedEventHandler((sender, _) =>
            {
                var page = (MapPage)sender;
                page.Dispatcher.BeginInvoke(
                    page.EnsureBulkPreferencesAfterInitialization,
                    DispatcherPriority.ContextIdle);
            }));
    }
}

public partial class MapPage
{
    private bool _bulkPreferenceSaveHooksAdded;

    internal void EnsureBulkPreferencesAfterInitialization()
    {
        // Runtime visibility remains false in the legacy per-control renderer;
        // the user's desired state lives in the dedicated bulk renderer.
        _settings.MarkerVisibility[MapMarkerKind.LootContainer] = false;
        _settings.MarkerVisibility[MapMarkerKind.LooseLoot] = false;

        if (!_bulkPreferenceSaveHooksAdded)
        {
            foreach (var checkBox in FindVisualChildren<CheckBox>(this))
            {
                if (checkBox.Tag is not string tag || tag is not ("LootContainer" or "LooseLoot"))
                    continue;
                checkBox.Checked += BulkPreferenceSave_Changed;
                checkBox.Unchecked += BulkPreferenceSave_Changed;
            }
            _bulkPreferenceSaveHooksAdded = true;
        }

        ScheduleBulkRefresh();
    }

    private void BulkPreferenceSave_Changed(object sender, RoutedEventArgs e)
    {
        _ = Dispatcher.BeginInvoke(
            async () => await PersistBulkPreferencesAsync(),
            DispatcherPriority.Background);
    }

    private async Task PersistBulkPreferencesAsync()
    {
        try
        {
            var directory = Path.GetDirectoryName(BulkPreferencePath)!;
            Directory.CreateDirectory(directory);
            var json = JsonSerializer.Serialize(new
            {
                showLootContainers = _showLootContainers,
                showLooseLoot = _showLooseLoot,
            });
            var temp = BulkPreferencePath + ".tmp";
            await File.WriteAllTextAsync(temp, json);
            File.Move(temp, BulkPreferencePath, overwrite: true);
        }
        catch (IOException)
        {
            // Map filter preference persistence is recoverable UI state.
        }
    }
}
