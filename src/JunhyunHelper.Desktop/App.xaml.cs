using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Threading;
using JunhyunHelper.Desktop.Updates;
using JunhyunHelper.Infrastructure.Updates;

namespace JunhyunHelper.Desktop;

public partial class App : System.Windows.Application
{
    private ProgramUpdateCoordinator? _programUpdateCoordinator;

    protected override void OnStartup(StartupEventArgs e)
    {
        DispatcherUnhandledException += App_DispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        base.OnStartup(e);

        if (ProgramUpdateCommandLine.IsApplyMode(e.Args))
        {
            RunProgramUpdateApplyMode(e.Args);
            return;
        }

        try
        {
            ProgramUpdateCoordinator.ScheduleStaleUpdaterRunnerCleanup();

            var window = new MainWindow();
            MainWindow = window;
            window.Show();

            if (!IsProductSmokeRun())
            {
                _programUpdateCoordinator = new ProgramUpdateCoordinator();
                _ = _programUpdateCoordinator.CheckAtStartupAsync(window);
            }
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

    protected override void OnExit(ExitEventArgs e)
    {
        _programUpdateCoordinator?.Dispose();
        base.OnExit(e);
    }

    private void RunProgramUpdateApplyMode(IReadOnlyList<string> arguments)
    {
        if (!ProgramUpdateCommandLine.TryParseApplyRequest(arguments, out var request, out var workDirectory) ||
            request is null ||
            string.IsNullOrWhiteSpace(workDirectory))
        {
            WriteDiagnostic(
                "Program update apply arguments invalid",
                new InvalidDataException("The updater command line is incomplete or invalid."));
            MessageBox.Show(
                "업데이트 실행 정보를 확인하지 못했습니다. 기존 준현 헬퍼를 직접 다시 실행해 주세요.",
                "준현 헬퍼 업데이트 실패",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(2);
            return;
        }

        try
        {
            ProgramUpdateApplier.ApplyAsync(request).GetAwaiter().GetResult();
            TryDeleteDirectory(workDirectory);
            StartProduct(request.RestartExecutable, request.TargetDirectory);
            Shutdown(0);
        }
        catch (Exception exception)
        {
            WriteDiagnostic("Program update apply failed", exception);
            TryDeleteDirectory(workDirectory);

            MessageBox.Show(
                "프로그램 업데이트에 실패했습니다.\n\n기존 프로그램 파일을 가능한 범위에서 복구했으며 준현 헬퍼를 다시 실행합니다.\n\n" +
                "오류 기록: %LocalAppData%\\JunhyunHelper\\logs\\startup.log",
                "준현 헬퍼 업데이트 실패",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            if (!IsProcessRunning(request.ParentProcessId) && File.Exists(request.RestartExecutable))
            {
                try
                {
                    StartProduct(request.RestartExecutable, request.TargetDirectory);
                }
                catch (Exception restartException)
                {
                    WriteDiagnostic("Program update recovery restart failed", restartException);
                }
            }

            Shutdown(2);
        }
    }

    private static bool IsProductSmokeRun() =>
        string.Equals(
            Environment.GetEnvironmentVariable("JUNHYUNHELPER_MAP_SMOKE"),
            "1",
            StringComparison.Ordinal);

    private static void StartProduct(string executablePath, string workingDirectory)
    {
        var startInfo = new ProcessStartInfo(executablePath)
        {
            UseShellExecute = true,
            WorkingDirectory = workingDirectory,
        };

        if (Process.Start(startInfo) is null)
            throw new InvalidOperationException("준현 헬퍼를 다시 실행하지 못했습니다.");
    }

    private static bool IsProcessRunning(int processId)
    {
        if (processId <= 0)
            return false;

        try
        {
            using var process = Process.GetProcessById(processId);
            return !process.HasExited;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
        catch
        {
            // Update cleanup is best-effort and must not replace the update result.
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
