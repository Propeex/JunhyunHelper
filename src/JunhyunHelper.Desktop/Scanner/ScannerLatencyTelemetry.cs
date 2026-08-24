using System.Diagnostics;
using System.Globalization;

namespace JunhyunHelper.Desktop.Scanner;

/// <summary>
/// Low-overhead per-scan latency telemetry. The active cycle flows through async calls,
/// so capture/detector, OCR/recovery, matching, and presentation can record into the
/// same sample without coupling those layers. Continuous detector-only samples are
/// throttled; semantic work and explicit one-shot scans are always persisted.
/// </summary>
internal static class ScannerLatencyTelemetry
{
    public const string Capture = "capture";
    public const string RectangleProposal = "rectangle-proposal";
    public const string SemanticHeader = "semantic-header";
    public const string OcrNormal = "ocr-normal";
    public const string OcrDeep = "ocr-deep";
    public const string VisualRecovery = "visual-recovery";
    public const string CatalogMatching = "catalog-matching";
    public const string Presentation = "presentation";

    private const int ContinuousDetectorSampleInterval = 20;
    private static readonly AsyncLocal<Cycle?> CurrentCycle = new();
    private static long _nextCycleId;
    private static long _continuousDetectorCycles;

    public static IDisposable BeginCycle(ScannerCaptureMode mode, string operation)
    {
        var previous = CurrentCycle.Value;
        var cycle = new Cycle(
            Interlocked.Increment(ref _nextCycleId),
            mode,
            operation,
            Stopwatch.GetTimestamp(),
            previous);
        CurrentCycle.Value = cycle;
        return cycle;
    }

    public static IDisposable Measure(string stage)
    {
        var cycle = CurrentCycle.Value;
        return cycle is null
            ? NoopScope.Instance
            : new StageScope(cycle, stage, Stopwatch.GetTimestamp());
    }

    private static double ToMilliseconds(long timestampDelta) =>
        timestampDelta * 1000.0 / Stopwatch.Frequency;

    private sealed class Cycle : IDisposable
    {
        private readonly object _gate = new();
        private readonly Dictionary<string, StageAggregate> _stages = new(StringComparer.Ordinal);
        private readonly long _startedTimestamp;
        private readonly Cycle? _previous;
        private int _disposed;

        public Cycle(
            long id,
            ScannerCaptureMode mode,
            string operation,
            long startedTimestamp,
            Cycle? previous)
        {
            Id = id;
            Mode = mode;
            Operation = string.IsNullOrWhiteSpace(operation) ? "unknown" : operation.Trim();
            _startedTimestamp = startedTimestamp;
            _previous = previous;
        }

        public long Id { get; }
        public ScannerCaptureMode Mode { get; }
        public string Operation { get; }

        public void Add(string stage, long elapsedTicks)
        {
            if (elapsedTicks < 0 || string.IsNullOrWhiteSpace(stage))
                return;

            lock (_gate)
            {
                if (_stages.TryGetValue(stage, out var existing))
                {
                    _stages[stage] = new StageAggregate(
                        existing.ElapsedTicks + elapsedTicks,
                        existing.Count + 1);
                }
                else
                {
                    _stages[stage] = new StageAggregate(elapsedTicks, 1);
                }
            }
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
                return;

            if (ReferenceEquals(CurrentCycle.Value, this))
                CurrentCycle.Value = _previous;

            Dictionary<string, StageAggregate> snapshot;
            lock (_gate)
                snapshot = new Dictionary<string, StageAggregate>(_stages, StringComparer.Ordinal);

            var elapsed = Stopwatch.GetTimestamp() - _startedTimestamp;
            var hasSemanticWork =
                snapshot.ContainsKey(OcrNormal) ||
                snapshot.ContainsKey(OcrDeep) ||
                snapshot.ContainsKey(VisualRecovery) ||
                snapshot.ContainsKey(CatalogMatching) ||
                snapshot.ContainsKey(Presentation);
            var isOneShot = Operation.StartsWith("one-shot", StringComparison.Ordinal);
            var sampledDetectorCycle = Interlocked.Increment(ref _continuousDetectorCycles) %
                ContinuousDetectorSampleInterval == 0;

            if (!isOneShot && !hasSemanticWork && !sampledDetectorCycle)
                return;

            double Ms(string stage) => snapshot.TryGetValue(stage, out var aggregate)
                ? ToMilliseconds(aggregate.ElapsedTicks)
                : 0;
            int Count(string stage) => snapshot.TryGetValue(stage, out var aggregate)
                ? aggregate.Count
                : 0;

            ScannerDiagnosticLog.Write(
                "scanner-latency",
                Mode,
                ("cycleId", Id),
                ("operation", Operation),
                ("captureMs", Format(Ms(Capture))),
                ("rectangleProposalMs", Format(Ms(RectangleProposal))),
                ("semanticHeaderMs", Format(Ms(SemanticHeader))),
                ("ocrNormalMs", Format(Ms(OcrNormal))),
                ("ocrNormalCount", Count(OcrNormal)),
                ("ocrDeepMs", Format(Ms(OcrDeep))),
                ("ocrDeepCount", Count(OcrDeep)),
                ("visualRecoveryMs", Format(Ms(VisualRecovery))),
                ("visualRecoveryCount", Count(VisualRecovery)),
                ("catalogMatchingMs", Format(Ms(CatalogMatching))),
                ("catalogMatchingCount", Count(CatalogMatching)),
                ("presentationMs", Format(Ms(Presentation))),
                ("presentationCount", Count(Presentation)),
                ("endToEndMs", Format(ToMilliseconds(elapsed))));
        }

        private static string Format(double milliseconds) =>
            milliseconds.ToString("F2", CultureInfo.InvariantCulture);
    }

    private sealed class StageScope : IDisposable
    {
        private readonly Cycle _cycle;
        private readonly string _stage;
        private readonly long _startedTimestamp;
        private int _disposed;

        public StageScope(Cycle cycle, string stage, long startedTimestamp)
        {
            _cycle = cycle;
            _stage = stage;
            _startedTimestamp = startedTimestamp;
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
                return;
            _cycle.Add(_stage, Stopwatch.GetTimestamp() - _startedTimestamp);
        }
    }

    private sealed class NoopScope : IDisposable
    {
        public static readonly NoopScope Instance = new();
        private NoopScope() { }
        public void Dispose() { }
    }

    private readonly record struct StageAggregate(long ElapsedTicks, int Count);
}
