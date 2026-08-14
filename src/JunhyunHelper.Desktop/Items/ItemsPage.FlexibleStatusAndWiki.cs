using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace JunhyunHelper.Desktop.Items;

public partial class ItemsPage
{
    private static readonly bool JunhyunItemEnhancementHandlerRegistered = RegisterJunhyunItemEnhancementHandler();

    private bool _junhyunItemEnhancementsInitialized;
    private bool _junhyunApplyingFlexibleStatusFilter;
    private FilterChoice[]? _junhyunNormalFilterChoices;
    private readonly FilterChoice[] _junhyunFlexibleFilterChoices =
    [
        new FilterChoice(ItemFilter.Needed, "필요"),
        new FilterChoice(ItemFilter.All, "전체"),
        new FilterChoice(ItemFilter.Satisfied, "충분"),
    ];
    private DependencyPropertyDescriptor? _junhyunFlexibleItemsSourceDescriptor;
    private Button? _junhyunItemWikiButton;

    private static bool RegisterJunhyunItemEnhancementHandler()
    {
        EventManager.RegisterClassHandler(
            typeof(ItemsPage),
            FrameworkElement.LoadedEvent,
            new RoutedEventHandler(OnJunhyunItemsPageLoaded));
        return true;
    }

    private static void OnJunhyunItemsPageLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is ItemsPage page)
            page.InitializeJunhyunItemEnhancements();
    }

    private void InitializeJunhyunItemEnhancements()
    {
        if (_junhyunItemEnhancementsInitialized)
            return;
        _junhyunItemEnhancementsInitialized = true;

        _junhyunNormalFilterChoices = FilterComboBox.Items.Cast<FilterChoice>().ToArray();
        ViewModeButton.Click += JunhyunViewModeButton_Click;
        FilterComboBox.SelectionChanged += JunhyunFilterComboBox_SelectionChanged;
        FilterComboBox.IsEnabledChanged += JunhyunFilterComboBox_IsEnabledChanged;
        ItemList.SelectionChanged += JunhyunItemSelectionChanged;
        FlexibleGroupScroll.PreviewMouseLeftButtonUp += JunhyunFlexibleGroup_MouseUp;
        DetailScroll.IsVisibleChanged += JunhyunDetailVisibilityChanged;

        _junhyunFlexibleItemsSourceDescriptor = DependencyPropertyDescriptor.FromProperty(
            ItemsControl.ItemsSourceProperty,
            typeof(ItemsControl));
        _junhyunFlexibleItemsSourceDescriptor?.AddValueChanged(
            FlexibleGroupItems,
            JunhyunFlexibleItemsSourceChanged);

        AddJunhyunItemWikiButton();
        SynchronizeJunhyunFlexibleStatusUi();
        UpdateJunhyunItemWikiButton();
    }

    private void AddJunhyunItemWikiButton()
    {
        if (_junhyunItemWikiButton is not null || DetailName.Parent is not Panel header)
            return;

        _junhyunItemWikiButton = new Button
        {
            Content = "위키",
            HorizontalAlignment = HorizontalAlignment.Left,
            Padding = new Thickness(12, 5, 12, 5),
            Margin = new Thickness(0, 8, 0, 0),
            IsEnabled = false,
            ToolTip = "이 아이템의 Escape from Tarkov Wiki 페이지 열기",
        };
        _junhyunItemWikiButton.Click += JunhyunItemWikiButton_Click;
        header.Children.Add(_junhyunItemWikiButton);
    }

    private void JunhyunViewModeButton_Click(object sender, RoutedEventArgs e) =>
        Dispatcher.BeginInvoke(
            () =>
            {
                SynchronizeJunhyunFlexibleStatusUi();
                if (_viewMode == ItemViewMode.Flexible)
                    ApplyJunhyunFlexibleStatusFilter();
                UpdateJunhyunItemWikiButton();
            },
            DispatcherPriority.Background);

    private void JunhyunFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_updatingFilters)
            return;

        Dispatcher.BeginInvoke(
            () =>
            {
                if (_viewMode == ItemViewMode.Flexible)
                    ApplyJunhyunFlexibleStatusFilter();
            },
            DispatcherPriority.Background);
    }

    private void JunhyunFilterComboBox_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (_viewMode == ItemViewMode.Flexible && !_busy && FilterComboBox.IsEnabled == false)
        {
            Dispatcher.BeginInvoke(
                () =>
                {
                    if (_viewMode == ItemViewMode.Flexible && !_busy)
                        FilterComboBox.IsEnabled = true;
                },
                DispatcherPriority.Background);
        }
    }

    private void JunhyunFlexibleItemsSourceChanged(object? sender, EventArgs e)
    {
        if (_junhyunApplyingFlexibleStatusFilter)
            return;

        Dispatcher.BeginInvoke(
            () =>
            {
                SynchronizeJunhyunFlexibleStatusUi();
                if (_viewMode == ItemViewMode.Flexible)
                    ApplyJunhyunFlexibleStatusFilter();
                UpdateJunhyunItemWikiButton();
            },
            DispatcherPriority.Background);
    }

    private void SynchronizeJunhyunFlexibleStatusUi()
    {
        if (_junhyunNormalFilterChoices is null)
            return;

        var selectedValue = (FilterComboBox.SelectedItem as FilterChoice)?.Value ?? ItemFilter.Needed;
        var targetChoices = _viewMode == ItemViewMode.Flexible
            ? _junhyunFlexibleFilterChoices
            : _junhyunNormalFilterChoices;

        var currentChoices = FilterComboBox.ItemsSource as IEnumerable<FilterChoice>;
        var needsReplacement = currentChoices is null || !currentChoices.SequenceEqual(targetChoices);
        if (needsReplacement)
        {
            _updatingFilters = true;
            try
            {
                FilterComboBox.ItemsSource = targetChoices;
                FilterComboBox.SelectedItem = targetChoices.FirstOrDefault(choice => choice.Value == selectedValue)
                    ?? targetChoices.First(choice => choice.Value == ItemFilter.Needed);
            }
            finally
            {
                _updatingFilters = false;
            }
        }

        FilterComboBox.IsEnabled = !_busy;
        UsageComboBox.IsEnabled = !_busy && _viewMode == ItemViewMode.Normal;
    }

    private void ApplyJunhyunFlexibleStatusFilter()
    {
        if (_junhyunApplyingFlexibleStatusFilter ||
            _viewMode != ItemViewMode.Flexible ||
            _workspace is null ||
            FlexibleGroupItems.ItemsSource is not IEnumerable<FlexibleGroupRow> source)
        {
            return;
        }

        var groups = source.ToArray();
        var filter = (FilterComboBox.SelectedItem as FilterChoice)?.Value ?? ItemFilter.Needed;
        var filtered = groups.Where(group => JunhyunFlexibleGroupMatches(group.QuestId, filter)).ToArray();
        if (groups.Length == filtered.Length && groups.SequenceEqual(filtered))
        {
            EmptyListText.Visibility = filtered.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
            return;
        }

        _junhyunApplyingFlexibleStatusFilter = true;
        try
        {
            FlexibleGroupItems.ItemsSource = filtered;
            EmptyListText.Visibility = filtered.Length == 0 ? Visibility.Visible : Visibility.Collapsed;

            var visibleItemIds = filtered
                .SelectMany(group => group.Candidates)
                .Select(row => row.ItemId)
                .ToHashSet(StringComparer.Ordinal);
            if (_selectedRow is null || !visibleItemIds.Contains(_selectedRow.ItemId))
                ShowDetail(filtered.SelectMany(group => group.Candidates).FirstOrDefault());
        }
        finally
        {
            _junhyunApplyingFlexibleStatusFilter = false;
        }
    }

    private bool JunhyunFlexibleGroupMatches(string questId, ItemFilter filter)
    {
        if (_workspace is null)
            return false;

        var progresses = _workspace.FlexibleQuestItemProgresses
            .Where(progress => string.Equals(progress.QuestId, questId, StringComparison.Ordinal))
            .ToArray();
        var fulfilled = progresses.Length > 0 && progresses.All(progress => progress.IsFulfilled);

        return filter switch
        {
            ItemFilter.All => true,
            ItemFilter.Needed => !fulfilled,
            ItemFilter.Satisfied => fulfilled,
            _ => false,
        };
    }

    private void JunhyunItemSelectionChanged(object sender, SelectionChangedEventArgs e) =>
        Dispatcher.BeginInvoke(UpdateJunhyunItemWikiButton, DispatcherPriority.Background);

    private void JunhyunFlexibleGroup_MouseUp(object sender, MouseButtonEventArgs e) =>
        Dispatcher.BeginInvoke(UpdateJunhyunItemWikiButton, DispatcherPriority.Background);

    private void JunhyunDetailVisibilityChanged(object sender, DependencyPropertyChangedEventArgs e) =>
        Dispatcher.BeginInvoke(UpdateJunhyunItemWikiButton, DispatcherPriority.Background);

    private void UpdateJunhyunItemWikiButton()
    {
        if (_junhyunItemWikiButton is null)
            return;

        var item = _selectedRow is null
            ? null
            : _content?.Items.FirstOrDefault(candidate =>
                string.Equals(candidate.Id, _selectedRow.ItemId, StringComparison.Ordinal));
        var valid = TryGetJunhyunWikiUri(item?.WikiUrl, out var uri);
        _junhyunItemWikiButton.IsEnabled = valid;
        _junhyunItemWikiButton.Tag = valid ? uri!.AbsoluteUri : null;
    }

    private void JunhyunItemWikiButton_Click(object sender, RoutedEventArgs e)
    {
        if (_junhyunItemWikiButton?.Tag is not string url ||
            !TryGetJunhyunWikiUri(url, out var uri))
        {
            return;
        }

        Process.Start(new ProcessStartInfo(uri!.AbsoluteUri)
        {
            UseShellExecute = true,
        });
    }

    private static bool TryGetJunhyunWikiUri(string? value, out Uri? uri)
    {
        uri = null;
        if (!Uri.TryCreate(value, UriKind.Absolute, out var candidate) ||
            (candidate.Scheme != Uri.UriSchemeHttps && candidate.Scheme != Uri.UriSchemeHttp))
        {
            return false;
        }

        uri = candidate;
        return true;
    }
}
