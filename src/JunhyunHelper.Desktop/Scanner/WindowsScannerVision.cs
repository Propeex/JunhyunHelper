using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using JunhyunHelper.Core.Scanner;
using Windows.Globalization;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage.Streams;

namespace JunhyunHelper.Desktop.Scanner;

/// <summary>
/// Captures only visible pixels. It does not read Tarkov memory, inject code, or inspect
/// game process internals. Real mode targets EscapeFromTarkov's client window; display
/// test mode intentionally scans every monitor so archived screenshots can exercise the
/// same detector/OCR/matcher pipeline without launching the game.
/// </summary>
public sealed class WindowsScannerInspectDetector : IScannerInspectDetector
{
    private static readonly TimeSpan WindowDiscoveryInterval = TimeSpan.FromSeconds(1.5);
    private readonly object _gate = new();
    private IntPtr _cachedTarkovWindow;
    private DateTimeOffset _lastWindowDiscoveryUtc = DateTimeOffset.MinValue;
    private ScannerCaptureMode _captureMode = ScannerCaptureMode.TarkovWindow;
    private string _statusMessage = "Tarkov 게임 창을 찾는 중입니다. (Borderless 지원)";

    public bool IsAvailable => OperatingSystem.IsWindows();
    public string AvailabilityMessage => IsAvailable
        ? "Windows 화면 캡처를 사용할 수 있습니다."
        : "Scanner 화면 캡처는 Windows에서만 사용할 수 있습니다.";

    public string StatusMessage
    {
        get
        {
            lock (_gate)
                return _statusMessage;
        }
    }

    public ScannerCaptureMode CaptureMode
    {
        get
        {
            lock (_gate)
                return _captureMode;
        }
    }

    public void SetCaptureMode(ScannerCaptureMode mode)
    {
        lock (_gate)
        {
            _captureMode = mode;
            _statusMessage = mode == ScannerCaptureMode.DisplayTest
                ? "테스트 모드 · 전체 디스플레이에서 상세창을 찾는 중입니다."
                : "Tarkov 게임 창을 찾는 중입니다. (Borderless 지원)";
        }
    }

    public Task<ScannerInspectCandidate?> ObserveAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var mode = CaptureMode;
        return Task.FromResult(mode == ScannerCaptureMode.DisplayTest
            ? ObserveDisplays(cancellationToken)
            : ObserveTarkovWindow(cancellationToken));
    }

    private ScannerInspectCandidate? ObserveTarkovWindow(CancellationToken cancellationToken)
    {
        var window = GetTarkovWindow();
        if (window == IntPtr.Zero)
        {
            SetStatus("Tarkov 게임 창을 찾는 중입니다. (Borderless 지원)");
            return null;
        }

        if (IsIconic(window) || !IsWindowVisible(window) || !TryGetClientScreenRect(window, out var screenRect))
        {
            SetStatus("Tarkov 창이 최소화되었거나 캡처할 수 없는 상태입니다.");
            return null;
        }

        cancellationToken.ThrowIfCancellationRequested();
        using var bitmap = CaptureWindowClient(window, screenRect);
        if (bitmap is null)
        {
            SetStatus("Tarkov 창을 찾았지만 화면 픽셀을 캡처하지 못했습니다.");
            return null;
        }

        SetStatus("Tarkov 창 감지됨 · 아이템 상세창을 기다리는 중입니다.");
        return Detect(bitmap, screenRect.Left, screenRect.Top, extendedScaleSearch: false, "tarkov");
    }

    private ScannerInspectCandidate? ObserveDisplays(CancellationToken cancellationToken)
    {
        var monitors = EnumerateMonitors();
        if (monitors.Count == 0)
        {
            SetStatus("테스트 모드 · 감지할 디스플레이를 찾지 못했습니다.");
            return null;
        }

        SetStatus($"테스트 모드 · {monitors.Count}개 디스플레이에서 상세창을 찾는 중입니다.");
        foreach (var monitor in monitors)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (monitor.Width < 640 || monitor.Height < 360)
                continue;

            using var bitmap = CaptureScreenRectangle(monitor);
            var candidate = Detect(bitmap, monitor.Left, monitor.Top, extendedScaleSearch: true, $"display:{monitor.DeviceName}");
            if (candidate is not null)
            {
                SetStatus($"테스트 모드 · {monitor.DeviceName}에서 상세창 후보를 감지했습니다.");
                return candidate;
            }
        }

        return null;
    }

    private ScannerInspectCandidate? Detect(
        Bitmap bitmap,
        int screenLeft,
        int screenTop,
        bool extendedScaleSearch,
        string sourceKey)
    {
        var data = ReadBgra(bitmap);
        var region = ScannerDetailGeometryDetector.Detect(
            data.Pixels,
            bitmap.Width,
            bitmap.Height,
            data.Stride,
            extendedScaleSearch);
        if (region is null)
            return null;

        var title = ScannerDetailGeometryDetector.GetTitleRegion(region.Value);
        var titlePixels = CropBgra(data.Pixels, data.Stride, title, bitmap.Width, bitmap.Height, out var titleStride);
        if (titlePixels.Length == 0)
            return null;

        var titleImage = BitmapSource.Create(
            title.Width,
            title.Height,
            96,
            96,
            PixelFormats.Bgra32,
            null,
            titlePixels,
            titleStride);
        titleImage.Freeze();

        var geometrySignature = string.Create(
            System.Globalization.CultureInfo.InvariantCulture,
            $"{sourceKey}:{Quantize(region.Value.X)}:{Quantize(region.Value.Y)}:{Quantize(region.Value.Width)}:{Quantize(region.Value.Height)}");
        var titleSignature = $"{HashPixels(titlePixels):X16}";

        return new ScannerInspectCandidate(
            new Rect(
                screenLeft + region.Value.X,
                screenTop + region.Value.Y,
                region.Value.Width,
                region.Value.Height),
            geometrySignature,
            titleSignature,
            titleImage);
    }

    private IntPtr GetTarkovWindow()
    {
        lock (_gate)
        {
            if (_cachedTarkovWindow != IntPtr.Zero && IsWindow(_cachedTarkovWindow))
                return _cachedTarkovWindow;
            if (DateTimeOffset.UtcNow - _lastWindowDiscoveryUtc < WindowDiscoveryInterval)
                return IntPtr.Zero;
            _lastWindowDiscoveryUtc = DateTimeOffset.UtcNow;
        }

        IntPtr discovered = IntPtr.Zero;
        try
        {
            foreach (var process in Process.GetProcessesByName("EscapeFromTarkov"))
            {
                using (process)
                {
                    var handle = process.MainWindowHandle;
                    if (handle == IntPtr.Zero || !IsWindow(handle) || !IsWindowVisible(handle))
                        continue;
                    discovered = handle;
                    break;
                }
            }
        }
        catch (InvalidOperationException)
        {
        }
        catch (System.ComponentModel.Win32Exception)
        {
        }

        lock (_gate)
            _cachedTarkovWindow = discovered;
        return discovered;
    }

    private static Bitmap? CaptureWindowClient(IntPtr window, NativeRect screenRect)
    {
        var width = screenRect.Width;
        var height = screenRect.Height;
        if (width < 1 || height < 1)
            return null;

        // PrintWindow is attempted first because it asks Windows for the target window
        // instead of copying the composed desktop; this prevents our own overlays from
        // becoming scanner input when the game's rendering path supports it.
        var printWindowBitmap = new Bitmap(width, height, PixelFormat.Format32bppPArgb);
        try
        {
            using var graphics = Graphics.FromImage(printWindowBitmap);
            var hdc = graphics.GetHdc();
            bool printed;
            try
            {
                printed = PrintWindow(window, hdc, PwClientOnly | PwRenderFullContent);
            }
            finally
            {
                graphics.ReleaseHdc(hdc);
            }

            if (printed && HasVisualContent(printWindowBitmap))
                return printWindowBitmap;
        }
        catch (ExternalException)
        {
        }

        printWindowBitmap.Dispose();

        // Borderless Tarkov is normally visible at the client rectangle. If the DirectX
        // presentation path does not support PrintWindow, use the exact client-area
        // screen pixels as a fallback. Gate A live testing decides whether this fallback
        // is required on the user's current Tarkov build.
        try
        {
            return CaptureScreenRectangle(screenRect);
        }
        catch (ExternalException)
        {
            return null;
        }
    }

    private static Bitmap CaptureScreenRectangle(NativeRect rect)
    {
        var bitmap = new Bitmap(rect.Width, rect.Height, PixelFormat.Format32bppPArgb);
        try
        {
            using var graphics = Graphics.FromImage(bitmap);
            graphics.CopyFromScreen(
                rect.Left,
                rect.Top,
                0,
                0,
                new System.Drawing.Size(rect.Width, rect.Height),
                CopyPixelOperation.SourceCopy);
            return bitmap;
        }
        catch
        {
            bitmap.Dispose();
            throw;
        }
    }

    private static bool HasVisualContent(Bitmap bitmap)
    {
        var data = ReadBgra(bitmap);
        if (data.Pixels.Length == 0)
            return false;

        var stepX = Math.Max(1, bitmap.Width / 16);
        var stepY = Math.Max(1, bitmap.Height / 10);
        var min = 255;
        var max = 0;
        var nonBlack = 0;
        var count = 0;
        for (var y = 0; y < bitmap.Height; y += stepY)
        {
            for (var x = 0; x < bitmap.Width; x += stepX)
            {
                var offset = y * data.Stride + x * 4;
                var value = (data.Pixels[offset] + data.Pixels[offset + 1] + data.Pixels[offset + 2]) / 3;
                min = Math.Min(min, value);
                max = Math.Max(max, value);
                if (value > 5)
                    nonBlack++;
                count++;
            }
        }
        return count > 0 && nonBlack >= count / 12 && max - min >= 10;
    }

    private static (byte[] Pixels, int Stride) ReadBgra(Bitmap bitmap)
    {
        var rectangle = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
        var locked = bitmap.LockBits(rectangle, ImageLockMode.ReadOnly, PixelFormat.Format32bppPArgb);
        try
        {
            var sourceStride = Math.Abs(locked.Stride);
            var targetStride = bitmap.Width * 4;
            var source = new byte[sourceStride * bitmap.Height];
            Marshal.Copy(locked.Scan0, source, 0, source.Length);
            if (sourceStride == targetStride && locked.Stride > 0)
                return (source, targetStride);

            var target = new byte[targetStride * bitmap.Height];
            for (var row = 0; row < bitmap.Height; row++)
            {
                var sourceRow = locked.Stride > 0 ? row : bitmap.Height - 1 - row;
                Buffer.BlockCopy(source, sourceRow * sourceStride, target, row * targetStride, targetStride);
            }
            return (target, targetStride);
        }
        finally
        {
            bitmap.UnlockBits(locked);
        }
    }

    private static byte[] CropBgra(
        byte[] source,
        int sourceStride,
        ScannerDetectedRegion region,
        int sourceWidth,
        int sourceHeight,
        out int targetStride)
    {
        var left = Math.Clamp(region.X, 0, sourceWidth - 1);
        var top = Math.Clamp(region.Y, 0, sourceHeight - 1);
        var width = Math.Clamp(region.Width, 1, sourceWidth - left);
        var height = Math.Clamp(region.Height, 1, sourceHeight - top);
        targetStride = width * 4;
        var result = new byte[targetStride * height];
        for (var row = 0; row < height; row++)
        {
            Buffer.BlockCopy(
                source,
                (top + row) * sourceStride + left * 4,
                result,
                row * targetStride,
                targetStride);
        }
        return result;
    }

    private static ulong HashPixels(ReadOnlySpan<byte> pixels)
    {
        const ulong offset = 14695981039346656037;
        const ulong prime = 1099511628211;
        var hash = offset;
        foreach (var value in pixels)
        {
            hash ^= value;
            hash *= prime;
        }
        return hash;
    }

    private static int Quantize(int value) => (int)Math.Round(value / 4.0) * 4;

    private static bool TryGetClientScreenRect(IntPtr window, out NativeRect rect)
    {
        rect = default;
        if (!GetClientRect(window, out var client) || client.Right <= client.Left || client.Bottom <= client.Top)
            return false;

        var origin = new NativePoint { X = client.Left, Y = client.Top };
        if (!ClientToScreen(window, ref origin))
            return false;

        rect = new NativeRect
        {
            Left = origin.X,
            Top = origin.Y,
            Right = origin.X + client.Right - client.Left,
            Bottom = origin.Y + client.Bottom - client.Top,
        };
        return rect.Width >= 640 && rect.Height >= 360;
    }

    private static List<NativeRect> EnumerateMonitors()
    {
        var monitors = new List<NativeRect>();
        EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (monitor, _, _, _) =>
        {
            var info = new MonitorInfoEx { Size = Marshal.SizeOf<MonitorInfoEx>() };
            if (GetMonitorInfo(monitor, ref info))
            {
                var rect = info.Monitor;
                rect.DeviceName = string.IsNullOrWhiteSpace(info.DeviceName) ? "DISPLAY" : info.DeviceName;
                monitors.Add(rect);
            }
            return true;
        }, IntPtr.Zero);
        return monitors;
    }

    private void SetStatus(string message)
    {
        lock (_gate)
            _statusMessage = message;
    }

    private const uint PwClientOnly = 0x00000001;
    private const uint PwRenderFullContent = 0x00000002;

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

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PrintWindow(IntPtr handle, IntPtr hdc, uint flags);

    private delegate bool MonitorEnumProc(IntPtr monitor, IntPtr hdc, IntPtr rect, IntPtr data);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr clipRect, MonitorEnumProc callback, IntPtr data);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfoEx info);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MonitorInfoEx
    {
        public int Size;
        public NativeRect Monitor;
        public NativeRect WorkArea;
        public uint Flags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string DeviceName;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string? DeviceName;

        public readonly int Width => Right - Left;
        public readonly int Height => Bottom - Top;
    }
}

/// <summary>
/// Local Korean Windows OCR. Activation is deliberately fail-closed because some
/// Windows builds/package configurations do not expose Windows.Media.Ocr to an
/// unpackaged desktop process. Scanner capture and the rest of JunhyunHelper remain
/// usable even when this engine is unavailable.
/// </summary>
public sealed class WindowsScannerOcrEngine : IScannerOcrEngine
{
    private readonly OcrEngine? _engine;

    public WindowsScannerOcrEngine()
    {
        try
        {
            var language = new Language("ko-KR");
            if (!OcrEngine.IsLanguageSupported(language))
            {
                AvailabilityMessage = "Windows 한국어 OCR 언어 팩(ko-KR)을 사용할 수 없습니다.";
                return;
            }

            _engine = OcrEngine.TryCreateFromLanguage(language);
            AvailabilityMessage = _engine is null
                ? "Windows 한국어 OCR 엔진을 초기화하지 못했습니다."
                : "Windows 한국어 OCR 준비 완료";
        }
        catch (Exception exception)
        {
            AvailabilityMessage = $"Windows 한국어 OCR을 사용할 수 없습니다. ({exception.GetType().Name})";
        }
    }

    public bool IsAvailable => _engine is not null;
    public string AvailabilityMessage { get; }

    public async Task<string> ReadTextAsync(BitmapSource titleImage, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(titleImage);
        if (_engine is null)
            return string.Empty;

        cancellationToken.ThrowIfCancellationRequested();
        var variants = new List<string>(2);

        var original = await RecognizeAsync(titleImage, cancellationToken);
        if (!string.IsNullOrWhiteSpace(original))
            variants.Add(original);

        var maxScale = Math.Min(2.0, OcrEngine.MaxImageDimension / (double)Math.Max(titleImage.PixelWidth, titleImage.PixelHeight));
        if (maxScale >= 1.35)
        {
            var scaled = new TransformedBitmap(titleImage, new ScaleTransform(maxScale, maxScale));
            scaled.Freeze();
            var enlarged = await RecognizeAsync(scaled, cancellationToken);
            if (!string.IsNullOrWhiteSpace(enlarged) && !variants.Contains(enlarged, StringComparer.Ordinal))
                variants.Add(enlarged);
        }

        return string.Join(" | ", variants);
    }

    private async Task<string> RecognizeAsync(BitmapSource image, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(image));
        byte[] png;
        using (var memory = new MemoryStream())
        {
            encoder.Save(memory);
            png = memory.ToArray();
        }

        using var stream = new InMemoryRandomAccessStream();
        using (var writer = new DataWriter(stream.GetOutputStreamAt(0)))
        {
            writer.WriteBytes(png);
            await writer.StoreAsync();
            await writer.FlushAsync();
            writer.DetachStream();
        }

        stream.Seek(0);
        var decoder = await BitmapDecoder.CreateAsync(stream);
        using var softwareBitmap = await decoder.GetSoftwareBitmapAsync(
            BitmapPixelFormat.Bgra8,
            BitmapAlphaMode.Premultiplied);

        cancellationToken.ThrowIfCancellationRequested();
        var result = await _engine!.RecognizeAsync(softwareBitmap);
        cancellationToken.ThrowIfCancellationRequested();
        return result.Text?.Trim() ?? string.Empty;
    }
}
