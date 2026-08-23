namespace JunhyunHelper.Desktop.Scanner;

/// <summary>
/// Serializes top-level Scanner coordinator operations that can start, stop, suspend,
/// or otherwise replace shared runtime/catalog/presentation state. One-shot scanning
/// uses the non-blocking entry point so duplicate hotkey/button requests remain a no-op,
/// while explicit user mode/context/catalog transitions wait for the current operation
/// to quiesce before mutating the same state.
/// </summary>
internal sealed class ScannerRuntimeTransitionGate
{
    private readonly SemaphoreSlim _gate = new(1, 1);

    public async ValueTask<IDisposable> EnterAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        return new Lease(_gate);
    }

    public async ValueTask<IDisposable?> TryEnterAsync(CancellationToken cancellationToken = default)
    {
        if (!await _gate.WaitAsync(0, cancellationToken))
            return null;
        return new Lease(_gate);
    }

    private sealed class Lease : IDisposable
    {
        private SemaphoreSlim? _gate;

        public Lease(SemaphoreSlim gate)
        {
            _gate = gate;
        }

        public void Dispose()
        {
            Interlocked.Exchange(ref _gate, null)?.Release();
        }
    }
}
