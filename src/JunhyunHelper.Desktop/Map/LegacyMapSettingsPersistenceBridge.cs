using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using TarkovHelper.Services.Map;

namespace JunhyunHelper.Desktop.Map;

/// <summary>
/// Makes user-facing Map settings survive process restarts independently from the
/// transplanted Tarkov Helper settings database. Existing handlers still own the
/// actual Map behavior; this bridge only restores/snapshots product controls.
/// </summary>
public sealed class LegacyMapSettingsPersistenceBridge : IDisposable
{
    private static readonly string[] ToggleNames =
    [
        "ChkShowQuestMarkers",
        "ChkShowExtractMarkers",
        "ChkFixedView",
        "ChkHideCompletedObjectives",
        "ChkShowPmcExtracts",
        "ChkShowScavExtracts",
        "ChkShowTransitExtracts",
        "ChkShowPmcSpawns",
        "ChkShowSniperScavs",
        "ChkShowRogues",
        "ChkShowCultists",
        "ChkShowLeversMarker",
        "ChkShowBosses",
        "ChkCurrentMapOnly",
        "ChkGroupByQuest",
    ];

    private static readonly string[] SliderNames =
    [
        "SliderQuestNameTextSize",
        "SliderMarkerSize",
        "SliderPlayerMarkerSize",
        "SliderExtractTextSize",
    ];

    private static readonly string[] ComboNames =
    [
        "CmbQuestMarkerStyle",
        "CmbStatusFilter",
        "CmbTypeFilter",
    ];

    private readonly TarkovHelper.Pages.Map.MapPage _page;
    private readonly JunhyunMapProductSettingsStore _store = JunhyunMapProductSettingsStore.Instance;
    private readonly List<CheckBox> _toggles = [];
    private readonly List<Slider> _sliders = [];
    private readonly List<ComboBox> _combos = [];
    private readonly Dictionary<string, double> _pendingSliderValues = new(StringComparer.Ordinal);
    private readonly DispatcherTimer _sliderSaveTimer;
    private TextBox? _screenshotFolder;
    private bool _applying;
    private bool _hooked;
    private bool _disposed;

    public LegacyMapSettingsPersistenceBridge(TarkovHelper.Pages.Map.MapPage page)
    {
        _page = page ?? throw new ArgumentNullException(nameof(page));
        _sliderSaveTimer = new DispatcherTimer(DispatcherPriority.Background, _page.Dispatcher)
        {
            Interval = TimeSpan.FromMilliseconds(250),
        };
        _sliderSaveTimer.Tick += SliderSaveTimer_Tick;

        _page.Loaded += Page_Loaded;
        if (_page.IsLoaded)
            _page.Dispatcher.BeginInvoke(ApplyAndHook, DispatcherPriority.ContextIdle);
    }

    private void Page_Loaded(object sender, RoutedEventArgs e) =>
        _page.Dispatcher.BeginInvoke(ApplyAndHook, DispatcherPriority.ContextIdle);

    private void ApplyAndHook()
    {
        if (_disposed)
            return;

        DiscoverControls();
        ApplyPersistedValues();
        HookControls();
    }

    private void DiscoverControls()
    {
        if (_toggles.Count == 0)
        {
            foreach (var name in ToggleNames)
            {
                if (_page.FindName(name) is CheckBox toggle)
                    _toggles.Add(toggle);
            }
        }

        if (_sliders.Count == 0)
        {
            foreach (var name in SliderNames)
            {
                if (_page.FindName(name) is Slider slider)
                    _sliders.Add(slider);
            }
        }

        if (_combos.Count == 0)
        {
            foreach (var name in ComboNames)
            {
                if (_page.FindName(name) is ComboBox combo)
                    _combos.Add(combo);
            }
        }

        _screenshotFolder ??= _page.FindName("TxtScreenshotFolder") as TextBox;
    }

    private void ApplyPersistedValues()
    {
        _applying = true;
        try
        {
            foreach (var toggle in _toggles)
            {
                if (!string.IsNullOrWhiteSpace(toggle.Name) && _store.GetToggle(toggle.Name) is { } value)
                    toggle.IsChecked = value;
            }

            foreach (var slider in _sliders)
            {
                if (string.IsNullOrWhiteSpace(slider.Name) || _store.GetValue(slider.Name) is not { } value)
                    continue;
                slider.Value = Math.Clamp(value, slider.Minimum, slider.Maximum);
            }

            foreach (var combo in _combos)
            {
                if (string.IsNullOrWhiteSpace(combo.Name) || _store.GetSelection(combo.Name) is not { } index)
                    continue;
                if (index >= 0 && index < combo.Items.Count)
                    combo.SelectedIndex = index;
            }

            var folder = _store.ScreenshotFolder;
            if (!string.IsNullOrWhiteSpace(folder) && Directory.Exists(folder))
            {
                if (_screenshotFolder is not null)
                    _screenshotFolder.Text = folder;
                MapTrackerService.Instance.ChangeScreenshotFolder(folder);
            }
        }
        finally
        {
            _applying = false;
        }
    }

    private void HookControls()
    {
        if (_hooked)
            return;
        _hooked = true;

        foreach (var toggle in _toggles)
        {
            toggle.Checked += Toggle_Changed;
            toggle.Unchecked += Toggle_Changed;
        }

        foreach (var slider in _sliders)
            slider.ValueChanged += Slider_Changed;

        foreach (var combo in _combos)
            combo.SelectionChanged += Combo_SelectionChanged;

        if (_screenshotFolder is not null)
            _screenshotFolder.TextChanged += ScreenshotFolder_TextChanged;
    }

    private void Toggle_Changed(object sender, RoutedEventArgs e)
    {
        if (!_applying && sender is CheckBox { Name.Length: > 0 } toggle)
            _store.SetToggle(toggle.Name, toggle.IsChecked == true);
    }

    private void Slider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_applying || sender is not Slider { Name.Length: > 0 } slider)
            return;

        _pendingSliderValues[slider.Name] = e.NewValue;
        _sliderSaveTimer.Stop();
        _sliderSaveTimer.Start();
    }

    private void SliderSaveTimer_Tick(object? sender, EventArgs e) => FlushPendingSliderValues();

    private void FlushPendingSliderValues()
    {
        _sliderSaveTimer.Stop();
        if (_pendingSliderValues.Count == 0)
            return;

        var values = new Dictionary<string, double>(_pendingSliderValues, StringComparer.Ordinal);
        _pendingSliderValues.Clear();
        _store.SetValues(values);
    }

    private void Combo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_applying && sender is ComboBox { Name.Length: > 0 } combo && combo.SelectedIndex >= 0)
            _store.SetSelection(combo.Name, combo.SelectedIndex);
    }

    private void ScreenshotFolder_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_applying || sender is not TextBox textBox)
            return;

        var folder = textBox.Text.Trim();
        if (!string.IsNullOrWhiteSpace(folder) && Directory.Exists(folder))
            _store.ScreenshotFolder = folder;
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;

        _page.Loaded -= Page_Loaded;
        FlushPendingSliderValues();
        _sliderSaveTimer.Tick -= SliderSaveTimer_Tick;

        if (!_hooked)
            return;

        foreach (var toggle in _toggles)
        {
            toggle.Checked -= Toggle_Changed;
            toggle.Unchecked -= Toggle_Changed;
        }
        foreach (var slider in _sliders)
            slider.ValueChanged -= Slider_Changed;
        foreach (var combo in _combos)
            combo.SelectionChanged -= Combo_SelectionChanged;
        if (_screenshotFolder is not null)
            _screenshotFolder.TextChanged -= ScreenshotFolder_TextChanged;
    }
}
