using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Threading;

namespace JunhyunHelper.Desktop.Scanner;

/// <summary>
/// Bounded in-memory trace for Scanner stall diagnosis. Fine-grained timing markers are
/// intentionally kept out of scanner.log so diagnostic instrumentation cannot amplify a
/// slow filesystem/antivirus environment. The trace is exported on demand with the
/// ordinary bounded Scanner logs.
/// </summary>
internal static class ScannerPerformanceTrace
{
    private const int MaximumEntries = 1200;
    private static readonly object Gate = new();
    private static readonly Queue<TraceEntry> Entries = new();
    private static ScannerUiResponsivenessMonitor? _uiMonitor;
    private static int _uiMonitorStarted;
    private static long _nextSequence;

    public static void Mark(string eventName, params (string Key, object? Value)[] fields)
    {
        if (string.IsNullOrWhiteSpace(eventName))
            return;

        EnsureUiMonitorStarted();
        var entry = new TraceEntry(
            Interlocked.Increment(ref _nextSequence),
            DateTimeOffset.UtcNow,
            ScannerLatencyTelemetry.CurrentCycleId,
            Environment.CurrentManagedThreadId,
            IsUiThread(),
            eventName.Trim(),
            fields.Select(field => new TraceField(field.Key, Convert.ToString(field.Value, CultureInfo.InvariantCulture) ?? string.Empty)).ToArray());

        lock (Gate)
        {
            Entries.Enqueue(entry);
            while (Entries.Count > MaximumEntries)
                Entries.Dequeue();
        }
    }

    public static string ExportText()
    {
        TraceEntry[] snapshot;
        lock (Gate)
            snapshot = Entries.ToArray();

        var builder = new StringBuilder();
        builder.AppendLine("# Junhyun Helper Scanner performance trace");
        builder.AppendLine("# Fine-grained in-memory trace; oldest entries are dropped after the bounded capacity is reached.");
        foreach (var entry in snapshot)
        {
            builder.Append(entry.TimestampUtc.ToString("O", CultureInfo.InvariantCulture))
                .Append(" | seq=").Append(entry.Sequence)
                .Append(" | event=").Append(Sanitize(entry.EventName))
                .Append(" | cycleId=").Append(entry.CycleId?.ToString(CultureInfo.InvariantCulture) ?? string.Empty)
                .Append(" | thread=").Append(entry.ManagedThreadId)
                .Append(" | uiThread=").Append(entry.UiThread);

            foreach (var field in entry.Fields)
            {
                builder.Append(" | ")
                    .Append(Sanitize(field.Key))
                    .Append('=')
                    .Append(Sanitize(field.Value));
            }
            builder.AppendLine();
        }
        return builder.ToString();
    }

    public static double ElapsedMilliseconds(long startedTimestamp) =>
        (Stopwatch.GetTimestamp() - startedTimestamp) * 1000.0 / Stopwatch.Frequency;

    private static void EnsureUiMonitorStarted()
    {
        if (Volatile.Read(ref _uiMonitorStarted) != 0)
            return;

        Dispatcher? dispatcher;
        try
        {
            dispatcher = Application.Current?.Dispatcher;
        }
        catch
        {
            return;
        }

        if (dispatcher is null || dispatcher.HasShutdownStarted || dispatcher.HasShutdownFinished)
            return;
        if (Interlocked.CompareExchange(ref _uiMonitorStarted, 1, 0) != 0)
            return;

        _uiMonitor = new ScannerUiResponsivenessMonitor(dispatcher);
    }

    private static bool IsUiThread()
    {
        try
        {
            return Application.Current?.Dispatcher?.CheckAccess() == true;
        }
        catch
        {
            return false;
        }
    }

    private static string Sanitize(string value) => value
        .Replace("\r", " ", StringComparison.Ordinal)
        .Replace("\n", " ", StringComparison.Ordinal)
        .Replace("|", "/", StringComparison.Ordinal)
        .Trim();

    private sealed record TraceEntry(
        long Sequence,
        DateTimeOffset TimestampUtc,
        long? CycleId,
        int ManagedThreadId,
        bool UiThread,
        string EventName,
        IReadOnlyList<TraceField> Fields);

    private sealed record TraceField(string Key, string Value);
}
