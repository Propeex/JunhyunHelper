using JunhyunHelper.Core.Content;
using JunhyunHelper.Core.Profiles;
using JunhyunHelper.Infrastructure.TarkovJson;
using JunhyunHelper.Infrastructure.Validation;

namespace JunhyunHelper.Infrastructure.Content;

public sealed record TarkovContentBuildResult(
    GameContentCatalog Content,
    ContentValidationResult Validation,
    IReadOnlyList<string> Warnings)
{
    public bool IsValid => Validation.IsValid;
}

public sealed class TarkovContentBuildService
{
    private readonly TarkovEndpointSourceLoader _sourceLoader;
    private readonly TarkovGameContentImporter _importer;
    private readonly GameContentValidator _validator;

    public TarkovContentBuildService(
        TarkovEndpointSourceLoader sourceLoader,
        TarkovGameContentImporter? importer = null,
        GameContentValidator? validator = null)
    {
        _sourceLoader = sourceLoader ?? throw new ArgumentNullException(nameof(sourceLoader));
        _importer = importer ?? new TarkovGameContentImporter();
        _validator = validator ?? new GameContentValidator();
    }

    public async Task<TarkovContentBuildResult> BuildAsync(
        GameMode gameMode,
        CancellationToken cancellationToken = default)
    {
        var itemsTask = _sourceLoader.LoadAsync(
            gameMode,
            TarkovEndpoint.Items,
            cancellationToken);
        var tradersTask = _sourceLoader.LoadAsync(
            gameMode,
            TarkovEndpoint.Traders,
            cancellationToken);
        var mapsTask = _sourceLoader.LoadAsync(
            gameMode,
            TarkovEndpoint.Maps,
            cancellationToken);
        var tasksTask = _sourceLoader.LoadAsync(
            gameMode,
            TarkovEndpoint.Tasks,
            cancellationToken);
        var hideoutTask = _sourceLoader.LoadAsync(
            gameMode,
            TarkovEndpoint.Hideout,
            cancellationToken);
        var bartersTask = _sourceLoader.LoadAsync(
            gameMode,
            TarkovEndpoint.Barters,
            cancellationToken);
        var craftsTask = _sourceLoader.LoadAsync(
            gameMode,
            TarkovEndpoint.Crafts,
            cancellationToken);

        await Task.WhenAll(
            itemsTask,
            tradersTask,
            mapsTask,
            tasksTask,
            hideoutTask,
            bartersTask,
            craftsTask);

        var items = await itemsTask;
        var traders = await tradersTask;
        var maps = await mapsTask;
        var tasks = await tasksTask;
        var hideout = await hideoutTask;
        var barters = await bartersTask;
        var crafts = await craftsTask;

        var content = _importer.Import(
            items.Source,
            traders.Source,
            maps.Source,
            tasks.Source,
            hideout.Source,
            barters.Source,
            crafts.Source);

        var warnings = new[]
            {
                items.Warnings,
                traders.Warnings,
                maps.Warnings,
                tasks.Warnings,
                hideout.Warnings,
                barters.Warnings,
                crafts.Warnings,
            }
            .SelectMany(static sourceWarnings => sourceWarnings)
            .ToArray();

        return new TarkovContentBuildResult(
            content,
            _validator.Validate(content),
            warnings);
    }
}
