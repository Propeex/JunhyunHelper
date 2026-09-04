using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace JunhyunHelper.Desktop.Scanner;

internal static class ScannerPageProductContractSmokeRegistration
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        EventManager.RegisterClassHandler(
            typeof(ScannerPage),
            FrameworkElement.LoadedEvent,
            new RoutedEventHandler(ScannerPage.HandleProductContractSmokeLoaded),
            handledEventsToo: true);
    }
}

public partial class ScannerPage
{
    private const string ProductSmokeEnvironmentVariable = "JUNHYUNHELPER_MAP_SMOKE";
    private const string ProductSmokeDiagnosticFileName = "junhyun-map-smoke-error.txt";
    private bool _productContractSmokeScheduled;

    internal static void HandleProductContractSmokeLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is not ScannerPage page ||
            page._productContractSmokeScheduled ||
            !string.Equals(
                Environment.GetEnvironmentVariable(ProductSmokeEnvironmentVariable),
                "1",
                StringComparison.Ordinal))
        {
            return;
        }

        page._productContractSmokeScheduled = true;
        _ = page.Dispatcher.BeginInvoke(
            DispatcherPriority.ContextIdle,
            new Action(page.RunProductContractSmoke));
    }

    private void RunProductContractSmoke()
    {
        try
        {
            if (_coordinator is null)
                throw new InvalidOperationException("Scanner coordinator was unavailable for product-contract smoke.");

            VerifyAmmoPickupSettingsRow();
            VerifySearchClearButton(Window.GetWindow(this) as MainWindow);
            VerifyMiniScannerSaveFeedback();
        }
        catch (Exception exception)
        {
            try
            {
                var path = Path.Combine(Path.GetTempPath(), ProductSmokeDiagnosticFileName);
                File.WriteAllText(path, exception.ToString());
            }
            catch
            {
            }

            Environment.Exit(86);
        }
    }

    private void VerifyAmmoPickupSettingsRow()
    {
        if (_coordinator is null)
            throw new InvalidOperationException("Scanner coordinator was unavailable for settings smoke.");

        var settingsWindow = new ScannerSettingsWindow(_coordinator);
        try
        {
            var labels = settingsWindow.InfoOrderList.Items
                .Cast<object>()
                .Select(item => item.GetType()
                    .GetProperty("Label", BindingFlags.Public | BindingFlags.Instance)?
                    .GetValue(item) as string)
                .Where(label => !string.IsNullOrWhiteSpace(label))
                .ToArray();

            if (!labels.Contains("탄약 줍기 판단", StringComparer.Ordinal))
            {
                throw new InvalidOperationException(
                    "Scanner settings did not expose the ammo pickup visibility/order row in the published UI.");
            }

            if (labels.Any(label => string.Equals(label, "플리마켓 최저가", StringComparison.Ordinal)))
                throw new InvalidOperationException("Scanner settings re-exposed flea minimum price presentation.");
        }
        finally
        {
            settingsWindow.Close();
        }
    }

    private static void VerifySearchClearButton(MainWindow? mainWindow)
    {
        if (mainWindow is null)
            throw new InvalidOperationException("MainWindow was unavailable for search clear smoke.");

        // Exercise the real page lifecycle. The previous smoke called the shared Attach
        // helper itself, which could manufacture a passing clear glyph even when the
        // actual Items/Hideout page never attached it for the user.
        mainWindow.ItemsPage.ApplyTemplate();
        mainWindow.HideoutPage.ApplyTemplate();
        mainWindow.ItemsPage.UpdateLayout();
        mainWindow.HideoutPage.UpdateLayout();

        VerifySearchClearButton(mainWindow.ItemsPage.SearchBox, "Items");
        VerifySearchClearButton(mainWindow.HideoutPage.SearchBox, "Hideout");
    }

    private static void VerifySearchClearButton(TextBox searchBox, string owner)
    {
        if (searchBox.Parent is not Grid parent)
            throw new InvalidOperationException($"{owner} search box parent is not the expected Grid.");

        var clearButtons = parent.Children
            .OfType<Button>()
            .Where(button => string.Equals(button.Content as string, "×", StringComparison.Ordinal))
            .ToArray();
        if (clearButtons.Length != 1)
        {
            throw new InvalidOperationException(
                $"{owner} search must have exactly one page-owned inline clear glyph, found {clearButtons.Length}.");
        }

        var clearButton = clearButtons[0];
        if (!string.Equals(clearButton.ToolTip as string, "검색어 지우기", StringComparison.Ordinal) ||
            clearButton.BorderThickness != new Thickness(0) ||
            clearButton.Background != System.Windows.Media.Brushes.Transparent)
        {
            throw new InvalidOperationException(
                $"{owner} search clear glyph did not use the shared product inline presentation.");
        }

        searchBox.Clear();
        searchBox.UpdateLayout();
        if (clearButton.Visibility != Visibility.Collapsed)
        {
            throw new InvalidOperationException(
                $"{owner} search clear glyph remained visible while the query was empty.");
        }

        searchBox.Text = "product-smoke";
        searchBox.UpdateLayout();
        if (clearButton.Visibility != Visibility.Visible)
        {
            throw new InvalidOperationException(
                $"{owner} search clear glyph was not shown after text entry.");
        }

        clearButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        if (searchBox.Text.Length != 0 || clearButton.Visibility != Visibility.Collapsed)
        {
            throw new InvalidOperationException(
                $"{owner} search clear glyph did not clear the query and return to the empty state.");
        }
    }

    private void VerifyMiniScannerSaveFeedback()
    {
        if (_coordinator is null)
            throw new InvalidOperationException("Scanner coordinator was unavailable for Mini Scanner feedback smoke.");

        var window = new MiniScannerWindow();
        try
        {
            window.ShowTransientStatus(
                ScannerCoordinator.CorrectionSaveCompletedStatus,
                _coordinator.Settings,
                hasItemContent: false);
            window.UpdateLayout();

            if (!string.Equals(
                    window.TransientStatusText.Text,
                    ScannerCoordinator.CorrectionSaveCompletedStatus,
                    StringComparison.Ordinal) ||
                window.TransientStatusBadge.Visibility != Visibility.Visible ||
                window.ItemContentPanel.Visibility != Visibility.Collapsed)
            {
                throw new InvalidOperationException(
                    "Mini Scanner did not render the correction-save confirmation as a status-only card.");
            }
        }
        finally
        {
            window.Close();
        }
    }
}
