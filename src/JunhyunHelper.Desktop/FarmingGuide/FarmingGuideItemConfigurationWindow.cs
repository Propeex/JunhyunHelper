using System.Windows;
using System.Windows.Controls;
using JunhyunHelper.Core.FarmingGuide;
using JunhyunHelper.Core.Items;

namespace JunhyunHelper.Desktop.FarmingGuide;

public sealed class FarmingGuideItemConfigurationWindow : Window
{
    private readonly GameItem _item;
    private readonly IReadOnlyDictionary<string, GameItem> _catalog;
    private readonly Dictionary<string, FarmingGuideItemState?> _attachments;
    private readonly Dictionary<string, FarmingGuideItemState?> _armorPlates;
    private readonly StackPanel _rows = new();

    public FarmingGuideItemConfigurationWindow(
        GameItem item,
        FarmingGuideItemState state,
        IReadOnlyDictionary<string, GameItem> catalog)
    {
        _item = item ?? throw new ArgumentNullException(nameof(item));
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        _attachments = state.Attachments.ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.Ordinal);
        _armorPlates = state.ArmorPlates.ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.Ordinal);

        Title = $"장비 설정 · {FarmingGuidePage.DisplayName(item)}";
        Width = 650;
        Height = 650;
        MinWidth = 560;
        MinHeight = 440;
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
            Text = "장착 슬롯과 교체형 방탄판을 실제 호환 목록에서 선택합니다.",
            Foreground = (System.Windows.Media.Brush)FindResource("TextSecondaryBrush"),
            Margin = new Thickness(0, 3, 0, 0),
        });
        DockPanel.SetDock(title, Dock.Top);
        root.Children.Add(title);

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 12, 0, 0),
        };
        var cancel = new Button { Content = "취소", MinWidth = 88, Margin = new Thickness(0, 0, 8, 0) };
        cancel.Click += (_, _) => DialogResult = false;
        var save = new Button { Content = "적용", MinWidth = 88, IsDefault = true };
        save.Click += (_, _) =>
        {
            Result = new FarmingGuideItemState(_item.Id, _attachments, _armorPlates);
            DialogResult = true;
        };
        buttons.Children.Add(cancel);
        buttons.Children.Add(save);
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
            return;

        foreach (var slot in layout.AttachmentSlots)
            _rows.Children.Add(CreateAttachmentRow(slot));

        foreach (var slot in layout.ArmorSlots.Where(slot => !slot.Locked))
            _rows.Children.Add(CreateArmorRow(slot));

        if (_rows.Children.Count == 0)
        {
            _rows.Children.Add(new TextBlock
            {
                Text = "사용자가 변경할 수 있는 내부 슬롯이 없습니다.",
                Foreground = (System.Windows.Media.Brush)FindResource("TextSecondaryBrush"),
            });
        }
    }

    private FrameworkElement CreateAttachmentRow(FarmingGuideAttachmentSlotDefinition slot)
    {
        var candidates = _catalog.Values
            .Where(item => FarmingGuideCompatibility.FilterAllows(item, slot.Filters))
            .Where(item => !FarmingGuideCompatibility.ItemsConflict(_item, item))
            .OrderBy(FarmingGuidePage.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .Select(item => new Candidate(item))
            .ToList();
        return CreateSelectorRow(
            slot.Name ?? slot.NameId,
            slot.Required,
            candidates,
            _attachments.GetValueOrDefault(slot.Id),
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
        return CreateSelectorRow(
            slot.Name ?? slot.NameId,
            required: false,
            candidates,
            _armorPlates.GetValueOrDefault(slot.Id),
            selected => _armorPlates[slot.Id] = selected,
            slot.Id);
    }

    private FrameworkElement CreateSelectorRow(
        string label,
        bool required,
        IReadOnlyList<Candidate> candidates,
        FarmingGuideItemState? current,
        Action<FarmingGuideItemState?> update,
        string slotId)
    {
        var border = new Border
        {
            Padding = new Thickness(10),
            Margin = new Thickness(0, 0, 0, 8),
            BorderBrush = (System.Windows.Media.Brush)FindResource("BorderBrush"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Background = (System.Windows.Media.Brush)FindResource("BackgroundMediumBrush"),
        };
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(170) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var labelText = new TextBlock
        {
            Text = required ? $"{label} · 필수" : label,
            VerticalAlignment = VerticalAlignment.Center,
            FontWeight = FontWeights.SemiBold,
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
            if (layout is null || (layout.AttachmentSlots.Count == 0 && layout.ArmorSlots.All(value => value.Locked)))
                return;
            var nested = new FarmingGuideItemConfigurationWindow(selected.Item, selectedState, _catalog)
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
