using System.Windows;
using System.Windows.Controls;
using JunhyunHelper.Core.FarmingGuide;
using JunhyunHelper.Core.Items;
using JunhyunHelper.Desktop.Scanner;

namespace JunhyunHelper.Desktop.FarmingGuide;

public partial class FarmingGuidePage
{
    private void VerifyDedicatedNestedRaidPrioritySmoke()
    {
        const string secureId = "__junhyun_smoke_priority_secure";
        const string caseId = "__junhyun_smoke_priority_case";
        const string keyId = "__junhyun_smoke_priority_key";
        const string keyCategoryId = "__junhyun_smoke_priority_key_category";
        const string parentInstanceId = "__junhyun_smoke_priority_parent";

        var ids = new[] { secureId, caseId, keyId };
        var previousItems = ids.ToDictionary(
            id => id,
            id => _itemsById.TryGetValue(id, out var item) ? item : null,
            StringComparer.Ordinal);
        var previousSecure = _secureContainer;

        var secure = SmokeItem(secureId) with
        {
            FarmingGuideData = new FarmingGuideItemLayout(
                "ItemPropertiesContainer",
                [new FarmingGuideStorageGridDefinition(2, 2, FarmingGuideItemFilter.Empty)],
                [],
                [],
                [],
                [],
                false,
                false),
        };
        var specializedCase = SmokeItem(caseId) with
        {
            FarmingGuideData = new FarmingGuideItemLayout(
                "ItemPropertiesContainer",
                [
                    new FarmingGuideStorageGridDefinition(
                        1,
                        2,
                        new FarmingGuideItemFilter([keyCategoryId], [], [], [])),
                ],
                [],
                [],
                [],
                [],
                false,
                false),
        };
        var key = SmokeItem(keyId) with
        {
            CategoryIds = [keyCategoryId],
        };

        _itemsById[secureId] = secure;
        _itemsById[caseId] = specializedCase;
        _itemsById[keyId] = key;
        _secureContainer = FarmingGuideItemState.Create(secureId);
        _storedItems.Add(new FarmingGuideStoredItemState(
            parentInstanceId,
            FarmingGuideItemState.Create(caseId),
            FarmingGuideStorageKind.SecureContainer,
            0,
            0,
            0,
            false));

        try
        {
            var scanned = new ScannerItemSnapshot(
                keyId,
                keyId,
                null,
                TraderSellPrice: 0,
                FleaAveragePrice: 0,
                TraderPricePerSlot: 0,
                FleaPricePerSlot: 0,
                Slots: 1,
                CurrentNeeded: 0);

            var recommendation = PlanScannedItemHardened(scanned, key);
            if (recommendation.Action != FarmingGuideInstructionAction.Store)
            {
                throw new InvalidOperationException(
                    $"Dedicated nested storage priority returned unexpected action {recommendation.Action}.");
            }

            var added = recommendation.ProposedSnapshot.StoredItems
                .FirstOrDefault(item =>
                    string.Equals(item.Item.ItemId, keyId, StringComparison.Ordinal) &&
                    string.Equals(item.ParentInstanceId, parentInstanceId, StringComparison.Ordinal));
            if (added is null)
            {
                throw new InvalidOperationException(
                    "Raid advisor consumed general storage before the compatible dedicated nested container.");
            }
        }
        finally
        {
            _storedItems.RemoveAll(item =>
                string.Equals(item.InstanceId, parentInstanceId, StringComparison.Ordinal) ||
                string.Equals(item.ParentInstanceId, parentInstanceId, StringComparison.Ordinal));
            _secureContainer = previousSecure;

            foreach (var id in ids)
            {
                if (previousItems[id] is { } original)
                    _itemsById[id] = original;
                else
                    _itemsById.Remove(id);
            }
        }

        VerifyRaidRepackingHardeningSmoke();
        VerifyNestedWorkbenchViewportSmoke();
    }

    private void VerifyRaidRepackingHardeningSmoke()
    {
        const string bagId = "__junhyun_smoke_repack_bag";
        const string blockerId = "__junhyun_smoke_repack_blocker";
        const string incomingId = "__junhyun_smoke_repack_incoming";
        const string blockerInstanceId = "__junhyun_smoke_repack_blocker_instance";

        var ids = new[] { bagId, blockerId, incomingId };
        var previousItems = ids.ToDictionary(
            id => id,
            id => _itemsById.TryGetValue(id, out var item) ? item : null,
            StringComparer.Ordinal);
        var previousBackpack = _backpack;
        var previousStored = _storedItems.ToArray();
        var previousLocks = BuildLockState();

        var bag = SmokeItem(bagId) with
        {
            FarmingGuideData = new FarmingGuideItemLayout(
                "ItemPropertiesBackpack",
                [new FarmingGuideStorageGridDefinition(3, 3, FarmingGuideItemFilter.Empty)],
                [],
                [],
                [],
                [],
                false,
                false),
        };
        var blocker = SmokeItem(blockerId) with { Width = 1, Height = 1 };
        var incoming = SmokeItem(incomingId) with { Width = 2, Height = 3 };

        _itemsById[bagId] = bag;
        _itemsById[blockerId] = blocker;
        _itemsById[incomingId] = incoming;
        _backpack = FarmingGuideItemState.Create(bagId);
        _storedItems.Clear();
        _storedItems.Add(new FarmingGuideStoredItemState(
            blockerInstanceId,
            FarmingGuideItemState.Create(blockerId),
            FarmingGuideStorageKind.Backpack,
            0,
            1,
            1,
            false));
        ApplyLockState(FarmingGuideLockState.Empty);

        try
        {
            var scanned = new ScannerItemSnapshot(
                incomingId,
                incomingId,
                null,
                TraderSellPrice: 100_000,
                FleaAveragePrice: 100_000,
                TraderPricePerSlot: 0,
                FleaPricePerSlot: 0,
                Slots: 6,
                CurrentNeeded: 0);

            var recommendation = PlanScannedItemHardened(scanned, incoming);
            if (recommendation.Action != FarmingGuideInstructionAction.Store)
            {
                throw new InvalidOperationException(
                    $"Fragmented free capacity did not repack before discard/replacement: {recommendation.Action}.");
            }

            var moved = recommendation.ProposedSnapshot.StoredItems.Single(value =>
                string.Equals(value.InstanceId, blockerInstanceId, StringComparison.Ordinal));
            if (moved.X == 1 && moved.Y == 1)
                throw new InvalidOperationException("Repacking recommendation left the blocking 1x1 item in place.");

            var added = recommendation.ProposedSnapshot.StoredItems.FirstOrDefault(value =>
                string.Equals(value.Item.ItemId, incomingId, StringComparison.Ordinal));
            if (added is null)
                throw new InvalidOperationException("Repacking recommendation did not preserve the incoming item.");
            if (!recommendation.Instruction.Contains("이동", StringComparison.Ordinal))
                throw new InvalidOperationException("Repacking recommendation does not tell the user that an item must move.");
        }
        finally
        {
            _backpack = previousBackpack;
            _storedItems.Clear();
            _storedItems.AddRange(previousStored);
            ApplyLockState(previousLocks);
            foreach (var id in ids)
            {
                if (previousItems[id] is { } original)
                    _itemsById[id] = original;
                else
                    _itemsById.Remove(id);
            }
        }
    }

    private void VerifyNestedWorkbenchViewportSmoke()
    {
        const string caseId = "__junhyun_smoke_workbench_viewport_case";
        const string parentInstanceId = "__junhyun_smoke_workbench_viewport_parent";

        var previousItem = _itemsById.TryGetValue(caseId, out var existing) ? existing : null;
        var sourceCase = SmokeItem(caseId) with
        {
            FarmingGuideData = new FarmingGuideItemLayout(
                "ItemPropertiesContainer",
                [new FarmingGuideStorageGridDefinition(4, 4, FarmingGuideItemFilter.Empty)],
                [],
                [],
                [],
                [],
                false,
                false),
        };
        _itemsById[caseId] = sourceCase;

        try
        {
            var placement = new FarmingGuideStoredItemState(
                parentInstanceId,
                FarmingGuideItemState.Create(caseId),
                FarmingGuideStorageKind.SecureContainer,
                0,
                0,
                0,
                false);
            OpenStoredWorkbench(new PlacedItemSource(placement));
            WorkbenchHost.UpdateLayout();

            if (!IsWorkbenchOpen)
                throw new InvalidOperationException("Source-backed 4x4 container did not open its storage workbench.");

            var dock = WorkbenchHost.Child as DockPanel
                ?? throw new InvalidOperationException("Nested storage workbench lost its DockPanel host.");
            var scroll = dock.Children.OfType<ScrollViewer>().FirstOrDefault()
                ?? throw new InvalidOperationException("Nested storage workbench lost its scroll viewport.");
            var gridHost = WorkbenchPanel.Children.OfType<FrameworkElement>().FirstOrDefault()
                ?? throw new InvalidOperationException("Nested storage workbench did not render its grid host.");

            WorkbenchHost.UpdateLayout();
            gridHost.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            var physicallyFits = gridHost.DesiredSize.Width +
                                 WorkbenchHost.Padding.Left + WorkbenchHost.Padding.Right +
                                 WorkbenchHost.BorderThickness.Left + WorkbenchHost.BorderThickness.Right +
                                 SystemParameters.VerticalScrollBarWidth <
                                 Math.Max(240d, RootGrid.ColumnDefinitions[1].ActualWidth - 24d);
            if (physicallyFits &&
                scroll.ComputedHorizontalScrollBarVisibility == Visibility.Visible)
            {
                throw new InvalidOperationException("Nested storage grid fits the viewport but still requires horizontal scrolling.");
            }
            if (physicallyFits && scroll.ViewportWidth + 0.5d < gridHost.ActualWidth)
            {
                throw new InvalidOperationException(
                    $"Nested storage viewport clips grid cells: viewport={scroll.ViewportWidth:0.#}, grid={gridHost.ActualWidth:0.#}.");
            }
        }
        finally
        {
            CloseWorkbench();
            if (previousItem is null)
                _itemsById.Remove(caseId);
            else
                _itemsById[caseId] = previousItem;
        }
    }
}
