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
            VerifyCompleteEquipmentAndNestedStorageSmoke();
            VerifyExactStorageVisualLayoutSmoke();
            var marker = Path.Combine(
                Path.GetTempPath(),
                "junhyun-farming-guide-v1152-smoke-success.txt");
            File.WriteAllLines(marker,
            [
                "nested-storage-grid=ok",
                "nested-storage-compact-host=ok",
                "nested-parent-drop=ok",
                "equipment-internal-editor-disabled=ok",
                "root-carrier-duplicate-editor-disabled=ok",
                "exact-storage-layout=ok",
            ]);
            _productSmokeCompleted = true;
        }
        catch (Exception exception)
        {
            var diagnostic = Path.Combine(Path.GetTempPath(), "junhyun-map-smoke-error.txt");
            File.WriteAllText(
                diagnostic,
                "Farming Guide complete-equipment/nested-storage smoke failed." + Environment.NewLine + exception);
            throw;
        }
        finally
        {
            _productSmokeRunning = false;
        }
    }

    private void VerifyCompleteEquipmentAndNestedStorageSmoke()
    {
        const string bagId = "__junhyun_smoke_nested_bag";
        const string lootId = "__junhyun_smoke_loot";
        const string weaponId = "__junhyun_smoke_weapon";
        const string parentInstanceId = "__junhyun_smoke_parent_instance";

        var ids = new[] { bagId, lootId, weaponId };
        var previous = ids.ToDictionary(
            id => id,
            id => _itemsById.TryGetValue(id, out var item) ? item : null,
            StringComparer.Ordinal);
        var previousWeapon = Equipment.GetValueOrDefault(FarmingGuideEquipmentSlot.PrimaryWeapon1);

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
        var weapon = SmokeItem(weaponId) with
        {
            FarmingGuideData = new FarmingGuideItemLayout(
                "ItemPropertiesWeapon",
                [],
                [
                    new FarmingGuideAttachmentSlotDefinition(
                        "scope",
                        "mod_scope",
                        "Smoke mod",
                        false,
                        FarmingGuideItemFilter.Empty),
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

        try
        {
            var placement = new FarmingGuideStoredItemState(
                parentInstanceId,
                FarmingGuideItemState.Create(bagId),
                FarmingGuideStorageKind.Backpack,
                0,
                0,
                0,
                false);
            OpenStoredWorkbench(new PlacedItemSource(placement));
            WorkbenchHost.UpdateLayout();

            if (!IsWorkbenchOpen)
                throw new InvalidOperationException("Stored backpack did not open its nested storage detail.");
            if (StoragePanel.Visibility != Visibility.Visible)
                throw new InvalidOperationException("Compact nested storage detail still hides the full main storage surface.");

            var grid = FindVisualChildren<Canvas>(WorkbenchPanel)
                .Select(canvas => canvas.Tag as GridDropTarget)
                .FirstOrDefault(target => target is not null)
                ?? throw new InvalidOperationException("Nested storage detail did not render an interactive grid target.");
            if (grid.Width != 2 ||
                grid.Height != 2 ||
                !string.Equals(grid.ParentInstanceId, parentInstanceId, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Nested storage grid did not preserve its real dimensions/parent address.");
            }

            if (WorkbenchHost.Width > 240d || WorkbenchHost.Height > 230d)
            {
                throw new InvalidOperationException(
                    $"Nested 2x2 detail is still oversized: {WorkbenchHost.Width:0.#}x{WorkbenchHost.Height:0.#}.");
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

            Equipment[FarmingGuideEquipmentSlot.PrimaryWeapon1] = FarmingGuideItemState.Create(weaponId);
            OpenEquipmentWorkbench(new EquipmentDropTarget(
                FarmingGuideEquipmentSlot.PrimaryWeapon1,
                false,
                new Border()));
            if (IsWorkbenchOpen)
                throw new InvalidOperationException("Complete weapon still opens an equipment-internal assembly editor.");

            OpenCarrierWorkbench(new CarrierDropTarget(FarmingGuideStorageKind.Backpack, new Border()));
            if (IsWorkbenchOpen)
                throw new InvalidOperationException("Root carrier opened a duplicate full-detail editor.");
        }
        finally
        {
            StoredItems.RemoveAll(item =>
                string.Equals(item.ParentInstanceId, parentInstanceId, StringComparison.Ordinal) ||
                string.Equals(item.Item.ItemId, lootId, StringComparison.Ordinal));
            CloseWorkbench();

            if (previousWeapon is null)
                Equipment.Remove(FarmingGuideEquipmentSlot.PrimaryWeapon1);
            else
                Equipment[FarmingGuideEquipmentSlot.PrimaryWeapon1] = previousWeapon;

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
