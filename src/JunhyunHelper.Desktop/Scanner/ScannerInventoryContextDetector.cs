using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace JunhyunHelper.Desktop.Scanner;

/// <summary>
/// Determines whether the foreground Tarkov client is showing the character inventory
/// surface used both in raid and in the out-of-raid stash. It uses only display pixels
/// and the existing Korean OCR engine; no process memory, injection or packet access.
/// </summary>
internal sealed class ScannerInventoryContextDetector
{
    private static readonly TimeSpan ProbeCacheDuration = TimeSpan.FromMilliseconds(850);
    private static readonly string[] InventoryAnchors =
    [
        "장비",
        "건강상태",
        "건강 상태",
        "스킬",
        "지도",
        "종합정보",
        "종합 정보",
    ];

    private readonly IScannerOcrEngine _ocr;
    private readonly SemaphoreSlim _probeGate = new(1, 1);
    private DateTimeOffset _validUntilUtc = DateTimeOffset.MinValue;
    private bool _cachedResult;

    public ScannerInventoryContextDetector(IScannerOcrEngine ocr)
    {
        _ocr = ocr ?? throw new ArgumentNullException(nameof(ocr));
    }

    public async Task<bool> IsInventoryOrStashAsync(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        if (now < _validUntilUtc)
            return _cachedResult;

        await _probeGate.WaitAsync(cancellationToken);
        try
        {
            now = DateTimeOffset.UtcNow;
            if (now < _validUntilUtc)
                return _cachedResult;

            var result = false;
            string text = string.Empty;
            try
            {
                var image = CaptureForegroundTarkovHeader();
                if (image is not null)
                {
                    text = await _ocr.ReadTextAsync(image, cancellationToken);
                    result = HasInventoryAnchors(text);
                    if (!result && _ocr is IScannerDeepOcrEngine deepOcr)
                    {
                        var deep = await deepOcr.ReadDeepTextAsync(image, cancellationToken);
                        if (!string.IsNullOrWhiteSpace(deep))
                        {
                            text = string.IsNullOrWhiteSpace(text) ? deep : $"{text} | {deep}";
                            result = HasInventoryAnchors(text);
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception) when (
                exception is ExternalException or
                System.ComponentModel.Win32Exception or
                InvalidOperationException)
            {
                result = false;
            }

            _cachedResult = result;
            _validUntilUtc = DateTimeOffset.UtcNow + ProbeCacheDuration;
            ScannerDiagnosticLog.Write(
                "inventory-context",
                ScannerCaptureMode.TarkovWindow,
                ("allowed", result),
                ("anchors", CountInventoryAnchors(text)));
            return result;
        }
        finally
        {
            _probeGate.Release();
        }
    }

    internal static bool HasInventoryAnchors(string? text) => CountInventoryAnchors(text) >= 2;

    internal static int CountInventoryAnchors(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0;

        var normalized = text.Replace("|", " ", StringComparison.Ordinal)
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal);

        var matched = new HashSet<string>(StringComparer.Ordinal);
        foreach (var anchor in InventoryAnchors)
        {
            if (!normalized.Contains(anchor, StringComparison.Ordinal))
                continue;

            // Treat spaced/unspaced variants as one semantic anchor.
            matched.Add(anchor.Replace(" ", string.Empty, StringComparison.Ordinal));
        }
        return matched.Count;
    }

    private static BitmapSource? CaptureForegroundTarkovHeader()
    {
        var tarkovWindow = FindTarkovWindow();
        if (tarkovWindow == IntPtr.Zero || GetForegroundWindow() != tarkovWindow)
            return null;
        if (IsIconic(tarkovWindow) || !IsWindowVisible(tarkovWindow))
            return null;
        if (!GetClientRect(tarkovWindow, out var client) || client.Right <= 0 || client.Bottom <= 0)
            return null;

        var origin = new NativePoint();
        if (!ClientToScreen(tarkovWindow, ref origin))
            return null;

        var width = client.Right - client.Left;
        var clientHeight = client.Bottom - client.Top;
        if (width < 640 || clientHeight < 360)
            return null;

        // Character/inventory navigation remains in the top band while inspect windows
        // occupy the center. Keep enough height for Korean glyph OCR without scanning the
        // entire frame.
        var height = Math.Clamp(clientHeight / 7, 90, 190);
        using var bitmap = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.CopyFromScreen(
                origin.X,
                origin.Y,
                0,
                0,
                new System.Drawing.Size(width, height),
                CopyPixelOperation.SourceCopy);
        }

        return ToBitmapSource(bitmap);
    }

    private static IntPtr FindTarkovWindow()
    {
        try
        {
            foreach (var process in Process.GetProcessesByName("EscapeFromTarkov"))
            {
                using (process)
                {
                    var handle = process.MainWindowHandle;
                    if (handle != IntPtr.Zero && IsWindow(handle) && IsWindowVisible(handle))
                        return handle;
                }
            }
        }
        catch (InvalidOperationException)
        {
        }
        catch (System.ComponentModel.Win32Exception)
        {
        }
        return IntPtr.Zero;
    }

    private static BitmapSource ToBitmapSource(Bitmap bitmap)
    {
        var rectangle = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
        var locked = bitmap.LockBits(
            rectangle,
            ImageLockMode.ReadOnly,
            System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
        try
        {
            var sourceStride = Math.Abs(locked.Stride);
            var targetStride = bitmap.Width * 4;
            var source = new byte[sourceStride * bitmap.Height];
            Marshal.Copy(locked.Scan0, source, 0, source.Length);
            var pixels = new byte[targetStride * bitmap.Height];
            for (var row = 0; row < bitmap.Height; row++)
            {
                var sourceRow = locked.Stride > 0 ? row : bitmap.Height - 1 - row;
                Buffer.BlockCopy(source, sourceRow * sourceStride, pixels, row * targetStride, targetStride);
            }

            var result = BitmapSource.Create(
                bitmap.Width,
                bitmap.Height,
                96,
                96,
                PixelFormats.Bgra32,
                null,
                pixels,
                targetStride);
            result.Freeze();
            return result;
        }
        finally
        {
            bitmap.UnlockBits(locked);
        }
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindow(IntPtr handle);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowVisible(IntPtr handle);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsIconic(IntPtr handle);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetClientRect(IntPtr handle, out NativeRect rect);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ClientToScreen(IntPtr handle, ref NativePoint point);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }
}
