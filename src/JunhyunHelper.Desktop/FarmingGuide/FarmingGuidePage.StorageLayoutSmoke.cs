using System.Windows.Controls;
using JunhyunHelper.Core.FarmingGuide;

namespace JunhyunHelper.Desktop.FarmingGuide;

public partial class FarmingGuidePage
{
    private void VerifyExactStorageVisualLayoutSmoke()
    {
        const string itemId = "__junhyun_smoke_exact_storage_layout";
        const string instanceId = "__junhyun_smoke_exact_storage_instance";

        _itemsById.TryGetValue(itemId, out var previousItem);
        var grids = Enumerable.Range(0, 15)
            .Select(static _ => new FarmingGuideStorageGridDefinition(
                1,
                1,
                FarmingGuideItemFilter.Empty))
            .ToArray();
        var state = FarmingGuideItemState.Create(itemId);
        var item = SmokeItem(itemId) with
        {
            FarmingGuideData = new FarmingGuideItemLayout(
                "ItemPropertiesChestRig",
                grids,
                [],
                [],
                [],
                [],
                false,
                false)
            {
                StorageLayoutName = "A18",
            },
        };

        _itemsById[itemId] = item;
        StoredItems.Add(new FarmingGuideStoredItemState(
            instanceId,
            state,
            FarmingGuideStorageKind.Backpack,
            GridIndex: 0,
            X: 0,
            Y: 0,
            Rotated: false));

        try
        {
            OpenWorkbench(
                state,
                WorkbenchMode.Storage,
                FarmingGuideStorageKind.Backpack,
                instanceId,
                static _ => { });
            WorkbenchHost.UpdateLayout();

            var exactHost = WorkbenchPanel.Children
                .OfType<Canvas>()
                .FirstOrDefault()
                ?? throw new InvalidOperationException(
                    "Exact storage layout did not render as a positioned Canvas host.");

            if (!FarmingGuideStorageVisualLayoutResolver.TryResolve(
                    itemId,
                    "A18",
                    grids,
                    CellSize,
                    out var expected))
            {
                throw new InvalidOperationException(
                    "Exact storage layout resolver rejected the smoke fixture.");
            }

            var renderedGrids = exactHost.Children.OfType<Canvas>().ToArray();
            if (renderedGrids.Length != expected.Grids.Count)
            {
                throw new InvalidOperationException(
                    $"Exact storage layout rendered {renderedGrids.Length} grids instead of {expected.Grids.Count}.");
            }

            if (Math.Abs(exactHost.Width - expected.Width) > 0.01 ||
                Math.Abs(exactHost.Height - expected.Height) > 0.01)
            {
                throw new InvalidOperationException(
                    "Exact storage layout host bounds do not match the resolved Tarkov layout.");
            }

            foreach (var placement in expected.Grids)
            {
                var rendered = renderedGrids[placement.GridIndex];
                if (Math.Abs(Canvas.GetLeft(rendered) - placement.Left) > 0.01 ||
                    Math.Abs(Canvas.GetTop(rendered) - placement.Top) > 0.01)
                {
                    throw new InvalidOperationException(
                        $"Exact storage grid {placement.GridIndex} was rendered at the wrong position.");
                }

                if (rendered.Tag is not GridDropTarget target ||
                    target.GridIndex != placement.GridIndex ||
                    !string.Equals(target.ParentInstanceId, instanceId, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        $"Exact storage grid {placement.GridIndex} lost its interactive drop target identity.");
                }
            }
        }
        finally
        {
            StoredItems.RemoveAll(item =>
                string.Equals(item.InstanceId, instanceId, StringComparison.Ordinal) ||
                string.Equals(item.ParentInstanceId, instanceId, StringComparison.Ordinal));
            CloseWorkbench();

            if (previousItem is not null)
                _itemsById[itemId] = previousItem;
            else
                _itemsById.Remove(itemId);
        }
    }
}
