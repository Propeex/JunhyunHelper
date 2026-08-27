using System.IO;
using System.Text.Json;
using JunhyunHelper.Infrastructure.Storage;
using TarkovHelper.Models.Map;

namespace JunhyunHelper.Desktop.Map;

/// <summary>
/// First-party persistence for MiniMap window geometry that must survive restarts.
/// This deliberately lives under the JunhyunHelper user-data root instead of relying
/// on the transplanted Tarkov Helper settings store.
/// </summary>
public sealed class JunhyunMiniMapWindowStateStore
{
    private static readonly Lazy<JunhyunMiniMapWindowStateStore> LazyInstance =
        new(() => new JunhyunMiniMapWindowStateStore());

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly object _gate = new();
    private readonly AtomicJsonFileStore _fileStore;
    private JunhyunMiniMapWindowState _state;

    private JunhyunMiniMapWindowStateStore()
    {
        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "JunhyunHelper");
        Directory.CreateDirectory(root);
        _fileStore = new AtomicJsonFileStore(Path.Combine(root, "minimap-window-state.json"));
        _state = Load();
    }

    public static JunhyunMiniMapWindowStateStore Instance => LazyInstance.Value;

    public bool TryGetSize(out double width, out double height)
    {
        lock (_gate)
        {
            if (_state.Width is not { } savedWidth ||
                _state.Height is not { } savedHeight ||
                !double.IsFinite(savedWidth) ||
                !double.IsFinite(savedHeight))
            {
                width = 0;
                height = 0;
                return false;
            }

            width = Math.Clamp(savedWidth, OverlayMiniMapSettings.MinWidth, OverlayMiniMapSettings.MaxWidth);
            height = Math.Clamp(savedHeight, OverlayMiniMapSettings.MinHeight, OverlayMiniMapSettings.MaxHeight);
            return true;
        }
    }

    public void SetSize(double width, double height)
    {
        if (!double.IsFinite(width) || !double.IsFinite(height))
            return;

        lock (_gate)
        {
            _state.Width = Math.Clamp(width, OverlayMiniMapSettings.MinWidth, OverlayMiniMapSettings.MaxWidth);
            _state.Height = Math.Clamp(height, OverlayMiniMapSettings.MinHeight, OverlayMiniMapSettings.MaxHeight);
            SaveLocked();
        }
    }

    private JunhyunMiniMapWindowState Load()
    {
        try
        {
            return _fileStore.LoadOrDefault(() => new JunhyunMiniMapWindowState(), JsonOptions);
        }
        catch (Exception exception)
        {
            App.WriteDiagnostic("Failed to load MiniMap window state", exception);
            return new JunhyunMiniMapWindowState();
        }
    }

    private void SaveLocked()
    {
        try
        {
            _fileStore.Save(_state, JsonOptions);
        }
        catch (Exception exception)
        {
            App.WriteDiagnostic("Failed to save MiniMap window state", exception);
        }
    }
}

public sealed class JunhyunMiniMapWindowState
{
    public double? Width { get; set; }
    public double? Height { get; set; }
}
