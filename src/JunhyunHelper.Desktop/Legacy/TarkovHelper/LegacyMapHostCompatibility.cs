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

namespace TarkovHelper.Services
{
    /// <summary>
    /// Host environment boundary for the transplanted subsystem. The old Map code keeps
    /// its original AppEnv calls while JunhyunHelper owns the physical user-data location.
    /// </summary>
    internal static class AppEnv
    {
        private static readonly string Root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "JunhyunHelper",
            "legacy-tarkov-helper");

        public static string ConfigPath { get; } = Ensure(Root);

        private static string Ensure(string path)
        {
            Directory.CreateDirectory(path);
            return path;
        }
    }

    /// <summary>
    /// The original Map DB readers depend on DatabaseUpdateService only for the bundled
    /// tarkov_data.db path and its refresh notification. JunhyunHelper supplies that host
    /// boundary while leaving all original Map DB readers unchanged.
    /// </summary>
    public sealed class DatabaseUpdateService
    {
        private static readonly DatabaseUpdateService Singleton = new();

        public static DatabaseUpdateService Instance => Singleton;

        public string DatabasePath { get; } = Path.Combine(
            AppContext.BaseDirectory,
            "Assets",
            "tarkov_data.db");

        public event EventHandler? DatabaseUpdated;

        internal void NotifyDatabaseUpdated() => DatabaseUpdated?.Invoke(this, EventArgs.Empty);
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
