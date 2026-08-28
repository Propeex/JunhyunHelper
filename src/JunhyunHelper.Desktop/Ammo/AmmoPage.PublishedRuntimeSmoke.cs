using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;
using JunhyunHelper.Core.Ammo;

namespace JunhyunHelper.Desktop.Ammo;

public partial class AmmoPage
{
    private const string PublishedAmmoSmokeCaliber = "published-smoke-caliber";

    internal void VerifyPublishedCaliberIconVisualContract()
    {
        if (!string.Equals(
                Environment.GetEnvironmentVariable("JUNHYUNHELPER_MAP_SMOKE"),
                "1",
                StringComparison.Ordinal))
        {
            return;
        }

        // Do not trust the Loaded hook itself as proof. The regression being guarded was
        // precisely that the hook could fail to run in the published executable. Invoke
        // the product initializer directly if needed, then inspect the rendered controls.
        if (!_productCaliberDropdownApplied)
            ApplyProductCaliberDropdownPolish();

        if (_productFavoriteCaliberComboBox is null || _productCaliberIconTimer is null)
            throw new InvalidOperationException("Ammo runtime selectors were not initialized for published visual smoke.");

        var originalVisibility = Visibility;
        var originalRows = _allRows;
        var originalItemsSource = CaliberComboBox.ItemsSource;
        var originalSelection = CaliberComboBox.SelectedItem;
        var originalFavorites = _favoriteCalibers.ToHashSet(StringComparer.Ordinal);
        var originalIndices = new Dictionary<string, int>(_productCaliberIconIndices, StringComparer.Ordinal);

        var firstIcon = CreatePublishedSmokeIcon(Brushes.White);
        var secondIcon = CreatePublishedSmokeIcon(Brushes.Gray);
        var firstRow = CreatePublishedSmokeRow("published-smoke-round-a", "Smoke A", firstIcon);
        var secondRow = CreatePublishedSmokeRow("published-smoke-round-b", "Smoke B", secondIcon);
        var smokeChoice = new CaliberChoice(PublishedAmmoSmokeCaliber, "Published smoke caliber");

        try
        {
            Visibility = Visibility.Visible;
            _allRows = [firstRow, secondRow];
            EnsureProductCaliberRows();

            CaliberComboBox.ItemsSource = new[]
            {
                new CaliberChoice(null, "전체 구경"),
                smokeChoice,
            };
            CaliberComboBox.SelectedItem = smokeChoice;

            _favoriteCalibers.Add(PublishedAmmoSmokeCaliber);
            RefreshProductFavoriteChoices();
            SyncProductFavoriteSelection();

            if (_productFavoriteCaliberComboBox.SelectedItem is not CaliberChoice favoriteChoice ||
                !string.Equals(favoriteChoice.RawCaliber, PublishedAmmoSmokeCaliber, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Ammo favorite selector did not select the same smoke caliber.");
            }

            _productCaliberIconIndices[PublishedAmmoSmokeCaliber] = 0;
            RefreshProductCaliberIconVisuals();
            ForcePublishedSmokeLayout();

            RequireRenderedCaliberIcon(CaliberComboBox, firstIcon, "caliber selector initial icon");
            RequireRenderedCaliberIcon(_productFavoriteCaliberComboBox, firstIcon, "favorite selector initial icon");

            // Exercise the exact callback used by the product DispatcherTimer. Both
            // selectors must observe the same per-caliber index after the tick.
            ProductCaliberIconTimer_Tick(null, EventArgs.Empty);
            ForcePublishedSmokeLayout();

            RequireRenderedCaliberIcon(CaliberComboBox, secondIcon, "caliber selector cycled icon");
            RequireRenderedCaliberIcon(_productFavoriteCaliberComboBox, secondIcon, "favorite selector shared cycled icon");

            if (!ReferenceEquals(CurrentProductCaliberIcon(PublishedAmmoSmokeCaliber), secondIcon))
                throw new InvalidOperationException("Ammo shared caliber icon state did not advance to the second icon.");

            var marker = Path.Combine(Path.GetTempPath(), "junhyun-ammo-ui-smoke-success.txt");
            File.WriteAllText(
                marker,
                "rendered-caliber-image=ok\nrendered-favorite-image=ok\nshared-timer-cycle=ok\n");
        }
        finally
        {
            _productCaliberIconTimer.Stop();
            CaliberComboBox.IsDropDownOpen = false;
            _productFavoriteCaliberComboBox.IsDropDownOpen = false;

            _favoriteCalibers.Clear();
            _favoriteCalibers.UnionWith(originalFavorites);
            _allRows = originalRows;
            EnsureProductCaliberRows();

            CaliberComboBox.ItemsSource = originalItemsSource;
            CaliberComboBox.SelectedItem = originalSelection;
            RefreshProductFavoriteChoices();
            SyncProductFavoriteSelection();

            _productCaliberIconIndices.Clear();
            foreach (var (caliber, index) in originalIndices)
                _productCaliberIconIndices[caliber] = index;

            RefreshProductCaliberIconVisuals();
            Visibility = originalVisibility;
        }
    }

    private void ForcePublishedSmokeLayout()
    {
        Measure(new Size(1200, 720));
        Arrange(new Rect(0, 0, 1200, 720));
        UpdateLayout();
        CaliberComboBox.ApplyTemplate();
        _productFavoriteCaliberComboBox?.ApplyTemplate();
        RefreshProductCaliberIconVisuals();
        UpdateLayout();
    }

    private static ImageSource CreatePublishedSmokeIcon(Brush brush)
    {
        var geometry = new RectangleGeometry(new Rect(0, 0, 20, 12));
        geometry.Freeze();
        var drawing = new GeometryDrawing(brush, null, geometry);
        drawing.Freeze();
        var image = new DrawingImage(drawing);
        image.Freeze();
        return image;
    }

    private static AmmoRow CreatePublishedSmokeRow(string itemId, string name, ImageSource icon)
    {
        var ammo = new AmmoDefinition(
            itemId,
            PublishedAmmoSmokeCaliber,
            null,
            1,
            1,
            1,
            1,
            0,
            0,
            0,
            0,
            1,
            0,
            0,
            false,
            null,
            Array.Empty<AmmoAcquisition>());

        var row = new AmmoRow(
            ammo,
            name,
            null,
            PublishedAmmoSmokeCaliber,
            "Published smoke caliber",
            1,
            "1",
            1,
            "1%",
            Array.Empty<ArmorEffectivenessCell>(),
            "1 m/s",
            "0%",
            "0%",
            "0%",
            "smoke");
        row.Icon = icon;
        return row;
    }

    private static void RequireRenderedCaliberIcon(ComboBox comboBox, ImageSource expected, string contract)
    {
        var images = EnumerateProductCaliberImages(comboBox).ToList();
        if (comboBox.Template.FindName("PART_Popup", comboBox) is Popup { Child: DependencyObject popupChild })
            images.AddRange(EnumerateProductCaliberImages(popupChild));

        var rendered = images
            .Where(image => string.Equals(image.Tag as string, PublishedAmmoSmokeCaliber, StringComparison.Ordinal))
            .ToArray();

        if (rendered.Length == 0)
            throw new InvalidOperationException($"Ammo published smoke did not render an Image for {contract}.");
        if (!rendered.Any(image => ReferenceEquals(image.Source, expected)))
            throw new InvalidOperationException($"Ammo published smoke rendered the wrong Image.Source for {contract}.");
        if (rendered.All(image => image.ActualWidth <= 0 || image.ActualHeight <= 0))
            throw new InvalidOperationException($"Ammo published smoke Image had no rendered geometry for {contract}.");
    }
}

internal static class AmmoPublishedRuntimeSmokeGate
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
                    window.AmmoPage.VerifyPublishedCaliberIconVisualContract();
                }
                catch (Exception exception)
                {
                    try
                    {
                        var diagnostic = Path.Combine(Path.GetTempPath(), "junhyun-map-smoke-error.txt");
                        File.WriteAllText(diagnostic, "Ammo published visual smoke failed.\n" + exception);
                    }
                    catch
                    {
                    }

                    Environment.Exit(86);
                }
            },
            DispatcherPriority.Loaded);
    }
}
