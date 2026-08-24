using System.Text.Json;
using System.Windows;
using System.Windows.Media.Imaging;
using JunhyunHelper.Core.Scanner;

namespace JunhyunHelper.Desktop.Scanner;

public sealed record ScannerDiagnosticCaseSummary(
    string CaseId,
    DateTimeOffset? Timestamp,
    string ReviewStatus,
    string PipelineStage,
    string ProgramResult,
    string GroundTruth,
    string ErrorType,
    string Retention,
    string CasePath)
{
    public string TimestampText => Timestamp?.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss") ?? "-";
    public string ReviewText => ReviewStatus == "reviewed" ? "검증됨" : "미검증";
    public string ResultText => string.IsNullOrWhiteSpace(ProgramResult) ? "-" : ProgramResult;
    public string GroundTruthText => string.IsNullOrWhiteSpace(GroundTruth) ? "-" : GroundTruth;
    public string ErrorText => string.IsNullOrWhiteSpace(ErrorType) ? "-" : ErrorType;
}

public sealed record ScannerStoredCorrectionCase(
    ScannerRecognitionDebugFrame Frame,
    string GroundTruthItemName,
    IReadOnlyList<ScannerGroundTruthSelection> Selections);

public static class ScannerDiagnosticCaseBrowser
{
    public static IReadOnlyList<ScannerDiagnosticCaseSummary> GetCases()
    {
        var casesRoot = Path.Combine(ScannerDiagnosticDataset.RootPath, "cases");
        if (!Directory.Exists(casesRoot))
            return [];

        var result = new List<ScannerDiagnosticCaseSummary>();
        foreach (var caseFile in Directory
                     .EnumerateFiles(casesRoot, "case.json", SearchOption.AllDirectories)
                     .OrderByDescending(path => path, StringComparer.Ordinal))
        {
            try
            {
                using var document = JsonDocument.Parse(File.ReadAllText(caseFile));
                var root = document.RootElement;
                var itemName = GetNestedObject(root, "fields", "item_name");
                var pipeline = GetObject(root, "pipeline");

                DateTimeOffset? timestamp = null;
                var timestampText = GetString(root, "timestamp");
                if (DateTimeOffset.TryParse(timestampText, out var parsedTimestamp))
                    timestamp = parsedTimestamp;

                result.Add(new ScannerDiagnosticCaseSummary(
                    GetString(root, "case_id"),
                    timestamp,
                    GetString(root, "review_status"),
                    GetString(pipeline, "stage"),
                    GetString(itemName, "program_result"),
                    GetString(itemName, "ground_truth"),
                    GetString(itemName, "ground_truth_error_type"),
                    GetString(root, "retention"),
                    Path.GetDirectoryName(caseFile) ?? string.Empty));
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
            {
                App.WriteDiagnostic("Scanner diagnostic case summary read failed", exception);
            }
        }

        return result
            .OrderByDescending(item => item.Timestamp ?? DateTimeOffset.MinValue)
            .ThenByDescending(item => item.CaseId, StringComparer.Ordinal)
            .ToArray();
    }

    public static bool TryLoadCase(
        ScannerDiagnosticCaseSummary summary,
        out ScannerStoredCorrectionCase storedCase,
        out string error)
    {
        ArgumentNullException.ThrowIfNull(summary);
        storedCase = null!;
        error = string.Empty;

        try
        {
            var caseJsonPath = Path.Combine(summary.CasePath, "case.json");
            var fullImagePath = Path.Combine(summary.CasePath, "full.png");
            if (!File.Exists(caseJsonPath) || !File.Exists(fullImagePath))
            {
                error = "이 Case의 원본 이미지 또는 메타데이터가 없습니다.";
                return false;
            }

            using var document = JsonDocument.Parse(File.ReadAllText(caseJsonPath));
            var root = document.RootElement;
            var screen = GetObject(root, "screen");
            var detail = GetObject(root, "detail_window");
            var pipeline = GetObject(root, "pipeline");
            var itemName = GetNestedObject(root, "fields", "item_name");

            var image = LoadFrozenBitmap(fullImagePath);
            var detectedDetail = GetRect(detail, "detected_roi");
            var detectedTitle = GetRect(itemName, "detected_roi");

            var sidecar = ReadCandidateSidecar(summary.CasePath);
            var selectedRuntimeCandidate = sidecar.Candidates.FirstOrDefault(candidate =>
                RectsEquivalent(candidate.Bounds, detectedDetail) &&
                (detectedTitle is null || RectsEquivalent(candidate.TitleBounds, detectedTitle)));
            var detectedClose = selectedRuntimeCandidate?.CloseBounds;
            var detectedMagnifier = selectedRuntimeCandidate?.MagnifierBounds;

            var rawOcr = GetString(itemName, "ocr_raw");
            var matcherText = GetString(itemName, "ocr_normalized");
            var userSubstituted = string.IsNullOrWhiteSpace(sidecar.UserSubstitutedOcr)
                ? rawOcr
                : sidecar.UserSubstitutedOcr;
            var timestamp = DateTimeOffset.TryParse(GetString(root, "timestamp"), out var parsedTimestamp)
                ? parsedTimestamp
                : DateTimeOffset.Now;
            var captureMode = Enum.TryParse<ScannerCaptureMode>(GetString(root, "capture_mode"), true, out var parsedMode)
                ? parsedMode
                : ScannerCaptureMode.TarkovWindow;

            var topCandidates = ReadTopCandidates(itemName);
            var frame = new ScannerRecognitionDebugFrame(
                image,
                GetInt32(screen, "capture_origin_x"),
                GetInt32(screen, "capture_origin_y"),
                GetString(root, "source"),
                detectedDetail,
                detectedTitle,
                detectedMagnifier,
                detectedClose,
                GetDouble(detail, "structural_confidence"),
                GetString(detail, "structural_reason"),
                GetDouble(detail, "header_confidence"),
                GetString(detail, "header_reason"),
                TitleSignature: null,
                Pass: GetString(pipeline, "pass"),
                OcrText: rawOcr,
                UserSubstitutedOcrText: userSubstituted,
                MatcherText: matcherText,
                ItemId: EmptyToNull(GetString(itemName, "program_item_id")),
                CandidateName: EmptyToNull(GetString(itemName, "program_result")),
                RecognitionReason: GetString(itemName, "recognition_reason"),
                Confidence: GetDouble(itemName, "confidence"),
                SecondScore: GetDouble(itemName, "second_score"),
                TopCandidates: topCandidates,
                UpdatedAt: timestamp,
                CaptureMode: captureMode,
                CaseId: GetString(root, "case_id"),
                Candidates: sidecar.Candidates)
            {
                Timestamp = timestamp,
            };

            var groundTruth = string.IsNullOrWhiteSpace(sidecar.GroundTruthItemName)
                ? GetString(itemName, "ground_truth")
                : sidecar.GroundTruthItemName;
            var selections = sidecar.Selections.Count > 0
                ? sidecar.Selections
                : BuildDefaultSelections(frame);

            storedCase = new ScannerStoredCorrectionCase(frame, groundTruth, selections);
            return true;
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or JsonException or NotSupportedException or InvalidDataException)
        {
            App.WriteDiagnostic("Scanner diagnostic case editor load failed", exception);
            error = "이 Case를 교정 화면으로 불러오지 못했습니다. 원본 데이터는 변경하지 않았습니다.";
            return false;
        }
    }

    private static SidecarData ReadCandidateSidecar(string casePath)
    {
        var path = Path.Combine(casePath, "candidate_selection.json");
        if (!File.Exists(path))
            return new SidecarData([], [], string.Empty, string.Empty);

        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var root = document.RootElement;
        var candidates = new List<ScannerDiagnosticCandidateEvidence>();
        if (root.TryGetProperty("candidates", out var candidateArray) && candidateArray.ValueKind == JsonValueKind.Array)
        {
            foreach (var candidate in candidateArray.EnumerateArray())
            {
                var bounds = GetRect(candidate, "detail_bounds");
                if (bounds is not { Width: > 0, Height: > 0 } detailBounds)
                    continue;
                candidates.Add(new ScannerDiagnosticCandidateEvidence(
                    GetString(candidate, "id"),
                    GetInt32(candidate, "rank"),
                    detailBounds,
                    GetDouble(candidate, "structural_score"),
                    GetString(candidate, "structural_reason"),
                    GetRect(candidate, "title_bounds"),
                    GetRect(candidate, "magnifier_bounds"),
                    GetRect(candidate, "close_bounds"),
                    GetDouble(candidate, "header_score"),
                    GetString(candidate, "header_reason")));
            }
        }

        var selections = new List<ScannerGroundTruthSelection>();
        if (root.TryGetProperty("selections", out var selectionArray) && selectionArray.ValueKind == JsonValueKind.Array)
        {
            foreach (var selection in selectionArray.EnumerateArray())
            {
                var field = GetString(selection, "field");
                if (string.IsNullOrWhiteSpace(field))
                    continue;
                selections.Add(new ScannerGroundTruthSelection(
                    field,
                    ParseSelectionMode(GetString(selection, "mode")),
                    GetRect(selection, "bounds"),
                    EmptyToNull(GetString(selection, "candidate_id")),
                    GetNullableInt32(selection, "candidate_rank"),
                    GetNullableDouble(selection, "candidate_score"),
                    EmptyToNull(GetString(selection, "candidate_reason"))));
            }
        }

        var ocr = GetObject(root, "ocr");
        return new SidecarData(
            candidates,
            selections,
            GetString(root, "ground_truth_item_name"),
            GetString(ocr, "user_substituted"));
    }

    private static IReadOnlyList<ScannerGroundTruthSelection> BuildDefaultSelections(ScannerRecognitionDebugFrame frame) =>
    [
        new ScannerGroundTruthSelection("detail_window", ScannerGroundTruthSelectionMode.Current, frame.SelectedBounds),
        new ScannerGroundTruthSelection("close_button", ScannerGroundTruthSelectionMode.Current, frame.CloseBounds),
        new ScannerGroundTruthSelection("magnifier", ScannerGroundTruthSelectionMode.Current, frame.MagnifierBounds),
        new ScannerGroundTruthSelection("item_name_roi", ScannerGroundTruthSelectionMode.Current, frame.TitleBounds),
    ];

    private static IReadOnlyList<ScannerMatchCandidate> ReadTopCandidates(JsonElement itemName)
    {
        if (!itemName.TryGetProperty("top_candidates", out var array) || array.ValueKind != JsonValueKind.Array)
            return [];

        var result = new List<ScannerMatchCandidate>();
        foreach (var candidate in array.EnumerateArray())
        {
            var itemId = GetString(candidate, "item_id");
            var officialName = GetString(candidate, "official_name");
            if (string.IsNullOrWhiteSpace(itemId) || string.IsNullOrWhiteSpace(officialName))
                continue;
            result.Add(new ScannerMatchCandidate(itemId, officialName, GetDouble(candidate, "score")));
        }
        return result;
    }

    private static BitmapImage LoadFrozenBitmap(string path)
    {
        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.UriSource = new Uri(Path.GetFullPath(path), UriKind.Absolute);
        image.EndInit();
        image.Freeze();
        return image;
    }

    private static ScannerGroundTruthSelectionMode ParseSelectionMode(string value) => value switch
    {
        "candidate" => ScannerGroundTruthSelectionMode.Candidate,
        "none" => ScannerGroundTruthSelectionMode.None,
        "manual" => ScannerGroundTruthSelectionMode.Manual,
        _ => ScannerGroundTruthSelectionMode.Current,
    };

    private static Rect? GetRect(JsonElement element, string property)
    {
        var value = GetObject(element, property);
        if (value.ValueKind != JsonValueKind.Object)
            return null;
        var width = GetDouble(value, "width");
        var height = GetDouble(value, "height");
        if (width <= 0 || height <= 0)
            return null;
        return new Rect(GetDouble(value, "x"), GetDouble(value, "y"), width, height);
    }

    private static JsonElement GetNestedObject(JsonElement root, string parent, string child) =>
        GetObject(GetObject(root, parent), child);

    private static JsonElement GetObject(JsonElement element, string property)
    {
        if (element.ValueKind == JsonValueKind.Object &&
            element.TryGetProperty(property, out var value) &&
            value.ValueKind == JsonValueKind.Object)
        {
            return value;
        }
        return default;
    }

    private static string GetString(JsonElement element, string property)
    {
        if (element.ValueKind != JsonValueKind.Object ||
            !element.TryGetProperty(property, out var value) ||
            value.ValueKind != JsonValueKind.String)
        {
            return string.Empty;
        }
        return value.GetString()?.Trim() ?? string.Empty;
    }

    private static int GetInt32(JsonElement element, string property) =>
        GetNullableInt32(element, property) ?? 0;

    private static int? GetNullableInt32(JsonElement element, string property)
    {
        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(property, out var value))
            return null;
        return value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var parsed) ? parsed : null;
    }

    private static double GetDouble(JsonElement element, string property) =>
        GetNullableDouble(element, property) ?? 0;

    private static double? GetNullableDouble(JsonElement element, string property)
    {
        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(property, out var value))
            return null;
        return value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out var parsed) ? parsed : null;
    }

    private static string? EmptyToNull(string value) => string.IsNullOrWhiteSpace(value) ? null : value;

    private static bool RectsEquivalent(Rect? left, Rect? right)
    {
        if (left is null || right is null)
            return left is null && right is null;
        return Math.Abs(left.Value.X - right.Value.X) < 0.5 &&
               Math.Abs(left.Value.Y - right.Value.Y) < 0.5 &&
               Math.Abs(left.Value.Width - right.Value.Width) < 0.5 &&
               Math.Abs(left.Value.Height - right.Value.Height) < 0.5;
    }

    private sealed record SidecarData(
        IReadOnlyList<ScannerDiagnosticCandidateEvidence> Candidates,
        IReadOnlyList<ScannerGroundTruthSelection> Selections,
        string GroundTruthItemName,
        string UserSubstitutedOcr);
}
