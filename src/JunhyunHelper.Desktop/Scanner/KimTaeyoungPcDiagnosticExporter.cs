using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace JunhyunHelper.Desktop.Scanner;

/// <summary>
/// Local, opt-in support bundle for the known Kim Taeyoung capture/display problem.
/// The bundle is intentionally broad across display/GPU/capture facts, but excludes
/// account identity, machine/user names, network identifiers, environment-variable
/// dumps, arbitrary process inventories, and application file paths.
/// </summary>
internal static class KimTaeyoungPcDiagnosticExporter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
    };

    private static readonly string[] RelevantProcessNames =
    [
        "Discord",
        "obs64",
        "obs32",
        "NVIDIA Share",
        "NVIDIA Overlay",
        "nvcontainer",
        "RadeonSoftware",
        "AMDRSServ",
        "RTSS",
        "MSIAfterburner",
        "GameBar",
        "GameBarFTServer",
        "XboxGameBar",
        "SteelSeriesGG",
        "SteelSeriesEngine",
        "Medal",
        "Overwolf",
        "LosslessScaling",
        "EscapeFromTarkov",
    ];

    public static Task<string> ExportAsync(
        ScannerCoordinator scanner,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(scanner);

        // Capture coordinator-owned values before moving the expensive probes off the
        // UI thread. None of the bundle probes changes Scanner settings or game state.
        var scannerSnapshot = new ScannerSnapshot(
            scanner.Settings,
            scanner.Status.ToString(),
            scanner.ActiveCaptureMode?.ToString(),
            scanner.TestEnabled,
            scanner.CatalogCount,
            scanner.CatalogMode?.ToString(),
            scanner.CatalogGeneratedAtUtc);

        return Task.Run(
            () => ExportCore(scannerSnapshot, cancellationToken),
            cancellationToken);
    }

    private static string ExportCore(
        ScannerSnapshot scannerSnapshot,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        if (string.IsNullOrWhiteSpace(desktop))
            desktop = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Desktop");
        Directory.CreateDirectory(desktop);

        var timestamp = DateTimeOffset.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
        var destinationPath = Path.Combine(
            desktop,
            $"JunhyunHelper-KimTaeyoung-Diagnostic-{timestamp}.zip");
        if (File.Exists(destinationPath))
            File.Delete(destinationPath);

        var temporaryRoot = Path.Combine(
            Path.GetTempPath(),
            $"JunhyunHelper-KimTaeyoung-Diagnostic-{Guid.NewGuid():N}");
        Directory.CreateDirectory(temporaryRoot);

        var probeErrors = new List<string>();
        try
        {
            var supportBundlePath = Path.Combine(temporaryRoot, "scanner-support.zip");
            TryProbe("scanner-support", probeErrors, () => ScannerSupportBundleExporter.Export(supportBundlePath));

            cancellationToken.ThrowIfCancellationRequested();
            var environment = BuildEnvironmentReport(probeErrors);
            var relevantProcesses = BuildRelevantProcessReport(probeErrors);
            var powershellReport = RunPowerShellDisplayProbe(probeErrors, cancellationToken);
            var dxDiagReport = RunDxDiagDisplayProbe(temporaryRoot, probeErrors, cancellationToken);

            using var archive = ZipFile.Open(destinationPath, ZipArchiveMode.Create);
            AddText(archive, "README.txt", BuildReadme());
            AddText(archive, "environment.txt", environment);
            AddText(archive, "relevant-processes.txt", relevantProcesses);
            AddText(archive, "display-gpu-powershell.json", powershellReport);
            AddText(archive, "dxdiag-display.txt", dxDiagReport);
            AddText(archive, "scanner-state.json", JsonSerializer.Serialize(scannerSnapshot, JsonOptions));

            CaptureDisplayEvidence(archive, probeErrors, cancellationToken);
            CaptureTarkovEvidence(archive, probeErrors, cancellationToken);

            if (File.Exists(supportBundlePath))
                AddFile(archive, supportBundlePath, "scanner/scanner-support.zip");

            AddText(
                archive,
                "probe-errors.txt",
                probeErrors.Count == 0
                    ? "none\r\n"
                    : string.Join(Environment.NewLine, probeErrors) + Environment.NewLine);
        }
        catch
        {
            try
            {
                if (File.Exists(destinationPath))
                    File.Delete(destinationPath);
            }
            catch
            {
            }
            throw;
        }
        finally
        {
            try
            {
                Directory.Delete(temporaryRoot, recursive: true);
            }
            catch
            {
            }
        }

        return destinationPath;
    }

    private static string BuildReadme() =>
        "준현 헬퍼 · 김태영 PC Scanner/화면 캡처 진단 자료\r\n" +
        "\r\n" +
        "목적: 모니터에서 직접 보는 화면은 정상인데 Discord 방송/캡처 및 Scanner 입력이 비정상적으로 밝아지는 원인을 분석합니다.\r\n" +
        "이 ZIP은 사용자가 명시적으로 진단을 실행한 경우에만 로컬 바탕화면에 생성됩니다. 자동 전송되지 않습니다.\r\n" +
        "\r\n" +
        "포함: Windows/런타임, 디스플레이/DPI/HDR 관련 공개 시스템 상태, GPU/드라이버, Scanner 설정/상태, 관련 캡처·오버레이 앱 존재 여부, 화면 캡처 증거와 밝기 통계, Scanner 지원 로그.\r\n" +
        "제외: Windows 사용자 이름, 컴퓨터 이름, IP/MAC, 네트워크 목록, 토큰/비밀번호, 임의의 전체 프로세스 목록, 프로그램 설치 경로.\r\n" +
        "화면 캡처 PNG에는 진단 당시 화면에 실제로 보이는 내용이 포함될 수 있습니다.\r\n";

    private static string BuildEnvironmentReport(List<string> errors)
    {
        var builder = new StringBuilder();
        var assembly = typeof(KimTaeyoungPcDiagnosticExporter).Assembly;
        var version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? assembly.GetName().Version?.ToString()
            ?? "unknown";

        builder.AppendLine("# Junhyun Helper Kim Taeyoung PC diagnostic")
            .AppendLine($"ExportUtc={DateTimeOffset.UtcNow:O}")
            .AppendLine($"ProductVersion={version}")
            .AppendLine($"OSDescription={RuntimeInformation.OSDescription}")
            .AppendLine($"OSVersion={Environment.OSVersion.Version}")
            .AppendLine($"OSArchitecture={RuntimeInformation.OSArchitecture}")
            .AppendLine($"ProcessArchitecture={RuntimeInformation.ProcessArchitecture}")
            .AppendLine($"RuntimeVersion={Environment.Version}")
            .AppendLine($"Is64BitOperatingSystem={Environment.Is64BitOperatingSystem}")
            .AppendLine($"Is64BitProcess={Environment.Is64BitProcess}")
            .AppendLine($"ProcessorCount={Environment.ProcessorCount}");

        TryProbe("screen-enumeration", errors, () =>
        {
            var screens = System.Windows.Forms.Screen.AllScreens;
            builder.AppendLine($"DisplayCount={screens.Length}");
            for (var index = 0; index < screens.Length; index++)
            {
                var screen = screens[index];
                builder.AppendLine(
                    $"Display[{index}].Device={screen.DeviceName};Bounds={screen.Bounds.X},{screen.Bounds.Y},{screen.Bounds.Width},{screen.Bounds.Height};WorkingArea={screen.WorkingArea.X},{screen.WorkingArea.Y},{screen.WorkingArea.Width},{screen.WorkingArea.Height};Primary={screen.Primary};BitsPerPixel={screen.BitsPerPixel}");
            }
        });

        TryProbe("system-metrics", errors, () =>
        {
            builder.AppendLine($"VirtualScreen={GetSystemMetrics(76)},{GetSystemMetrics(77)},{GetSystemMetrics(78)},{GetSystemMetrics(79)}")
                .AppendLine($"RemoteSession={GetSystemMetrics(0x1000) != 0}")
                .AppendLine($"SystemDpi={GetDpiForSystem()}");
        });

        return builder.ToString();
    }

    private static string BuildRelevantProcessReport(List<string> errors)
    {
        var builder = new StringBuilder("# Allowlisted capture/overlay/game processes only\r\n");
        foreach (var processName in RelevantProcessNames)
        {
            TryProbe($"process:{processName}", errors, () =>
            {
                var processes = Process.GetProcessesByName(processName);
                try
                {
                    foreach (var process in processes)
                    {
                        using (process)
                        {
                            var version = "unknown";
                            try
                            {
                                version = process.MainModule?.FileVersionInfo.FileVersion ?? "unknown";
                            }
                            catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception or NotSupportedException)
                            {
                            }

                            builder.AppendLine($"{processName};Running=true;Version={Sanitize(version)}");
                        }
                    }
                }
                finally
                {
                    foreach (var process in processes)
                        process.Dispose();
                }
            });
        }
        return builder.ToString();
    }

    private static string RunPowerShellDisplayProbe(
        List<string> errors,
        CancellationToken cancellationToken)
    {
        const string script = "$ErrorActionPreference='SilentlyContinue';" +
            "$os=Get-CimInstance Win32_OperatingSystem|Select-Object Caption,Version,BuildNumber,OSArchitecture;" +
            "$gpu=@(Get-CimInstance Win32_VideoController|Select-Object Name,DriverVersion,DriverDate,VideoModeDescription,CurrentHorizontalResolution,CurrentVerticalResolution,CurrentRefreshRate,CurrentBitsPerPixel,AdapterRAM,Status);" +
            "$mon=@(Get-CimInstance Win32_DesktopMonitor|Select-Object Name,MonitorType,ScreenWidth,ScreenHeight,Status);" +
            "$pnp=@(Get-PnpDevice -Class Monitor|Select-Object FriendlyName,Status,Class);" +
            "[pscustomobject]@{Windows=$os;VideoControllers=$gpu;DesktopMonitors=$mon;PnpMonitors=$pnp}|ConvertTo-Json -Depth 6";

        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                },
            };
            process.StartInfo.ArgumentList.Add("-NoProfile");
            process.StartInfo.ArgumentList.Add("-NonInteractive");
            process.StartInfo.ArgumentList.Add("-ExecutionPolicy");
            process.StartInfo.ArgumentList.Add("Bypass");
            process.StartInfo.ArgumentList.Add("-Command");
            process.StartInfo.ArgumentList.Add(script);
            if (!process.Start())
                return "{\"error\":\"powershell-start-failed\"}";

            var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);
            if (!process.WaitForExit(15000))
            {
                process.Kill(entireProcessTree: true);
                errors.Add("powershell-display: timeout");
                return "{\"error\":\"timeout\"}";
            }

            var output = outputTask.GetAwaiter().GetResult();
            var error = errorTask.GetAwaiter().GetResult();
            if (!string.IsNullOrWhiteSpace(error))
                errors.Add($"powershell-display: {Sanitize(error)}");
            return string.IsNullOrWhiteSpace(output)
                ? "{\"error\":\"no-output\"}"
                : output;
        }
        catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception or IOException or UnauthorizedAccessException)
        {
            errors.Add($"powershell-display: {exception.GetType().Name}:{Sanitize(exception.Message)}");
            return $"{{\"error\":\"{exception.GetType().Name}\"}}";
        }
    }

    private static string RunDxDiagDisplayProbe(
        string temporaryRoot,
        List<string> errors,
        CancellationToken cancellationToken)
    {
        var dxPath = Path.Combine(temporaryRoot, "dxdiag.txt");
        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "dxdiag.exe",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                },
            };
            process.StartInfo.ArgumentList.Add("/dontskip");
            process.StartInfo.ArgumentList.Add("/whql:off");
            process.StartInfo.ArgumentList.Add("/t");
            process.StartInfo.ArgumentList.Add(dxPath);
            if (!process.Start())
                return "dxdiag-start-failed\r\n";

            var deadline = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(25);
            while (!process.HasExited && DateTimeOffset.UtcNow < deadline)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Thread.Sleep(100);
            }
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                errors.Add("dxdiag: timeout");
                return "dxdiag-timeout\r\n";
            }

            for (var retry = 0; retry < 20 && !File.Exists(dxPath); retry++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Thread.Sleep(100);
            }
            if (!File.Exists(dxPath))
                return "dxdiag-output-missing\r\n";

            var allowedKeys = new[]
            {
                "Operating System:", "Language:", "System Manufacturer:", "System Model:",
                "BIOS:", "Processor:", "Memory:", "Available OS Memory:", "Page File:",
                "DirectX Version:", "Miracast:", "Microsoft Graphics Hybrid:",
                "Card name:", "Manufacturer:", "Chip type:", "DAC type:", "Device Type:",
                "Device Key:", "Device Status:", "Device Problem Code:",
                "Display Memory:", "Dedicated Memory:", "Shared Memory:", "Current Mode:",
                "HDR Support:", "Display Color Space:", "Color Primaries:", "Display Luminance:",
                "Monitor Name:", "Monitor Model:", "Monitor Id:", "Native Mode:", "Output Type:",
                "Driver Name:", "Driver File Version:", "Driver Version:", "Driver Date/Size:",
                "Driver Model:", "Driver Attributes:", "Driver Strong Name:",
                "Rank Of Driver:", "Video Accel:", "DXVA2 Modes:", "D3D12 Encode Modes:",
                "D3D12 Decode Modes:", "D3D12 Video Processing Caps:",
                "Hybrid Graphics GPU:", "Power P-states:", "Virtualization:", "Block List:",
            };

            var builder = new StringBuilder("# Sanitized dxdiag display/graphics fields\r\n");
            foreach (var rawLine in File.ReadLines(dxPath))
            {
                var line = rawLine.Trim();
                if (allowedKeys.Any(key => line.StartsWith(key, StringComparison.OrdinalIgnoreCase)))
                    builder.AppendLine(line);
            }
            return builder.ToString();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception or IOException or UnauthorizedAccessException)
        {
            errors.Add($"dxdiag: {exception.GetType().Name}:{Sanitize(exception.Message)}");
            return $"dxdiag-error={exception.GetType().Name}\r\n";
        }
    }

    private static void CaptureDisplayEvidence(
        ZipArchive archive,
        List<string> errors,
        CancellationToken cancellationToken)
    {
        var screens = System.Windows.Forms.Screen.AllScreens;
        for (var index = 0; index < screens.Length; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var currentIndex = index;
            TryProbe($"display-capture:{currentIndex}", errors, () =>
            {
                var bounds = screens[currentIndex].Bounds;
                using var bitmap = CaptureScreen(bounds.X, bounds.Y, bounds.Width, bounds.Height);
                AddBitmap(archive, $"captures/display-{currentIndex}.png", bitmap);
                AddText(
                    archive,
                    $"captures/display-{currentIndex}-stats.json",
                    JsonSerializer.Serialize(AnalyzeBitmap(bitmap), JsonOptions));
            });
        }
    }

    private static void CaptureTarkovEvidence(
        ZipArchive archive,
        List<string> errors,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Process[] processes;
        try
        {
            processes = Process.GetProcessesByName("EscapeFromTarkov");
        }
        catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            errors.Add($"tarkov-discovery: {exception.GetType().Name}:{Sanitize(exception.Message)}");
            return;
        }

        try
        {
            var target = processes.FirstOrDefault(process => process.MainWindowHandle != IntPtr.Zero);
            if (target is null)
            {
                AddText(archive, "captures/tarkov.txt", "EscapeFromTarkov window not found.\r\n");
                return;
            }

            using (target)
            {
                var handle = target.MainWindowHandle;
                if (!TryGetClientScreenRect(handle, out var rect))
                {
                    AddText(archive, "captures/tarkov.txt", "Tarkov client rectangle unavailable.\r\n");
                    return;
                }

                AddText(
                    archive,
                    "captures/tarkov.txt",
                    $"ClientRect={rect.Left},{rect.Top},{rect.Width},{rect.Height}\r\n" +
                    $"Iconic={IsIconic(handle)}\r\n" +
                    $"Visible={IsWindowVisible(handle)}\r\n");

                TryProbe("tarkov-screen-copy", errors, () =>
                {
                    using var screenCopy = CaptureScreen(rect.Left, rect.Top, rect.Width, rect.Height);
                    AddBitmap(archive, "captures/tarkov-screen-copy.png", screenCopy);
                    AddText(
                        archive,
                        "captures/tarkov-screen-copy-stats.json",
                        JsonSerializer.Serialize(AnalyzeBitmap(screenCopy), JsonOptions));
                });

                TryProbe("tarkov-print-window", errors, () =>
                {
                    using var printWindow = CapturePrintWindow(handle, rect.Width, rect.Height);
                    if (printWindow is null)
                    {
                        AddText(archive, "captures/tarkov-print-window.txt", "PrintWindow returned no usable image.\r\n");
                        return;
                    }
                    AddBitmap(archive, "captures/tarkov-print-window.png", printWindow);
                    AddText(
                        archive,
                        "captures/tarkov-print-window-stats.json",
                        JsonSerializer.Serialize(AnalyzeBitmap(printWindow), JsonOptions));
                });
            }
        }
        finally
        {
            foreach (var process in processes)
                process.Dispose();
        }
    }

    private static Bitmap CaptureScreen(int left, int top, int width, int height)
    {
        var bitmap = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
        try
        {
            using var graphics = Graphics.FromImage(bitmap);
            graphics.CopyFromScreen(
                left,
                top,
                0,
                0,
                new System.Drawing.Size(width, height),
                CopyPixelOperation.SourceCopy);
            return bitmap;
        }
        catch
        {
            bitmap.Dispose();
            throw;
        }
    }

    private static Bitmap? CapturePrintWindow(IntPtr window, int width, int height)
    {
        if (width < 1 || height < 1)
            return null;
        var bitmap = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
        try
        {
            using var graphics = Graphics.FromImage(bitmap);
            var hdc = graphics.GetHdc();
            bool printed;
            try
            {
                printed = PrintWindow(window, hdc, 0x00000001 | 0x00000002);
            }
            finally
            {
                graphics.ReleaseHdc(hdc);
            }
            if (!printed)
            {
                bitmap.Dispose();
                return null;
            }
            return bitmap;
        }
        catch
        {
            bitmap.Dispose();
            throw;
        }
    }

    private static BitmapStats AnalyzeBitmap(Bitmap bitmap)
    {
        var rectangle = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
        var data = bitmap.LockBits(rectangle, ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
        try
        {
            var stride = Math.Abs(data.Stride);
            var bytes = new byte[stride * bitmap.Height];
            Marshal.Copy(data.Scan0, bytes, 0, bytes.Length);
            var stepX = Math.Max(1, bitmap.Width / 640);
            var stepY = Math.Max(1, bitmap.Height / 360);
            long sumR = 0;
            long sumG = 0;
            long sumB = 0;
            double sumLuma = 0;
            var minLuma = 255.0;
            var maxLuma = 0.0;
            long clippedHigh = 0;
            long clippedLow = 0;
            long samples = 0;

            for (var y = 0; y < bitmap.Height; y += stepY)
            {
                var sourceY = data.Stride > 0 ? y : bitmap.Height - 1 - y;
                for (var x = 0; x < bitmap.Width; x += stepX)
                {
                    var offset = sourceY * stride + x * 4;
                    var b = bytes[offset];
                    var g = bytes[offset + 1];
                    var r = bytes[offset + 2];
                    var luma = 0.2126 * r + 0.7152 * g + 0.0722 * b;
                    sumR += r;
                    sumG += g;
                    sumB += b;
                    sumLuma += luma;
                    minLuma = Math.Min(minLuma, luma);
                    maxLuma = Math.Max(maxLuma, luma);
                    if (r >= 250 || g >= 250 || b >= 250)
                        clippedHigh++;
                    if (r <= 5 && g <= 5 && b <= 5)
                        clippedLow++;
                    samples++;
                }
            }

            var denominator = Math.Max(1, samples);
            return new BitmapStats(
                bitmap.Width,
                bitmap.Height,
                samples,
                sumR / (double)denominator,
                sumG / (double)denominator,
                sumB / (double)denominator,
                sumLuma / denominator,
                minLuma,
                maxLuma,
                clippedHigh * 100.0 / denominator,
                clippedLow * 100.0 / denominator);
        }
        finally
        {
            bitmap.UnlockBits(data);
        }
    }

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
        return rect.Width >= 1 && rect.Height >= 1;
    }

    private static void AddBitmap(ZipArchive archive, string name, Bitmap bitmap)
    {
        var entry = archive.CreateEntry(name, CompressionLevel.Fastest);
        using var stream = entry.Open();
        bitmap.Save(stream, ImageFormat.Png);
    }

    private static void AddText(ZipArchive archive, string name, string content)
    {
        var entry = archive.CreateEntry(name, CompressionLevel.Fastest);
        using var stream = entry.Open();
        using var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        writer.Write(content);
    }

    private static void AddFile(ZipArchive archive, string sourcePath, string entryName)
    {
        var entry = archive.CreateEntry(entryName, CompressionLevel.Fastest);
        using var input = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        using var output = entry.Open();
        input.CopyTo(output);
    }

    private static void TryProbe(string name, ICollection<string> errors, Action action)
    {
        try
        {
            action();
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            errors.Add($"{name}: {exception.GetType().Name}:{Sanitize(exception.Message)}");
        }
    }

    private static string Sanitize(string? value) => (value ?? string.Empty)
        .Replace("\r", " ", StringComparison.Ordinal)
        .Replace("\n", " ", StringComparison.Ordinal)
        .Trim();

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int index);

    [DllImport("user32.dll")]
    private static extern uint GetDpiForSystem();

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetClientRect(IntPtr window, out NativeRect rect);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ClientToScreen(IntPtr window, ref NativePoint point);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsIconic(IntPtr window);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowVisible(IntPtr window);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PrintWindow(IntPtr window, IntPtr hdc, uint flags);

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

    private sealed record ScannerSnapshot(
        ScannerDisplaySettings Settings,
        string Status,
        string? ActiveCaptureMode,
        bool TestEnabled,
        int CatalogCount,
        string? CatalogMode,
        DateTimeOffset? CatalogGeneratedAtUtc);

    private sealed record BitmapStats(
        int Width,
        int Height,
        long SampleCount,
        double MeanR,
        double MeanG,
        double MeanB,
        double MeanLuminance,
        double MinimumLuminance,
        double MaximumLuminance,
        double HighClipPercent,
        double NearBlackPercent);
}
