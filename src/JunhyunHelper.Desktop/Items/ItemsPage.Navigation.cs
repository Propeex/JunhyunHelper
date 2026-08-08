using JunhyunHelper.Core.Items;

namespace JunhyunHelper.Desktop.Items;

public partial class ItemsPage
{
    public void NavigateToAnyItem(string itemId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(itemId);

        if (_allRows.All(row => row.ItemId != itemId) && _content is not null)
        {
            var item = _content.Items.FirstOrDefault(candidate =>
                string.Equals(candidate.Id, itemId, StringComparison.Ordinal));
            if (item is not null)
            {
                var category = ItemCategoryClassifier.Classify(item);
                var referenceRow = new ItemRow(
                    item.Id,
                    DisplayName(item.NameKo, item.NameEn, item.Id),
                    item.IconUrl,
                    category,
                    ItemCategoryClassifier.Label(category),
                    isFlexibleOnly: false,
                    requiredTotal: 0,
                    requiredFir: 0,
                    ownedFir: 0,
                    ownedNonFir: 0,
                    remainingTotal: 0,
                    remainingFir: 0,
                    surplusFir: 0,
                    surplusNonFir: 0,
                    protections: Array.Empty<CleanupProtection>(),
                    flexibleProgresses: Array.Empty<FlexibleQuestItemProgress>(),
                    sources: Array.Empty<SourceRow>(),
                    sourceSummary: "현재 진행 기준 필요 출처 없음",
                    statusText: "참고",
                    statusBrush: StatusBrush(0, 0, deferred: false, flexiblePending: false));

                _allRows = _allRows.Concat([referenceRow]).ToArray();
                PopulateCategoryFilter();

                if (_imageCache is not null && !string.IsNullOrWhiteSpace(referenceRow.IconUrl))
                    _ = LoadReferenceIconAsync(referenceRow);
            }
        }

        NavigateToItem(itemId);
    }

    private async Task LoadReferenceIconAsync(ItemRow row)
    {
        if (_imageCache is null || string.IsNullOrWhiteSpace(row.IconUrl))
            return;

        try
        {
            var image = await _imageCache.LoadAsync($"item-{row.ItemId}", row.IconUrl);
            if (image is null)
                return;

            row.Icon = image;
            if (ReferenceEquals(row, _selectedRow))
                DetailIcon.Source = image;
        }
        catch (OperationCanceledException)
        {
        }
    }
}
