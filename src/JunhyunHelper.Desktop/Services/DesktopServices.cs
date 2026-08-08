using System.IO;
using System.Net.Http;
using JunhyunHelper.Application.Hideout;
using JunhyunHelper.Application.Items;
using JunhyunHelper.Application.Profiles;
using JunhyunHelper.Application.Quests;
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
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("JunhyunHelper/0.1");

        var sourceLoader = new TarkovEndpointSourceLoader(new TarkovJsonClient(_httpClient));
        var buildService = new TarkovContentBuildService(
            sourceLoader,
            new TarkovEditionCatalogClient(_httpClient));

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

    public ProfileApplicationService ProfileManagement { get; }

    public QuestApplicationService Quests { get; }

    public HideoutApplicationService Hideout { get; }

    public ItemsApplicationService Items { get; }

    public void Dispose() => _httpClient.Dispose();
}
