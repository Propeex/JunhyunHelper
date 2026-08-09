using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace JunhyunHelper.Desktop.Map;

internal static class MapBulkPreferenceCanonicalWriterBootstrap
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
                    page.AttachCanonicalBulkPreferenceWriter,
                    DispatcherPriority.ContextIdle);
            }));
    }
}

public partial class MapPage
{
    private bool _canonicalBulkPreferenceWriterAttached;

    internal void AttachCanonicalBulkPreferenceWriter()
    {
        if (_canonicalBulkPreferenceWriterAttached)
            return;

        foreach (var checkBox in FindVisualChildren<CheckBox>(this))
        {
            if (checkBox.Tag is not string tag || tag is not ("LootContainer" or "LooseLoot"))
                continue;
            checkBox.Checked += CanonicalBulkPreference_Changed;
            checkBox.Unchecked += CanonicalBulkPreference_Changed;
        }
        _canonicalBulkPreferenceWriterAttached = true;
    }

    private void CanonicalBulkPreference_Changed(object sender, RoutedEventArgs e)
    {
        _ = Dispatcher.BeginInvoke(
            async () => await WriteCanonicalBulkPreferencesAsync(),
            DispatcherPriority.ApplicationIdle);
    }

    private async Task WriteCanonicalBulkPreferencesAsync()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(BulkPreferencePath)!);
            var json = JsonSerializer.Serialize(new Dictionary<string, bool>
            {
                ["ShowLootContainers"] = _showLootContainers,
                ["ShowLooseLoot"] = _showLooseLoot,
            });
            var temp = BulkPreferencePath + ".canonical.tmp";
            await File.WriteAllTextAsync(temp, json);
            File.Move(temp, BulkPreferencePath, overwrite: true);
        }
        catch (IOException)
        {
        }
    }
}
