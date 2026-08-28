using JunhyunHelper.Infrastructure.Storage;

namespace JunhyunHelper.Tests.Infrastructure;

public sealed class ScannerItemUiStateStoreTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"JunhyunHelper.ScannerUi.{Guid.NewGuid():N}");

    [Fact]
    public void Favorites_are_newest_first_persisted_and_independent_from_recents()
    {
        var store = new ScannerItemUiStateStore(_root);

        store.ToggleFavorite(" item-a ");
        store.ToggleFavorite("item-b");
        store.RecordRecent("item-a");
        store.RecordRecent("item-b");
        store.ClearRecents();

        var current = store.Current;
        Assert.Equal(new[] { "item-b", "item-a" }, current.FavoriteItemIds);
        Assert.Empty(current.RecentItemIds);
        Assert.EndsWith("scanner-item-ui-state.json", store.FilePath, StringComparison.OrdinalIgnoreCase);

        var reloaded = new ScannerItemUiStateStore(_root).Current;
        Assert.Equal(current.FavoriteItemIds, reloaded.FavoriteItemIds);
        Assert.Empty(reloaded.RecentItemIds);
    }

    [Fact]
    public void Toggling_existing_favorite_removes_only_that_item()
    {
        var store = new ScannerItemUiStateStore(_root);
        store.ToggleFavorite("item-a");
        store.ToggleFavorite("item-b");

        store.ToggleFavorite("item-a");

        Assert.Equal(new[] { "item-b" }, store.Current.FavoriteItemIds);
    }

    [Fact]
    public void Recents_are_deduplicated_moved_to_front_and_capped_at_fifty()
    {
        var store = new ScannerItemUiStateStore(_root);
        for (var index = 0; index < 55; index++)
            store.RecordRecent($"item-{index}");

        var capped = store.Current;
        Assert.Equal(ScannerItemUiStateStore.MaximumRecentItems, capped.RecentItemIds.Count);
        Assert.Equal("item-54", capped.RecentItemIds[0]);
        Assert.DoesNotContain("item-0", capped.RecentItemIds);

        store.RecordRecent("item-10");
        var moved = store.Current;
        Assert.Equal("item-10", moved.RecentItemIds[0]);
        Assert.Equal(1, moved.RecentItemIds.Count(value => value == "item-10"));
        Assert.Equal(ScannerItemUiStateStore.MaximumRecentItems, moved.RecentItemIds.Count);

        var reloaded = new ScannerItemUiStateStore(_root).Current;
        Assert.Equal(moved.RecentItemIds, reloaded.RecentItemIds);
    }

    [Fact]
    public void Per_row_removal_does_not_touch_the_other_collection()
    {
        var store = new ScannerItemUiStateStore(_root);
        store.ToggleFavorite("item-a");
        store.RecordRecent("item-a");
        store.RecordRecent("item-b");

        store.RemoveRecent("item-a");
        Assert.Equal(new[] { "item-a" }, store.Current.FavoriteItemIds);
        Assert.Equal(new[] { "item-b" }, store.Current.RecentItemIds);

        store.RemoveFavorite("item-a");
        Assert.Empty(store.Current.FavoriteItemIds);
        Assert.Equal(new[] { "item-b" }, store.Current.RecentItemIds);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_root))
                Directory.Delete(_root, recursive: true);
        }
        catch
        {
        }
    }
}
