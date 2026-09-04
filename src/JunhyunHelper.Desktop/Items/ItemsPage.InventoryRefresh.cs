using JunhyunHelper.Application.Items;
using JunhyunHelper.Core.Content;

namespace JunhyunHelper.Desktop.Items;

public partial class ItemsPage
{
    /// <summary>
    /// Inventory-only refresh. Keeps already decoded images and does not restart a full
    /// image-cache walk for every +/- click or manual quantity save.
    /// </summary>
    public void SetInventoryData(GameContentCatalog content, ItemsWorkspace workspace)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(workspace);
        EnsureContentIndexes(content);
        _content = content;
        _workspace = workspace;

        var selectedItemId = _selectedRow?.ItemId;
        var previousRows = _allRows.ToDictionary(row => row.ItemId, StringComparer.Ordinal);
        var nextRows = BuildRows(content, workspace);
        var newlyVisible = new List<ItemRow>();

        foreach (var row in nextRows)
        {
            if (previousRows.TryGetValue(row.ItemId, out var previous) && previous.Icon is not null)
                row.Icon = previous.Icon;
            else if (row.Icon is null)
                newlyVisible.Add(row);
        }

        _allRows = nextRows;
        _rowsById = _allRows.ToDictionary(row => row.ItemId, StringComparer.Ordinal);
        ApplyFilter();

        if (!string.IsNullOrWhiteSpace(selectedItemId))
            SelectVisibleItem(selectedItemId, scrollIntoView: false);

        // Existing icon loads are deliberately not cancelled. Only items that appeared
        // for the first time because their owned quantity changed need a new request.
        if (newlyVisible.Count > 0 && _iconLoadCts is { IsCancellationRequested: false })
            _ = LoadIconsAsync(newlyVisible, _iconLoadCts.Token);
    }
}
