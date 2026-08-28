using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace JunhyunHelper.Desktop.Scanner;

public partial class ScannerPage
{
    internal void VerifyPublishedItemDetailVisualContract()
    {
        if (!string.Equals(
                Environment.GetEnvironmentVariable("JUNHYUNHELPER_MAP_SMOKE"),
                "1",
                StringComparison.Ordinal))
        {
            return;
        }

        BuildItemRelationshipPresentation();

        var materials = new[]
        {
            new ScannerItemMaterialRow(
                new ScannerItemLink("smoke-material-a", "매우 긴 런타임 재료 아이템 A", null), 2, false),
            new ScannerItemMaterialRow(
                new ScannerItemLink("smoke-material-b", "매우 긴 런타임 교환 아이템 B", null), 3, false),
            new ScannerItemMaterialRow(
                new ScannerItemLink("smoke-material-c", "매우 긴 런타임 재료 아이템 C", null), 4, true),
        };
        var snapshot = new ScannerItemSnapshot(
            "smoke-current-item",
            "런타임 현재 아이템",
            null,
            54321,
            65432,
            null,
            null,
            4,
            7,
            "테라피스트");
        var details = new ScannerItemSearchDetails(
            snapshot,
            null,
            new ScannerItemBasicDetails("교환품", 2, 2, 1.5m, true, 10000),
            new ScannerItemRelationshipDetails(
                [],
                [],
                [
                    new ScannerItemUsageRow(
                        "작업대",
                        2,
                        new ScannerItemLink("smoke-craft-product", "제작 결과 아이템", null),
                        1,
                        materials),
                ],
                [],
                [
                    new ScannerItemAcquisitionRow(
                        ScannerItemAcquisitionKind.HideoutCraft,
                        "작업대",
                        2,
                        materials,
                        1,
                        DurationSeconds: 3600),
                    new ScannerItemAcquisitionRow(
                        ScannerItemAcquisitionKind.TraderBarter,
                        "프라퍼",
                        3,
                        materials,
                        1,
                        BuyLimit: 2),
                    new ScannerItemAcquisitionRow(
                        ScannerItemAcquisitionKind.TraderPurchase,
                        "테라피스트",
                        2,
                        [],
                        Price: 12345,
                        CurrencyCode: "RUB"),
                    new ScannerItemAcquisitionRow(
                        ScannerItemAcquisitionKind.FleaMarket,
                        "플리마켓",
                        null,
                        [],
                        FleaAveragePrice: 65432),
                    new ScannerItemAcquisitionRow(
                        ScannerItemAcquisitionKind.Raid,
                        "레이드",
                        null,
                        []),
                ]));

        RenderProductItemExtensions(details);
        SelectedItemPanel.Measure(new Size(460, 2000));
        SelectedItemPanel.Arrange(new Rect(0, 0, 460, Math.Max(1200, SelectedItemPanel.DesiredSize.Height)));
        SelectedItemPanel.UpdateLayout();

        if (_basicInfoHost is null || _basicInfoItems is null ||
            _questUsageHost is null || _hideoutUsageHost is null ||
            _craftUsageHost is null || _craftUsageItems is null ||
            _acquisitionHost is null || _acquisitionItems is null)
        {
            throw new InvalidOperationException("Scanner v1.8.4 item-detail runtime hosts were not created.");
        }

        if (_basicInfoHost.Visibility != Visibility.Visible || _basicInfoItems.Children.Count != 4)
            throw new InvalidOperationException("Scanner basic-info runtime block did not render exactly four rows.");

        var basicRows = _basicInfoItems.Children.OfType<Grid>().ToArray();
        var labels = basicRows
            .Select(row => row.Children.OfType<TextBlock>().FirstOrDefault(text => Grid.GetColumn(text) == 0)?.Text)
            .ToArray();
        var expectedLabels = new[] { "크기", "플리마켓 평균가", "최고 상인 판매가", "필요 개수" };
        if (!labels.SequenceEqual(expectedLabels, StringComparer.Ordinal))
        {
            throw new InvalidOperationException(
                "Scanner basic-info labels drifted: " + string.Join(", ", labels.Select(value => value ?? "<null>")));
        }

        var basicValues = basicRows
            .Select(row => row.Children.OfType<TextBlock>().FirstOrDefault(text => Grid.GetColumn(text) == 1)?.Text ?? string.Empty)
            .ToArray();
        if (!basicValues.Any(value => value.Contains("65,432₽", StringComparison.Ordinal)) ||
            !basicValues.Any(value => value.Contains("54,321₽", StringComparison.Ordinal)) ||
            !basicValues.Any(value => value.Contains("7개", StringComparison.Ordinal)))
        {
            throw new InvalidOperationException(
                "Scanner basic-info price/needed values did not use the approved runtime presentation: " +
                string.Join(" | ", basicValues));
        }

        if (_questUsageHost.Visibility != Visibility.Collapsed || _hideoutUsageHost.Visibility != Visibility.Collapsed)
            throw new InvalidOperationException("Scanner rendered an empty Quest/Hideout relationship shell.");
        if (_craftUsageHost.Visibility != Visibility.Visible || _acquisitionHost.Visibility != Visibility.Visible)
            throw new InvalidOperationException("Scanner craft/acquisition relationship sections were not rendered.");

        var subsectionTitles = _acquisitionItems.Children
            .OfType<StackPanel>()
            .Select(panel => panel.Children.OfType<TextBlock>().FirstOrDefault()?.Text)
            .Where(text => !string.IsNullOrWhiteSpace(text))
            .ToArray();
        var expectedSubsections = new[] { "제작", "교환", "구매", "레이드 획득" };
        if (!subsectionTitles.SequenceEqual(expectedSubsections, StringComparer.Ordinal))
        {
            throw new InvalidOperationException(
                "Scanner acquisition subsections drifted: " + string.Join(", ", subsectionTitles));
        }

        var acquisitionText = EnumerateSmokeDescendants<TextBlock>(_acquisitionItems)
            .Select(text => text.Text)
            .Where(text => !string.IsNullOrWhiteSpace(text))
            .ToArray();
        if (!acquisitionText.Contains("레이드 획득 가능", StringComparer.Ordinal) ||
            !acquisitionText.Any(text => text.Contains("12,345 ₽", StringComparison.Ordinal)) ||
            !acquisitionText.Any(text => text.Contains("65,432 ₽", StringComparison.Ordinal)))
        {
            throw new InvalidOperationException("Scanner purchase/raid runtime text did not match the v1.8.4 contract.");
        }

        var relationshipButtons = EnumerateSmokeDescendants<Button>(_craftUsageItems)
            .Concat(EnumerateSmokeDescendants<Button>(_acquisitionItems))
            .Where(button => button.Tag is string)
            .ToArray();
        foreach (var itemId in new[] { "smoke-craft-product", "smoke-material-a", "smoke-material-b", "smoke-material-c" })
        {
            if (!relationshipButtons.Any(button => string.Equals(button.Tag as string, itemId, StringComparison.Ordinal)))
                throw new InvalidOperationException($"Scanner related-item runtime link was missing: {itemId}.");
        }
        if (relationshipButtons.Any(button => button.Cursor != Cursors.Hand))
            throw new InvalidOperationException("Scanner related-item runtime link did not render with clickable cursor semantics.");

        var materialWrap = EnumerateSmokeDescendants<WrapPanel>(_craftUsageItems)
            .FirstOrDefault(panel => panel.Children.OfType<Button>().Count() >= 3)
            ?? throw new InvalidOperationException("Scanner craft materials did not render inside a wrapping row.");
        materialWrap.Width = 190;
        materialWrap.Measure(new Size(190, 800));
        materialWrap.Arrange(new Rect(0, 0, 190, Math.Max(300, materialWrap.DesiredSize.Height)));
        materialWrap.UpdateLayout();
        var materialButtons = materialWrap.Children.OfType<Button>().ToArray();
        var materialY = materialButtons.Select(button => button.TranslatePoint(new Point(0, 0), materialWrap).Y).ToArray();
        if (materialY.Length < 3 || materialY.Max() - materialY.Min() < 1)
            throw new InvalidOperationException("Scanner material links did not wrap onto multiple runtime rows at narrow width.");

        var clickableMaterial = materialButtons.First(button => string.Equals(button.Tag as string, "smoke-material-a", StringComparison.Ordinal));
        var clickObserved = false;
        RoutedEventHandler probe = (_, _) => clickObserved = true;
        clickableMaterial.Click += probe;
        try
        {
            clickableMaterial.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        }
        finally
        {
            clickableMaterial.Click -= probe;
        }
        if (!clickObserved)
            throw new InvalidOperationException("Scanner related-item Button did not dispatch its runtime click event.");

        if (_neededSourcesHost is { Visibility: not Visibility.Collapsed })
            throw new InvalidOperationException("Scanner legacy duplicate needed-source block remained visible.");

        var marker = Path.Combine(Path.GetTempPath(), "junhyun-scanner-item-detail-smoke-success.txt");
        File.WriteAllText(
            marker,
            "basic-four-fields=ok\nempty-sections-hidden=ok\nrecipe-wrap=ok\nrelated-item-buttons=ok\nacquisition-groups=ok\n");
    }

    private static IEnumerable<T> EnumerateSmokeDescendants<T>(DependencyObject root)
        where T : DependencyObject
    {
        IEnumerable<DependencyObject> Children()
        {
            if (root is Panel panel)
            {
                foreach (UIElement child in panel.Children)
                    yield return child;
            }
            else if (root is Border { Child: DependencyObject child })
            {
                yield return child;
            }
            else if (root is ContentControl { Content: DependencyObject content })
            {
                yield return content;
            }
            else if (root is Decorator { Child: DependencyObject decorated })
            {
                yield return decorated;
            }
        }

        foreach (var child in Children())
        {
            if (child is T typed)
                yield return typed;
            foreach (var descendant in EnumerateSmokeDescendants<T>(child))
                yield return descendant;
        }
    }
}

internal static class ScannerItemDetailPublishedSmokeGate
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        EventManager.RegisterClassHandler(
            typeof(MainWindow),
            FrameworkElement.LoadedEvent,
            new RoutedEventHandler(OnMainWindowLoaded));
    }

    private static void OnMainWindowLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is not MainWindow window ||
            !ReferenceEquals(e.OriginalSource, window) ||
            !string.Equals(Environment.GetEnvironmentVariable("JUNHYUNHELPER_MAP_SMOKE"), "1", StringComparison.Ordinal))
        {
            return;
        }

        window.Dispatcher.BeginInvoke(
            () =>
            {
                try
                {
                    window.ScannerPlaceholder.VerifyPublishedItemDetailVisualContract();
                }
                catch (Exception exception)
                {
                    try
                    {
                        var diagnostic = Path.Combine(Path.GetTempPath(), "junhyun-map-smoke-error.txt");
                        File.WriteAllText(diagnostic, "Scanner v1.8.4 published item-detail smoke failed.\n" + exception);
                    }
                    catch
                    {
                    }

                    Environment.Exit(89);
                }
            },
            DispatcherPriority.ContextIdle);
    }
}
