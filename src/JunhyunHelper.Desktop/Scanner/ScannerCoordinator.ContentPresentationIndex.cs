using JunhyunHelper.Core.Content;
using JunhyunHelper.Core.Hideout;
using JunhyunHelper.Core.Items;
using JunhyunHelper.Core.Quests;
using JunhyunHelper.Core.Reference;

namespace JunhyunHelper.Desktop.Scanner;

public sealed partial class ScannerCoordinator
{
    private readonly object _contentPresentationIndexGate = new();
    private GameContentCatalog? _indexedPresentationContent;
    private ScannerContentPresentationIndex? _contentPresentationIndex;

    private ScannerContentPresentationIndex GetContentPresentationIndex(GameContentCatalog content)
    {
        lock (_contentPresentationIndexGate)
        {
            if (ReferenceEquals(_indexedPresentationContent, content) &&
                _contentPresentationIndex is not null)
            {
                return _contentPresentationIndex;
            }

            var index = new ScannerContentPresentationIndex(
                content.Items.ToDictionary(item => item.Id, StringComparer.Ordinal),
                content.Quests.ToDictionary(quest => quest.Id, StringComparer.Ordinal),
                content.Traders.ToDictionary(trader => trader.Id, StringComparer.Ordinal),
                content.HideoutStations.ToDictionary(station => station.Id, StringComparer.Ordinal));

            _indexedPresentationContent = content;
            _contentPresentationIndex = index;
            return index;
        }
    }

    private sealed record ScannerContentPresentationIndex(
        IReadOnlyDictionary<string, GameItem> ItemsById,
        IReadOnlyDictionary<string, QuestDefinition> QuestsById,
        IReadOnlyDictionary<string, TraderDefinition> TradersById,
        IReadOnlyDictionary<string, HideoutStation> StationsById);
}
