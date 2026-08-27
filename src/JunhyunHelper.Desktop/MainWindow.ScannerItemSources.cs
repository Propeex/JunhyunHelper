using JunhyunHelper.Core.Items;
using JunhyunHelper.Desktop.Items;

namespace JunhyunHelper.Desktop;

internal enum ScannerNeededSourceKind
{
    Quest,
    Hideout,
}

internal sealed record ScannerNeededSourceRow(
    ScannerNeededSourceKind Kind,
    string TargetId,
    string Title,
    string Detail);

public partial class MainWindow
{
    internal IReadOnlyList<ScannerNeededSourceRow> GetScannerNeededSources(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId) || _activeItemsWorkspace is null || _activeContent is null)
            return [];

        var needed = _activeItemsWorkspace.Plan.NeededItems
            .FirstOrDefault(item => string.Equals(item.ItemId, itemId, StringComparison.Ordinal));
        if (needed is null || (needed.RemainingTotal <= 0 && needed.RemainingFir <= 0))
            return [];

        return needed.Sources
            .Select(BuildScannerNeededSource)
            .Where(row => row is not null)
            .Cast<ScannerNeededSourceRow>()
            .DistinctBy(row => (row.Kind, row.TargetId, row.Detail))
            .ToArray();
    }

    internal void NavigateFromScannerNeededSource(ScannerNeededSourceRow source)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (source.Kind == ScannerNeededSourceKind.Quest)
        {
            ItemsPage_QuestNavigationRequested(
                this,
                new ItemQuestNavigationRequestedEventArgs(source.TargetId));
            return;
        }

        ItemsPage_HideoutNavigationRequested(
            this,
            new ItemHideoutNavigationRequestedEventArgs(source.TargetId));
    }

    private ScannerNeededSourceRow? BuildScannerNeededSource(ItemRequirementSource source)
    {
        if (_activeContent is null)
            return null;

        if (source.Kind == ItemRequirementSourceKind.Quest)
        {
            var quest = _activeContent.Quests.FirstOrDefault(candidate =>
                string.Equals(candidate.Id, source.SourceId, StringComparison.Ordinal));
            return new ScannerNeededSourceRow(
                ScannerNeededSourceKind.Quest,
                source.SourceId,
                $"퀘스트 · {ScannerDisplayName(quest?.NameKo, quest?.NameEn, source.SourceId)}",
                "클릭해서 퀘스트 정보로 이동");
        }

        var station = _activeContent.HideoutStations.FirstOrDefault(candidate =>
            string.Equals(candidate.Id, source.SourceId, StringComparison.Ordinal));
        return new ScannerNeededSourceRow(
            ScannerNeededSourceKind.Hideout,
            source.SourceId,
            $"은신처 · {ScannerDisplayName(station?.NameKo, station?.NameEn, source.SourceId)}",
            string.IsNullOrWhiteSpace(source.DetailId) ? "업그레이드 재료" : $"Lv.{source.DetailId} 업그레이드");
    }

    private static string ScannerDisplayName(string? korean, string? english, string fallback) =>
        !string.IsNullOrWhiteSpace(korean)
            ? korean
            : !string.IsNullOrWhiteSpace(english)
                ? english
                : fallback;
}
