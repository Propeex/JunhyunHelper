using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace JunhyunHelper.Desktop.Map;

/// <summary>
/// Reorganizes the transplanted floating marker controls into product-level sections
/// without changing the underlying original checkbox handlers.
/// </summary>
public sealed class LegacyMapMarkerSettingsV2Bridge : IDisposable
{
    private const string QuestMarkerToggleName = "ChkShowQuestMarkers";

    private readonly TarkovHelper.Pages.Map.MapPage _page;
    private readonly StackPanel? _content;
    private readonly StackPanel _combatRows = new();
    private readonly StackPanel _mapRows = new();
    private readonly StackPanel _extractRows = new();
    private readonly DispatcherTimer _retryTimer;
    private CheckBox? _legacyQuestToggle;
    private CheckBox? _productQuestToggle;
    private bool _syncingQuestToggle;
    private bool _initialized;
    private int _retries;

    public LegacyMapMarkerSettingsV2Bridge(TarkovHelper.Pages.Map.MapPage page)
    {
        _page = page ?? throw new ArgumentNullException(nameof(page));
        _content = _page.FindName("MapMarkersContent") as StackPanel;
        _page.Loaded += Page_Loaded;

        if (_content is not null)
            InitializeLayout();

        _retryTimer = new DispatcherTimer(
            TimeSpan.FromMilliseconds(250),
            DispatcherPriority.Background,
            (_, _) => RetryLateRows(),
            _page.Dispatcher);
        _retryTimer.Start();
    }

    private void Page_Loaded(object sender, RoutedEventArgs e) =>
        ApplyPersistedQuestToggle();

    private void InitializeLayout()
    {
        if (_initialized || _content is null)
            return;
        _initialized = true;

        var root = new StackPanel();
        root.Children.Add(CreateSection("퀘스트", CreateQuestRows()));
        root.Children.Add(CreateSection("전투 / 스폰", _combatRows));
        root.Children.Add(CreateSection("지도 요소", _mapRows));
        root.Children.Add(CreateSection("탈출 / 이동", _extractRows));
        _content.Children.Insert(0, root);

        MoveExistingRow("ChkShowPmcSpawns", _combatRows);
        MoveExistingRow("ChkShowSniperScavs", _combatRows);
        MoveExistingRow("ChkShowRogues", _combatRows);
        MoveExistingRow("ChkShowCultists", _combatRows);
        MoveExistingRow("ChkShowBosses", _combatRows);
        MoveExistingRow("ChkShowLeversMarker", _mapRows);

        TryMoveExtractRows();

        CollapseLegacyDividers();
        TryMoveRaiderRow();
    }

    private StackPanel CreateQuestRows()
    {
        var rows = new StackPanel();
        _legacyQuestToggle = _page.FindName(QuestMarkerToggleName) as CheckBox;
        var persisted = JunhyunMapProductSettingsStore.Instance.GetToggle(QuestMarkerToggleName) ?? true;

        if (_legacyQuestToggle is not null)
        {
            // Product persistence is authoritative. Seed the vendor behavior endpoint
            // before exposing the product control, then keep it hidden.
            _legacyQuestToggle.IsChecked = persisted;
            Detach(_legacyQuestToggle);
            _legacyQuestToggle.Visibility = Visibility.Collapsed;
            _legacyQuestToggle.Checked += LegacyQuestToggle_Changed;
            _legacyQuestToggle.Unchecked += LegacyQuestToggle_Changed;
        }

        _productQuestToggle = new CheckBox
        {
            Content = "퀘스트 마커 표시",
            IsChecked = persisted,
            IsThreeState = false,
            IsEnabled = true,
            IsHitTestVisible = true,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(2),
            Foreground = Brush("TextPrimaryBrush", Brushes.White),
            ToolTip = "왼쪽 퀘스트 목록에서 선택한 퀘스트 마커를 지도와 미니맵에 표시합니다.",
        };
        _productQuestToggle.Checked += ProductQuestToggle_Changed;
        _productQuestToggle.Unchecked += ProductQuestToggle_Changed;
        rows.Children.Add(Wrap(_productQuestToggle));
        return rows;
    }

    private void ProductQuestToggle_Changed(object sender, RoutedEventArgs e)
    {
        if (_syncingQuestToggle || _productQuestToggle is null)
            return;

        var enabled = _productQuestToggle.IsChecked == true;
        JunhyunMapProductSettingsStore.Instance.SetToggle(QuestMarkerToggleName, enabled);

        _syncingQuestToggle = true;
        try
        {
            if (_legacyQuestToggle is not null)
                _legacyQuestToggle.IsChecked = enabled;
        }
        finally
        {
            _syncingQuestToggle = false;
        }
    }

    private void LegacyQuestToggle_Changed(object sender, RoutedEventArgs e)
    {
        if (_syncingQuestToggle)
            return;

        // The legacy checkbox is hidden and is only a behavior endpoint. Vendor startup
        // code may still assign its default later; never let that late assignment overwrite
        // the user's persisted product preference. Re-assert the saved value instead.
        ApplyPersistedQuestToggle();
    }

    private void ApplyPersistedQuestToggle()
    {
        var enabled = JunhyunMapProductSettingsStore.Instance.GetToggle(QuestMarkerToggleName)
                      ?? _productQuestToggle?.IsChecked
                      ?? true;

        _syncingQuestToggle = true;
        try
        {
            if (_productQuestToggle is not null)
                _productQuestToggle.IsChecked = enabled;
            if (_legacyQuestToggle is not null)
                _legacyQuestToggle.IsChecked = enabled;
        }
        finally
        {
            _syncingQuestToggle = false;
        }
    }

    private FrameworkElement CreateSection(string title, StackPanel rows)
    {
        var stack = new StackPanel { Margin = new Thickness(0, 0, 0, 8) };
        stack.Children.Add(new TextBlock
        {
            Text = title,
            Foreground = Brush("TextPrimaryBrush", Brushes.White),
            FontWeight = FontWeights.SemiBold,
            FontSize = 11,
            Margin = new Thickness(2, 0, 0, 4),
        });
        stack.Children.Add(new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(215, 38, 38, 38)),
            BorderBrush = Brush("BorderBrush", Brushes.DimGray),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(5, 4, 5, 4),
            Child = rows,
        });
        return stack;
    }

    private void MoveExistingRow(string checkBoxName, StackPanel destination)
    {
        if (_page.FindName(checkBoxName) is not CheckBox checkBox ||
            IsDescendantOf(checkBox, destination) ||
            checkBox.Parent is not FrameworkElement row)
        {
            return;
        }

        if (row is StackPanel stackRow && stackRow.Orientation == Orientation.Horizontal)
        {
            Detach(stackRow);
            stackRow.Margin = new Thickness(2);
            destination.Children.Add(Wrap(stackRow));
            return;
        }

        MoveCheckBox(checkBoxName, destination);
    }

    private bool MoveCheckBox(string name, StackPanel destination)
    {
        if (_page.FindName(name) is not CheckBox checkBox)
            return false;
        if (IsDescendantOf(checkBox, destination))
            return true;

        Detach(checkBox);
        checkBox.Margin = new Thickness(2);
        destination.Children.Add(Wrap(checkBox));
        return true;
    }

    private bool TryMoveExtractRows()
    {
        var pmc = MoveCheckBox("ChkShowPmcExtracts", _extractRows);
        var scav = MoveCheckBox("ChkShowScavExtracts", _extractRows);
        var transit = MoveCheckBox("ChkShowTransitExtracts", _extractRows);
        return pmc && scav && transit;
    }

    private Border Wrap(UIElement child) => new()
    {
        Background = new SolidColorBrush(Color.FromArgb(90, 255, 255, 255)),
        CornerRadius = new CornerRadius(4),
        Padding = new Thickness(6, 4, 6, 4),
        Margin = new Thickness(0, 2, 0, 2),
        HorizontalAlignment = HorizontalAlignment.Stretch,
        Child = child,
    };

    private void RetryLateRows()
    {
        _retries++;
        TryMoveRaiderRow();
        var extractsReady = TryMoveExtractRows();
        CollapseLegacyDividers();
        ApplyPersistedQuestToggle();
        if (extractsReady || _retries >= 24)
            _retryTimer.Stop();
    }

    private void TryMoveRaiderRow()
    {
        if (_content is null)
            return;

        var row = _content.Children
            .OfType<StackPanel>()
            .FirstOrDefault(panel => panel.Children
                .OfType<TextBlock>()
                .Any(text => string.Equals(text.Text, "레이더", StringComparison.Ordinal)));
        if (row is null)
            return;

        _content.Children.Remove(row);
        row.Margin = new Thickness(2);
        _combatRows.Children.Add(Wrap(row));
    }

    private void CollapseLegacyDividers()
    {
        if (_content is null)
            return;

        foreach (var divider in _content.Children.OfType<Border>().Where(border => border.Height == 1))
            divider.Visibility = Visibility.Collapsed;
    }

    private static bool IsDescendantOf(FrameworkElement element, DependencyObject ancestor)
    {
        DependencyObject? current = element;
        while (current is not null)
        {
            if (ReferenceEquals(current, ancestor))
                return true;
            current = current switch
            {
                FrameworkElement framework => framework.Parent,
                FrameworkContentElement content => content.Parent,
                _ => null,
            };
        }

        return false;
    }

    private static void Detach(UIElement element)
    {
        switch (element)
        {
            case FrameworkElement framework when framework.Parent is Panel panel:
                panel.Children.Remove(element);
                break;
            case FrameworkElement framework when framework.Parent is Decorator decorator:
                decorator.Child = null;
                break;
        }
    }

    private Brush Brush(string key, Brush fallback) =>
        _page.TryFindResource(key) as Brush ?? fallback;

    public void Dispose()
    {
        _retryTimer.Stop();
        _page.Loaded -= Page_Loaded;

        if (_productQuestToggle is not null)
        {
            _productQuestToggle.Checked -= ProductQuestToggle_Changed;
            _productQuestToggle.Unchecked -= ProductQuestToggle_Changed;
        }

        if (_legacyQuestToggle is not null)
        {
            _legacyQuestToggle.Checked -= LegacyQuestToggle_Changed;
            _legacyQuestToggle.Unchecked -= LegacyQuestToggle_Changed;
        }
    }
}
