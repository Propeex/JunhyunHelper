using System.IO;
using System.Net.Http;
using JunhyunHelper.Application.Hideout;
using JunhyunHelper.Application.Items;
using JunhyunHelper.Application.Profiles;
using JunhyunHelper.Application.Quests;
using JunhyunHelper.Desktop.Scanner;
using JunhyunHelper.Infrastructure.Content;
using JunhyunHelper.Infrastructure.EditionData;
using JunhyunHelper.Infrastructure.Storage;
using JunhyunHelper.Infrastructure.TarkovJson;

namespace JunhyunHelper.Desktop.Services;

public sealed class DesktopServices : IDisposable
{
    private readonly HttpClient _httpClient;

    public DesktopServices(string? rootDirectory = null)
    {
        RootDirectory = Path.GetFullPath(
            rootDirectory ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "JunhyunHelper"));

        Directory.CreateDirectory(RootDirectory);

        Profiles = new UserProfileStore(Path.Combine(RootDirectory, "user.db"));
        Content = new ContentActivationService(Path.Combine(RootDirectory, "content"));

        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(3),
        };
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
            $"JunhyunHelper/{ProductUserAgentVersion()} (+https://github.com/Propeex/JunhyunHelper)");

        Images = new ImageCacheService(_httpClient, RootDirectory);
        AmmoFavorites = new AmmoFavoriteStore(RootDirectory);
        ScannerItemUiState = new ScannerItemUiStateStore(RootDirectory);
        Scanner = new ScannerCoordinator(_httpClient, RootDirectory);

        var sourceLoader = new TarkovEndpointSourceLoader(new TarkovJsonClient(_httpClient));
        var buildService = new TarkovContentBuildService(
            sourceLoader,
            new TarkovEditionCatalogClient(_httpClient),
            effectivenessClient: new WikiBallisticsEffectivenessClient(_httpClient));

        ContentUpdater = new TarkovContentUpdateService(buildService, Content);
        ProfileManagement = new ProfileApplicationService(Profiles);
        Quests = new QuestApplicationService(Profiles);
        Hideout = new HideoutApplicationService(Profiles);
        Items = new ItemsApplicationService(Profiles);
    }

    public string RootDirectory { get; }

    public UserProfileStore Profiles { get; }

    public ContentActivationService Content { get; }

    public TarkovContentUpdateService ContentUpdater { get; }

    public ImageCacheService Images { get; }

    public AmmoFavoriteStore AmmoFavorites { get; }

    public ScannerItemUiStateStore ScannerItemUiState { get; }

    public ScannerCoordinator Scanner { get; }

    public ProfileApplicationService ProfileManagement { get; }

    public QuestApplicationService Quests { get; }

    public HideoutApplicationService Hideout { get; }

    public ItemsApplicationService Items { get; }

    public void Dispose()
    {
        Scanner.Dispose();
        _httpClient.Dispose();
    }

    private static string ProductUserAgentVersion()
    {
        var version = typeof(DesktopServices).Assembly.GetName().Version;
        return version is null
            ? "1.0"
            : $"{Math.Max(0, version.Major)}.{Math.Max(0, version.Minor)}";
    }
}
