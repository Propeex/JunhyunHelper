using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using JunhyunHelper.Core.Scanner;
using JunhyunHelper.Infrastructure.Scanner;

namespace JunhyunHelper.Desktop.Scanner;

public sealed record ScannerGroundTruthRegressionResult(
    int ReviewedCases,
    int ExecutedCases,
    int StillCorrect,
    int Solved,
    int StillFailing,
    int Regressions,
    int Errors,
    double? CurrentAccuracy,
    string JsonPath,
    string MarkdownPath);

/// <summary>
/// Replays reviewed Scanner Ground Truth cases from the preserved full.png through the
/// current production geometry/header/OCR/catalog path. Detector and OCR consume original
/// pixels; this is deliberately not a metadata-only regression comparison.
/// </summary>
internal sealed class ScannerGroundTruthRegressionService
{
    private const int CandidateLimit = 8;
    private const int DeepOcrCandidateLimit = 3;
    private const double CandidateStructuralFloor = 0.34;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    private readonly IScannerOcrEngine _ocr;
    private readonly ScannerCatalogService _catalog;
    private readonly Func<string, ScannerItemSnapshot?> _snapshotProvider;

    public ScannerGroundTruthRegressionService(
        IScannerOcrEngine ocr,
        ScannerCatalogService catalog,
        Func<string, ScannerItemSnapshot?> snapshotProvider)
    {
        _ocr = ocr ?? throw new ArgumentNullException(nameof(ocr));
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        _snapshotProvider = snapshotProvider ?? throw new ArgumentNullException(nameof(snapshotProvider));
    }

    public async Task<ScannerGroundTruthRegressionResult> RunAsync(
        string datasetRoot,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(datasetRoot);
        cancellationToken.ThrowIfCancellationRequested();

        if (!_ocr.IsAvailable)
            throw new InvalidOperationException(_ocr.AvailabilityMessage);
        if (!_catalog.HasHealthyCatalog)
            throw new InvalidOperationException("현재 게임 모드의 Scanner 아이템 카탈로그가 준비되지 않았습니다.");

        var root = Path.GetFullPath(datasetRoot);
        var casesRoot = Path.Combine(root, "cases");
        Directory.CreateDirectory(casesRoot);

        var catalogByNormalizedName = _catalog.GetItemsSnapshot()
            .Where(item => !string.IsNullOrWhiteSpace(item.Id) && !string.IsNullOrWhiteSpace(item.OfficialName))
            .GroupBy(item => ScannerItemMatcher.Normalize(item.OfficialName), StringComparer.Ordinal)
            .Where(group => group.Key.Length > 0)
            .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);

        var results = new List<CaseReplayResult>();
        var caseFiles = Directory
            .EnumerateFiles(casesRoot, "case.json", SearchOption.AllDirectories)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        var reviewedCases = 0;
        foreach (var caseFile in caseFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            CaseExpectation? expected;
            try
            {
                expected = ReadExpectation(caseFile, catalogByNormalizedName);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
            {
                results.Add(CaseReplayResult.ReplayError(
                    Path.GetFileName(Path.GetDirectoryName(caseFile)) ?? "unknown",
                    exception.Message));
                continue;
            }

            if (expected is null)
                continue;
            reviewedCases++;

            try
            {
                var fullPath = Path.Combine(Path.GetDirectoryName(caseFile)!, "full.png");
                if (!File.Exists(fullPath))
                {
                    results.Add(CaseReplayResult.ReplayError(expected.CaseId, "full.png is missing"));
                    continue;
                }

                var image = LoadBgra(fullPath);
                var search = await ReplayImageAsync(image, cancellationToken);
                var currentCorrect = search.Recognition.Success && MatchesGroundTruth(search.Recognition, expected);
                var detailIou = expected.CorrectedDetail is { } detail && search.DetailBounds is { } predictedDetail
                    ? IntersectionOverUnion(predictedDetail, detail)
                    : (double?)null;
                var titleIou = expected.CorrectedTitle is { } title && search.TitleBounds is { } predictedTitle
                    ? IntersectionOverUnion(predictedTitle, title)
                    : (double?)null;
                var category = Classify(expected.PreviousCorrect, currentCorrect);

                results.Add(new CaseReplayResult(
                    expected.CaseId,
                    category,
                    expected.PreviousCorrect,
                    currentCorrect,
                    expected.GroundTruthName,
                    expected.GroundTruthItemId,
                    search.Recognition.Success,
                    search.Recognition.ItemId,
                    search.Recognition.OfficialName,
                    search.Recognition.Reason,
                    search.Recognition.Confidence,
                    search.Recognition.SecondScore,
                    search.Pass,
                    search.OcrRaw,
                    search.MatcherText,
                    ToRoi(search.DetailBounds),
                    ToRoi(search.TitleBounds),
                    detailIou,
                    titleIou,
                    ToCandidateEvidence(search.Recognition.TopCandidates),
                    CompareMappedData(expected, search.Recognition),
                    null));
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception) when (exception is not OutOfMemoryException)
            {
                results.Add(CaseReplayResult.ReplayError(
                    expected.CaseId,
                    $"{exception.GetType().Name}: {exception.Message}"));
            }
        }

        var executed = results.Count(result => result.Category != "ERROR");
        var stillCorrect = results.Count(result => result.Category == "STILL_CORRECT");
        var solved = results.Count(result => result.Category == "SOLVED");
        var stillFailing = results.Count(result => result.Category == "STILL_FAILING");
        var regressions = results.Count(result => result.Category == "REGRESSION");
        var errors = results.Count(result => result.Category == "ERROR");
        var currentCorrectCount = results.Count(result => result.Category is "STILL_CORRECT" or "SOLVED");
        double? currentAccuracy = executed > 0
            ? currentCorrectCount / (double)executed
            : null;

        var document = new RegressionDocument(
            DateTimeOffset.UtcNow,
            "full_pipeline_replay_v1",
            reviewedCases,
            executed,
            stillCorrect,
            solved,
            stillFailing,
            regressions,
            errors,
            currentAccuracy,
            results);

        Directory.CreateDirectory(root);
        var jsonPath = Path.Combine(root, "regression.json");
        var markdownPath = Path.Combine(root, "regression.md");
        WriteAtomic(jsonPath, JsonSerializer.Serialize(document, JsonOptions));
        WriteAtomic(markdownPath, BuildMarkdown(document));

        return new ScannerGroundTruthRegressionResult(
            reviewedCases,
            executed,
            stillCorrect,
            solved,
            stillFailing,
            regressions,
            errors,
            currentAccuracy,
            jsonPath,
            markdownPath);
    }

    private async Task<ReplaySearchResult> ReplayImageAsync(
        BgraImage image,
        CancellationToken cancellationToken)
    {
        var structural = ScannerDetailGeometryDetector.FindCandidates(
            image.Pixels,
            image.Width,
            image.Height,
            image.Stride,
            12);
        if (structural.Count == 0)
            return ReplaySearchResult.Failed(ScannerRecognition.Failed("DETAIL_WINDOW_NOT_DETECTED"));

        var candidates = new List<ReplayCandidate>(structural.Count);
        foreach (var candidate in structural)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var anchors = ScannerTitleAnchorRefiner.Refine(
                image.Pixels,
                image.Width,
                image.Height,
                image.Stride,
                candidate);

            if (!IsSemanticReady(anchors))
            {
                candidates.Add(new ReplayCandidate(
                    candidate,
                    anchors,
                    null,
                    ToRect(candidate.Window),
                    anchors.Title.Width > 0 ? ToRect(anchors.Title) : null));
                continue;
            }

            candidates.Add(new ReplayCandidate(
                candidate,
                anchors,
                CropToBitmapSource(image, anchors.Title),
                ToRect(RefineLockedWindow(candidate.Window, anchors, image.Width, image.Height)),
                ToRect(anchors.Title)));
        }

        var ordered = candidates
            .OrderByDescending(candidate => candidate.SemanticReady)
            .ThenByDescending(candidate => candidate.Structural.Score)
            .Take(12)
            .ToArray();
        var limit = Math.Min(CandidateLimit, ordered.Length);
        ReplaySearchResult? bestSuccess = null;
        ReplaySearchResult? bestFailure = null;

        for (var index = 0; index < limit; index++)
        {
            var candidate = ordered[index];
            if (candidate.Structural.Score < CandidateStructuralFloor)
                continue;

            if (!candidate.SemanticReady || candidate.TitleImage is null)
            {
                var rejected = ReplaySearchResult.FromCandidate(
                    candidate,
                    ScannerRecognition.Failed("TITLE_ANCHOR_INCOMPLETE"),
                    string.Empty,
                    string.Empty,
                    "ORIGINAL",
                    candidate.Structural.Score * 0.05);
                bestFailure = PickBetterFailure(bestFailure, rejected);
                continue;
            }

            var text = await _ocr.ReadTextAsync(candidate.TitleImage, cancellationToken);
            var recognition = _catalog.ResolveOcrText(text, out var assessment);
            var result = ReplaySearchResult.FromCandidate(
                candidate,
                recognition,
                text,
                assessment.FilteredText,
                "ORIGINAL",
                CombinedScore(candidate, recognition, 0.82, 0.18));
            bestFailure = PickBetterFailure(bestFailure, result);
            if (recognition.Success && (bestSuccess is null || result.CombinedScore > bestSuccess.CombinedScore))
                bestSuccess = result;
        }

        if (bestSuccess is not null)
            return bestSuccess;

        if (_ocr is IScannerDeepOcrEngine deepOcr)
        {
            var deepLimit = Math.Min(DeepOcrCandidateLimit, limit);
            for (var index = 0; index < deepLimit; index++)
            {
                var candidate = ordered[index];
                if (candidate.Structural.Score < CandidateStructuralFloor ||
                    !candidate.SemanticReady || candidate.TitleImage is null)
                {
                    continue;
                }

                var text = await deepOcr.ReadDeepTextAsync(candidate.TitleImage, cancellationToken);
                var recognition = _catalog.ResolveOcrText(text, out var assessment);
                var result = ReplaySearchResult.FromCandidate(
                    candidate,
                    recognition,
                    text,
                    assessment.FilteredText,
                    "DEEP",
                    CombinedScore(candidate, recognition, 0.86, 0.14));
                bestFailure = PickBetterFailure(bestFailure, result);
                if (recognition.Success && (bestSuccess is null || result.CombinedScore > bestSuccess.CombinedScore))
                    bestSuccess = result;
            }
        }

        return bestSuccess ?? bestFailure ?? ReplaySearchResult.Failed(ScannerRecognition.Failed("EMPTY_OCR"));
    }

    private ScannerMappedDataComparison? CompareMappedData(
        CaseExpectation expected,
        ScannerRecognition recognition)
    {
        if (expected.PreviousCorrect != true || expected.MappedData is null ||
            !recognition.Success || string.IsNullOrWhiteSpace(recognition.ItemId))
        {
            return null;
        }

        var current = _snapshotProvider(recognition.ItemId);
        if (current is null)
            return new ScannerMappedDataComparison(false, "CURRENT_SNAPSHOT_UNAVAILABLE");

        var previous = expected.MappedData;
        var matches = string.Equals(previous.ItemId, current.ItemId, StringComparison.Ordinal) &&
                      previous.TraderSellPrice == current.TraderSellPrice &&
                      previous.FleaAveragePrice == current.FleaAveragePrice &&
                      previous.TraderPricePerSlot == current.TraderPricePerSlot &&
                      previous.FleaPricePerSlot == current.FleaPricePerSlot &&
                      previous.Slots == current.Slots &&
                      previous.RequiredTotal == current.CurrentNeeded;
        return new ScannerMappedDataComparison(matches, matches ? "UNCHANGED" : "MAPPED_DATA_CHANGED");
    }

    private static bool MatchesGroundTruth(ScannerRecognition recognition, CaseExpectation expected)
    {
        if (!string.IsNullOrWhiteSpace(expected.GroundTruthItemId))
            return string.Equals(recognition.ItemId, expected.GroundTruthItemId, StringComparison.Ordinal);

        return string.Equals(
            ScannerItemMatcher.Normalize(recognition.OfficialName ?? string.Empty),
            ScannerItemMatcher.Normalize(expected.GroundTruthName),
            StringComparison.Ordinal);
    }

    private static CaseExpectation? ReadExpectation(
        string caseFile,
        IReadOnlyDictionary<string, ScannerCatalogItem[]> catalogByNormalizedName)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(caseFile, Encoding.UTF8));
        var root = document.RootElement;
        if (!root.TryGetProperty("review_status", out var review) || review.GetString() != "reviewed")
            return null;
        if (!root.TryGetProperty("fields", out var fields) ||
            !fields.TryGetProperty("item_name", out var itemName))
        {
            return null;
        }

        var groundTruth = GetString(itemName, "ground_truth");
        if (groundTruth.Length == 0 &&
            root.TryGetProperty("user_confirmed", out var confirmed) &&
            confirmed.ValueKind == JsonValueKind.True)
        {
            groundTruth = GetString(itemName, "program_result");
        }
        if (groundTruth.Length == 0)
            return null;

        var normalizedGroundTruth = ScannerItemMatcher.Normalize(groundTruth);
        string? expectedItemId = null;
        if (catalogByNormalizedName.TryGetValue(normalizedGroundTruth, out var matches) && matches.Length == 1)
            expectedItemId = matches[0].Id;

        bool? previousCorrect = null;
        if (root.TryGetProperty("program_correct", out var previous))
        {
            previousCorrect = previous.ValueKind switch
            {
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                _ => null,
            };
        }

        Rect? correctedDetail = null;
        if (root.TryGetProperty("detail_window", out var detail))
            correctedDetail = ReadRoi(detail, "corrected_roi");

        return new CaseExpectation(
            GetString(root, "case_id"),
            groundTruth,
            expectedItemId,
            previousCorrect,
            correctedDetail,
            ReadRoi(itemName, "corrected_roi"),
            ReadMappedData(root));
    }

    private static StoredMappedData? ReadMappedData(JsonElement root)
    {
        if (!root.TryGetProperty("mapped_data", out var mapped) || mapped.ValueKind != JsonValueKind.Object)
            return null;
        return new StoredMappedData(
            GetString(mapped, "item_id"),
            GetNullableInt(mapped, "highest_trader_sell_price"),
            GetNullableInt(mapped, "flea_average_price"),
            GetNullableInt(mapped, "trader_price_per_slot"),
            GetNullableInt(mapped, "flea_price_per_slot"),
            GetInt(mapped, "slots"),
            GetInt(mapped, "required_total"));
    }

    private static Rect? ReadRoi(JsonElement parent, string property)
    {
        if (!parent.TryGetProperty(property, out var roi) || roi.ValueKind != JsonValueKind.Object)
            return null;
        if (!TryGetDouble(roi, "x", out var x) ||
            !TryGetDouble(roi, "y", out var y) ||
            !TryGetDouble(roi, "width", out var width) ||
            !TryGetDouble(roi, "height", out var height) ||
            width <= 0 || height <= 0)
        {
            return null;
        }
        return new Rect(x, y, width, height);
    }

    private static ReplaySearchResult? PickBetterFailure(
        ReplaySearchResult? current,
        ReplaySearchResult candidate)
    {
        if (current is null)
            return candidate;
        if (!string.IsNullOrWhiteSpace(candidate.MatcherText) && string.IsNullOrWhiteSpace(current.MatcherText))
            return candidate;
        if (candidate.Recognition.Confidence > current.Recognition.Confidence)
            return candidate;
        if (Math.Abs(candidate.Recognition.Confidence - current.Recognition.Confidence) < 0.0001 &&
            candidate.CombinedScore > current.CombinedScore)
        {
            return candidate;
        }
        return current;
    }

    private static double CombinedScore(
        ReplayCandidate candidate,
        ScannerRecognition recognition,
        double semanticWeight,
        double structuralWeight) =>
        recognition.Confidence * semanticWeight +
        (candidate.Structural.Score * 0.55 + candidate.Anchors.Score * 0.45) * structuralWeight;

    private static bool IsSemanticReady(ScannerTitleAnchorRefinement anchors) =>
        anchors.Title.Width > 0 && anchors.Title.Height > 0 &&
        anchors.Magnifier.Width > 0 && anchors.Magnifier.Height > 0 &&
        anchors.CloseButton.Width > 0 && anchors.CloseButton.Height > 0 &&
        anchors.Score >= 0.68 &&
        string.Equals(anchors.Reason, "HEADER_FRAME_LOCKED", StringComparison.Ordinal);

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
        var structuralBottom = Math.Clamp(structural.Y + structural.Height, top + 80, height);
        return new ScannerDetectedRegion(
            left,
            top,
            right - left,
            structuralBottom - top,
            structural.Score);
    }

    private static BitmapSource CropToBitmapSource(BgraImage source, ScannerDetectedRegion region)
    {
        var x = Math.Clamp(region.X, 0, source.Width - 1);
        var y = Math.Clamp(region.Y, 0, source.Height - 1);
        var width = Math.Clamp(region.Width, 1, source.Width - x);
        var height = Math.Clamp(region.Height, 1, source.Height - y);
        var stride = width * 4;
        var pixels = new byte[stride * height];
        for (var row = 0; row < height; row++)
        {
            Buffer.BlockCopy(
                source.Pixels,
                (y + row) * source.Stride + x * 4,
                pixels,
                row * stride,
                stride);
        }

        var result = BitmapSource.Create(
            width,
            height,
            96,
            96,
            PixelFormats.Bgra32,
            null,
            pixels,
            stride);
        result.Freeze();
        return result;
    }

    private static BgraImage LoadBgra(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        var decoder = BitmapDecoder.Create(
            stream,
            BitmapCreateOptions.PreservePixelFormat,
            BitmapCacheOption.OnLoad);
        var frame = decoder.Frames[0];
        BitmapSource source = frame;
        if (source.Format != PixelFormats.Bgra32)
        {
            var converted = new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0);
            converted.Freeze();
            source = converted;
        }

        var stride = source.PixelWidth * 4;
        var pixels = new byte[stride * source.PixelHeight];
        source.CopyPixels(pixels, stride, 0);
        return new BgraImage(source.PixelWidth, source.PixelHeight, stride, pixels);
    }

    private static double IntersectionOverUnion(Rect left, Rect right)
    {
        var intersection = Rect.Intersect(left, right);
        if (intersection.IsEmpty || intersection.Width <= 0 || intersection.Height <= 0)
            return 0;
        var intersectionArea = intersection.Width * intersection.Height;
        var unionArea = left.Width * left.Height + right.Width * right.Height - intersectionArea;
        return unionArea <= 0 ? 0 : intersectionArea / unionArea;
    }

    private static object? ToRoi(Rect? rect) => rect is not { } value
        ? null
        : new { x = value.X, y = value.Y, width = value.Width, height = value.Height };

    private static object[] ToCandidateEvidence(IReadOnlyList<ScannerMatchCandidate>? candidates) =>
        (candidates ?? [])
            .Take(10)
            .Select((candidate, index) => (object)new
            {
                rank = index + 1,
                item_id = candidate.ItemId,
                official_name = candidate.OfficialName,
                score = candidate.Score,
            })
            .ToArray();

    private static Rect ToRect(ScannerDetectedRegion region) =>
        new(region.X, region.Y, region.Width, region.Height);

    private static string Classify(bool? previousCorrect, bool currentCorrect) =>
        previousCorrect switch
        {
            true when currentCorrect => "STILL_CORRECT",
            true => "REGRESSION",
            false when currentCorrect => "SOLVED",
            false => "STILL_FAILING",
            _ when currentCorrect => "CURRENT_CORRECT_NO_BASELINE",
            _ => "CURRENT_FAIL_NO_BASELINE",
        };

    private static string BuildMarkdown(RegressionDocument document)
    {
        var builder = new StringBuilder()
            .AppendLine("# Scanner Full-Pipeline Regression")
            .AppendLine()
            .AppendLine($"- Generated: {document.GeneratedAt:O}")
            .AppendLine($"- Mode: {document.Mode}")
            .AppendLine($"- Reviewed cases with final Ground Truth: {document.ReviewedCases}")
            .AppendLine($"- Executed cases: {document.ExecutedCases}")
            .AppendLine($"- Current accuracy: {(document.CurrentAccuracy is null ? "n/a" : document.CurrentAccuracy.Value.ToString("P2"))}")
            .AppendLine($"- Still correct: {document.StillCorrect}")
            .AppendLine($"- Solved: {document.Solved}")
            .AppendLine($"- Still failing: {document.StillFailing}")
            .AppendLine($"- Regressions: {document.Regressions}")
            .AppendLine($"- Replay errors: {document.Errors}")
            .AppendLine();

        AppendCaseSection(builder, "Regressions", document.Cases.Where(item => item.Category == "REGRESSION"));
        AppendCaseSection(builder, "Solved", document.Cases.Where(item => item.Category == "SOLVED"));
        AppendCaseSection(builder, "Still failing", document.Cases.Where(item => item.Category == "STILL_FAILING"));
        AppendCaseSection(builder, "Replay errors", document.Cases.Where(item => item.Category == "ERROR"));
        return builder.ToString();
    }

    private static void AppendCaseSection(
        StringBuilder builder,
        string heading,
        IEnumerable<CaseReplayResult> cases)
    {
        var materialized = cases.ToArray();
        builder.AppendLine($"## {heading}");
        if (materialized.Length == 0)
        {
            builder.AppendLine("- None").AppendLine();
            return;
        }

        foreach (var item in materialized)
        {
            builder.Append("- ").Append(item.CaseId);
            if (!string.IsNullOrWhiteSpace(item.GroundTruthName))
                builder.Append(" | GT: ").Append(item.GroundTruthName);
            if (!string.IsNullOrWhiteSpace(item.CurrentOfficialName))
                builder.Append(" | current: ").Append(item.CurrentOfficialName);
            if (!string.IsNullOrWhiteSpace(item.RecognitionReason))
                builder.Append(" | ").Append(item.RecognitionReason);
            if (!string.IsNullOrWhiteSpace(item.Error))
                builder.Append(" | ").Append(item.Error);
            builder.AppendLine();
        }
        builder.AppendLine();
    }

    private static void WriteAtomic(string path, string content)
    {
        var temporary = path + ".tmp";
        File.WriteAllText(temporary, content, new UTF8Encoding(false));
        File.Move(temporary, path, overwrite: true);
    }

    private static string GetString(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()?.Trim() ?? string.Empty
            : string.Empty;

    private static int GetInt(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.TryGetInt32(out var parsed) ? parsed : 0;

    private static int? GetNullableInt(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.TryGetInt32(out var parsed) ? parsed : null;

    private static bool TryGetDouble(JsonElement element, string property, out double result)
    {
        result = 0;
        return element.TryGetProperty(property, out var value) && value.TryGetDouble(out result);
    }

    private sealed record BgraImage(int Width, int Height, int Stride, byte[] Pixels);

    private sealed record ReplayCandidate(
        ScannerDetectedCandidate Structural,
        ScannerTitleAnchorRefinement Anchors,
        BitmapSource? TitleImage,
        Rect? DetailBounds,
        Rect? TitleBounds)
    {
        public bool SemanticReady => TitleImage is not null && IsSemanticReady(Anchors);
    }

    private sealed record ReplaySearchResult(
        ScannerRecognition Recognition,
        string OcrRaw,
        string MatcherText,
        string Pass,
        Rect? DetailBounds,
        Rect? TitleBounds,
        double CombinedScore)
    {
        public static ReplaySearchResult Failed(ScannerRecognition recognition) =>
            new(recognition, string.Empty, string.Empty, "NONE", null, null, 0);

        public static ReplaySearchResult FromCandidate(
            ReplayCandidate candidate,
            ScannerRecognition recognition,
            string raw,
            string matcher,
            string pass,
            double combinedScore) =>
            new(recognition, raw, matcher, pass, candidate.DetailBounds, candidate.TitleBounds, combinedScore);
    }

    private sealed record CaseExpectation(
        string CaseId,
        string GroundTruthName,
        string? GroundTruthItemId,
        bool? PreviousCorrect,
        Rect? CorrectedDetail,
        Rect? CorrectedTitle,
        StoredMappedData? MappedData);

    private sealed record StoredMappedData(
        string ItemId,
        int? TraderSellPrice,
        int? FleaAveragePrice,
        int? TraderPricePerSlot,
        int? FleaPricePerSlot,
        int Slots,
        int RequiredTotal);

    private sealed record ScannerMappedDataComparison(bool Matches, string Reason);

    private sealed record RegressionDocument(
        DateTimeOffset GeneratedAt,
        string Mode,
        int ReviewedCases,
        int ExecutedCases,
        int StillCorrect,
        int Solved,
        int StillFailing,
        int Regressions,
        int Errors,
        double? CurrentAccuracy,
        IReadOnlyList<CaseReplayResult> Cases);

    private sealed record CaseReplayResult(
        string CaseId,
        string Category,
        bool? PreviousCorrect,
        bool CurrentCorrect,
        string GroundTruthName,
        string? GroundTruthItemId,
        bool RecognitionSuccess,
        string? CurrentItemId,
        string? CurrentOfficialName,
        string RecognitionReason,
        double Confidence,
        double SecondScore,
        string Pass,
        string OcrRaw,
        string MatcherText,
        object? PredictedDetailRoi,
        object? PredictedTitleRoi,
        double? DetailIou,
        double? TitleIou,
        object[] TopCandidates,
        ScannerMappedDataComparison? MappedData,
        string? Error)
    {
        public static CaseReplayResult ReplayError(string caseId, string error) => new(
            caseId,
            "ERROR",
            null,
            false,
            string.Empty,
            null,
            false,
            null,
            null,
            "REPLAY_ERROR",
            0,
            0,
            "NONE",
            string.Empty,
            string.Empty,
            null,
            null,
            null,
            null,
            [],
            null,
            error);
    }
}
