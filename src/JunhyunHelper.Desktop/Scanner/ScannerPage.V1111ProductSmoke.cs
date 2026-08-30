using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace JunhyunHelper.Desktop.Scanner;

public partial class ScannerPage
{
    private const string ProductSmokeEnvironmentVariable = "JUNHYUNHELPER_MAP_SMOKE";
    private const string ProductSmokeDiagnosticFileName = "junhyun-map-smoke-error.txt";
    private bool _v1111ProductSmokeScheduled;

    static ScannerPage()
    {
        EventManager.RegisterClassHandler(
            typeof(ScannerPage),
            LoadedEvent,
            new RoutedEventHandler(ScannerPage_V1111ProductSmokeLoaded),
            handledEventsToo: true);
    }

    private static void ScannerPage_V1111ProductSmokeLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is not ScannerPage page ||
            page._v1111ProductSmokeScheduled ||
            !string.Equals(
                Environment.GetEnvironmentVariable(ProductSmokeEnvironmentVariable),
                "1",
                StringComparison.Ordinal))
        {
            return;
        }

        page._v1111ProductSmokeScheduled = true;
        _ = page.Dispatcher.BeginInvoke(
            DispatcherPriority.ContextIdle,
            new Action(page.RunV1111ProductSmoke));
    }

    private void RunV1111ProductSmoke()
    {
        try
        {
            if (_coordinator is null)
                throw new InvalidOperationException("Scanner coordinator was unavailable for v1.11.1 product smoke.");

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

        VerifySearchClearButton(mainWindow.ItemsPage.SearchBox, "Items");
        VerifySearchClearButton(mainWindow.HideoutPage.SearchBox, "Hideout");
    }

    private static void VerifySearchClearButton(TextBox searchBox, string owner)
    {
        if (searchBox.Parent is not Grid parent)
            throw new InvalidOperationException($"{owner} search box parent is not the expected Grid.");

        var clearButton = parent.Children
            .OfType<Button>()
            .FirstOrDefault(button =>
                string.Equals(button.Content as string, "×", StringComparison.Ordinal) &&
                string.Equals(button.ToolTip as string, "검색어 지우기", StringComparison.Ordinal));
        if (clearButton is null)
            throw new InvalidOperationException($"{owner} search clear button was not rendered.");

        searchBox.Text = "v1111-smoke";
        clearButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        if (searchBox.Text.Length != 0)
            throw new InvalidOperationException($"{owner} search clear button did not clear the query.");
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
