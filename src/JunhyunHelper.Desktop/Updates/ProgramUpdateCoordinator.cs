using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using JunhyunHelper.Infrastructure.Updates;

namespace JunhyunHelper.Desktop.Updates;

internal sealed class ProgramUpdateCoordinator : IDisposable
{
    private readonly GitHubProgramUpdateClient _client = new();
    private int _started;

    public async Task CheckAtStartupAsync(Window owner)
    {
        ArgumentNullException.ThrowIfNull(owner);
        if (Interlocked.Exchange(ref _started, 1) != 0)
            return;

        ProgramUpdateRelease? release;
        try
        {
            release = await _client.GetLatestReleaseAsync(GetCurrentProductVersion());
        }
        catch (Exception exception)
        {
            App.WriteDiagnostic("Program update check failed", exception);
            return;
        }

        if (release is null || !owner.IsVisible)
            return;

        var consent = MessageBox.Show(
            owner,
            $"새 버전 {release.TagName}이 있습니다.\n\n지금 업데이트하시겠습니까?\n업데이트가 끝나면 준현 헬퍼가 자동으로 다시 실행됩니다.",
            "준현 헬퍼 업데이트",
            MessageBoxButton.YesNo,
            MessageBoxImage.Information,
            MessageBoxResult.No);

        if (consent != MessageBoxResult.Yes)
            return;

        var progressWindow = new ProgramUpdateProgressWindow
        {
            Owner = owner,
        };
        PreparedProgramUpdate? preparedUpdate = null;

        owner.IsEnabled = false;
        progressWindow.Show();

        try
        {
            var localApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var progress = new Progress<ProgramUpdateProgress>(progressWindow.UpdateProgress);
            preparedUpdate = await _client.PrepareUpdateAsync(
                release,
                localApplicationData,
                progress);

            progressWindow.UpdateProgress(new ProgramUpdateProgress("프로그램을 재시작하는 중...", 1));
            LaunchUpdater(preparedUpdate);
            progressWindow.CompleteAndClose();
            System.Windows.Application.Current.Shutdown(0);
        }
        catch (Exception exception)
        {
            if (preparedUpdate is not null)
                ProgramUpdateApplier.TryCleanupPreparedUpdate(preparedUpdate);

            App.WriteDiagnostic("Program update preparation failed", exception);
            progressWindow.CompleteAndClose();
            owner.IsEnabled = true;
            owner.Activate();

            MessageBox.Show(
                owner,
                "업데이트를 완료하지 못했습니다. 기존 버전은 변경되지 않았습니다.\n\n" +
                "인터넷 연결 또는 프로그램 폴더의 쓰기 권한을 확인한 뒤 다음 실행에서 다시 시도할 수 있습니다.",
                "준현 헬퍼 업데이트 실패",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    internal static void ScheduleStaleUpdaterRunnerCleanup()
    {
        var updaterRoot = Path.Combine(Path.GetTempPath(), "JunhyunHelper", "updater");
        if (!Directory.Exists(updaterRoot))
            return;

        _ = Task.Run(async () =>
        {
            for (var attempt = 0; attempt < 8; attempt++)
            {
                try
                {
                    foreach (var directory in Directory.EnumerateDirectories(updaterRoot))
                    {
                        try
                        {
                            Directory.Delete(directory, recursive: true);
                        }
                        catch
                        {
                            // A just-finished updater may still hold its self-copy open.
                        }
                    }

                    if (!Directory.EnumerateDirectories(updaterRoot).Any())
                    {
                        try
                        {
                            Directory.Delete(updaterRoot, recursive: false);
                        }
                        catch
                        {
                            // Parent cleanup is optional.
                        }

                        return;
                    }
                }
                catch
                {
                    return;
                }

                await Task.Delay(TimeSpan.FromMilliseconds(500)).ConfigureAwait(false);
            }
        });
    }

    private static Version GetCurrentProductVersion()
    {
        var version = Assembly.GetEntryAssembly()?.GetName().Version;
        if (version is null)
            return new Version(0, 0, 0);

        return new Version(
            Math.Max(0, version.Major),
            Math.Max(0, version.Minor),
            Math.Max(0, version.Build));
    }

    private static void LaunchUpdater(PreparedProgramUpdate preparedUpdate)
    {
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Program self-update is supported only on Windows.");

        var processPath = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(processPath) || !File.Exists(processPath))
            throw new InvalidOperationException("The current executable path could not be resolved.");

        if (!string.Equals(Path.GetFileName(processPath), "준현 헬퍼.exe", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Program self-update is available only from the packaged 준현 헬퍼.exe.");

        var targetDirectory = Path.GetDirectoryName(processPath)
            ?? throw new InvalidOperationException("The program directory could not be resolved.");
        var runnerDirectory = Path.Combine(
            Path.GetTempPath(),
            "JunhyunHelper",
            "updater",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(runnerDirectory);

        var runnerPath = Path.Combine(runnerDirectory, "준현 헬퍼 업데이트.exe");
        File.Copy(processPath, runnerPath, overwrite: false);
        using (var runnerStream = new FileStream(runnerPath, FileMode.Open, FileAccess.ReadWrite, FileShare.Read))
            runnerStream.Flush(flushToDisk: true);

        var startInfo = new ProcessStartInfo(runnerPath)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = runnerDirectory,
        };
        startInfo.ArgumentList.Add(ProgramUpdateCommandLine.ApplySwitch);
        startInfo.ArgumentList.Add("--parent-pid");
        startInfo.ArgumentList.Add(Environment.ProcessId.ToString(System.Globalization.CultureInfo.InvariantCulture));
        startInfo.ArgumentList.Add("--staging");
        startInfo.ArgumentList.Add(preparedUpdate.StagingDirectory);
        startInfo.ArgumentList.Add("--work");
        startInfo.ArgumentList.Add(preparedUpdate.WorkDirectory);
        startInfo.ArgumentList.Add("--target");
        startInfo.ArgumentList.Add(targetDirectory);
        startInfo.ArgumentList.Add("--restart");
        startInfo.ArgumentList.Add(processPath);
        startInfo.ArgumentList.Add("--version");
        startInfo.ArgumentList.Add(preparedUpdate.Release.Version.ToString(3));

        if (Process.Start(startInfo) is null)
            throw new InvalidOperationException("The update replacement process could not be started.");
    }

    public void Dispose() => _client.Dispose();
}

internal static class ProgramUpdateCommandLine
{
    internal const string ApplySwitch = "--apply-program-update";

    internal static bool IsApplyMode(IReadOnlyList<string> arguments) =>
        arguments.Any(argument => string.Equals(argument, ApplySwitch, StringComparison.Ordinal));

    internal static bool TryParseApplyRequest(
        IReadOnlyList<string> arguments,
        out ProgramUpdateApplyRequest? request,
        out string? workDirectory)
    {
        request = null;
        workDirectory = null;
        if (!IsApplyMode(arguments))
            return false;

        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        for (var index = 0; index < arguments.Count; index++)
        {
            var key = arguments[index];
            if (string.Equals(key, ApplySwitch, StringComparison.Ordinal))
                continue;

            if (!key.StartsWith("--", StringComparison.Ordinal) || index + 1 >= arguments.Count)
                return false;

            var value = arguments[++index];
            if (!values.TryAdd(key, value))
                return false;
        }

        if (!values.TryGetValue("--parent-pid", out var parentProcessText) ||
            !int.TryParse(parentProcessText, out var parentProcessId) ||
            !values.TryGetValue("--staging", out var stagingDirectory) ||
            !values.TryGetValue("--work", out workDirectory) ||
            !values.TryGetValue("--target", out var targetDirectory) ||
            !values.TryGetValue("--restart", out var restartExecutable) ||
            !values.TryGetValue("--version", out var versionText))
        {
            request = null;
            workDirectory = null;
            return false;
        }

        request = new ProgramUpdateApplyRequest(
            parentProcessId,
            stagingDirectory,
            targetDirectory,
            restartExecutable,
            versionText);
        return true;
    }
}

internal sealed class ProgramUpdateProgressWindow : Window
{
    private readonly TextBlock _statusText;
    private readonly ProgressBar _progressBar;
    private bool _allowClose;

    public ProgramUpdateProgressWindow()
    {
        Title = "준현 헬퍼 업데이트";
        Width = 430;
        Height = 155;
        ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ShowInTaskbar = false;
        Background = new SolidColorBrush(Color.FromRgb(28, 28, 28));
        Foreground = Brushes.White;

        var root = new Grid
        {
            Margin = new Thickness(24, 20, 24, 20),
        };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(16) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        _statusText = new TextBlock
        {
            Text = "업데이트를 준비하는 중...",
            FontSize = 14,
            TextWrapping = TextWrapping.Wrap,
        };
        Grid.SetRow(_statusText, 0);
        root.Children.Add(_statusText);

        _progressBar = new ProgressBar
        {
            Minimum = 0,
            Maximum = 100,
            Height = 18,
            IsIndeterminate = true,
        };
        Grid.SetRow(_progressBar, 2);
        root.Children.Add(_progressBar);

        Content = root;
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!_allowClose)
        {
            e.Cancel = true;
            return;
        }

        base.OnClosing(e);
    }

    public void CompleteAndClose()
    {
        _allowClose = true;
        Close();
    }

    public void UpdateProgress(ProgramUpdateProgress progress)
    {
        _statusText.Text = progress.Message;
        if (progress.Fraction is null)
        {
            _progressBar.IsIndeterminate = true;
            return;
        }

        _progressBar.IsIndeterminate = false;
        _progressBar.Value = Math.Clamp(progress.Fraction.Value, 0, 1) * 100;
    }
}
