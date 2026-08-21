using System.Windows.Media.Imaging;

namespace JunhyunHelper.Desktop.Scanner;

/// <summary>
/// Windows OCR is shared by title recognition and the inventory/stash context gate.
/// Keep access serialized so a second probe cannot race the active item recognition
/// pipeline or create a second OCR runtime solely for overlay visibility decisions.
/// </summary>
internal sealed class SerializedScannerOcrEngine : IScannerDeepOcrEngine
{
    private readonly IScannerOcrEngine _inner;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public SerializedScannerOcrEngine(IScannerOcrEngine inner)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    public bool IsAvailable => _inner.IsAvailable;
    public string AvailabilityMessage => _inner.AvailabilityMessage;

    public async Task<string> ReadTextAsync(BitmapSource titleImage, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(titleImage);
        await _gate.WaitAsync(cancellationToken);
        try
        {
            return await _inner.ReadTextAsync(titleImage, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<string> ReadDeepTextAsync(BitmapSource titleImage, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(titleImage);
        await _gate.WaitAsync(cancellationToken);
        try
        {
            return _inner is IScannerDeepOcrEngine deep
                ? await deep.ReadDeepTextAsync(titleImage, cancellationToken)
                : await _inner.ReadTextAsync(titleImage, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }
}
