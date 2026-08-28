using JunhyunHelper.Core.Content;
using JunhyunHelper.Core.Profiles;
using JunhyunHelper.Infrastructure.EditionData;
using JunhyunHelper.Infrastructure.TarkovJson;
using JunhyunHelper.Infrastructure.TarkovJson.Items;
using JunhyunHelper.Infrastructure.TarkovJson.Quests;
using JunhyunHelper.Infrastructure.Validation;

namespace JunhyunHelper.Infrastructure.Content;

public sealed record TarkovContentBuildResult(
    GameContentCatalog Content,
    ContentValidationResult Validation,
    IReadOnlyList<string> Warnings)
{
    public bool IsValid => Validation.IsValid;
}

/// <summary>
/// Narrow build seam for the transactional update coordinator. Production uses
/// TarkovContentBuildService; tests can supply a deterministic candidate without network
/// or importer coupling and verify activation/failure behavior directly.
/// </summary>
public interface ITarkovContentBuildService
{
    Task<TarkovContentBuildResult> BuildAsync(
        GameMode gameMode,
        CancellationToken cancellationToken = default,
        IProgress<ContentUpdateProgress>? progress = null);
}

public sealed class TarkovContentBuildService : ITarkovContentBuildService
{
    private const int PrimarySourceCount = 8;

    private readonly TarkovEndpointSourceLoader _sourceLoader;
    private readonly TarkovEditionCatalogClient _editionClient;
    private readonly TarkovGameContentImporter _importer;
    private readonly TarkovItemRelationshipImporter _itemRelationshipImporter;
    private readonly GameContentIntegrityValidator _validator;
    private readonly ItemRelationshipIntegrityValidator _itemRelationshipValidator;
    private readonly WikiBallisticsEffectivenessClient? _effectivenessClient;

    public TarkovContentBuildService(
        TarkovEndpointSourceLoader sourceLoader,
        TarkovEditionCatalogClient editionClient,
        TarkovGameContentImporter? importer = null,
        GameContentValidator? validator = null,
        WikiBallisticsEffectivenessClient? effectivenessClient = null)
    {
        _sourceLoader = sourceLoader ?? throw new ArgumentNullException(nameof(sourceLoader));
        _editionClient = editionClient ?? throw new ArgumentNullException(nameof(editionClient));
        _importer = importer ?? new TarkovGameContentImporter();
        _itemRelationshipImporter = new TarkovItemRelationshipImporter();
        _validator = new GameContentIntegrityValidator(validator);
        _itemRelationshipValidator = new ItemRelationshipIntegrityValidator();
        _effectivenessClient = effectivenessClient;
    }

    public async Task<TarkovContentBuildResult> BuildAsync(
        GameMode gameMode,
        CancellationToken cancellationToken = default,
        IProgress<ContentUpdateProgress>? progress = null)
    {
        progress?.Report(new ContentUpdateProgress(
            ContentUpdateStage.Preparing,
            "온라인 데이터 요청을 준비하는 중...",
            3));

        var completedSources = 0;
        var sourceCount = PrimarySourceCount + (_effectivenessClient is null ? 0 : 1);

        async Task<T> TrackSourceAsync<T>(Task<T> task, string sourceName)
        {
            var result = await task;
            var completed = Interlocked.Increment(ref completedSources);
            progress?.Report(ContentUpdateProgress.ForDownloadedSource(
                sourceName,
                completed,
                sourceCount));
            return result;
        }

        var itemsTask = TrackSourceAsync(
            _sourceLoader.LoadAsync(gameMode, TarkovEndpoint.Items, cancellationToken),
            "아이템");
        var tradersTask = TrackSourceAsync(
            _sourceLoader.LoadAsync(gameMode, TarkovEndpoint.Traders, cancellationToken),
            "상인");
        var mapsTask = TrackSourceAsync(
            _sourceLoader.LoadAsync(gameMode, TarkovEndpoint.Maps, cancellationToken),
            "지도");
        var tasksTask = TrackSourceAsync(
            _sourceLoader.LoadAsync(gameMode, TarkovEndpoint.Tasks, cancellationToken),
            "퀘스트");
        var hideoutTask = TrackSourceAsync(
            _sourceLoader.LoadAsync(gameMode, TarkovEndpoint.Hideout, cancellationToken),
            "은신처");
        var bartersTask = TrackSourceAsync(
            _sourceLoader.LoadAsync(gameMode, TarkovEndpoint.Barters, cancellationToken),
            "물물교환");
        var craftsTask = TrackSourceAsync(
            _sourceLoader.LoadAsync(gameMode, TarkovEndpoint.Crafts, cancellationToken),
            "제작");
        var editionsTask = TrackSourceAsync(
            _editionClient.GetAsync(cancellationToken),
            "에디션 규칙");
        var effectivenessTask = _effectivenessClient is null
            ? Task.FromResult(new WikiArmorEffectivenessSource(
                Available: false,
                Rows: Array.Empty<WikiArmorEffectivenessRow>(),
                Warnings: Array.Empty<string>()))
            : TrackSourceAsync(
                _effectivenessClient.LoadAsync(cancellationToken),
                WikiBallisticsEffectivenessClient.SourceName);

        await Task.WhenAll(
            itemsTask,
            tradersTask,
            mapsTask,
            tasksTask,
            hideoutTask,
            bartersTask,
            craftsTask,
            editionsTask,
            effectivenessTask);

        var items = await itemsTask;
        var traders = await tradersTask;
        var maps = await mapsTask;
        var tasks = await tasksTask;
        var hideout = await hideoutTask;
        var barters = await bartersTask;
        var crafts = await craftsTask;
        var editions = await editionsTask;
        var effectiveness = await effectivenessTask;

        progress?.Report(new ContentUpdateProgress(
            ContentUpdateStage.Importing,
            "다운로드한 데이터를 준현 헬퍼 형식으로 변환하는 중...",
            70));

        var content = _importer.Import(
            items.Source,
            traders.Source,
            maps.Source,
            tasks.Source,
            hideout.Source,
            barters.Source,
            crafts.Source,
            editions,
            gameMode);

        var itemRelationships = _itemRelationshipImporter.Import(
            items.Source.BaseDocument,
            barters.Source.BaseDocument,
            crafts.Source.BaseDocument);

        // json.tarkov.dev currently exposes a small legacy/introductory quest set only
        // through opaque dialogue gates. Apply the narrow audited compatibility mapping
        // before validation so both live builds and persisted snapshots share the exact
        // same prerequisite semantics.
        content = content with
        {
            Quests = TarkovDialogueAvailabilityCompatibility.Apply(content.Quests),
            ItemRelationshipData = itemRelationships,
        };

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
            .ToList();

        if (_effectivenessClient is not null)
        {
            var enrichment = WikiBallisticsEffectivenessClient.Enrich(content, effectiveness);
            content = enrichment.Content;
            warnings.AddRange(enrichment.Warnings);
        }

        progress?.Report(new ContentUpdateProgress(
            ContentUpdateStage.Validating,
            "아이템·퀘스트·상인·지도·은신처·탄약·제작·교환 관계를 검증하는 중...",
            80));

        var baseValidation = _validator.Validate(content);
        var relationshipValidation = _itemRelationshipValidator.Validate(content);
        var validation = new ContentValidationResult(
            baseValidation.Issues.Concat(relationshipValidation.Issues).ToArray());

        return new TarkovContentBuildResult(
            content,
            validation,
            warnings.ToArray());
    }
}
