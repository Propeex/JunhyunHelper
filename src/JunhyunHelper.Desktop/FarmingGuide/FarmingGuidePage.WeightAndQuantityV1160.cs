using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using JunhyunHelper.Core.FarmingGuide;
using JunhyunHelper.Core.Items;

namespace JunhyunHelper.Desktop.FarmingGuide;

public partial class FarmingGuidePage
{
    private FarmingGuideWeightSettings _weightSettingsV1160 = FarmingGuideWeightSettings.Default;
    private string? _weightSettingsProfileIdV1160;
    private Button? _weightButtonV1160;
    private Popup? _weightPopupV1160;
    private TextBox? _strengthInputV1160;
    private Popup? _quantityPopupV1160;
    private TextBox? _quantityEditInputV1160;
    private string? _quantityEditInstanceIdV1160;
    private bool _v1160UiInitialized;

    private void InitializeV1160UiHooks()
    {
        Loaded += FarmingGuideV1160_Loaded;
        LayoutUpdated += FarmingGuideV1160_LayoutUpdated;
        AddHandler(Mouse.PreviewMouseDownEvent, new MouseButtonEventHandler(FarmingGuideV1160_PreviewMouseDown), true);
    }

    private void FarmingGuideV1160_Loaded(object sender, RoutedEventArgs e)
    {
        EnsureV1160Ui();
        RefreshWeightPresentationV1160();
        DecorateQuantityCardsV1160();
    }

    private void FarmingGuideV1160_LayoutUpdated(object? sender, EventArgs e)
    {
        if (!IsLoaded)
            return;
        EnsureV1160Ui();
        RefreshWeightPresentationV1160();
        DecorateQuantityCardsV1160();
    }

    private void EnsureV1160Ui()
    {
        if (_v1160UiInitialized)
            return;
        _v1160UiInitialized = true;

        if (WeightSummaryText.Parent is Grid host)
        {
            var column = Grid.GetColumn(WeightSummaryText);
            var row = Grid.GetRow(WeightSummaryText);
            WeightSummaryText.Visibility = Visibility.Collapsed;
            _weightButtonV1160 = new Button
            {
                MinWidth = 112,
                Height = 28,
                HorizontalAlignment = HorizontalAlignment.Right,
                Padding = new Thickness(8, 2, 8, 2),
                ToolTip = "무게 계산에 사용하는 힘 레벨 설정",
            };
            Grid.SetColumn(_weightButtonV1160, column);
            Grid.SetRow(_weightButtonV1160, row);
            _weightButtonV1160.Click += (_, _) => OpenWeightPopupV1160();
            host.Children.Add(_weightButtonV1160);
        }
    }

    private void EnsureWeightSettingsLoadedV1160()
    {
        if (string.IsNullOrWhiteSpace(_profileId))
        {
            _weightSettingsV1160 = FarmingGuideWeightSettings.Default;
            _weightSettingsProfileIdV1160 = null;
            return;
        }
        if (string.Equals(_weightSettingsProfileIdV1160, _profileId, StringComparison.Ordinal))
            return;

        _weightSettingsV1160 = _presetStore?.LoadProfile(_profileId).WeightSettings?.Normalized()
            ?? FarmingGuideWeightSettings.Default;
        _weightSettingsProfileIdV1160 = _profileId;
    }

    private void RefreshWeightPresentationV1160()
    {
        EnsureWeightSettingsLoadedV1160();
        if (_weightButtonV1160 is null)
            return;
        var current = BuildSnapshot();
        var weight = CalculateSnapshotWeightKgV1160(current);
        var limit = FarmingGuideWeightPolicy.MaximumCarryWeightKg(_weightSettingsV1160);
        var content = $"{weight:0.0} / {limit:0.0} kg";
        if (!string.Equals(_weightButtonV1160.Content as string, content, StringComparison.Ordinal))
            _weightButtonV1160.Content = content;
    }

    private decimal CalculateSnapshotWeightKgV1160(FarmingGuideLoadoutSnapshot snapshot)
    {
        decimal total = 0m;
        foreach (var pair in snapshot.Equipment)
        {
            if (!FarmingGuideWeightPolicy.EquipmentCountsTowardWeight(pair.Key, _weightSettingsV1160))
                continue;
            if (ResolveItem(pair.Value) is { } item)
                total += FarmingGuideWeightPolicy.ItemWeightKg(item);
        }

        foreach (var state in new[] { snapshot.Rig, snapshot.Backpack, snapshot.SecureContainer })
        {
            if (ResolveItem(state) is { } item)
                total += FarmingGuideWeightPolicy.ItemWeightKg(item);
        }

        foreach (var stored in snapshot.StoredItems)
        {
            if (ResolveItem(stored.Item) is { } item)
                total += FarmingGuideWeightPolicy.ItemWeightKg(item, stored.NormalizedQuantity);
        }
        return total;
    }

    private RaidRecommendation ApplyRaidWeightConstraintV1160(
        FarmingGuideLoadoutSnapshot current,
        RaidRecommendation recommendation)
    {
        EnsureWeightSettingsLoadedV1160();
        var currentWeight = CalculateSnapshotWeightKgV1160(current);
        var proposedWeight = CalculateSnapshotWeightKgV1160(recommendation.ProposedSnapshot);
        var limit = FarmingGuideWeightPolicy.MaximumCarryWeightKg(_weightSettingsV1160);

        if (proposedWeight <= limit)
            return recommendation;

        // If the user starts from an already-over-limit manually reflected state, allow a
        // recommendation that reduces or preserves that weight. Never make the situation
        // heavier until the modeled state is back within the configured limit.
        if (currentWeight > limit && proposedWeight <= currentWeight)
            return recommendation;

        return new RaidRecommendation(
            "버리기",
            FarmingGuideInstructionAction.Discard,
            current);
    }

    private void OpenWeightPopupV1160()
    {
        EnsureWeightSettingsLoadedV1160();
        if (_weightButtonV1160 is null)
            return;

        _strengthInputV1160 = new TextBox
        {
            Width = 72,
            Text = _weightSettingsV1160.StrengthLevel.ToString(CultureInfo.InvariantCulture),
            HorizontalContentAlignment = HorizontalAlignment.Right,
            Padding = new Thickness(6, 3, 6, 3),
        };
        _strengthInputV1160.PreviewTextInput += NumericOnlyV1160;
        DataObject.AddPastingHandler(_strengthInputV1160, NumericPasteOnlyV1160);
        _strengthInputV1160.TextChanged += (_, _) =>
        {
            if (!int.TryParse(_strengthInputV1160.Text, NumberStyles.None, CultureInfo.InvariantCulture, out var level))
                return;
            _weightSettingsV1160 = new FarmingGuideWeightSettings(level).Normalized();
            if (_presetStore is not null && !string.IsNullOrWhiteSpace(_profileId))
                _presetStore.SaveWeightSettings(_profileId, _weightSettingsV1160);
            RefreshWeightPresentationV1160();
        };

        var body = new StackPanel();
        body.Children.Add(new TextBlock
        {
            Text = "힘 레벨",
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 6),
        });
        body.Children.Add(_strengthInputV1160);
        body.Children.Add(new TextBlock
        {
            Text = "0–51 · 입력 즉시 반영",
            FontSize = 11,
            Foreground = (Brush)FindResource("TextSecondaryBrush"),
            Margin = new Thickness(0, 5, 0, 0),
        });

        _weightPopupV1160 = new Popup
        {
            PlacementTarget = _weightButtonV1160,
            Placement = PlacementMode.Top,
            StaysOpen = false,
            AllowsTransparency = true,
            Child = CreatePopupCardV1160(body),
            IsOpen = true,
        };
        _strengthInputV1160.Focus();
        _strengthInputV1160.SelectAll();
    }

    private void FarmingGuideV1160_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount != 2)
            return;

        for (DependencyObject? current = e.OriginalSource as DependencyObject;
             current is not null;
             current = VisualTreeHelper.GetParent(current))
        {
            if (current is not Border { Tag: PlacedItemSource source } card)
                continue;
            var item = ResolveItem(source.Placement.Item);
            if (item is null || !FarmingGuideStackQuantityPolicy.RequiresQuantity(item))
                return;
            OpenQuantityPopupV1160(card, source.Placement);
            e.Handled = true;
            return;
        }
    }

    private void OpenQuantityPopupV1160(Border target, FarmingGuideStoredItemState stored)
    {
        _quantityEditInstanceIdV1160 = stored.InstanceId;
        _quantityEditInputV1160 = new TextBox
        {
            Width = 92,
            Text = stored.NormalizedQuantity.ToString(CultureInfo.InvariantCulture),
            HorizontalContentAlignment = HorizontalAlignment.Right,
            Padding = new Thickness(6, 3, 6, 3),
        };
        _quantityEditInputV1160.PreviewTextInput += NumericOnlyV1160;
        DataObject.AddPastingHandler(_quantityEditInputV1160, NumericPasteOnlyV1160);
        _quantityEditInputV1160.KeyDown += (_, e) =>
        {
            if (e.Key != Key.Enter)
                return;
            if (!int.TryParse(_quantityEditInputV1160.Text, NumberStyles.None, CultureInfo.InvariantCulture, out var quantity) || quantity <= 0)
            {
                _quantityEditInputV1160.SelectAll();
                e.Handled = true;
                return;
            }
            CommitQuantityEditV1160(quantity);
            e.Handled = true;
        };

        var body = new StackPanel();
        body.Children.Add(new TextBlock
        {
            Text = "개수",
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 6),
        });
        body.Children.Add(_quantityEditInputV1160);
        _quantityPopupV1160 = new Popup
        {
            PlacementTarget = target,
            Placement = PlacementMode.Bottom,
            StaysOpen = false,
            AllowsTransparency = true,
            Child = CreatePopupCardV1160(body),
            IsOpen = true,
        };
        _quantityEditInputV1160.Focus();
        _quantityEditInputV1160.SelectAll();
    }

    private void CommitQuantityEditV1160(int quantity)
    {
        if (string.IsNullOrWhiteSpace(_quantityEditInstanceIdV1160))
            return;
        var index = _storedItems.FindIndex(value =>
            string.Equals(value.InstanceId, _quantityEditInstanceIdV1160, StringComparison.Ordinal));
        if (index < 0)
            return;
        _storedItems[index] = _storedItems[index] with
        {
            Quantity = FarmingGuideStackQuantityPolicy.NormalizeQuantity(quantity),
        };
        if (_quantityPopupV1160 is not null)
            _quantityPopupV1160.IsOpen = false;
        _quantityEditInstanceIdV1160 = null;
        MarkChanged();
    }

    private Border CreatePopupCardV1160(UIElement body) => new()
    {
        Background = (Brush)FindResource("BackgroundMediumBrush"),
        BorderBrush = (Brush)FindResource("BorderBrush"),
        BorderThickness = new Thickness(1),
        CornerRadius = new CornerRadius(6),
        Padding = new Thickness(10),
        Margin = new Thickness(4),
        Child = body,
    };

    private void DecorateQuantityCardsV1160()
    {
        foreach (var card in EnumeratePlacedCardsV1160(this))
        {
            if (card.Tag is not PlacedItemSource source || source.Placement.NormalizedQuantity <= 1)
                continue;
            if (card.Child is Grid existingGrid && existingGrid.Tag is QuantityBadgeMarkerV1160)
            {
                if (existingGrid.Children.OfType<Border>().FirstOrDefault(value => value.Tag is QuantityBadgeMarkerV1160)
                    is { Child: TextBlock badgeText })
                {
                    var text = source.Placement.NormalizedQuantity.ToString("N0", CultureInfo.InvariantCulture);
                    if (!string.Equals(badgeText.Text, text, StringComparison.Ordinal))
                        badgeText.Text = text;
                }
                continue;
            }

            var original = card.Child as UIElement;
            if (original is null)
                continue;
            card.Child = null;
            var grid = new Grid { Tag = new QuantityBadgeMarkerV1160() };
            grid.Children.Add(original);
            var badge = new Border
            {
                Tag = new QuantityBadgeMarkerV1160(),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(2),
                Padding = new Thickness(4, 1, 4, 1),
                CornerRadius = new CornerRadius(3),
                Background = (Brush)FindResource("BackgroundDarkBrush"),
                BorderBrush = (Brush)FindResource("AccentBrush"),
                BorderThickness = new Thickness(1),
                IsHitTestVisible = false,
                Child = new TextBlock
                {
                    Text = source.Placement.NormalizedQuantity.ToString("N0", CultureInfo.InvariantCulture),
                    FontSize = 11,
                    FontWeight = FontWeights.SemiBold,
                },
            };
            grid.Children.Add(badge);
            card.Child = grid;
        }
    }

    private static IEnumerable<Border> EnumeratePlacedCardsV1160(DependencyObject root)
    {
        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is Border { Tag: PlacedItemSource } border)
                yield return border;
            foreach (var nested in EnumeratePlacedCardsV1160(child))
                yield return nested;
        }
    }

    private static void NumericOnlyV1160(object sender, TextCompositionEventArgs e) =>
        e.Handled = e.Text.Any(ch => !char.IsDigit(ch));

    private static void NumericPasteOnlyV1160(object sender, DataObjectPastingEventArgs e)
    {
        if (!e.DataObject.GetDataPresent(DataFormats.Text) ||
            e.DataObject.GetData(DataFormats.Text) is not string text ||
            text.Any(ch => !char.IsDigit(ch)))
        {
            e.CancelCommand();
        }
    }

    private sealed class QuantityBadgeMarkerV1160;
}