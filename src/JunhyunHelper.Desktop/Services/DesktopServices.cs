using System.IO;
using System.Net.Http;
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
        Quests = new QuestApplicationService(Profiles);
    }

    public string RootDirectory { get; }

    public UserProfileStore Profiles { get; }

    public ContentActivationService Content { get; }

    public TarkovContentUpdateService ContentUpdater { get; }

    public QuestApplicationService Quests { get; }

    public void Dispose() => _httpClient.Dispose();
}
