using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace JunhyunHelper.Desktop.Ammo;

public partial class AmmoPage
{
    internal void VerifyPublishedToolbarLayoutContract()
    {
        if (!string.Equals(
                Environment.GetEnvironmentVariable("JUNHYUNHELPER_MAP_SMOKE"),
                "1",
                StringComparison.Ordinal))
        {
            return;
        }

        VerifyProductCaliberSelectorInitialization();

        if (CaliberComboBox.Parent is not Grid header)
            throw new InvalidOperationException("Ammo toolbar parent is not the expected Grid.");
        header.Measure(new Size(1200, 80));
        header.Arrange(new Rect(0, 0, 1200, Math.Max(50, header.DesiredSize.Height)));
        header.UpdateLayout();
        FavoriteCaliberComboBox.ApplyTemplate();
        ColumnMenuButton.ApplyTemplate();
        header.UpdateLayout();

        if (header.ColumnDefinitions.Count != 7)
            throw new InvalidOperationException($"Ammo toolbar expected 7 product columns but rendered {header.ColumnDefinitions.Count}.");
        if (Grid.GetColumn(CaliberComboBox) != 0 ||
            Grid.GetColumn(FavoriteCaliberButton) != 1 ||
            Grid.GetColumn(FavoriteCaliberComboBox) != 2 ||
            Grid.GetColumn(ColumnMenuButton) != 6)
        {
            throw new InvalidOperationException(
                $"Ammo toolbar column contract drifted: caliber={Grid.GetColumn(CaliberComboBox)}, " +
                $"favoriteStar={Grid.GetColumn(FavoriteCaliberButton)}, favoriteSelector={Grid.GetColumn(FavoriteCaliberComboBox)}, " +
                $"columns={Grid.GetColumn(ColumnMenuButton)}.");
        }

        if (ColumnMenuButton.Visibility != Visibility.Visible ||
            !ColumnMenuButton.IsHitTestVisible ||
            !string.Equals(ColumnMenuButton.Content as string, "표시 열", StringComparison.Ordinal) ||
            ColumnMenuButton.ActualWidth <= 0 || ColumnMenuButton.ActualHeight <= 0)
        {
            throw new InvalidOperationException("Ammo displayed-columns button is not visible and interactive in the published toolbar.");
        }
        if (FavoriteCaliberComboBox.Visibility != Visibility.Visible ||
            !FavoriteCaliberComboBox.IsHitTestVisible ||
            FavoriteCaliberComboBox.ActualWidth <= 0 || FavoriteCaliberComboBox.ActualHeight <= 0)
        {
            throw new InvalidOperationException("Ammo favorite selector is not visible and interactive in the published toolbar.");
        }

        var caliberX = CaliberComboBox.TranslatePoint(new Point(0, 0), header).X;
        var favoriteStarX = FavoriteCaliberButton.TranslatePoint(new Point(0, 0), header).X;
        var favoriteSelectorX = FavoriteCaliberComboBox.TranslatePoint(new Point(0, 0), header).X;
        var columnMenuX = ColumnMenuButton.TranslatePoint(new Point(0, 0), header).X;
        var columnMenuRight = columnMenuX + ColumnMenuButton.ActualWidth;
        var rightGap = header.ActualWidth - columnMenuRight;

        if (!(caliberX < favoriteStarX && favoriteStarX < favoriteSelectorX && favoriteSelectorX < columnMenuX))
        {
            throw new InvalidOperationException(
                $"Ammo toolbar rendered order drifted: caliber={caliberX:F1}, star={favoriteStarX:F1}, " +
                $"favorite={favoriteSelectorX:F1}, columns={columnMenuX:F1}.");
        }
        if (rightGap < -0.75 || rightGap > 1.5)
        {
            throw new InvalidOperationException(
                $"Ammo displayed-columns button is not pinned to the right edge: rightGap={rightGap:F1}, header={header.ActualWidth:F1}.");
        }

        if (ProductSearchBox.Parent is not Grid searchHost)
            throw new InvalidOperationException("Ammo search box is not hosted in the canonical toolbar search lane.");
        if (Grid.GetColumn(searchHost) != 4)
            throw new InvalidOperationException($"Ammo search host rendered in column {Grid.GetColumn(searchHost)} instead of 4.");
        var searchX = searchHost.TranslatePoint(new Point(0, 0), header).X;
        if (!(favoriteSelectorX < searchX && searchX < columnMenuX))
        {
            throw new InvalidOperationException(
                $"Ammo search field is not between favorite selector and displayed-columns button: " +
                $"favorite={favoriteSelectorX:F1}, search={searchX:F1}, columns={columnMenuX:F1}.");
        }

        var marker = Path.Combine(Path.GetTempPath(), "junhyun-ammo-toolbar-smoke-success.txt");
        File.WriteAllText(
            marker,
            "favorite-selector-left=ok\ndisplayed-columns-visible=ok\ndisplayed-columns-right-edge=ok\n");
    }
}

internal static class AmmoToolbarPublishedSmokeGate
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
                    window.AmmoPage.VerifyPublishedToolbarLayoutContract();
                }
                catch (Exception exception)
                {
                    try
                    {
                        var diagnostic = Path.Combine(Path.GetTempPath(), "junhyun-map-smoke-error.txt");
                        File.WriteAllText(diagnostic, "Ammo published toolbar smoke failed.\n" + exception);
                    }
                    catch
                    {
                    }

                    Environment.Exit(90);
                }
            },
            DispatcherPriority.ContextIdle);
    }
}
