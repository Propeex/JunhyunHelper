global using System.IO;

namespace TarkovHelper
{
    /// <summary>
    /// Minimal host contract expected by the original Tarkov Helper MapPage.
    /// The legacy Map source remains unchanged; JunhyunHelper implements this contract.
    /// </summary>
    public interface MainWindow
    {
        void SetFullScreenMode(bool enabled);
    }
}

namespace TarkovHelper.Debug
{
    /// <summary>
    /// Keeps the original namespace available for legacy source files that import it
    /// without requiring the unrelated old debug toolbox window.
    /// </summary>
    internal static class LegacyMapDebugNamespaceMarker
    {
    }
}
