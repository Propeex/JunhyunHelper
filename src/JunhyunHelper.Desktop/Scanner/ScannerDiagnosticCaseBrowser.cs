using System.Text.Json;

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
                var fields = root.TryGetProperty("fields", out var fieldsElement)
                    ? fieldsElement
                    : default;
                var itemName = fields.ValueKind == JsonValueKind.Object &&
                               fields.TryGetProperty("item_name", out var itemElement)
                    ? itemElement
                    : default;
                var pipeline = root.TryGetProperty("pipeline", out var pipelineElement)
                    ? pipelineElement
                    : default;

                DateTimeOffset? timestamp = null;
                var timestampText = GetString(root, "timestamp");
                if (DateTimeOffset.TryParse(timestampText, out var parsedTimestamp))
                    timestamp = parsedTimestamp;

                result.Add(new ScannerDiagnosticCaseSummary(
                    GetString(root, "case_id"),
                    timestamp,
                    GetString(root, "review_status"),
                    pipeline.ValueKind == JsonValueKind.Object ? GetString(pipeline, "stage") : string.Empty,
                    itemName.ValueKind == JsonValueKind.Object ? GetString(itemName, "program_result") : string.Empty,
                    itemName.ValueKind == JsonValueKind.Object ? GetString(itemName, "ground_truth") : string.Empty,
                    itemName.ValueKind == JsonValueKind.Object ? GetString(itemName, "ground_truth_error_type") : string.Empty,
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
}
