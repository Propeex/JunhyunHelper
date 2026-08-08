using JunhyunHelper.Core.Content;
using JunhyunHelper.Infrastructure.TarkovJson.Ammo;
using JunhyunHelper.Infrastructure.TarkovJson.Hideout;
using JunhyunHelper.Infrastructure.TarkovJson.Items;
using JunhyunHelper.Infrastructure.TarkovJson.Quests;
using JunhyunHelper.Infrastructure.TarkovJson.Reference;

namespace JunhyunHelper.Infrastructure.TarkovJson;

public sealed class TarkovGameContentImporter
{
    private readonly TarkovItemImporter _itemImporter = new();
    private readonly TarkovTraderImporter _traderImporter = new();
    private readonly TarkovMapReferenceImporter _mapImporter = new();
    private readonly TarkovQuestImporter _questImporter = new();
    private readonly TarkovQuestObjectiveImporter _questObjectiveImporter = new();
    private readonly TarkovHideoutImporter _hideoutImporter = new();
    private readonly TarkovAmmoImporter _ammoImporter = new();

    public GameContentCatalog Import(
        TarkovEndpointSource items,
        TarkovEndpointSource traders,
        TarkovEndpointSource maps,
        TarkovEndpointSource tasks,
        TarkovEndpointSource hideout,
        TarkovEndpointSource barters,
        TarkovEndpointSource crafts)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(traders);
        ArgumentNullException.ThrowIfNull(maps);
        ArgumentNullException.ThrowIfNull(tasks);
        ArgumentNullException.ThrowIfNull(hideout);
        ArgumentNullException.ThrowIfNull(barters);
        ArgumentNullException.ThrowIfNull(crafts);

        var questObjectives = _questObjectiveImporter.Import(
            tasks.BaseDocument,
            tasks.Localization);

        return new GameContentCatalog(
            _itemImporter.Import(items.BaseDocument, items.Localization),
            _traderImporter.Import(traders.BaseDocument, traders.Localization),
            _mapImporter.Import(maps.BaseDocument, maps.Localization),
            _questImporter.Import(tasks.BaseDocument, tasks.Localization),
            questObjectives.Objectives,
            questObjectives.ItemRequirements,
            _hideoutImporter.Import(hideout.BaseDocument, hideout.Localization),
            _ammoImporter.Import(
                items.BaseDocument,
                barters.BaseDocument,
                crafts.BaseDocument));
    }
}
