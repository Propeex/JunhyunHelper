using System.IO;
using System.Windows;
using System.Windows.Controls;
using JunhyunHelper.Core.FarmingGuide;
using JunhyunHelper.Core.Items;

namespace JunhyunHelper.Desktop.FarmingGuide;

public partial class FarmingGuidePage
{
    private bool _productSmokeRunning;
    private bool _productSmokeCompleted;

    protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
    {
        base.OnRenderSizeChanged(sizeInfo);

        if (_productSmokeRunning ||
            _productSmokeCompleted ||
            ActualWidth <= 0 ||
            ActualHeight <= 0 ||
            !string.Equals(
                Environment.GetEnvironmentVariable("JUNHYUNHELPER_MAP_SMOKE"),
                "1",
                StringComparison.Ordinal))
        {
            return;
        }

        _productSmokeRunning = true;
        try
        {
            VerifyLiveInventoryWorkbenchSmoke();
            VerifyExactStorageVisualLayoutSmoke();
            var marker = Path.Combine(
                Path.GetTempPath(),
                "junhyun-farming-guide-v113-smoke-success.txt");
            File.WriteAllLines(marker,
            [
                "live-storage-grid=ok",
                "nested-parent-drop=ok",
                "attachment-slot-drag-drop=ok",
                "occupied-slot-overwrite-blocked=ok",
                "attachment-drag-out=ok",
                "exact-storage-layout=ok",
            ]);
            _productSmokeCompleted = true;
        }
        catch (Exception exception)
        {
            var diagnostic = Path.Combine(Path.GetTempPath(), "junhyun-map-smoke-error.txt");
            File.WriteAllText(
                diagnostic,
                "Farming Guide live workbench smoke failed." + Environment.NewLine + exception);
            throw;
        }
        finally
        {
            _productSmokeRunning = false;
        }
    }

    private void VerifyLiveInventoryWorkbenchSmoke()
    {
        const string bagId = "__junhyun_smoke_nested_bag";
        const string lootId = "__junhyun_smoke_loot";
        const string weaponId = "__junhyun_smoke_weapon";
        const string modId = "__junhyun_smoke_mod";
        const string parentInstanceId = "__junhyun_smoke_parent_instance";
        const string modSlotId = "__junhyun_smoke_mod_slot";

        var ids = new[] { bagId, lootId, weaponId, modId };
        var previous = ids.ToDictionary(
            id => id,
            id => _itemsById.TryGetValue(id, out var item) ? item : null,
            StringComparer.Ordinal);

        var bag = SmokeItem(bagId) with
        {
            FarmingGuideData = new FarmingGuideItemLayout(
                "ItemPropertiesBackpack",
                [new FarmingGuideStorageGridDefinition(2, 2, FarmingGuideItemFilter.Empty)],
                [],
                [],
                [],
                [],
                false,
                false),
        };
        var loot = SmokeItem(lootId);
        var mod = SmokeItem(modId);
        var attachmentFilter = new FarmingGuideItemFilter([], [modId], [], []);
        var weapon = SmokeItem(weaponId) with
        {
            FarmingGuideData = new FarmingGuideItemLayout(
                "ItemPropertiesWeapon",
                [],
                [
                    new FarmingGuideAttachmentSlotDefinition(
                        modSlotId,
                        "mod_scope",
                        "Smoke mod",
                        false,
                        attachmentFilter),
                ],
                [],
                [],
                [],
                false,
                false),
        };

        _itemsById[bagId] = bag;
        _itemsById[lootId] = loot;
        _itemsById[weaponId] = weapon;
        _itemsById[modId] = mod;

        try
        {
            OpenWorkbench(
                FarmingGuideItemState.Create(bagId),
                WorkbenchMode.Storage,
                FarmingGuideStorageKind.Backpack,
                parentInstanceId,
                static _ => { });
            WorkbenchHost.UpdateLayout();

            if (!IsWorkbenchOpen || StoragePanel.Visibility != Visibility.Collapsed)
                throw new InvalidOperationException("Nested storage workbench did not replace the main storage surface.");

            var grid = FindVisualChildren<Canvas>(WorkbenchPanel)
                .Select(canvas => canvas.Tag as GridDropTarget)
                .FirstOrDefault(target => target is not null)
                ?? throw new InvalidOperationException("Nested storage workbench did not render an interactive grid target.");
            if (grid.Width != 2 ||
                grid.Height != 2 ||
                !string.Equals(grid.ParentInstanceId, parentInstanceId, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Nested storage grid did not preserve its parent-instance address.");
            }

            var lootDrag = new DragSession
            {
                Item = loot,
                State = FarmingGuideItemState.Create(lootId),
                Origin = DragOriginKind.Search,
                MouseDown = default,
            };
            var gridProbe = ProbeGrid(grid, lootDrag, new Point(CellSize / 2d, CellSize / 2d));
            if (!gridProbe.Valid)
                throw new InvalidOperationException("Search item was not accepted by the rendered nested storage grid.");
            ApplyDrop(lootDrag, gridProbe);

            var nested = StoredItems.FirstOrDefault(item =>
                string.Equals(item.ParentInstanceId, parentInstanceId, StringComparison.Ordinal) &&
                string.Equals(item.Item.ItemId, lootId, StringComparison.Ordinal));
            if (nested is null)
                throw new InvalidOperationException("Nested grid drop did not create a parent-addressed stored placement.");
            RemoveStoredTree(nested.InstanceId);
            CloseWorkbench();

            FarmingGuideItemState? appliedWeapon = null;
            OpenWorkbench(
                FarmingGuideItemState.Create(weaponId),
                WorkbenchMode.Slots,
                FarmingGuideStorageKind.Pockets,
                parentInstanceId: null,
                updated => appliedWeapon = updated);
            WorkbenchHost.UpdateLayout();

            var slot = FindVisualChildren<Border>(WorkbenchPanel)
                .Select(border => border.Tag as WorkbenchSlotDropTarget)
                .FirstOrDefault(target => target is not null)
                ?? throw new InvalidOperationException("Weapon workbench did not render an attachment drop slot.");
            if (!string.Equals(slot.SlotId, modSlotId, StringComparison.Ordinal) ||
                !CanDropIntoWorkbenchSlot(slot, mod))
            {
                throw new InvalidOperationException("Rendered weapon attachment slot rejected its allowed mod.");
            }

            var modDrag = new DragSession
            {
                Item = mod,
                State = FarmingGuideItemState.Create(modId),
                Origin = DragOriginKind.Search,
                MouseDown = default,
            };
            ApplyDrop(modDrag, new DropProbe(slot, true));
            if (appliedWeapon?.Attachments.GetValueOrDefault(modSlotId)?.ItemId != modId)
                throw new InvalidOperationException("Attachment drag/drop did not update the workbench item state.");
            if (CanDropIntoWorkbenchSlot(slot, mod))
                throw new InvalidOperationException("Occupied attachment slot still accepts an implicit overwrite.");

            var childDrag = new DragSession
            {
                Item = mod,
                State = FarmingGuideItemState.Create(modId),
                Origin = DragOriginKind.WorkbenchSlot,
                WorkbenchSlotKind = WorkbenchSlotKind.Attachment,
                WorkbenchSlotId = modSlotId,
                MouseDown = default,
            };
            RemoveOrigin(childDrag, destructiveCarrierRemoval: false, destructiveStoredRemoval: false);
            if (GetWorkbenchSlotState(slot) is not null)
                throw new InvalidOperationException("Attachment drag-out did not empty the rendered slot.");
        }
        finally
        {
            StoredItems.RemoveAll(item =>
                string.Equals(item.ParentInstanceId, parentInstanceId, StringComparison.Ordinal) ||
                string.Equals(item.Item.ItemId, lootId, StringComparison.Ordinal));
            CloseWorkbench();

            foreach (var id in ids)
            {
                if (previous[id] is { } original)
                    _itemsById[id] = original;
                else
                    _itemsById.Remove(id);
            }
        }
    }

    private static GameItem SmokeItem(string id) =>
        new(
            id,
            id,
            id,
            id,
            id,
            null,
            null,
            [],
            [],
            [],
            1,
            1);
}
