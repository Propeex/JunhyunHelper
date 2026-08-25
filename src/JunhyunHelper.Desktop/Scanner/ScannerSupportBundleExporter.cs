using System.Diagnostics;
using System.Globalization;
using System.IO.Compression;
using System.Reflection;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Media;
using Windows.Media.Ocr;

namespace JunhyunHelper.Desktop.Scanner;

internal static class ScannerSupportBundleExporter
{
    public static void Export(string destinationPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);
        var fullPath = System.IO.Path.GetFullPath(destinationPath);
        var directory = System.IO.Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);
        if (File.Exists(fullPath))
            File.Delete(fullPath);

        using var archive = ZipFile.Open(fullPath, ZipArchiveMode.Create);
        AddText(archive, "environment.txt", BuildEnvironmentReport());
        AddText(archive, "scanner-performance-trace.txt", ScannerPerformanceTrace.ExportText());
        AddText(
            archive,
            "README.txt",
            "준현 헬퍼 Scanner 성능 진단 자료입니다.\r\n" +
            "scanner-performance-trace.txt는 전체 Scanner stage와 세부 OCR/UI timing을, scanner.log는 기존 Scanner 결정을 기록합니다.\r\n" +
            "Ground Truth 이미지, 프로필 DB, 게임 계정 정보는 이 ZIP에 포함하지 않습니다.\r\n");

        AddFileIfPresent(archive, ScannerDiagnosticLog.Path, "scanner.log");
        AddFileIfPresent(archive, ScannerDiagnosticLog.Path + ".1", "scanner.log.1");

        var startupLog = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "JunhyunHelper",
            "logs",
            "startup.log");
        AddFileIfPresent(archive, startupLog, "startup.log");
    }

    private static string BuildEnvironmentReport()
    {
        var builder = new StringBuilder();
        var assembly = typeof(ScannerSupportBundleExporter).Assembly;
        var productVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? assembly.GetName().Version?.ToString()
            ?? "unknown";

        builder.AppendLine("# Junhyun Helper Scanner environment")
            .AppendLine($"ExportUtc={DateTimeOffset.UtcNow:O}")
            .AppendLine($"ProductVersion={productVersion}")
            .AppendLine($"OSVersion={Environment.OSVersion.VersionString}")
            .AppendLine($"OSDescription={RuntimeInformation.OSDescription}")
            .AppendLine($"OSArchitecture={RuntimeInformation.OSArchitecture}")
            .AppendLine($"ProcessArchitecture={RuntimeInformation.ProcessArchitecture}")
            .AppendLine($"Is64BitProcess={Environment.Is64BitProcess}")
            .AppendLine($"RuntimeVersion={Environment.Version}")
            .AppendLine($"CurrentCulture={CultureInfo.CurrentCulture.Name}")
            .AppendLine($"CurrentUICulture={CultureInfo.CurrentUICulture.Name}")
            .AppendLine($"ProcessorCount={Environment.ProcessorCount}")
            .AppendLine($"ProcessorIdentifier={Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER") ?? string.Empty}")
            .AppendLine($"ServerGC={GCSettings.IsServerGC}")
            .AppendLine($"GCLatencyMode={GCSettings.LatencyMode}")
            .AppendLine($"GCGen0Collections={GC.CollectionCount(0)}")
            .AppendLine($"GCGen1Collections={GC.CollectionCount(1)}")
            .AppendLine($"GCGen2Collections={GC.CollectionCount(2)}")
            .AppendLine($"ManagedMemoryBytes={GC.GetTotalMemory(forceFullCollection: false)}");

        try
        {
            using var process = Process.GetCurrentProcess();
            process.Refresh();
            builder.AppendLine($"ProcessWorkingSetBytes={process.WorkingSet64}")
                .AppendLine($"ProcessPrivateMemoryBytes={process.PrivateMemorySize64}")
                .AppendLine($"ProcessVirtualMemoryBytes={process.VirtualMemorySize64}")
                .AppendLine($"ProcessThreadCount={process.Threads.Count}")
                .AppendLine($"ProcessHandleCount={process.HandleCount}")
                .AppendLine($"ProcessTotalCpuMs={process.TotalProcessorTime.TotalMilliseconds:F2}");
        }
        catch (Exception exception)
        {
            builder.AppendLine($"ProcessMetricsError={exception.GetType().Name}:{Sanitize(exception.Message)}");
        }

        try
        {
            var recognizers = OcrEngine.AvailableRecognizerLanguages
                .Select(language => language.LanguageTag)
                .OrderBy(tag => tag, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            builder.AppendLine($"OcrLanguages={string.Join(',', recognizers)}")
                .AppendLine($"KoKrOcrAvailable={recognizers.Contains("ko-KR", StringComparer.OrdinalIgnoreCase)}");
        }
        catch (Exception exception)
        {
            builder.AppendLine($"OcrLanguagesError={exception.GetType().Name}:{Sanitize(exception.Message)}");
        }

        try
        {
            var screens = System.Windows.Forms.Screen.AllScreens;
            builder.AppendLine($"DisplayCount={screens.Length}");
            for (var index = 0; index < screens.Length; index++)
            {
                var screen = screens[index];
                builder.AppendLine(
                    $"Display[{index}]={screen.DeviceName};Bounds={screen.Bounds.X},{screen.Bounds.Y},{screen.Bounds.Width},{screen.Bounds.Height};Primary={screen.Primary}");
            }
        }
        catch (Exception exception)
        {
            builder.AppendLine($"DisplayError={exception.GetType().Name}:{Sanitize(exception.Message)}");
        }

        AppendWpfEnvironment(builder);
        AppendScannerLogWriteProbe(builder);
        AppendFileIoProbe(builder);
        return builder.ToString();
    }

    private static void AppendWpfEnvironment(StringBuilder builder)
    {
        try
        {
            var dispatcher = System.Windows.Application.Current?.Dispatcher;
            if (dispatcher is null || dispatcher.HasShutdownStarted || dispatcher.HasShutdownFinished)
            {
                builder.AppendLine("WpfEnvironment=dispatcher-unavailable");
                return;
            }

            var report = dispatcher.CheckAccess()
                ? BuildWpfEnvironmentReport()
                : dispatcher.Invoke(BuildWpfEnvironmentReport);
            builder.Append(report);
        }
        catch (Exception exception)
        {
            builder.AppendLine($"WpfEnvironmentError={exception.GetType().Name}:{Sanitize(exception.Message)}");
        }
    }

    private static string BuildWpfEnvironmentReport()
    {
        var builder = new StringBuilder()
            .AppendLine($"WpfRenderTier={RenderCapability.Tier >> 16}");
        var window = System.Windows.Application.Current?.MainWindow;
        if (window is null)
        {
            builder.AppendLine("WpfMainWindow=unavailable");
            return builder.ToString();
        }

        var dpi = VisualTreeHelper.GetDpi(window);
        builder.AppendLine($"WpfDpiScale={dpi.DpiScaleX:F3},{dpi.DpiScaleY:F3}")
            .AppendLine($"WpfPixelsPerDip={dpi.PixelsPerDip:F3}");
        return builder.ToString();
    }

    private static void AppendScannerLogWriteProbe(StringBuilder builder)
    {
        try
        {
            var started = Stopwatch.StartNew();
            ScannerDiagnosticLog.Write(
                "diagnostic-file-io-probe",
                null,
                ("purpose", "support-bundle"));
            started.Stop();
            builder.AppendLine($"ScannerDiagnosticLogWriteProbeMs={started.Elapsed.TotalMilliseconds:F2}");
        }
        catch (Exception exception)
        {
            builder.AppendLine($"ScannerDiagnosticLogWriteProbeError={exception.GetType().Name}:{Sanitize(exception.Message)}");
        }
    }

    private static void AppendFileIoProbe(StringBuilder builder)
    {
        var root = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "JunhyunHelper",
            "logs");
        var probePath = System.IO.Path.Combine(root, ".scanner-io-probe.tmp");
        try
        {
            Directory.CreateDirectory(root);
            if (File.Exists(probePath))
                File.Delete(probePath);

            const int writes = 24;
            var total = Stopwatch.StartNew();
            var maximumMs = 0.0;
            for (var index = 0; index < writes; index++)
            {
                var one = Stopwatch.StartNew();
                File.AppendAllText(
                    probePath,
                    $"{DateTimeOffset.UtcNow:O} | probe={index} | payload=scanner-diagnostic-file-append-probe{Environment.NewLine}",
                    Encoding.UTF8);
                one.Stop();
                maximumMs = Math.Max(maximumMs, one.Elapsed.TotalMilliseconds);
            }
            total.Stop();
            builder.AppendLine($"DiagnosticFileAppendCount={writes}")
                .AppendLine($"DiagnosticFileAppendTotalMs={total.Elapsed.TotalMilliseconds:F2}")
                .AppendLine($"DiagnosticFileAppendAverageMs={total.Elapsed.TotalMilliseconds / writes:F2}")
                .AppendLine($"DiagnosticFileAppendMaximumMs={maximumMs:F2}");
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            builder.AppendLine($"DiagnosticFileAppendError={exception.GetType().Name}:{Sanitize(exception.Message)}");
        }
        finally
        {
            try
            {
                if (File.Exists(probePath))
                    File.Delete(probePath);
            }
            catch
            {
            }
        }
    }

    private static void AddText(ZipArchive archive, string name, string content)
    {
        var entry = archive.CreateEntry(name, CompressionLevel.Fastest);
        using var stream = entry.Open();
        using var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        writer.Write(content);
    }

    private static void AddFileIfPresent(ZipArchive archive, string sourcePath, string entryName)
    {
        try
        {
            if (!File.Exists(sourcePath))
                return;
            var entry = archive.CreateEntry(entryName, CompressionLevel.Fastest);
            using var input = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            using var output = entry.Open();
            input.CopyTo(output);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static string Sanitize(string value) => value
        .Replace("\r", " ", StringComparison.Ordinal)
        .Replace("\n", " ", StringComparison.Ordinal)
        .Trim();
}
