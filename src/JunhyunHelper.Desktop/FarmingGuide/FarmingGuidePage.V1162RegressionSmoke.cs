using System.Windows;
using System.Windows.Controls;
using JunhyunHelper.Core.FarmingGuide;

namespace JunhyunHelper.Desktop.FarmingGuide;

public partial class FarmingGuidePage
{
    private void VerifyV1162RaidValueAndReservedCellSmoke()
    {
        const string itemId = "__junhyun_smoke_v1162_value_loot";
        const string instanceId = "__junhyun_smoke_v1162_value_instance";
        const int fleaAveragePrice = 12_345;

        _itemsById.TryGetValue(itemId, out var previousItem);
        var previousRaidSession = _raidSession;
        var previousPrice = _raidFleaAveragePrices.TryGetValue(itemId, out var remembered)
            ? remembered
            : (int?)null;
        var reserved = new FarmingGuideLockedCell(
            FarmingGuideStorageKind.Pockets,
            0,
            0,
            0);
        var hadReservation = _reservedCells.Contains(reserved);

        var item = SmokeItem(itemId);
        _itemsById[itemId] = item;
        var baseline = BuildSnapshot();
        var placement = new FarmingGuideStoredItemState(
            instanceId,
            FarmingGuideItemState.Create(itemId, raidAcquired: true),
            FarmingGuideStorageKind.Pockets,
            0,
            0,
            0,
            false);

        try
        {
            _raidSession = new FarmingGuideRaidSession(baseline, BuildLockState());
            _raidFleaAveragePrices[itemId] = fleaAveragePrice;
            StoredItems.Add(placement);
            _reservedCells.Add(reserved);

            RefreshRaidUi();
            var expectedValue = $"₽{fleaAveragePrice:N0}";
            if (!string.Equals(ValueSummaryText.Text, expectedValue, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Raid farming value summary rendered '{ValueSummaryText.Text}' instead of '{expectedValue}'.");
            }

            var canvas = CreateGridCanvas(
                FarmingGuideStorageKind.Pockets,
                0,
                new FarmingGuideStorageGridDefinition(1, 1, FarmingGuideItemFilter.Empty));
            if (canvas.Tag is not GridDropTarget grid)
                throw new InvalidOperationException("Reserved-cell smoke grid lost its drop target identity.");
            AddReservedCellVisuals(canvas, grid);

            var card = canvas.Children
                .OfType<Border>()
                .FirstOrDefault(value => value.Tag is PlacedItemSource source &&
                                         string.Equals(source.Placement.InstanceId, instanceId, StringComparison.Ordinal))
                ?? throw new InvalidOperationException("Item placed in the reserved cell was not rendered.");
            var overlay = canvas.Children
                .OfType<Border>()
                .FirstOrDefault(value => value.ToolTip?.ToString() == "자동 배치 사용 금지")
                ?? throw new InvalidOperationException("Reserved-cell marker was not rendered.");

            if (Panel.GetZIndex(overlay) >= Panel.GetZIndex(card))
            {
                throw new InvalidOperationException(
                    $"Reserved-cell marker z-index {Panel.GetZIndex(overlay)} still masks item z-index {Panel.GetZIndex(card)}.");
            }
        }
        finally
        {
            StoredItems.RemoveAll(value => string.Equals(value.InstanceId, instanceId, StringComparison.Ordinal));
            if (!hadReservation)
                _reservedCells.Remove(reserved);
            _raidSession = previousRaidSession;
            if (previousPrice is { } oldPrice)
                _raidFleaAveragePrices[itemId] = oldPrice;
            else
                _raidFleaAveragePrices.Remove(itemId);

            if (previousItem is not null)
                _itemsById[itemId] = previousItem;
            else
                _itemsById.Remove(itemId);

            RefreshRaidUi();
        }
    }
}
