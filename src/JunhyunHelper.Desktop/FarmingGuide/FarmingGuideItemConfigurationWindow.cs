using System.Windows;
using System.Windows.Controls;
using JunhyunHelper.Core.FarmingGuide;
using JunhyunHelper.Core.Items;

namespace JunhyunHelper.Desktop.FarmingGuide;

public sealed class FarmingGuideItemConfigurationWindow : Window
{
    private const double PreviewCellSize = 20d;

    private readonly GameItem _item;
    private readonly IReadOnlyDictionary<string, GameItem> _catalog;
    private readonly Dictionary<string, FarmingGuideItemState?> _attachments;
    private readonly Dictionary<string, FarmingGuideItemState?> _armorPlates;
    private readonly StackPanel _rows = new();
    private readonly bool _readOnly;

    public FarmingGuideItemConfigurationWindow(
        GameItem item,
        FarmingGuideItemState state,
        IReadOnlyDictionary<string, GameItem> catalog,
        bool readOnly = false)
    {
        _item = item ?? throw new ArgumentNullException(nameof(item));
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        _readOnly = readOnly;
        _attachments = state.Attachments.ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.Ordinal);
        _armorPlates = state.ArmorPlates.ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.Ordinal);

        Title = $"{(_readOnly ? "장비 정보" : "장비 설정")} · {FarmingGuidePage.DisplayName(item)}";
        Width = 680;
        Height = 680;
        MinWidth = 580;
        MinHeight = 460;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ShowInTaskbar = false;

        var root = new DockPanel { Margin = new Thickness(16) };
        var title = new StackPanel { Margin = new Thickness(0, 0, 0, 14) };
        title.Children.Add(new TextBlock
        {
            Text = FarmingGuidePage.DisplayName(item),
            FontSize = 20,
            FontWeight = FontWeights.SemiBold,
        });
        title.Children.Add(new TextBlock
        {
            Text = _readOnly
                ? "실제 Tarkov 데이터의 수납 공간과 장착 구조를 확인합니다."
                : "수납 구조를 확인하고 장착 슬롯과 교체형 방탄판을 실제 호환 목록에서 설정합니다.",
            Foreground = (System.Windows.Media.Brush)FindResource("TextSecondaryBrush"),
            Margin = new Thickness(0, 3, 0, 0),
            TextWrapping = TextWrapping.Wrap,
        });
        DockPanel.SetDock(title, Dock.Top);
        root.Children.Add(title);

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 12, 0, 0),
        };
        if (_readOnly)
        {
            var close = new Button
            {
                Content = "닫기",
                MinWidth = 88,
                MinHeight = 34,
                IsDefault = true,
                IsCancel = true,
            };
            close.Click += (_, _) => DialogResult = false;
            buttons.Children.Add(close);
        }
        else
        {
            var cancel = new Button
            {
                Content = "취소",
                MinWidth = 88,
                MinHeight = 34,
                Margin = new Thickness(0, 0, 8, 0),
                IsCancel = true,
            };
            cancel.Click += (_, _) => DialogResult = false;
            var save = new Button
            {
                Content = "적용",
                MinWidth = 88,
                MinHeight = 34,
                IsDefault = true,
            };
            save.Click += (_, _) =>
            {
                Result = new FarmingGuideItemState(_item.Id, _attachments, _armorPlates);
                DialogResult = true;
            };
            buttons.Children.Add(cancel);
            buttons.Children.Add(save);
        }
        DockPanel.SetDock(buttons, Dock.Bottom);
        root.Children.Add(buttons);

        var scroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = _rows,
        };
        root.Children.Add(scroll);
        Content = root;

        RenderRows();
    }

    public FarmingGuideItemState? Result { get; private set; }

    private void RenderRows()
    {
        _rows.Children.Clear();
        var layout = _item.FarmingGuideData;
        if (layout is null)
        {
            AddEmptyMessage("현재 데이터에 표시할 수 있는 내부 구조 정보가 없습니다.");
            return;
        }

        var rendered = false;
        if (layout.StorageGrids.Count > 0)
        {
            _rows.Children.Add(CreateSectionHeader("수납 구조"));
            for (var index = 0; index < layout.StorageGrids.Count; index++)
                _rows.Children.Add(CreateStorageGridRow(index, layout.StorageGrids[index]));
            rendered = true;
        }

        if (layout.AttachmentSlots.Count > 0)
        {
            _rows.Children.Add(CreateSectionHeader("장착 슬롯"));
            foreach (var slot in layout.AttachmentSlots)
                _rows.Children.Add(CreateAttachmentRow(slot));
            rendered = true;
        }

        if (layout.ArmorSlots.Count > 0)
        {
            _rows.Children.Add(CreateSectionHeader("방탄판 구조"));
            foreach (var slot in layout.ArmorSlots)
            {
                _rows.Children.Add(slot.Locked
                    ? CreateReadOnlySlotRow(slot.Name ?? slot.NameId, "내장형 · 교체 불가")
                    : CreateArmorRow(slot));
            }
            rendered = true;
        }

        if (!rendered)
            AddEmptyMessage("현재 데이터에 수납 공간이나 별도 장착 슬롯이 없습니다.");
    }

    private FrameworkElement CreateSectionHeader(string text) => new TextBlock
    {
        Text = text,
        FontSize = 15,
        FontWeight = FontWeights.SemiBold,
        Margin = new Thickness(1, 8, 0, 8),
    };

    private void AddEmptyMessage(string text)
    {
        _rows.Children.Add(new TextBlock
        {
            Text = text,
            Foreground = (System.Windows.Media.Brush)FindResource("TextSecondaryBrush"),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(1, 4, 1, 4),
        });
    }

    private FrameworkElement CreateStorageGridRow(
        int index,
        FarmingGuideStorageGridDefinition definition)
    {
        var border = CreateRowBorder();
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var preview = new Canvas
        {
            Width = definition.Width * PreviewCellSize,
            Height = definition.Height * PreviewCellSize,
            Background = (System.Windows.Media.Brush)FindResource("BackgroundDarkBrush"),
            Margin = new Thickness(0, 0, 14, 0),
        };
        for (var y = 0; y < definition.Height; y++)
        {
            for (var x = 0; x < definition.Width; x++)
            {
                var cell = new Border
                {
                    Width = PreviewCellSize,
                    Height = PreviewCellSize,
                    BorderBrush = (System.Windows.Media.Brush)FindResource("BorderBrush"),
                    BorderThickness = new Thickness(0.5),
                    Background = System.Windows.Media.Brushes.Transparent,
                    IsHitTestVisible = false,
                };
                Canvas.SetLeft(cell, x * PreviewCellSize);
                Canvas.SetTop(cell, y * PreviewCellSize);
                preview.Children.Add(cell);
            }
        }
        grid.Children.Add(preview);

        var detail = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        detail.Children.Add(new TextBlock
        {
            Text = $"그리드 {index + 1} · {definition.Width}×{definition.Height}",
            FontWeight = FontWeights.SemiBold,
        });
        detail.Children.Add(new TextBlock
        {
            Text = $"{definition.Width * definition.Height}칸 · {FilterSummary(definition.Filters)}",
            Foreground = (System.Windows.Media.Brush)FindResource("TextSecondaryBrush"),
            Margin = new Thickness(0, 3, 0, 0),
        });
        Grid.SetColumn(detail, 1);
        grid.Children.Add(detail);

        border.Child = grid;
        return border;
    }

    private FrameworkElement CreateAttachmentRow(FarmingGuideAttachmentSlotDefinition slot)
    {
        var candidates = _catalog.Values
            .Where(item => FarmingGuideCompatibility.FilterAllows(item, slot.Filters))
            .Where(item => !FarmingGuideCompatibility.ItemsConflict(_item, item))
            .OrderBy(FarmingGuidePage.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .Select(item => new Candidate(item))
            .ToList();
        var current = _attachments.GetValueOrDefault(slot.Id);
        if (_readOnly)
        {
            return CreateReadOnlySlotRow(
                slot.Required ? $"{slot.Name ?? slot.NameId} · 필수" : slot.Name ?? slot.NameId,
                CurrentOrCandidateSummary(current, candidates.Count));
        }

        return CreateSelectorRow(
            slot.Name ?? slot.NameId,
            slot.Required,
            candidates,
            current,
            selected => _attachments[slot.Id] = selected,
            slot.Id);
    }

    private FrameworkElement CreateArmorRow(FarmingGuideArmorSlotDefinition slot)
    {
        var allowed = slot.AllowedPlateIds.ToHashSet(StringComparer.Ordinal);
        var candidates = _catalog.Values
            .Where(item => allowed.Contains(item.Id))
            .OrderBy(FarmingGuidePage.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .Select(item => new Candidate(item))
            .ToList();
        var current = _armorPlates.GetValueOrDefault(slot.Id);
        if (_readOnly)
            return CreateReadOnlySlotRow(slot.Name ?? slot.NameId, CurrentOrCandidateSummary(current, candidates.Count));

        return CreateSelectorRow(
            slot.Name ?? slot.NameId,
            required: false,
            candidates,
            current,
            selected => _armorPlates[slot.Id] = selected,
            slot.Id);
    }

    private FrameworkElement CreateReadOnlySlotRow(string label, string detail)
    {
        var border = CreateRowBorder();
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(190) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        grid.Children.Add(new TextBlock
        {
            Text = label,
            VerticalAlignment = VerticalAlignment.Center,
            FontWeight = FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap,
        });
        var detailText = new TextBlock
        {
            Text = detail,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = (System.Windows.Media.Brush)FindResource("TextSecondaryBrush"),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(12, 0, 0, 0),
        };
        Grid.SetColumn(detailText, 1);
        grid.Children.Add(detailText);
        border.Child = grid;
        return border;
    }

    private FrameworkElement CreateSelectorRow(
        string label,
        bool required,
        IReadOnlyList<Candidate> candidates,
        FarmingGuideItemState? current,
        Action<FarmingGuideItemState?> update,
        string slotId)
    {
        var border = CreateRowBorder();
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(170) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var labelText = new TextBlock
        {
            Text = required ? $"{label} · 필수" : label,
            VerticalAlignment = VerticalAlignment.Center,
            FontWeight = FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap,
        };
        grid.Children.Add(labelText);

        var choices = new List<Candidate?> { null };
        choices.AddRange(candidates);
        var combo = new ComboBox
        {
            ItemsSource = choices,
            DisplayMemberPath = nameof(Candidate.Name),
            MinHeight = 34,
            Margin = new Thickness(8, 0, 8, 0),
        };
        combo.ItemStringFormat = "{0}";
        var currentChoice = current is null
            ? null
            : candidates.FirstOrDefault(candidate => candidate.Item.Id == current.ItemId);
        combo.SelectedItem = currentChoice;
        combo.SelectionChanged += (_, _) =>
        {
            var selected = combo.SelectedItem as Candidate;
            var previous = GetSelectedState(slotId, current);
            FarmingGuideItemState? next = selected is null
                ? null
                : previous is not null && previous.ItemId == selected.Item.Id
                    ? previous
                    : FarmingGuideItemState.Create(selected.Item.Id);
            update(next);
        };
        Grid.SetColumn(combo, 1);
        grid.Children.Add(combo);

        var childButton = new Button
        {
            Content = "하위 슬롯",
            MinWidth = 84,
            Padding = new Thickness(8, 5, 8, 5),
        };
        childButton.Click += (_, _) =>
        {
            if (combo.SelectedItem is not Candidate selected)
                return;
            var selectedState = GetSelectedState(slotId, current);
            if (selectedState is null || selectedState.ItemId != selected.Item.Id)
                selectedState = FarmingGuideItemState.Create(selected.Item.Id);
            var layout = selected.Item.FarmingGuideData;
            if (layout is null ||
                (layout.StorageGrids.Count == 0 &&
                 layout.AttachmentSlots.Count == 0 &&
                 layout.ArmorSlots.Count == 0))
            {
                return;
            }

            var editable = layout.AttachmentSlots.Count > 0 || layout.ArmorSlots.Any(value => !value.Locked);
            var nested = new FarmingGuideItemConfigurationWindow(
                selected.Item,
                selectedState,
                _catalog,
                readOnly: !editable)
            {
                Owner = this,
            };
            if (nested.ShowDialog() == true && nested.Result is not null)
                update(nested.Result);
        };
        Grid.SetColumn(childButton, 2);
        grid.Children.Add(childButton);

        border.Child = grid;
        return border;
    }

    private Border CreateRowBorder() => new()
    {
        Padding = new Thickness(10),
        Margin = new Thickness(0, 0, 0, 8),
        BorderBrush = (System.Windows.Media.Brush)FindResource("BorderBrush"),
        BorderThickness = new Thickness(1),
        CornerRadius = new CornerRadius(6),
        Background = (System.Windows.Media.Brush)FindResource("BackgroundMediumBrush"),
    };

    private string CurrentOrCandidateSummary(FarmingGuideItemState? current, int candidateCount)
    {
        if (current is not null && _catalog.TryGetValue(current.ItemId, out var item))
            return $"현재: {FarmingGuidePage.DisplayName(item)}";
        return candidateCount > 0 ? $"장착 가능 {candidateCount}종" : "장착 가능한 항목 없음";
    }

    private static string FilterSummary(FarmingGuideItemFilter filter)
    {
        var restricted = filter.AllowedCategoryIds.Count > 0 ||
                         filter.AllowedItemIds.Count > 0 ||
                         filter.ExcludedCategoryIds.Count > 0 ||
                         filter.ExcludedItemIds.Count > 0;
        return restricted ? "수납 제한 있음" : "수납 제한 없음";
    }

    private FarmingGuideItemState? GetSelectedState(string slotId, FarmingGuideItemState? fallback)
    {
        if (_attachments.TryGetValue(slotId, out var attachment))
            return attachment;
        if (_armorPlates.TryGetValue(slotId, out var plate))
            return plate;
        return fallback;
    }

    private sealed record Candidate(GameItem Item)
    {
        public string Name => FarmingGuidePage.DisplayName(Item);
        public override string ToString() => Name;
    }
}
