using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using JunhyunHelper.Desktop.Map;

namespace JunhyunHelper.Desktop;

public partial class MainWindow
{
    /// <summary>
    /// Rendered WPF layout assertions for the exact UI regressions reported from the
    /// v0.1.11 screenshots. These assertions intentionally inspect arranged pixels/X
    /// positions rather than treating build success or source strings as UI validation.
    /// </summary>
    private async Task VerifyProductUiLayoutAsync()
    {
        VerifyFlexibleCandidateRenderedLayout();
        VerifyAmmoRenderedControls();
        await VerifyQuestSidebarRenderedLayoutAsync();
    }

    private void VerifyFlexibleCandidateRenderedLayout()
    {
        if (ItemsPage.Resources["FlexibleCandidateTemplate"] is not DataTemplate template)
            throw new InvalidOperationException("Flexible candidate template was not found for rendered UI smoke.");

        if (template.LoadContent() is not Button candidateButton || candidateButton.Content is not Grid rowGrid)
            throw new InvalidOperationException("Flexible candidate template did not render the expected Button/Grid row.");

        candidateButton.DataContext = new FlexibleCandidateLayoutProbe
        {
            ItemId = "ui-smoke-item",
            Name = "UI alignment probe item",
            CategoryLabel = "장비",
            OwnedFir = 12,
            OwnedNonFir = 34,
        };

        var host = new Grid
        {
            Width = 900,
            Height = 74,
        };
        host.Children.Add(candidateButton);
        host.Measure(new Size(900, 74));
        host.Arrange(new Rect(0, 0, 900, 74));
        candidateButton.ApplyTemplate();
        host.UpdateLayout();

        // The old global Button template hard-centered its ContentPresenter, leaving the
        // ~426px row grid floating in the middle of a ~900px candidate button. The
        // candidate's four lanes must now occupy the full row width.
        if (rowGrid.ActualWidth < 820)
        {
            throw new InvalidOperationException(
                $"Flexible candidate content is still centered/content-sized: gridWidth={rowGrid.ActualWidth:F1}, " +
                $"buttonWidth={candidateButton.ActualWidth:F1}.");
        }

        if (rowGrid.ColumnDefinitions.Count != 4 ||
            Math.Abs(rowGrid.ColumnDefinitions[0].ActualWidth - 52) > 0.5 ||
            Math.Abs(rowGrid.ColumnDefinitions[2].ActualWidth - 108) > 0.5 ||
            Math.Abs(rowGrid.ColumnDefinitions[3].ActualWidth - 96) > 0.5)
        {
            throw new InvalidOperationException(
                "Flexible candidate fixed icon/FIR/general lanes did not render at their canonical widths.");
        }

        var iconBorder = rowGrid.Children
            .OfType<Border>()
            .FirstOrDefault(element => Grid.GetColumn(element) == 0)
            ?? throw new InvalidOperationException("Flexible candidate icon lane was not rendered.");
        var nameStack = rowGrid.Children
            .OfType<StackPanel>()
            .FirstOrDefault(element => Grid.GetColumn(element) == 1)
            ?? throw new InvalidOperationException("Flexible candidate name lane was not rendered.");
        var firStack = rowGrid.Children
            .OfType<StackPanel>()
            .FirstOrDefault(element => Grid.GetColumn(element) == 2)
            ?? throw new InvalidOperationException("Flexible candidate FIR lane was not rendered.");
        var generalStack = rowGrid.Children
            .OfType<StackPanel>()
            .FirstOrDefault(element => Grid.GetColumn(element) == 3)
            ?? throw new InvalidOperationException("Flexible candidate general lane was not rendered.");

        var iconX = iconBorder.TranslatePoint(new Point(0, 0), rowGrid).X;
        var nameX = nameStack.TranslatePoint(new Point(0, 0), rowGrid).X;
        var firX = firStack.TranslatePoint(new Point(0, 0), rowGrid).X;
        var generalX = generalStack.TranslatePoint(new Point(0, 0), rowGrid).X;
        if (Math.Abs(iconX) > 0.5 ||
            Math.Abs(nameX - 52) > 0.5 ||
            Math.Abs(firX - (rowGrid.ActualWidth - 204)) > 0.75 ||
            Math.Abs(generalX - (rowGrid.ActualWidth - 96)) > 0.75)
        {
            throw new InvalidOperationException(
                $"Flexible candidate rendered X lanes drifted: icon={iconX:F1}, name={nameX:F1}, " +
                $"fir={firX:F1}, general={generalX:F1}, row={rowGrid.ActualWidth:F1}.");
        }
    }

    private void VerifyAmmoRenderedControls()
    {
        AmmoPage.UpdateLayout();

        var favorite = AmmoPage.FavoriteCaliberButton.Content as string;
        if (favorite is not ("☆" or "★"))
        {
            throw new InvalidOperationException(
                $"Ammo favorite button still contains text instead of a single star: '{favorite ?? "<null>"}'.");
        }

        if (AmmoPage.FavoriteCaliberButton.Width > 50)
            throw new InvalidOperationException("Ammo favorite star button is wider than the compact control contract.");

        var toggle = AmmoPage.ProductDetailToggleButton;
        var initial = toggle.Content as string;
        if (initial != "▼")
        {
            throw new InvalidOperationException(
                $"Expanded ammo detail handle must render ▼ only, but rendered '{initial ?? "<null>"}'.");
        }

        if (toggle.Width > 50)
            throw new InvalidOperationException("Ammo detail handle is still a text-width button instead of a compact arrow handle.");

        toggle.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        AmmoPage.UpdateLayout();
        if (toggle.Content as string != "▲" || AmmoPage.ProductDetailHost.Visibility != Visibility.Collapsed)
            throw new InvalidOperationException("Collapsed ammo detail state did not render ▲ with the detail host hidden.");

        // Restore the normal expanded default for the remainder of the product smoke.
        toggle.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        AmmoPage.UpdateLayout();
        if (toggle.Content as string != "▼" || AmmoPage.ProductDetailHost.Visibility != Visibility.Visible)
            throw new InvalidOperationException("Expanded ammo detail state did not restore ▼ with the detail host visible.");
    }

    private async Task VerifyQuestSidebarRenderedLayoutAsync()
    {
        var sidebar = _legacyMapQuestSidebarV2
            ?? throw new InvalidOperationException("Quest sidebar was not available for rendered UI smoke.");

        var marker = new JunhyunQuestMarkerProjectionV2(
            "ui-smoke-marked",
            "UI Marked Quest",
            "ui-smoke-objective",
            "Objective",
            "A",
            100,
            100,
            null);
        sidebar.SetState(
            "UI smoke",
            [
                new LegacyMapQuestEntryV2("ui-smoke-marked", "UI Marked Quest", [marker], true, "A"),
                new LegacyMapQuestEntryV2("ui-smoke-unmarked", "UI Unmarked Quest", [], true, null),
                new LegacyMapQuestEntryV2("ui-smoke-disabled", "UI Disabled Marker Quest", [], false, "C"),
            ]);

        await Dispatcher.InvokeAsync(sidebar.UpdateLayout);

        var toggle = FindVisualDescendants<Button>(sidebar)
            .FirstOrDefault(button => button.Content as string is "▶" or "◀")
            ?? throw new InvalidOperationException("Quest sidebar handle was not rendered.");
        if (toggle.Content as string == "▶")
        {
            toggle.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await Dispatcher.InvokeAsync(sidebar.UpdateLayout);
        }

        var handleX = toggle.TranslatePoint(new Point(0, 0), sidebar).X;
        var rightGap = sidebar.ActualWidth - (handleX + toggle.ActualWidth);
        if (sidebar.ActualWidth < LegacyMapQuestSidebarV2.ExpandedWidth - 1 || rightGap > 6)
        {
            throw new InvalidOperationException(
                $"Expanded Quest sidebar handle is not attached to the outside/right edge: " +
                $"sidebar={sidebar.ActualWidth:F1}, handleX={handleX:F1}, handleWidth={toggle.ActualWidth:F1}, rightGap={rightGap:F1}.");
        }

        var titles = FindVisualDescendants<TextBlock>(sidebar)
            .Where(text => text.Text is "UI Marked Quest" or "UI Unmarked Quest" or "UI Disabled Marker Quest")
            .ToArray();
        if (titles.Length != 3)
            throw new InvalidOperationException($"Quest sidebar smoke expected 3 rendered Quest titles but found {titles.Length}.");

        var xPositions = titles
            .Select(text => text.TranslatePoint(new Point(0, 0), sidebar).X)
            .ToArray();
        if (xPositions.Max() - xPositions.Min() > 0.75)
        {
            throw new InvalidOperationException(
                "Quest title starts are still content-centered instead of sharing one X axis: " +
                string.Join(", ", xPositions.Select(value => value.ToString("F1"))));
        }
    }

    private static IEnumerable<T> FindVisualDescendants<T>(DependencyObject root)
        where T : DependencyObject
    {
        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var index = 0; index < count; index++)
        {
            var child = VisualTreeHelper.GetChild(root, index);
            if (child is T typed)
                yield return typed;

            foreach (var descendant in FindVisualDescendants<T>(child))
                yield return descendant;
        }
    }

    private sealed class FlexibleCandidateLayoutProbe
    {
        public required string ItemId { get; init; }
        public required string Name { get; init; }
        public required string CategoryLabel { get; init; }
        public required int OwnedFir { get; init; }
        public required int OwnedNonFir { get; init; }
    }
}
