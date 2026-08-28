using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using JunhyunHelper.Infrastructure.Storage;

namespace JunhyunHelper.Desktop.Scanner;

public sealed record ScannerUserItemRow(string ItemId, string Name, ImageSource? Icon);

public partial class ScannerPage
{
    private static readonly TimeSpan ScannerPresentationContextInterval = TimeSpan.FromMilliseconds(750);

    private readonly ObservableCollection<ScannerUserItemRow> _favoriteItemRows = [];
    private readonly ObservableCollection<ScannerUserItemRow> _recentItemRows = [];
    private ScannerItemUiStateStore? _scannerItemUiState;
    private DispatcherTimer? _scannerPresentationContextTimer;
    private string? _selectedScannerItemId;
    private bool _scannerUserCollectionsBound;
    private bool _scannerPresentationContextRefreshActive;
    private bool _scannerFavoritesRecentsSmokeRan;

    private void InitializeScannerUserItemCollections()
    {
        if (_scannerUserCollectionsBound)
            return;

        FavoriteItems.ItemsSource = _favoriteItemRows;
        RecentItems.ItemsSource = _recentItemRows;
        _scannerUserCollectionsBound = true;

        IsVisibleChanged += ScannerPresentationContext_IsVisibleChanged;
        Unloaded += ScannerPresentationContext_Unloaded;
        UpdateScannerPresentationContextMonitor();
        UpdateScannerUserListEmptyStates();
    }

    private void AttachScannerItemUiState(MainWindow mainWindow)
    {
        InitializeScannerUserItemCollections();
        _scannerItemUiState ??= mainWindow.ScannerItemUiState;
        RefreshScannerUserItemLists();
    }

    private void OnScannerItemOpened(ScannerItemSearchDetails details)
    {
        ArgumentNullException.ThrowIfNull(details);
        _selectedScannerItemId = details.Snapshot.ItemId;

        if (_scannerItemUiState is null && Window.GetWindow(this) is MainWindow mainWindow)
            AttachScannerItemUiState(mainWindow);

        if (_scannerItemUiState is not null)
            _scannerItemUiState.RecordRecent(details.Snapshot.ItemId);

        UpdateDetailFavoriteAction();
        RefreshScannerUserItemLists();
        ScheduleScannerFavoritesRecentsPublishedSmoke(details);
    }

    private void RefreshScannerUserItemLists()
    {
        if (!_scannerUserCollectionsBound)
            InitializeScannerUserItemCollections();

        if (_scannerItemUiState is null || _coordinator is null)
        {
            UpdateScannerUserListEmptyStates();
            UpdateDetailFavoriteAction();
            return;
        }

        var state = _scannerItemUiState.Current;
        ReplaceResolvedRows(_favoriteItemRows, state.FavoriteItemIds);
        ReplaceResolvedRows(_recentItemRows, state.RecentItemIds);
        UpdateScannerUserListEmptyStates();
        UpdateDetailFavoriteAction();
    }

    private void ReplaceResolvedRows(
        ObservableCollection<ScannerUserItemRow> target,
        IReadOnlyList<string> itemIds)
    {
        target.Clear();
        if (_coordinator is null)
            return;

        foreach (var itemId in itemIds)
        {
            // Persistence owns only canonical identity/order. Presentation always resolves
            // from the active GameMode catalog. An ID unavailable in the current mode is
            // skipped visually without deleting the persisted user preference. This list
            // path deliberately avoids full relationship construction for up to 50 rows.
            var hit = _coordinator.GetSearchItemHit(itemId);
            if (hit is null)
                continue;

            target.Add(new ScannerUserItemRow(
                hit.ItemId,
                hit.OfficialName,
                hit.Icon));
        }
    }

    private void UpdateScannerUserListEmptyStates()
    {
        EmptyFavoriteItemsText.Visibility = _favoriteItemRows.Count == 0
            ? Visibility.Visible
            : Visibility.Collapsed;
        EmptyRecentItemsText.Visibility = _recentItemRows.Count == 0
            ? Visibility.Visible
            : Visibility.Collapsed;
        ClearRecentItemsButton.IsEnabled = _recentItemRows.Count > 0;
    }

    private void UpdateDetailFavoriteAction()
    {
        var itemId = _selectedScannerItemId;
        var canToggle = !string.IsNullOrWhiteSpace(itemId) && _scannerItemUiState is not null;
        FavoriteItemButton.IsEnabled = canToggle;

        var favorite = canToggle && _scannerItemUiState!.IsFavorite(itemId);
        FavoriteItemButton.Content = favorite ? "★" : "☆";
        FavoriteItemButton.ToolTip = favorite ? "즐겨찾기 해제" : "즐겨찾기 등록";
    }

    private void FavoriteItemButton_Click(object sender, RoutedEventArgs e)
    {
        if (_scannerItemUiState is null || string.IsNullOrWhiteSpace(_selectedScannerItemId))
            return;

        _scannerItemUiState.ToggleFavorite(_selectedScannerItemId);
        RefreshScannerUserItemLists();
        e.Handled = true;
    }

    private void FavoriteItemBodyButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string itemId } && !string.IsNullOrWhiteSpace(itemId))
            SelectSearchItemById(itemId);
    }

    private void RecentItemBodyButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string itemId } && !string.IsNullOrWhiteSpace(itemId))
            SelectSearchItemById(itemId);
    }

    private void FavoriteItemRemoveButton_Click(object sender, RoutedEventArgs e)
    {
        if (_scannerItemUiState is not null && sender is Button { Tag: string itemId })
        {
            _scannerItemUiState.RemoveFavorite(itemId);
            RefreshScannerUserItemLists();
        }
        e.Handled = true;
    }

    private void RecentItemRemoveButton_Click(object sender, RoutedEventArgs e)
    {
        if (_scannerItemUiState is not null && sender is Button { Tag: string itemId })
        {
            _scannerItemUiState.RemoveRecent(itemId);
            RefreshScannerUserItemLists();
        }
        e.Handled = true;
    }

    private void ClearRecentItemsButton_Click(object sender, RoutedEventArgs e)
    {
        if (_scannerItemUiState is null)
            return;

        _scannerItemUiState.ClearRecents();
        RefreshScannerUserItemLists();
        e.Handled = true;
    }

    private void ScannerPresentationContext_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        UpdateScannerPresentationContextMonitor();
        if (e.NewValue is true)
            _ = SynchronizeScannerPresentationContextAsync();
    }

    private void ScannerPresentationContext_Unloaded(object sender, RoutedEventArgs e) =>
        _scannerPresentationContextTimer?.Stop();

    private void UpdateScannerPresentationContextMonitor()
    {
        _scannerPresentationContextTimer ??= CreateScannerPresentationContextTimer();
        if (IsLoaded && IsVisible)
            _scannerPresentationContextTimer.Start();
        else
            _scannerPresentationContextTimer.Stop();
    }

    private DispatcherTimer CreateScannerPresentationContextTimer()
    {
        var timer = new DispatcherTimer(DispatcherPriority.Background, Dispatcher)
        {
            Interval = ScannerPresentationContextInterval,
        };
        timer.Tick += ScannerPresentationContextTimer_Tick;
        return timer;
    }

    private async void ScannerPresentationContextTimer_Tick(object? sender, EventArgs e) =>
        await SynchronizeScannerPresentationContextAsync();

    private async Task SynchronizeScannerPresentationContextAsync()
    {
        if (_scannerPresentationContextRefreshActive ||
            !IsVisible ||
            _coordinator is null ||
            Window.GetWindow(this) is not MainWindow mainWindow)
        {
            return;
        }

        var context = mainWindow.GetScannerDataContext();
        if (context is null || _coordinator.CatalogMode == context.GameMode)
            return;

        _scannerPresentationContextRefreshActive = true;
        try
        {
            await _coordinator.RefreshContextAsync();
            RefreshScannerUserItemLists();
            RefreshOpenScannerItemForCurrentContext();
        }
        catch (Exception exception)
        {
            App.WriteDiagnostic("Scanner saved-item presentation context refresh failed", exception);
        }
        finally
        {
            _scannerPresentationContextRefreshActive = false;
        }
    }

    private void RefreshOpenScannerItemForCurrentContext()
    {
        if (_coordinator is null || string.IsNullOrWhiteSpace(_selectedScannerItemId))
            return;

        var details = _coordinator.GetSearchItemDetails(_selectedScannerItemId);
        if (details is null)
        {
            _selectedScannerItemId = null;
            _selectedWikiUrl = null;
            WikiButton.IsEnabled = false;
            FavoriteItemButton.IsEnabled = false;
            SelectedItemPanel.Visibility = Visibility.Collapsed;
            EmptyItemText.Text = "현재 게임 모드에서 이 아이템 정보를 불러올 수 없습니다.";
            EmptyItemText.Visibility = Visibility.Visible;
            return;
        }

        _suppressSearchRefresh = true;
        try
        {
            ItemSearchBox.Text = details.Snapshot.OfficialName;
            ItemSearchBox.CaretIndex = ItemSearchBox.Text.Length;
        }
        finally
        {
            _suppressSearchRefresh = false;
        }

        RenderSearchDetails(details);
        RenderProductItemExtensions(details);
        UpdateDetailFavoriteAction();
    }

    private void ScheduleScannerFavoritesRecentsPublishedSmoke(ScannerItemSearchDetails details)
    {
        if (_scannerFavoritesRecentsSmokeRan ||
            !string.Equals(Environment.GetEnvironmentVariable("JUNHYUNHELPER_MAP_SMOKE"), "1", StringComparison.Ordinal))
        {
            return;
        }

        _scannerFavoritesRecentsSmokeRan = true;
        Dispatcher.BeginInvoke(() =>
        {
            try
            {
                VerifyScannerFavoritesRecentsPublishedContract(details);
            }
            catch (Exception exception)
            {
                try
                {
                    var diagnostic = Path.Combine(Path.GetTempPath(), "junhyun-map-smoke-error.txt");
                    File.WriteAllText(diagnostic, "Scanner v1.9.0 favorites/recents published smoke failed.\n" + exception);
                }
                catch
                {
                }

                Environment.Exit(89);
            }
        });
    }

    private void VerifyScannerFavoritesRecentsPublishedContract(ScannerItemSearchDetails details)
    {
        if (_scannerItemUiState is null)
            throw new InvalidOperationException("Scanner item UI state was not attached through the real MainWindow lifecycle.");
        if (!_scannerUserCollectionsBound ||
            !ReferenceEquals(FavoriteItems.ItemsSource, _favoriteItemRows) ||
            !ReferenceEquals(RecentItems.ItemsSource, _recentItemRows))
        {
            throw new InvalidOperationException("Scanner favorites/recents ItemsControls were not bound by the real ScannerPage lifecycle.");
        }
        if (FavoriteItemsScrollViewer.HorizontalScrollBarVisibility != ScrollBarVisibility.Disabled ||
            RecentItemsScrollViewer.HorizontalScrollBarVisibility != ScrollBarVisibility.Disabled ||
            FavoriteItemsScrollViewer.VerticalScrollBarVisibility != ScrollBarVisibility.Auto ||
            RecentItemsScrollViewer.VerticalScrollBarVisibility != ScrollBarVisibility.Auto)
        {
            throw new InvalidOperationException("Scanner favorites/recents independent scroll contract drifted.");
        }

        var favoriteSection = FindScannerAncestor<Border>(FavoriteItemsScrollViewer)
            ?? throw new InvalidOperationException("Scanner Favorites section container was not present in the runtime tree.");
        var recentSection = FindScannerAncestor<Border>(RecentItemsScrollViewer)
            ?? throw new InvalidOperationException("Scanner Recents section container was not present in the runtime tree.");
        if (LogicalTreeHelper.GetParent(favoriteSection) is not Grid rightPane ||
            !ReferenceEquals(LogicalTreeHelper.GetParent(recentSection), rightPane) ||
            rightPane.RowDefinitions.Count != 3 ||
            rightPane.RowDefinitions[0].Height.GridUnitType != GridUnitType.Star ||
            Math.Abs(rightPane.RowDefinitions[0].Height.Value - 2d) > 0.001 ||
            rightPane.RowDefinitions[1].Height.GridUnitType != GridUnitType.Pixel ||
            Math.Abs(rightPane.RowDefinitions[1].Height.Value - 10d) > 0.001 ||
            rightPane.RowDefinitions[2].Height.GridUnitType != GridUnitType.Star ||
            Math.Abs(rightPane.RowDefinitions[2].Height.Value - 1d) > 0.001 ||
            Grid.GetRow(favoriteSection) != 0 ||
            Grid.GetRow(recentSection) != 2)
        {
            throw new InvalidOperationException("Scanner right pane is not the approved Favorites 2/3 + Recents 1/3 layout.");
        }

        if (ActivityItems.Parent is not FrameworkElement diagnosticActivityHost ||
            diagnosticActivityHost.Visibility != Visibility.Collapsed ||
            diagnosticActivityHost.IsHitTestVisible)
        {
            throw new InvalidOperationException("Scanner diagnostic activity feed is still exposed as user-facing UI.");
        }

        var itemId = details.Snapshot.ItemId;
        var itemName = details.Snapshot.OfficialName;
        var recent = _scannerItemUiState.Current.RecentItemIds;
        if (recent.Count == 0 || !string.Equals(recent[0], itemId, StringComparison.Ordinal))
            throw new InvalidOperationException("Opening a Scanner item did not record it at the top of recent history.");

        ItemSearchBox.Text = "temporary-search-text";
        ItemSearchBox.Clear();
        if (SearchResultsPopup.IsOpen ||
            SelectedItemPanel.Visibility != Visibility.Visible ||
            !string.Equals(_selectedScannerItemId, itemId, StringComparison.Ordinal) ||
            !string.Equals(SelectedItemNameText.Text, itemName, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Clearing Scanner search state also cleared or replaced the open item detail.");
        }

        var wasFavorite = _scannerItemUiState.IsFavorite(itemId);
        FavoriteItemButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        if (_scannerItemUiState.IsFavorite(itemId) == wasFavorite)
            throw new InvalidOperationException("Scanner detail favorite action did not toggle canonical item identity.");

        var root = Path.GetDirectoryName(_scannerItemUiState.FilePath)
            ?? throw new InvalidOperationException("Scanner item UI state persistence root was unavailable.");
        var reloaded = new ScannerItemUiStateStore(root).Current;
        if (!reloaded.RecentItemIds.Contains(itemId, StringComparer.Ordinal))
            throw new InvalidOperationException("Scanner recent item identity did not persist across store reload.");
        if (reloaded.FavoriteItemIds.Contains(itemId, StringComparer.Ordinal) == wasFavorite)
            throw new InvalidOperationException("Scanner favorite toggle did not persist across store reload.");

        // Restore the pre-smoke favorite state and remove only the smoke-added recent.
        if (_scannerItemUiState.IsFavorite(itemId) != wasFavorite)
            _scannerItemUiState.ToggleFavorite(itemId);
        _scannerItemUiState.RemoveRecent(itemId);
        RefreshScannerUserItemLists();

        var marker = Path.Combine(Path.GetTempPath(), "junhyun-scanner-favorites-recents-smoke-success.txt");
        File.WriteAllText(
            marker,
            "search-clear-detail=ok\n" +
            "favorite-toggle-persistence=ok\n" +
            "recent-open-persistence=ok\n" +
            "right-pane-two-to-one=ok\n" +
            "independent-scroll=ok\n" +
            "user-log-pane-hidden=ok\n" +
            "canonical-item-id=ok\n");
    }

    private static T? FindScannerAncestor<T>(DependencyObject start)
        where T : DependencyObject
    {
        for (DependencyObject? current = LogicalTreeHelper.GetParent(start);
             current is not null;
             current = LogicalTreeHelper.GetParent(current))
        {
            if (current is T typed)
                return typed;
        }
        return null;
    }
}
