using System.Diagnostics;
using System.Windows.Threading;

namespace JunhyunHelper.Desktop.Scanner;

/// <summary>
/// Independently probes the WPF dispatcher so Scanner backend latency and actual UI
/// thread starvation can be distinguished in the same diagnostic trace.
/// </summary>
internal sealed class ScannerUiResponsivenessMonitor : IDisposable
{
    private static readonly TimeSpan ProbeInterval = TimeSpan.FromMilliseconds(500);
    private static readonly TimeSpan StallThreshold = TimeSpan.FromMilliseconds(750);

    private readonly Dispatcher _dispatcher;
    private readonly CancellationTokenSource _cancellation = new();
    private readonly Task _monitorTask;
    private bool _disposed;

    public ScannerUiResponsivenessMonitor(Dispatcher dispatcher)
    {
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        _monitorTask = Task.Run(() => MonitorAsync(_cancellation.Token), CancellationToken.None);
    }

    private async Task MonitorAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(ProbeInterval, cancellationToken).ConfigureAwait(false);
                if (_dispatcher.HasShutdownStarted || _dispatcher.HasShutdownFinished)
                    return;

                var started = Stopwatch.GetTimestamp();
                var completion = new TaskCompletionSource(
                    TaskCreationOptions.RunContinuationsAsynchronously);
                try
                {
                    // Normal priority measures whether the ordinary application message
                    // pump is progressing. Background priority can be delayed by healthy
                    // input/render traffic and would over-report UI starvation.
                    _ = _dispatcher.BeginInvoke(
                        DispatcherPriority.Normal,
                        new Action(() => completion.TrySetResult()));
                }
                catch (InvalidOperationException)
                {
                    return;
                }

                var thresholdTask = Task.Delay(StallThreshold, cancellationToken);
                var first = await Task.WhenAny(completion.Task, thresholdTask).ConfigureAwait(false);
                var reportedPending = first != completion.Task;
                if (reportedPending)
                {
                    ScannerPerformanceTrace.Mark(
                        "ui-dispatcher-stall-pending",
                        ("elapsedMs", ScannerPerformanceTrace.ElapsedMilliseconds(started).ToString("F2", System.Globalization.CultureInfo.InvariantCulture)));
                }

                await completion.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
                var elapsedMs = ScannerPerformanceTrace.ElapsedMilliseconds(started);
                if (elapsedMs >= StallThreshold.TotalMilliseconds)
                {
                    ScannerPerformanceTrace.Mark(
                        "ui-dispatcher-stall-recovered",
                        ("elapsedMs", elapsedMs.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)),
                        ("pendingMarker", reportedPending));
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _cancellation.Cancel();
        _ = _monitorTask.ContinueWith(
            _ => _cancellation.Dispose(),
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
        GC.SuppressFinalize(this);
    }
}
