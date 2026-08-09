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
    private readonly TarkovHelper.Pages.Map.MapPage _page;
    private readonly StackPanel? _content;
    private readonly StackPanel _combatRows = new();
    private readonly StackPanel _mapRows = new();
    private readonly StackPanel _extractRows = new();
    private readonly DispatcherTimer _retryTimer;
    private bool _initialized;
    private int _retries;

    public LegacyMapMarkerSettingsV2Bridge(TarkovHelper.Pages.Map.MapPage page)
    {
        _page = page ?? throw new ArgumentNullException(nameof(page));
        _content = _page.FindName("MapMarkersContent") as StackPanel;

        if (_content is not null)
            InitializeLayout();

        _retryTimer = new DispatcherTimer(
            TimeSpan.FromMilliseconds(250),
            DispatcherPriority.Background,
            (_, _) => RetryLateRows(),
            _page.Dispatcher);
        _retryTimer.Start();
    }

    private void InitializeLayout()
    {
        if (_initialized || _content is null)
            return;
        _initialized = true;

        var root = new StackPanel();
        root.Children.Add(CreateSection(
            "퀘스트",
            CreateQuestRows()));
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

        MoveCheckBox("ChkShowPmcExtracts", _extractRows);
        MoveCheckBox("ChkShowScavExtracts", _extractRows);
        MoveCheckBox("ChkShowTransitExtracts", _extractRows);

        CollapseLegacyDividers();
        TryMoveRaiderRow();
    }

    private StackPanel CreateQuestRows()
    {
        var rows = new StackPanel();
        if (_page.FindName("ChkShowQuestMarkers") is CheckBox questToggle)
        {
            Detach(questToggle);

            // The product adapter hides this control in the original top bar before
            // it is moved here. Moving a collapsed element preserves that state, which
            // left the Quest section looking like an empty grey strip on Windows.
            // Restore it explicitly as the single global Quest marker control.
            questToggle.Visibility = Visibility.Visible;
            questToggle.IsEnabled = true;
            questToggle.IsHitTestVisible = true;
            questToggle.Content = "퀘스트 마커 표시";
            questToggle.Margin = new Thickness(2, 2, 2, 2);
            questToggle.HorizontalAlignment = HorizontalAlignment.Left;
            rows.Children.Add(Wrap(questToggle));
        }
        return rows;
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
            checkBox.Parent is not FrameworkElement row)
        {
            return;
        }

        if (row is StackPanel stackRow && stackRow.Orientation == Orientation.Horizontal)
        {
            Detach(stackRow);
            stackRow.Margin = new Thickness(2, 2, 2, 2);
            destination.Children.Add(Wrap(stackRow));
            return;
        }

        MoveCheckBox(checkBoxName, destination);
    }

    private void MoveCheckBox(string name, StackPanel destination)
    {
        if (_page.FindName(name) is not CheckBox checkBox)
            return;

        Detach(checkBox);
        checkBox.Margin = new Thickness(2, 2, 2, 2);
        destination.Children.Add(Wrap(checkBox));
    }

    private Border Wrap(UIElement child) => new()
    {
        Background = new SolidColorBrush(Color.FromArgb(90, 255, 255, 255)),
        CornerRadius = new CornerRadius(4),
        Padding = new Thickness(6, 4, 6, 4),
        Margin = new Thickness(0, 2, 0, 2),
        Child = child,
    };

    private void RetryLateRows()
    {
        _retries++;
        TryMoveRaiderRow();
        CollapseLegacyDividers();
        if (_retries >= 24)
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

    public void Dispose() => _retryTimer.Stop();
}