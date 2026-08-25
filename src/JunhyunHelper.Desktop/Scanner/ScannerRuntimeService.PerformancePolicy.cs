using System.Diagnostics;
using JunhyunHelper.Core.Scanner;

namespace JunhyunHelper.Desktop.Scanner;

/// <summary>
/// Continuous Scanner pacing policy for the v1.7.4 throughput-regression fix.
///
/// v1.7.3 changed the observation target from 350 ms to 200 ms. The platform
/// PeriodicTimer keeps wall-clock cadence, so when capture/detection overruns that
/// budget the next pending tick can be consumed immediately. In real Tarkov use that
/// can turn the Scanner into an almost back-to-back capture loop and starve the OCR /
/// semantic work that the faster cadence was intended to reach sooner.
///
/// Keep the 200 ms target when cycles are cheap, but never replay missed ticks. An
/// over-budget cycle yields briefly before the next observation. This preserves the
/// v1.7.3 adaptive semantic retry and direct BGRA OCR path while preventing capture
/// saturation. Recognition thresholds, candidate caps, and identity semantics are
/// unchanged.
/// </summary>
public sealed partial class ScannerRuntimeService
{
    private sealed class PeriodicTimer : IDisposable
    {
        private readonly TimeSpan _interval;
        private long _lastTickTimestamp;
        private int _disposed;

        public PeriodicTimer(TimeSpan interval)
        {
            if (interval <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(interval));

            _interval = interval;
            _lastTickTimestamp = Stopwatch.GetTimestamp();
        }

        public async ValueTask<bool> WaitForNextTickAsync(CancellationToken cancellationToken = default)
        {
            if (Volatile.Read(ref _disposed) != 0)
                return false;

            var elapsed = Stopwatch.GetElapsedTime(_lastTickTimestamp);
            var delay = ScannerObservationPacingPolicy.NextDelay(_interval, elapsed);

            await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            if (Volatile.Read(ref _disposed) != 0)
                return false;

            _lastTickTimestamp = Stopwatch.GetTimestamp();
            return true;
        }

        public void Dispose() => Interlocked.Exchange(ref _disposed, 1);
    }
}
