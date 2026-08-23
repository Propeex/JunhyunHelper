using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using JunhyunHelper.Core.Scanner;
using Windows.Globalization;
using Windows.Media.Ocr;
using Windows.Storage.Streams;

namespace JunhyunHelper.Desktop.Scanner;

/// <summary>
/// Windows capture adapter for the restored Scanner Lab 3.8 detector. It keeps the
/// JunhyunHelper capture contract (Tarkov client only in real mode; all displays in
/// test mode) while restoring v3.8's ranked structural candidate set.
/// </summary>
public sealed class ScannerLab38InspectDetector : IScannerCandidateInspectDetector
{
    private const int CandidateLimit = 12;
    private static readonly TimeSpan WindowDiscoveryInterval = TimeSpan.FromSeconds(1.5);

    private readonly object _gate = new();
    private IntPtr _cachedTarkovWindow;
    private DateTimeOffset _lastWindowDiscoveryUtc = DateTimeOffset.MinValue;
    private ScannerCaptureMode _captureMode = ScannerCaptureMode.TarkovWindow;
    private string _statusMessage = "Tarkov 게임 창을 찾는 중입니다. (Borderless 지원)";

    public bool IsAvailable => OperatingSystem.IsWindows();
    public string AvailabilityMessage => IsAvailable
        ? "Scanner Lab 3.8 화면 감지 준비 완료"
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
                ? "테스트 모드 · 전체 디스플레이에서 상세창 후보를 찾는 중입니다."
                : "Tarkov 게임 창을 찾는 중입니다. (Borderless 지원)";
        }
    }

    public async Task<ScannerInspectCandidate?> ObserveAsync(CancellationToken cancellationToken)
    {
        var candidates = await ObserveCandidatesAsync(cancellationToken);
        return candidates.Count == 0 ? null : candidates[0];
    }

    public Task<IReadOnlyList<ScannerInspectCandidate>> ObserveCandidatesAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(CaptureMode == ScannerCaptureMode.DisplayTest
            ? ObserveDisplays(cancellationToken)
            : ObserveTarkovWindow(cancellationToken));
    }

    private IReadOnlyList<ScannerInspectCandidate> ObserveTarkovWindow(CancellationToken cancellationToken)
    {
        var window = GetTarkovWindow();
        if (window == IntPtr.Zero)
        {
            SetStatus("Tarkov 게임 창을 찾는 중입니다. (Borderless 지원)");
            return [];
        }

        if (IsIconic(window) || !IsWindowVisible(window) || !TryGetClientScreenRect(window, out var screenRect))
        {
            SetStatus("Tarkov 창이 최소화되었거나 캡처할 수 없는 상태입니다.");
            return [];
        }

        cancellationToken.ThrowIfCancellationRequested();
        using var bitmap = CaptureWindowClient(window, screenRect);
        if (bitmap is null)
        {
            SetStatus("Tarkov 창을 찾았지만 화면 픽셀을 캡처하지 못했습니다.");
            return [];
        }

        var candidates = DetectCandidates(bitmap, screenRect.Left, screenRect.Top, "tarkov");
        SetStatus(candidates.Count > 0
            ? $"Tarkov 창 감지됨 · 상세창 후보 {candidates.Count}개"
            : "Tarkov 창 감지됨 · 아이템 상세창을 기다리는 중입니다.");
        return candidates;
    }

    private IReadOnlyList<ScannerInspectCandidate> ObserveDisplays(CancellationToken cancellationToken)
    {
        var monitors = EnumerateMonitors();
        if (monitors.Count == 0)
        {
            SetStatus("테스트 모드 · 감지할 디스플레이를 찾지 못했습니다.");
            return [];
        }

        var all = new List<ScannerInspectCandidate>();
        foreach (var monitor in monitors)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (monitor.Bounds.Width < 150 || monitor.Bounds.Height < 110)
                continue;

            using var bitmap = CaptureScreenRectangle(monitor.Bounds);
            all.AddRange(DetectCandidates(
                bitmap,
                monitor.Bounds.Left,
                monitor.Bounds.Top,
                $"display:{monitor.DeviceName}"));
        }

        var result = all
            .OrderByDescending(candidate => candidate.StructuralScore)
            .Take(CandidateLimit)
            .ToArray();
        SetStatus(result.Length > 0
            ? $"테스트 모드 · 상세창 후보 {result.Length}개"
            : $"테스트 모드 · {monitors.Count}개 디스플레이에서 상세창을 찾는 중입니다.");
        return result;
    }

    private static IReadOnlyList<ScannerInspectCandidate> DetectCandidates(
        Bitmap bitmap,
        int screenLeft,
        int screenTop,
        string sourceKey)
    {
        var data = ReadBgra(bitmap);
        var structural = ScannerDetailGeometryDetector.FindCandidates(
            data.Pixels,
            bitmap.Width,
            bitmap.Height,
            data.Stride,
            CandidateLimit);
        if (structural.Count == 0)
        {
            PublishDebugCaptureIfNeeded(
                data.Pixels, data.Stride, bitmap.Width, bitmap.Height,
                screenLeft, screenTop, sourceKey, null);
            return [];
        }

        var result = new List<ScannerInspectCandidate>(structural.Count);
        foreach (var candidate in structural)
        {
            var anchors = ScannerTitleAnchorRefiner.Refine(
                data.Pixels, bitmap.Width, bitmap.Height, data.Stride, candidate);
            if (anchors.Reason != "HEADER_FRAME_LOCKED" ||
                anchors.Score < 0.68 ||
                anchors.Magnifier.Width <= 0 ||
                anchors.CloseButton.Width <= 0)
            {
                continue;
            }

            var lockedWindow = RefineLockedWindow(
                candidate.Window, anchors, bitmap.Width, bitmap.Height);
            var title = anchors.Title;
            var titlePixels = CropBgra(data.Pixels, data.Stride, title, bitmap.Width, bitmap.Height, out var titleStride);
            if (titlePixels.Length == 0)
                continue;

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

            var geometrySignature = $"{sourceKey}:{Quantize(lockedWindow.X)}:{Quantize(lockedWindow.Y)}:{Quantize(lockedWindow.Width)}:{Quantize(lockedWindow.Height)}";
            var titleSignature = $"{HashPixels(titlePixels):X16}";
            var windowBounds = ToScreenRect(lockedWindow, screenLeft, screenTop);
            var titleBounds = ToScreenRect(title, screenLeft, screenTop);
            Rect? magnifierBounds = anchors.Magnifier.Width > 0
                ? ToScreenRect(anchors.Magnifier, screenLeft, screenTop)
                : null;
            Rect? closeBounds = anchors.CloseButton.Width > 0
                ? ToScreenRect(anchors.CloseButton, screenLeft, screenTop)
                : null;

            result.Add(new ScannerInspectCandidate(
                windowBounds,
                geometrySignature,
                titleSignature,
                titleImage,
                candidate.Score,
                candidate.Reason,
                titleBounds,
                magnifierBounds,
                closeBounds,
                anchors.Score,
                anchors.Reason));
        }

        var ordered = result
            .OrderByDescending(candidate => candidate.StructuralScore)
            .Take(CandidateLimit)
            .ToArray();
        PublishDebugCaptureIfNeeded(
            data.Pixels, data.Stride, bitmap.Width, bitmap.Height,
            screenLeft, screenTop, sourceKey, ordered.FirstOrDefault());
        return ordered;
    }

    private static ScannerDetectedRegion RefineLockedWindow(
        ScannerDetectedRegion structural,
        ScannerTitleAnchorRefinement anchors,
        int width,
        int height)
    {
        var scale = Math.Clamp(anchors.CloseButton.Height / 17.0, 0.55, 1.85);
        var left = anchors.Magnifier.X - (int)Math.Round(12.0 * scale);
        var top = anchors.CloseButton.Y - (int)Math.Round(5.0 * scale);
        var right = anchors.CloseButton.X + anchors.CloseButton.Width + (int)Math.Round(4.0 * scale);

        left = Math.Clamp(left, 0, width - 2);
        top = Math.Clamp(top, 0, height - 2);
        right = Math.Clamp(right, left + 2, width);

        // Header lock gives authoritative top/left/right. Preserve the structural bottom
        // only because inspect-window height legitimately varies with item/stat panels.
        var structuralBottom = Math.Clamp(structural.Y + structural.Height, top + 80, height);
        return new ScannerDetectedRegion(
            left,
            top,
            right - left,
            structuralBottom - top,
            structural.Score);
    }

    private static void PublishDebugCaptureIfNeeded(
        byte[] pixels,
        int stride,
        int width,
        int height,
        int screenLeft,
        int screenTop,
        string sourceKey,
        ScannerInspectCandidate? candidate)
    {
        var signature = candidate?.TitleSignature;
        if (!ScannerRecognitionDebugStore.ShouldCapture(signature, candidate is not null))
            return;

        var image = BitmapSource.Create(
            width,
            height,
            96,
            96,
            PixelFormats.Bgra32,
            null,
            pixels,
            stride);
        image.Freeze();

        Rect? Local(Rect? value) => value is not { } rect
            ? null
            : new Rect(rect.X - screenLeft, rect.Y - screenTop, rect.Width, rect.Height);

        ScannerRecognitionDebugStore.PublishCapture(new ScannerRecognitionDebugFrame(
            image,
            screenLeft,
            screenTop,
            sourceKey,
            Local(candidate?.Bounds),
            Local(candidate?.TitleBounds),
            Local(candidate?.MagnifierBounds),
            Local(candidate?.CloseBounds),
            candidate?.StructuralScore ?? 0,
            candidate?.StructuralReason ?? "NO_DETAIL_CANDIDATE",
            candidate?.TitleAnchorScore ?? 0,
            candidate?.TitleAnchorReason ?? "NOT_RUN",
            candidate?.TitleSignature));
    }

    private static Rect ToScreenRect(ScannerDetectedRegion region, int screenLeft, int screenTop) =>
        new(screenLeft + region.X, screenTop + region.Y, region.Width, region.Height);

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

        var printWindowBitmap = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
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
        try
        {
            return CaptureScreenRectangle(screenRect);
        }
        catch (Exception exception) when (exception is ExternalException or System.ComponentModel.Win32Exception)
        {
            return null;
        }
    }

    private static Bitmap CaptureScreenRectangle(NativeRect rect)
    {
        var bitmap = new Bitmap(rect.Width, rect.Height, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
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
        // PrintWindow validation needs only a sparse sample. Read those pixels
        // directly from the locked bitmap instead of allocating/copying an entire
        // 1440p/4K framebuffer before DetectCandidates copies it once for real use.
        var rectangle = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
        var locked = bitmap.LockBits(
            rectangle,
            ImageLockMode.ReadOnly,
            System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
        try
        {
            var sourceStride = Math.Abs(locked.Stride);
            var stepX = Math.Max(1, bitmap.Width / 16);
            var stepY = Math.Max(1, bitmap.Height / 10);
            var min = 255;
            var max = 0;
            var nonBlack = 0;
            var count = 0;

            for (var y = 0; y < bitmap.Height; y += stepY)
            {
                var sourceRow = locked.Stride > 0 ? y : bitmap.Height - 1 - y;
                var row = IntPtr.Add(locked.Scan0, sourceRow * sourceStride);
                for (var x = 0; x < bitmap.Width; x += stepX)
                {
                    var pixel = IntPtr.Add(row, x * 4);
                    var b = Marshal.ReadByte(pixel);
                    var g = Marshal.ReadByte(pixel, 1);
                    var r = Marshal.ReadByte(pixel, 2);
                    var value = (b + g + r) / 3;
                    min = Math.Min(min, value);
                    max = Math.Max(max, value);
                    if (value > 5)
                        nonBlack++;
                    count++;
                }
            }

            return count > 0 && nonBlack >= count / 12 && max - min >= 10;
        }
        finally
        {
            bitmap.UnlockBits(locked);
        }
    }

    private static (byte[] Pixels, int Stride) ReadBgra(Bitmap bitmap)
    {
        var rectangle = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
        var locked = bitmap.LockBits(rectangle, ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
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
                System.Buffer.BlockCopy(source, sourceRow * sourceStride, target, row * targetStride, targetStride);
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
            System.Buffer.BlockCopy(
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
        return rect.Width >= 150 && rect.Height >= 110;
    }

    private static List<MonitorCaptureTarget> EnumerateMonitors()
    {
        var monitors = new List<MonitorCaptureTarget>();
        EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (monitor, _, _, _) =>
        {
            var info = new MonitorInfoEx { Size = Marshal.SizeOf<MonitorInfoEx>() };
            if (GetMonitorInfo(monitor, ref info))
            {
                monitors.Add(new MonitorCaptureTarget(
                    info.Monitor,
                    string.IsNullOrWhiteSpace(info.DeviceName) ? "DISPLAY" : info.DeviceName));
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

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
        public readonly int Width => Right - Left;
        public readonly int Height => Bottom - Top;
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

    private readonly record struct MonitorCaptureTarget(NativeRect Bounds, string DeviceName);
}

/// <summary>
/// Scanner Lab 3.8 OCR strategy on top of the current Windows ko-KR OCR backend.
/// </summary>
public sealed class ScannerLab38OcrEngine : IScannerDeepOcrEngine
{
    private readonly OcrEngine? _engine;

    public ScannerLab38OcrEngine()
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
                : "Scanner Lab 3.8 한국어 OCR 준비 완료";
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

        var enlarged = EnlargeTitle(titleImage);
        return await RecognizeLinesAndPairsAsync(enlarged, cancellationToken);
    }

    public async Task<string> ReadDeepTextAsync(BitmapSource titleImage, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(titleImage);
        if (_engine is null)
            return string.Empty;

        var enlarged = EnlargeTitle(titleImage);
        var variants = new[]
        {
            enlarged,
            CreateVariant(enlarged, 1),
            CreateVariant(enlarged, 2),
            CreateVariant(enlarged, 3),
        };

        var results = new List<string>();
        foreach (var variant in variants)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var text = await RecognizeLinesAndPairsAsync(variant, cancellationToken);
            if (!string.IsNullOrWhiteSpace(text))
                results.AddRange(text.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        }

        return string.Join(" | ", results.Distinct(StringComparer.Ordinal));
    }

    private static BitmapSource EnlargeTitle(BitmapSource source)
    {
        var requested = source.PixelHeight <= 14
            ? 8.0
            : source.PixelHeight <= 20
                ? 6.0
                : 4.0;
        var maximumDimension = Math.Max(source.PixelWidth, source.PixelHeight);
        var allowed = maximumDimension <= 0
            ? 1.0
            : Math.Max(1.0, Math.Floor(OcrEngine.MaxImageDimension / (double)maximumDimension));
        var scale = Math.Max(1.0, Math.Min(requested, allowed));
        if (scale <= 1.0)
            return source;

        var transformed = new TransformedBitmap(source, new ScaleTransform(scale, scale));
        transformed.Freeze();
        return transformed;
    }

    private static BitmapSource CreateVariant(BitmapSource source, int mode)
    {
        var converted = new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0);
        converted.Freeze();
        var stride = converted.PixelWidth * 4;
        var pixels = new byte[stride * converted.PixelHeight];
        converted.CopyPixels(pixels, stride, 0);

        for (var offset = 0; offset < pixels.Length; offset += 4)
        {
            var b = pixels[offset];
            var g = pixels[offset + 1];
            var r = pixels[offset + 2];
            var gray = (77 * r + 150 * g + 29 * b) >> 8;
            int output = mode switch
            {
                1 => Math.Clamp((int)((gray - 55) * 1.8), 0, 255),
                2 => gray >= 105 ? 255 : 0,
                3 => gray >= 105 ? 0 : 255,
                _ => gray,
            };
            pixels[offset] = (byte)output;
            pixels[offset + 1] = (byte)output;
            pixels[offset + 2] = (byte)output;
            pixels[offset + 3] = 255;
        }

        var result = BitmapSource.Create(
            converted.PixelWidth,
            converted.PixelHeight,
            96,
            96,
            PixelFormats.Bgra32,
            null,
            pixels,
            stride);
        result.Freeze();
        return result;
    }

    private async Task<string> RecognizeLinesAndPairsAsync(BitmapSource image, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var result = await RecognizeAsync(image, cancellationToken);
        var lines = result.Lines
            .Select(line => line.Text?.Trim() ?? string.Empty)
            .Where(line => line.Length >= 2)
            .ToArray();
        if (lines.Length == 0)
            return string.Empty;

        var candidates = new List<string>(lines.Length * 2);
        candidates.AddRange(lines);
        for (var index = 0; index < lines.Length - 1; index++)
        {
            var pair = $"{lines[index]} {lines[index + 1]}".Trim();
            if (pair.Length >= 3)
                candidates.Add(pair);
        }
        return string.Join(" | ", candidates.Distinct(StringComparer.Ordinal));
    }

    private async Task<OcrResult> RecognizeAsync(BitmapSource image, CancellationToken cancellationToken)
    {
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(image));
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
        var decoder = await Windows.Graphics.Imaging.BitmapDecoder.CreateAsync(stream);
        using var softwareBitmap = await decoder.GetSoftwareBitmapAsync(
            Windows.Graphics.Imaging.BitmapPixelFormat.Bgra8,
            Windows.Graphics.Imaging.BitmapAlphaMode.Premultiplied);

        cancellationToken.ThrowIfCancellationRequested();
        var result = await _engine!.RecognizeAsync(softwareBitmap);
        cancellationToken.ThrowIfCancellationRequested();
        return result;
    }
}
