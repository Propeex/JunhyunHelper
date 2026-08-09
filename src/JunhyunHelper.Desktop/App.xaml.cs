using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Threading;

namespace JunhyunHelper.Desktop;

public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        DispatcherUnhandledException += App_DispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        base.OnStartup(e);

        try
        {
            var window = new MainWindow();
            MainWindow = window;
            window.Show();
        }
        catch (Exception exception)
        {
            WriteDiagnostic("Application startup failed", exception);
            MessageBox.Show(
                "준현 헬퍼를 시작하지 못했습니다.\n\n" +
                exception.Message +
                "\n\n오류 기록: %LocalAppData%\\JunhyunHelper\\logs\\startup.log",
                "준현 헬퍼 시작 오류",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    internal static void WriteDiagnostic(string context, Exception exception)
    {
        try
        {
            var root = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "JunhyunHelper",
                "logs");
            Directory.CreateDirectory(root);
            var path = Path.Combine(root, "startup.log");
            var entry = new StringBuilder()
                .AppendLine("============================================================")
                .AppendLine($"UTC: {DateTimeOffset.UtcNow:O}")
                .AppendLine($"Context: {context}")
                .AppendLine($"OS: {Environment.OSVersion}")
                .AppendLine($"Runtime: {Environment.Version}")
                .AppendLine($"BaseDirectory: {AppContext.BaseDirectory}")
                .AppendLine($"ProcessPath: {Environment.ProcessPath ?? "<unknown>"}")
                .AppendLine(exception.ToString())
                .AppendLine()
                .ToString();
            File.AppendAllText(path, entry, Encoding.UTF8);
        }
        catch
        {
            // Diagnostics must never replace the original failure.
        }
    }

    private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        WriteDiagnostic("Unhandled dispatcher exception", e.Exception);
        e.Handled = true;
        MessageBox.Show(
            "준현 헬퍼에서 처리하지 못한 오류가 발생했습니다.\n\n" +
            e.Exception.Message +
            "\n\n오류 기록: %LocalAppData%\\JunhyunHelper\\logs\\startup.log",
            "준현 헬퍼 오류",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        Shutdown(1);
    }

    private static void CurrentDomain_UnhandledException(object? sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception exception)
            WriteDiagnostic("Unhandled AppDomain exception", exception);
        else
            WriteDiagnostic("Unhandled AppDomain exception", new InvalidOperationException(e.ExceptionObject?.ToString() ?? "Unknown fatal error"));
    }
}
