using System.Text;
using System.Text.Json;
using System.Windows;

namespace JunhyunHelper.Desktop.Scanner;

public enum ScannerGroundTruthSelectionMode
{
    Current,
    Candidate,
    None,
    Manual,
}

public sealed record ScannerGroundTruthSelection(
    string Field,
    ScannerGroundTruthSelectionMode Mode,
    Rect? Bounds,
    string? CandidateId = null,
    int? CandidateRank = null,
    double? CandidateScore = null,
    string? CandidateReason = null);

/// <summary>
/// User-review sidecar for candidate-ranking supervision. case.json remains backwards
/// compatible with the existing regression corpus; this additive document captures
/// exactly which proposal/anchor/ROI candidate the reviewer chose, including explicit
/// NONE and manual-fallback choices.
/// </summary>
public static class ScannerCandidateGroundTruth
{
    private const string Schema = "candidate-selection-v1";
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    public static void Save(
        ScannerRecognitionDebugFrame frame,
        IReadOnlyList<ScannerGroundTruthSelection> selections,
        string? groundTruthItemName)
    {
        ArgumentNullException.ThrowIfNull(frame);
        ArgumentNullException.ThrowIfNull(selections);
        if (string.IsNullOrWhiteSpace(frame.CaseId))
            throw new InvalidDataException("Case ID가 없습니다.");

        var safeCaseId = SafeCaseId(frame.CaseId);
        var caseRoot = Path.Combine(ScannerDiagnosticDataset.RootPath, "cases", safeCaseId);
        Directory.CreateDirectory(caseRoot);
        var path = Path.Combine(caseRoot, "candidate_selection.json");
        var temporary = path + ".tmp";

        var candidates = (frame.Candidates ?? [])
            .Select(candidate => new
            {
                id = candidate.Id,
                rank = candidate.Rank,
                detail_bounds = Roi(candidate.Bounds, frame.Image.PixelWidth, frame.Image.PixelHeight),
                structural_score = candidate.StructuralScore,
                structural_reason = candidate.StructuralReason,
                title_bounds = Roi(candidate.TitleBounds, frame.Image.PixelWidth, frame.Image.PixelHeight),
                magnifier_bounds = Roi(candidate.MagnifierBounds, frame.Image.PixelWidth, frame.Image.PixelHeight),
                close_bounds = Roi(candidate.CloseBounds, frame.Image.PixelWidth, frame.Image.PixelHeight),
                header_score = candidate.TitleAnchorScore,
                header_reason = candidate.TitleAnchorReason,
            })
            .ToArray();

        var document = new
        {
            schema = Schema,
            case_id = frame.CaseId,
            reviewed_at_utc = DateTimeOffset.UtcNow.ToString("O"),
            ground_truth_item_name = groundTruthItemName?.Trim() ?? string.Empty,
            ocr = new
            {
                raw = frame.OcrText,
                user_substituted = string.IsNullOrWhiteSpace(frame.UserSubstitutedOcrText)
                    ? frame.OcrText
                    : frame.UserSubstitutedOcrText,
                matcher_text = frame.MatcherText,
            },
            selections = selections.Select(selection => new
            {
                field = selection.Field,
                mode = selection.Mode.ToString().ToLowerInvariant(),
                bounds = Roi(selection.Bounds, frame.Image.PixelWidth, frame.Image.PixelHeight),
                candidate_id = selection.CandidateId,
                candidate_rank = selection.CandidateRank,
                candidate_score = selection.CandidateScore,
                candidate_reason = selection.CandidateReason,
            }).ToArray(),
            candidates,
        };

        File.WriteAllText(
            temporary,
            JsonSerializer.Serialize(document, JsonOptions),
            new UTF8Encoding(false));
        File.Move(temporary, path, overwrite: true);
    }

    private static object? Roi(Rect? value, int width, int height)
    {
        if (value is not { } rect || rect.Width <= 0 || rect.Height <= 0 || width <= 0 || height <= 0)
            return null;
        return new
        {
            x = rect.X,
            y = rect.Y,
            width = rect.Width,
            height = rect.Height,
            x_ratio = rect.X / width,
            y_ratio = rect.Y / height,
            width_ratio = rect.Width / width,
            height_ratio = rect.Height / height,
        };
    }

    private static string SafeCaseId(string caseId)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var builder = new StringBuilder(caseId.Length);
        foreach (var character in caseId)
            builder.Append(invalid.Contains(character) ? '_' : character);
        return builder.ToString();
    }
}
