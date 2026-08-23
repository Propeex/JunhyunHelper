using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Windows.Media.Ocr;

namespace JunhyunHelper.Desktop.Scanner;

public enum ScannerDiagnosticErrorType
{
    None,
    DetailWindowDetection,
    FieldLocalization,
    OcrRecognition,
    CandidateMatching,
    Parsing,
    DataMapping,
    UnknownMultiple,
}

public sealed record ScannerCorrectionSubmission(
    ScannerRecognitionDebugFrame Frame,
    Rect? CorrectedDetailBounds,
    Rect? CorrectedTitleBounds,
    string? GroundTruthItemName,
    bool UserConfirmed,
    ScannerItemSnapshot? Presentation = null);

public sealed record ScannerDatasetStorageInfo(int CaseCount, long Bytes)
{
    public string SizeText => Bytes < 1024 * 1024
        ? $"{Bytes / 1024d:F1} KB"
        : Bytes < 1024L * 1024 * 1024
            ? $"{Bytes / (1024d * 1024):F1} MB"
            : $"{Bytes / (1024d * 1024 * 1024):F2} GB";
}

public sealed record ScannerDatasetSaveResult(bool Success, string CaseId, string Message);

/// <summary>
/// Scanner correction/diagnostic persistence boundary. Runtime recognition remains
/// independent from this best-effort subsystem: failures here are logged and never alter
/// recognition decisions. Original pixels are retained only for bounded automatic samples
/// or user-confirmed/corrected cases.
/// </summary>
public static class ScannerDiagnosticDataset
{
    private const string ScannerDatasetVersion = "ground-truth-v1";
    private const double LowConfidenceSampleThreshold = 0.93;
    private const int MaximumAutomaticFingerprints = 512;
    private static readonly object Gate = new();
    private static readonly HashSet<string> AutomaticFingerprints = new(StringComparer.Ordinal);
    private static readonly Queue<string> AutomaticFingerprintOrder = new();
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };
    private static readonly JsonSerializerOptions CompactJsonOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    public static string RootPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "JunhyunHelper",
        "scanner",
        "diagnostics");

    public static ScannerDatasetStorageInfo GetStorageInfo()
    {
        lock (Gate)
        {
            if (!Directory.Exists(RootPath))
                return new ScannerDatasetStorageInfo(0, 0);

            var casesPath = Path.Combine(RootPath, "cases");
            var caseCount = Directory.Exists(casesPath)
                ? Directory.EnumerateDirectories(casesPath, "case_*", SearchOption.TopDirectoryOnly).Count()
                : 0;
            long bytes = 0;
            try
            {
                foreach (var file in Directory.EnumerateFiles(RootPath, "*", SearchOption.AllDirectories))
                    bytes += new FileInfo(file).Length;
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
            return new ScannerDatasetStorageInfo(caseCount, bytes);
        }
    }

    public static void QueueAutomaticObservation(ScannerRecognitionDebugFrame? frame)
    {
        if (frame is null || !ShouldRetainAutomatically(frame))
            return;
        var fingerprint = BuildAutomaticFingerprint(frame);
        lock (Gate)
        {
            if (!AutomaticFingerprints.Add(fingerprint))
                return;
            AutomaticFingerprintOrder.Enqueue(fingerprint);
            while (AutomaticFingerprintOrder.Count > MaximumAutomaticFingerprints)
            {
                var old = AutomaticFingerprintOrder.Dequeue();
                AutomaticFingerprints.Remove(old);
            }
        }

        _ = Task.Run(() =>
        {
            try
            {
                PersistCase(new ScannerCorrectionSubmission(frame, null, null, null, false), automatic: true);
            }
            catch (Exception exception)
            {
                App.WriteDiagnostic("Scanner automatic diagnostic case persistence failed", exception);
            }
        });
    }

    public static Task<ScannerDatasetSaveResult> SaveCorrectionAsync(
        ScannerCorrectionSubmission submission,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(submission);
        cancellationToken.ThrowIfCancellationRequested();
        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                return PersistCase(submission, automatic: false);
            }
            catch (Exception exception) when (
                exception is IOException or UnauthorizedAccessException or NotSupportedException or JsonException)
            {
                return new ScannerDatasetSaveResult(false, submission.Frame.CaseId, exception.Message);
            }
        }, cancellationToken);
    }

    public static Task ExportAsync(string destinationZipPath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationZipPath);
        cancellationToken.ThrowIfCancellationRequested();
        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            lock (Gate)
            {
                EnsureDatasetScaffold();
                RebuildIndexesUnsafe();

                var destination = Path.GetFullPath(destinationZipPath);
                var directory = Path.GetDirectoryName(destination);
                if (!string.IsNullOrWhiteSpace(directory))
                    Directory.CreateDirectory(directory);
                var temporary = destination + ".tmp";
                if (File.Exists(temporary))
                    File.Delete(temporary);

                try
                {
                    using (var archive = ZipFile.Open(temporary, ZipArchiveMode.Create))
                    {
                        AddDirectoryToArchive(archive, RootPath, RootPath, cancellationToken);
                        AddLogIfPresent(archive, ScannerDiagnosticLog.Path, "logs/scanner.log");
                        AddLogIfPresent(archive, ScannerDiagnosticLog.Path + ".1", "logs/scanner.log.1");
                    }
                    File.Move(temporary, destination, overwrite: true);
                }
                finally
                {
                    if (File.Exists(temporary))
                        File.Delete(temporary);
                }
            }
        }, cancellationToken);
    }

    public static bool ClearAll()
    {
        lock (Gate)
        {
            try
            {
                if (Directory.Exists(RootPath))
                    Directory.Delete(RootPath, recursive: true);
                AutomaticFingerprints.Clear();
                AutomaticFingerprintOrder.Clear();
                return true;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                App.WriteDiagnostic("Scanner diagnostic dataset clear failed", exception);
                return false;
            }
        }
    }

    private static ScannerDatasetSaveResult PersistCase(ScannerCorrectionSubmission submission, bool automatic)
    {
        var frame = submission.Frame;
        if (string.IsNullOrWhiteSpace(frame.CaseId))
            return new ScannerDatasetSaveResult(false, string.Empty, "Case ID가 없습니다.");
        if (frame.Image.PixelWidth <= 0 || frame.Image.PixelHeight <= 0)
            return new ScannerDatasetSaveResult(false, frame.CaseId, "원본 캡처 이미지가 유효하지 않습니다.");
        if (submission.UserConfirmed && string.IsNullOrWhiteSpace(frame.CandidateName))
            return new ScannerDatasetSaveResult(false, frame.CaseId, "확정할 프로그램 판정값이 없습니다.");

        lock (Gate)
        {
            EnsureDatasetScaffold();
            var casesPath = Path.Combine(RootPath, "cases");
            var casePath = Path.Combine(casesPath, SafeCaseId(frame.CaseId));
            Directory.CreateDirectory(casePath);
            var itemNamePath = Path.Combine(casePath, "item_name");
            Directory.CreateDirectory(itemNamePath);

            SavePng(frame.Image, Path.Combine(casePath, "full.png"));

            var detailDetected = ClampRect(frame.SelectedBounds, frame.Image.PixelWidth, frame.Image.PixelHeight);
            var detailCorrected = ClampRect(submission.CorrectedDetailBounds, frame.Image.PixelWidth, frame.Image.PixelHeight);
            var detailGroundTruth = detailCorrected ?? detailDetected;
            if (detailGroundTruth is { } detail)
                SavePng(Crop(frame.Image, detail), Path.Combine(casePath, "detail_window.png"));

            var titleDetected = ClampRect(frame.TitleBounds, frame.Image.PixelWidth, frame.Image.PixelHeight);
            var titleCorrected = ClampRect(submission.CorrectedTitleBounds, frame.Image.PixelWidth, frame.Image.PixelHeight);
            if (titleDetected is { } detectedTitle)
            {
                var detectedImage = Crop(frame.Image, detectedTitle);
                SavePng(detectedImage, Path.Combine(itemNamePath, "detected_roi.png"));
                SaveOcrVariants(detectedImage, itemNamePath);
            }
            if (titleCorrected is { } correctedTitle)
                SavePng(Crop(frame.Image, correctedTitle), Path.Combine(itemNamePath, "corrected_roi.png"));

            SavePng(
                RenderAnnotated(frame, detailCorrected, titleCorrected),
                Path.Combine(casePath, "annotated.png"));

            var groundTruth = NormalizeText(submission.GroundTruthItemName);
            if (submission.UserConfirmed && groundTruth.Length == 0)
                groundTruth = NormalizeText(frame.CandidateName);

            var reviewed = IsReviewed(submission, groundTruth, detailCorrected, titleCorrected);
            var groundTruthErrorType = reviewed
                ? ClassifyError(frame, detailDetected, detailCorrected, titleDetected, titleCorrected, groundTruth)
                : (ScannerDiagnosticErrorType?)null;
            var pipelineStage = DeterminePipelineStage(frame);
            var metadata = BuildCaseMetadata(
                submission,
                automatic,
                reviewed,
                detailDetected,
                detailCorrected,
                titleDetected,
                titleCorrected,
                groundTruth,
                groundTruthErrorType,
                pipelineStage);
            File.WriteAllText(
                Path.Combine(casePath, "case.json"),
                JsonSerializer.Serialize(metadata, JsonOptions),
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

            RebuildIndexesUnsafe();
            ScannerDiagnosticLog.Write(
                automatic ? "diagnostic-case-auto" : "diagnostic-case-user",
                frame.CaptureMode,
                ("caseId", frame.CaseId),
                ("groundTruthErrorType", groundTruthErrorType is null ? "UNREVIEWED" : ErrorTypeText(groundTruthErrorType.Value)),
                ("pipelineStage", pipelineStage),
                ("groundTruth", groundTruth),
                ("path", casePath));

            return new ScannerDatasetSaveResult(
                true,
                frame.CaseId,
                automatic ? "진단 사례를 보존했습니다." : "교정/검증 데이터를 저장했습니다.");
        }
    }

    private static object BuildCaseMetadata(
        ScannerCorrectionSubmission submission,
        bool automatic,
        bool reviewed,
        Rect? detailDetected,
        Rect? detailCorrected,
        Rect? titleDetected,
        Rect? titleCorrected,
        string groundTruth,
        ScannerDiagnosticErrorType? groundTruthErrorType,
        string pipelineStage)
    {
        var frame = submission.Frame;
        var programVersion = Assembly.GetEntryAssembly()?.GetName().Version?.ToString()
            ?? Assembly.GetExecutingAssembly().GetName().Version?.ToString()
            ?? "unknown";
        var detailDelta = BuildDelta(detailDetected, detailCorrected);
        var titleDelta = BuildDelta(titleDetected, titleCorrected);
        var presentation = submission.Presentation;
        var hasFinalGroundTruth = groundTruth.Length > 0 || submission.UserConfirmed;
        var confirmedCorrect = submission.UserConfirmed ||
            (groundTruth.Length > 0 && string.Equals(
                NormalizeComparison(frame.CandidateName),
                NormalizeComparison(groundTruth),
                StringComparison.Ordinal));

        return new
        {
            case_id = frame.CaseId,
            dataset_version = ScannerDatasetVersion,
            program_version = programVersion,
            scanner_version = "scanner-lab-3.8",
            timestamp = frame.Timestamp.ToUniversalTime().ToString("O"),
            capture_mode = frame.CaptureMode?.ToString() ?? "Unknown",
            source = frame.Source,
            retention = automatic ? "automatic_sample" : "user_reviewed",
            review_status = reviewed ? "reviewed" : "unreviewed",
            user_confirmed = submission.UserConfirmed,
            program_correct = hasFinalGroundTruth ? confirmedCorrect : (bool?)null,
            pipeline = new
            {
                stage = pipelineStage,
                recognition_reason = frame.RecognitionReason,
                pass = frame.Pass,
            },
            screen = new
            {
                width = frame.Image.PixelWidth,
                height = frame.Image.PixelHeight,
                capture_origin_x = frame.CaptureOriginX,
                capture_origin_y = frame.CaptureOriginY,
                dpi_x = frame.Image.DpiX,
                dpi_y = frame.Image.DpiY,
                windows_system_dpi = TryGetSystemDpi(),
            },
            detail_window = new
            {
                detected_roi = BuildRoi(detailDetected, frame.Image.PixelWidth, frame.Image.PixelHeight),
                corrected_roi = BuildRoi(detailCorrected, frame.Image.PixelWidth, frame.Image.PixelHeight),
                delta = detailDelta,
                structural_confidence = frame.StructuralScore,
                structural_reason = frame.StructuralReason,
                header_confidence = frame.TitleAnchorScore,
                header_reason = frame.TitleAnchorReason,
            },
            fields = new
            {
                item_name = new
                {
                    detected_roi = BuildRoi(titleDetected, frame.Image.PixelWidth, frame.Image.PixelHeight),
                    corrected_roi = BuildRoi(titleCorrected, frame.Image.PixelWidth, frame.Image.PixelHeight),
                    delta = titleDelta,
                    ocr_raw = frame.OcrText,
                    ocr_normalized = frame.MatcherText,
                    program_result = frame.CandidateName ?? string.Empty,
                    program_item_id = frame.ItemId ?? string.Empty,
                    ground_truth = groundTruth,
                    confidence = frame.Confidence,
                    second_score = frame.SecondScore,
                    match_margin = Math.Max(0, frame.Confidence - frame.SecondScore),
                    recognition_reason = frame.RecognitionReason,
                    pass = frame.Pass,
                    ground_truth_error_type = groundTruthErrorType is null ? null : ErrorTypeText(groundTruthErrorType.Value),
                },
            },
            mapped_data = presentation is null ? null : new
            {
                item_id = presentation.ItemId,
                official_name = presentation.OfficialName,
                highest_trader_sell_price = presentation.TraderSellPrice,
                flea_average_price = presentation.FleaAveragePrice,
                trader_price_per_slot = presentation.TraderPricePerSlot,
                flea_price_per_slot = presentation.FleaPricePerSlot,
                slots = presentation.Slots,
                required_total = presentation.CurrentNeeded,
            },
            artifacts = new
            {
                full = "full.png",
                detail_window = detailGroundTruthArtifact(detailDetected, detailCorrected),
                annotated = "annotated.png",
                item_name_detected = titleDetected is null ? null : "item_name/detected_roi.png",
                item_name_corrected = titleCorrected is null ? null : "item_name/corrected_roi.png",
                item_name_processed = titleDetected is null ? null : "item_name/processed_roi.png",
            },
        };
    }

    private static string? detailGroundTruthArtifact(Rect? detected, Rect? corrected) =>
        detected is null && corrected is null ? null : "detail_window.png";

    private static bool IsReviewed(
        ScannerCorrectionSubmission submission,
        string groundTruth,
        Rect? correctedDetail,
        Rect? correctedTitle) =>
        submission.UserConfirmed || groundTruth.Length > 0 || correctedDetail is not null || correctedTitle is not null;

    private static ScannerDiagnosticErrorType ClassifyError(
        ScannerRecognitionDebugFrame frame,
        Rect? detailDetected,
        Rect? detailCorrected,
        Rect? titleDetected,
        Rect? titleCorrected,
        string groundTruth)
    {
        var detailChanged = IsMeaningfullyDifferent(detailDetected, detailCorrected);
        var titleChanged = IsMeaningfullyDifferent(titleDetected, titleCorrected);
        var textChanged = groundTruth.Length > 0 && !string.Equals(
            NormalizeComparison(frame.CandidateName),
            NormalizeComparison(groundTruth),
            StringComparison.Ordinal);
        var changes = (detailChanged ? 1 : 0) + (titleChanged ? 1 : 0) + (textChanged ? 1 : 0);
        if (changes > 1)
            return ScannerDiagnosticErrorType.UnknownMultiple;
        if (detailChanged)
            return ScannerDiagnosticErrorType.DetailWindowDetection;
        if (titleChanged)
            return ScannerDiagnosticErrorType.FieldLocalization;
        if (!textChanged)
            return ScannerDiagnosticErrorType.None;

        return string.Equals(
            NormalizeComparison(frame.MatcherText),
            NormalizeComparison(groundTruth),
            StringComparison.Ordinal)
            ? ScannerDiagnosticErrorType.CandidateMatching
            : ScannerDiagnosticErrorType.OcrRecognition;
    }

    private static string DeterminePipelineStage(ScannerRecognitionDebugFrame frame)
    {
        if (string.Equals(frame.RecognitionReason, "DETAIL_WINDOW_NOT_DETECTED", StringComparison.Ordinal))
            return "DETAIL_WINDOW_DETECTION_FAILED";
        if (string.Equals(frame.RecognitionReason, "TITLE_ANCHOR_NOT_LOCKED", StringComparison.Ordinal))
            return "DETAIL_HEADER_LOCK_FAILED";
        if (!string.IsNullOrWhiteSpace(frame.ItemId))
            return "FINALIZED";
        if (string.Equals(frame.RecognitionReason, "NOT_RUN", StringComparison.Ordinal))
            return "NOT_RUN";
        if (string.IsNullOrWhiteSpace(frame.OcrText))
            return "OCR_OR_PREPROCESSING_FAILED";
        return "IDENTITY_MATCH_FAILED";
    }

    private static bool ShouldRetainAutomatically(ScannerRecognitionDebugFrame frame)
    {
        if (string.IsNullOrWhiteSpace(frame.ItemId))
            return frame.RecognitionReason is not "NOT_RUN";
        if (frame.Confidence < LowConfidenceSampleThreshold)
            return true;
        if (string.IsNullOrWhiteSpace(frame.TitleSignature))
            return false;

        var checksum = 0;
        foreach (var character in frame.TitleSignature)
            checksum = unchecked(checksum * 31 + character);
        return Math.Abs(checksum % 20) == 0;
    }

    private static string BuildAutomaticFingerprint(ScannerRecognitionDebugFrame frame) =>
        string.Join('|',
            frame.CaptureMode?.ToString() ?? "Unknown",
            frame.Source,
            frame.TitleSignature ?? string.Empty,
            frame.RecognitionReason,
            frame.CandidateName ?? string.Empty,
            FormatRect(frame.SelectedBounds),
            FormatRect(frame.TitleBounds));

    private static string FormatRect(Rect? value) => value is not { } rect
        ? "-"
        : $"{Math.Round(rect.X)},{Math.Round(rect.Y)},{Math.Round(rect.Width)},{Math.Round(rect.Height)}";

    private static string SafeCaseId(string caseId)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var builder = new StringBuilder(caseId.Length);
        foreach (var character in caseId)
            builder.Append(invalid.Contains(character) ? '_' : character);
        return builder.ToString();
    }

    private static Rect? ClampRect(Rect? value, int width, int height)
    {
        if (value is not { } rect || rect.Width <= 0 || rect.Height <= 0 || width <= 0 || height <= 0)
            return null;
        var left = Math.Clamp(rect.X, 0, width - 1d);
        var top = Math.Clamp(rect.Y, 0, height - 1d);
        var right = Math.Clamp(rect.Right, left + 1, width);
        var bottom = Math.Clamp(rect.Bottom, top + 1, height);
        return new Rect(left, top, right - left, bottom - top);
    }

    private static BitmapSource Crop(BitmapSource source, Rect rect)
    {
        var pixelRect = new Int32Rect(
            Math.Clamp((int)Math.Floor(rect.X), 0, source.PixelWidth - 1),
            Math.Clamp((int)Math.Floor(rect.Y), 0, source.PixelHeight - 1),
            1,
            1);
        pixelRect.Width = Math.Clamp((int)Math.Ceiling(rect.Right) - pixelRect.X, 1, source.PixelWidth - pixelRect.X);
        pixelRect.Height = Math.Clamp((int)Math.Ceiling(rect.Bottom) - pixelRect.Y, 1, source.PixelHeight - pixelRect.Y);
        var cropped = new CroppedBitmap(source, pixelRect);
        cropped.Freeze();
        return cropped;
    }

    private static void SavePng(BitmapSource image, string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temporary = path + ".tmp";
        using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(image));
            encoder.Save(stream);
            stream.Flush(flushToDisk: true);
        }
        File.Move(temporary, path, overwrite: true);
    }

    private static void SaveOcrVariants(BitmapSource detectedTitleImage, string itemNamePath)
    {
        var primary = EnlargeTitle(detectedTitleImage);
        SavePng(primary, Path.Combine(itemNamePath, "processed_roi.png"));
        for (var mode = 1; mode <= 3; mode++)
            SavePng(CreateVariant(primary, mode), Path.Combine(itemNamePath, $"processed_variant_{mode}.png"));
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
            var output = mode switch
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

    private static BitmapSource RenderAnnotated(
        ScannerRecognitionDebugFrame frame,
        Rect? correctedDetail,
        Rect? correctedTitle)
    {
        var width = frame.Image.PixelWidth;
        var height = frame.Image.PixelHeight;
        var visual = new DrawingVisual();
        using (var context = visual.RenderOpen())
        {
            context.DrawImage(frame.Image, new Rect(0, 0, width, height));
            DrawRegion(context, frame.SelectedBounds, Brushes.Lime, 3);
            DrawRegion(context, frame.TitleBounds, Brushes.DeepSkyBlue, 2);
            DrawRegion(context, frame.MagnifierBounds, Brushes.Gold, 2);
            DrawRegion(context, frame.CloseBounds, Brushes.OrangeRed, 2);
            DrawRegion(context, correctedDetail, Brushes.Magenta, 3);
            DrawRegion(context, correctedTitle, Brushes.Cyan, 3);
        }
        var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        bitmap.Freeze();
        return bitmap;
    }

    private static void DrawRegion(DrawingContext context, Rect? region, Brush brush, double thickness)
    {
        if (region is not { } rect || rect.Width <= 0 || rect.Height <= 0)
            return;
        var pen = new Pen(brush, thickness);
        pen.Freeze();
        var inset = thickness / 2;
        context.DrawRectangle(
            null,
            pen,
            new Rect(
                rect.X + inset,
                rect.Y + inset,
                Math.Max(1, rect.Width - thickness),
                Math.Max(1, rect.Height - thickness)));
    }

    private static object? BuildRoi(Rect? value, int width, int height)
    {
        if (value is not { } rect)
            return null;
        return new
        {
            x = rect.X,
            y = rect.Y,
            width = rect.Width,
            height = rect.Height,
            relative_x = width > 0 ? rect.X / width : 0,
            relative_y = height > 0 ? rect.Y / height : 0,
            relative_width = width > 0 ? rect.Width / width : 0,
            relative_height = height > 0 ? rect.Height / height : 0,
        };
    }

    private static object? BuildDelta(Rect? detected, Rect? corrected)
    {
        if (detected is not { } left || corrected is not { } right)
            return null;
        return new
        {
            delta_x = right.X - left.X,
            delta_y = right.Y - left.Y,
            delta_width = right.Width - left.Width,
            delta_height = right.Height - left.Height,
        };
    }

    private static bool IsMeaningfullyDifferent(Rect? detected, Rect? corrected)
    {
        if (corrected is null)
            return false;
        if (detected is null)
            return true;
        var left = detected.Value;
        var right = corrected.Value;
        return Math.Abs(left.X - right.X) >= 0.5 ||
               Math.Abs(left.Y - right.Y) >= 0.5 ||
               Math.Abs(left.Width - right.Width) >= 0.5 ||
               Math.Abs(left.Height - right.Height) >= 0.5;
    }

    private static string NormalizeText(string? value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();

    private static string NormalizeComparison(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;
        return new string(value
            .Where(character => !char.IsWhiteSpace(character))
            .Select(char.ToUpperInvariant)
            .ToArray());
    }

    private static string ErrorTypeText(ScannerDiagnosticErrorType type) => type switch
    {
        ScannerDiagnosticErrorType.None => "NONE",
        ScannerDiagnosticErrorType.DetailWindowDetection => "DETAIL_WINDOW_DETECTION",
        ScannerDiagnosticErrorType.FieldLocalization => "FIELD_LOCALIZATION",
        ScannerDiagnosticErrorType.OcrRecognition => "OCR_RECOGNITION",
        ScannerDiagnosticErrorType.CandidateMatching => "CANDIDATE_MATCHING",
        ScannerDiagnosticErrorType.Parsing => "PARSING",
        ScannerDiagnosticErrorType.DataMapping => "DATA_MAPPING",
        _ => "UNKNOWN_MULTIPLE",
    };

    private static void EnsureDatasetScaffold()
    {
        Directory.CreateDirectory(RootPath);
        Directory.CreateDirectory(Path.Combine(RootPath, "cases"));
        var readme = Path.Combine(RootPath, "README.md");
        if (!File.Exists(readme))
            File.WriteAllText(readme, BuildReadme(), new UTF8Encoding(false));
        File.WriteAllText(
            Path.Combine(RootPath, "environment.json"),
            JsonSerializer.Serialize(BuildEnvironment(), JsonOptions),
            new UTF8Encoding(false));
    }

    private static object BuildEnvironment() => new
    {
        generated_at = DateTimeOffset.UtcNow.ToString("O"),
        dataset_version = ScannerDatasetVersion,
        os = Environment.OSVersion.VersionString,
        process_architecture = RuntimeInformation.ProcessArchitecture.ToString(),
        os_architecture = RuntimeInformation.OSArchitecture.ToString(),
        framework = RuntimeInformation.FrameworkDescription,
        windows_system_dpi = TryGetSystemDpi(),
        program_version = Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "unknown",
        scanner = "scanner-lab-3.8",
    };

    private static uint? TryGetSystemDpi()
    {
        if (!OperatingSystem.IsWindows())
            return null;
        try
        {
            return GetDpiForSystem();
        }
        catch (Exception exception) when (exception is DllNotFoundException or EntryPointNotFoundException)
        {
            return null;
        }
    }

    private static void RebuildIndexesUnsafe()
    {
        var caseFiles = Directory
            .EnumerateFiles(Path.Combine(RootPath, "cases"), "case.json", SearchOption.AllDirectories)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        var records = new List<JsonDocument>(caseFiles.Length);
        try
        {
            foreach (var path in caseFiles)
            {
                try
                {
                    records.Add(JsonDocument.Parse(File.ReadAllText(path, Encoding.UTF8)));
                }
                catch (JsonException)
                {
                }
            }

            using (var writer = new StreamWriter(
                Path.Combine(RootPath, "dataset.jsonl"),
                append: false,
                new UTF8Encoding(false)))
            {
                foreach (var record in records)
                    writer.WriteLine(JsonSerializer.Serialize(record.RootElement, CompactJsonOptions));
            }

            var summary = BuildSummary(records);
            File.WriteAllText(
                Path.Combine(RootPath, "summary.json"),
                JsonSerializer.Serialize(summary, JsonOptions),
                new UTF8Encoding(false));
            File.WriteAllText(
                Path.Combine(RootPath, "summary.md"),
                BuildSummaryMarkdown(summary),
                new UTF8Encoding(false));
        }
        finally
        {
            foreach (var record in records)
                record.Dispose();
        }
    }

    private static ScannerDatasetSummary BuildSummary(IReadOnlyList<JsonDocument> records)
    {
        var groundTruthErrors = new Dictionary<string, int>(StringComparer.Ordinal);
        var pipelineStages = new Dictionary<string, int>(StringComparer.Ordinal);
        var reviewed = 0;
        var finalReviewed = 0;
        var correct = 0;
        var corrections = 0;
        var detailDeltas = new List<(double X, double Y, double W, double H)>();
        var itemNameDeltas = new List<(double X, double Y, double W, double H)>();

        foreach (var document in records)
        {
            var root = document.RootElement;
            var isReviewed = root.TryGetProperty("review_status", out var review) && review.GetString() == "reviewed";
            if (isReviewed)
                reviewed++;

            if (root.TryGetProperty("program_correct", out var programCorrect) &&
                programCorrect.ValueKind is JsonValueKind.True or JsonValueKind.False)
            {
                finalReviewed++;
                if (programCorrect.ValueKind == JsonValueKind.True)
                    correct++;
            }

            if (root.TryGetProperty("pipeline", out var pipeline) &&
                pipeline.TryGetProperty("stage", out var stageElement))
            {
                var stage = stageElement.GetString() ?? "UNKNOWN";
                pipelineStages[stage] = pipelineStages.GetValueOrDefault(stage) + 1;
            }

            if (root.TryGetProperty("fields", out var fields) &&
                fields.TryGetProperty("item_name", out var itemName))
            {
                if (isReviewed &&
                    itemName.TryGetProperty("ground_truth_error_type", out var errorElement) &&
                    errorElement.ValueKind == JsonValueKind.String)
                {
                    var error = errorElement.GetString() ?? "UNKNOWN_MULTIPLE";
                    groundTruthErrors[error] = groundTruthErrors.GetValueOrDefault(error) + 1;
                    if (error != "NONE")
                        corrections++;
                }
                if (itemName.TryGetProperty("delta", out var delta) && delta.ValueKind == JsonValueKind.Object)
                    AddDelta(delta, itemNameDeltas);
            }
            if (root.TryGetProperty("detail_window", out var detail) &&
                detail.TryGetProperty("delta", out var detailDelta) && detailDelta.ValueKind == JsonValueKind.Object)
            {
                AddDelta(detailDelta, detailDeltas);
            }
        }

        return new ScannerDatasetSummary(
            DateTimeOffset.UtcNow,
            records.Count,
            reviewed,
            finalReviewed,
            correct,
            corrections,
            finalReviewed > 0 ? correct / (double)finalReviewed : null,
            groundTruthErrors,
            pipelineStages,
            CalculateDeltaStatistics(detailDeltas),
            CalculateDeltaStatistics(itemNameDeltas));
    }

    private static void AddDelta(JsonElement delta, ICollection<(double X, double Y, double W, double H)> target)
    {
        target.Add((
            GetDouble(delta, "delta_x"),
            GetDouble(delta, "delta_y"),
            GetDouble(delta, "delta_width"),
            GetDouble(delta, "delta_height")));
    }

    private static double GetDouble(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.TryGetDouble(out var parsed) ? parsed : 0;

    private static ScannerRoiDeltaStatistics? CalculateDeltaStatistics(IReadOnlyList<(double X, double Y, double W, double H)> values)
    {
        if (values.Count == 0)
            return null;
        var meanX = values.Average(value => value.X);
        var meanY = values.Average(value => value.Y);
        var meanW = values.Average(value => value.W);
        var meanH = values.Average(value => value.H);
        double Std(Func<(double X, double Y, double W, double H), double> selector, double mean) =>
            Math.Sqrt(values.Average(value => Math.Pow(selector(value) - mean, 2)));
        return new ScannerRoiDeltaStatistics(
            values.Count,
            meanX,
            meanY,
            meanW,
            meanH,
            Std(value => value.X, meanX),
            Std(value => value.Y, meanY),
            Std(value => value.W, meanW),
            Std(value => value.H, meanH));
    }

    private static string BuildSummaryMarkdown(ScannerDatasetSummary summary)
    {
        var builder = new StringBuilder()
            .AppendLine("# Scanner Ground Truth Summary")
            .AppendLine()
            .AppendLine($"- Generated: {summary.GeneratedAt:O}")
            .AppendLine($"- Total cases: {summary.TotalCases}")
            .AppendLine($"- User-reviewed cases: {summary.ReviewedCases}")
            .AppendLine($"- Final-result reviewed cases: {summary.FinalReviewedCases}")
            .AppendLine($"- Program-correct final results: {summary.ProgramCorrectCases}")
            .AppendLine($"- Ground Truth corrections: {summary.Corrections}")
            .AppendLine($"- Reviewed final accuracy: {(summary.ReviewedAccuracy is null ? "n/a" : summary.ReviewedAccuracy.Value.ToString("P2"))}")
            .AppendLine()
            .AppendLine("## Ground Truth error types");
        if (summary.GroundTruthErrorTypes.Count == 0)
            builder.AppendLine("- No reviewed Ground Truth error labels yet.");
        else
            foreach (var pair in summary.GroundTruthErrorTypes.OrderByDescending(pair => pair.Value).ThenBy(pair => pair.Key, StringComparer.Ordinal))
                builder.AppendLine($"- {pair.Key}: {pair.Value}");

        builder.AppendLine()
            .AppendLine("## Observed pipeline stages");
        if (summary.PipelineStages.Count == 0)
            builder.AppendLine("- No pipeline observations yet.");
        else
            foreach (var pair in summary.PipelineStages.OrderByDescending(pair => pair.Value).ThenBy(pair => pair.Key, StringComparer.Ordinal))
                builder.AppendLine($"- {pair.Key}: {pair.Value}");

        AppendDeltaMarkdown(builder, "Detail window ROI correction delta", summary.DetailWindowRoiDelta);
        AppendDeltaMarkdown(builder, "Item name ROI correction delta", summary.ItemNameRoiDelta);
        return builder.ToString();
    }

    private static void AppendDeltaMarkdown(
        StringBuilder builder,
        string heading,
        ScannerRoiDeltaStatistics? delta)
    {
        if (delta is null)
            return;
        builder.AppendLine()
            .AppendLine($"## {heading}")
            .AppendLine($"- Samples: {delta.Samples}")
            .AppendLine($"- Mean ΔX: {delta.MeanX:+0.00;-0.00;0.00}px")
            .AppendLine($"- Mean ΔY: {delta.MeanY:+0.00;-0.00;0.00}px")
            .AppendLine($"- Mean ΔW: {delta.MeanWidth:+0.00;-0.00;0.00}px")
            .AppendLine($"- Mean ΔH: {delta.MeanHeight:+0.00;-0.00;0.00}px")
            .AppendLine($"- Std ΔX: {delta.StdX:0.00}px")
            .AppendLine($"- Std ΔY: {delta.StdY:0.00}px")
            .AppendLine($"- Std ΔW: {delta.StdWidth:0.00}px")
            .AppendLine($"- Std ΔH: {delta.StdHeight:0.00}px");
    }

    private static string BuildReadme() => """
# 준현 헬퍼 Scanner Ground Truth Dataset

이 데이터는 준현 헬퍼 Tarkov Scanner의 실제 사용 과정에서 자동 보존되거나 사용자가 직접 검증/교정한 재현용 진단 데이터입니다.

분석 목적:

1. 상세보기 창 탐지 정확도 개선
2. 아이템 이름 ROI 위치 정확도 개선
3. OCR 정확도 개선
4. 공식 Tarkov 아이템 후보 매칭 개선
5. Item ID 이후 가격/필요 개수 데이터 매핑 검증
6. 기존 정상 사례 regression 방지

`corrected_roi`와 `ground_truth`는 사용자가 직접 지정한 경우 Ground Truth입니다. `review_status=unreviewed`인 자동 보존 사례는 실패 재현용 증거이며 정답으로 간주하면 안 됩니다.

`ground_truth_error_type`은 사용자 검증이 존재할 때만 기록됩니다. 자동 보존 사례의 `pipeline.stage`는 프로그램이 실제로 어느 단계까지 진행했는지를 나타내는 관찰값이며 실패 원인의 Ground Truth 라벨이 아닙니다.

현재 준현 헬퍼 Scanner는 가격과 필요 개수를 화면에서 OCR하지 않습니다. 아이템 이름으로 Item ID를 확정한 뒤 로컬 데이터에서 최고 상점가, 플리마켓 평균가, 슬롯, 필요한 개수를 조회합니다. 따라서 현재 OCR 필드는 `item_name`이며 가격/필요 개수는 `mapped_data` 검증 대상입니다.

`full.png`는 전처리 전 원본 캡처, `detail_window.png`는 상세창, `item_name/detected_roi.png`는 프로그램 ROI, `item_name/corrected_roi.png`는 사용자 교정 ROI, `item_name/processed_roi.png` 및 `processed_variant_*.png`는 현재 Windows OCR 전처리 규칙을 재현한 입력 이미지입니다. `annotated.png`는 프로그램/사용자 영역을 함께 표시합니다.

알고리즘 수정 전 실패 단계를 먼저 분리하고, 수정 후 전체 Ground Truth를 재실행하여 새롭게 해결된 사례와 회귀 사례를 모두 확인하십시오.
""";

    private static void AddDirectoryToArchive(
        ZipArchive archive,
        string root,
        string current,
        CancellationToken cancellationToken)
    {
        foreach (var file in Directory.EnumerateFiles(current).OrderBy(path => path, StringComparer.Ordinal))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var relative = Path.GetRelativePath(root, file).Replace('\\', '/');
            archive.CreateEntryFromFile(file, relative, CompressionLevel.Optimal);
        }
        foreach (var directory in Directory.EnumerateDirectories(current).OrderBy(path => path, StringComparer.Ordinal))
            AddDirectoryToArchive(archive, root, directory, cancellationToken);
    }

    private static void AddLogIfPresent(ZipArchive archive, string sourcePath, string archivePath)
    {
        if (!File.Exists(sourcePath))
            return;
        try
        {
            archive.CreateEntryFromFile(sourcePath, archivePath, CompressionLevel.Optimal);
        }
        catch (IOException)
        {
        }
    }

    [DllImport("user32.dll")]
    private static extern uint GetDpiForSystem();

    private sealed record ScannerDatasetSummary(
        DateTimeOffset GeneratedAt,
        int TotalCases,
        int ReviewedCases,
        int FinalReviewedCases,
        int ProgramCorrectCases,
        int Corrections,
        double? ReviewedAccuracy,
        IReadOnlyDictionary<string, int> GroundTruthErrorTypes,
        IReadOnlyDictionary<string, int> PipelineStages,
        ScannerRoiDeltaStatistics? DetailWindowRoiDelta,
        ScannerRoiDeltaStatistics? ItemNameRoiDelta);

    private sealed record ScannerRoiDeltaStatistics(
        int Samples,
        double MeanX,
        double MeanY,
        double MeanWidth,
        double MeanHeight,
        double StdX,
        double StdY,
        double StdWidth,
        double StdHeight);
}
