using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using JunhyunHelper.Application.Items;
using JunhyunHelper.Core.Content;
using JunhyunHelper.Core.Profiles;

namespace JunhyunHelper.Desktop.Scanner;

public sealed record ScannerDataContext(
    GameMode GameMode,
    GameContentCatalog Content,
    ItemsWorkspace ItemsWorkspace);

public sealed record ScannerItemSnapshot(
    string ItemId,
    string OfficialName,
    ImageSource? Icon,
    int? TraderSellPrice,
    int? FleaAveragePrice,
    int? TraderPricePerSlot,
    int? FleaPricePerSlot,
    int Slots,
    int CurrentNeeded,
    string? BestTraderName = null)
{
    public int? FleaMinimumPrice { get; init; }
}

public sealed record ScannerInspectCandidate(
    Rect Bounds,
    string GeometrySignature,
    string TitleSignature,
    BitmapSource? TitleImage,
    double StructuralScore = 0,
    string StructuralReason = "",
    Rect TitleBounds = default,
    Rect? MagnifierBounds = null,
    Rect? CloseBounds = null,
    double TitleAnchorScore = 0,
    string TitleAnchorReason = "");

public enum ScannerCaptureMode
{
    TarkovWindow,
    DisplayTest,
}

public enum ScannerRuntimeState
{
    Disabled,
    NoProfile,
    CatalogUnavailable,
    WaitingForVision,
    WaitingForInspectWindow,
    Stabilizing,
    ReadingTitle,
    ShowingItem,
    Uncertain,
    Error,
}

public sealed record ScannerRuntimeStatus(
    ScannerRuntimeState State,
    string Message,
    string? ItemId = null,
    string? OfficialName = null,
    DateTimeOffset? UpdatedAt = null,
    ScannerCaptureMode? CaptureMode = null)
{
    public DateTimeOffset Timestamp { get; } = UpdatedAt ?? DateTimeOffset.Now;
}

/// <summary>
/// User-facing recognition history. This is deliberately separate from scanner.log:
/// the UI receives only the OCR text, nearest official candidate, similarity and the
/// final decision, while the developer log keeps lower-level capture/runtime metadata.
/// CaseId is an exact join key and CorrectionAvailable is set only when the Scanner page
/// can prove that the matching persisted/current evidence is available.
/// </summary>
public sealed record ScannerActivityEntry(
    DateTimeOffset Timestamp,
    ScannerCaptureMode CaptureMode,
    string OcrText,
    string? CandidateName,
    double Confidence,
    double SecondScore,
    bool Success,
    string Reason,
    string? CaseId = null,
    bool CorrectionAvailable = false)
{
    public string TimeText => Timestamp.ToLocalTime().ToString("HH:mm:ss");

    public string ModeLabel => CaptureMode == ScannerCaptureMode.DisplayTest
        ? "테스트"
        : "스캐너";

    public string ResultLabel => Success ? "식별 성공" : "식별 보류";

    public bool CanCorrect => CorrectionAvailable && !string.IsNullOrWhiteSpace(CaseId);

    public string Summary
    {
        get
        {
            var observed = NormalizeForDisplay(OcrText);
            if (string.IsNullOrWhiteSpace(observed))
            {
                if (Success && !string.IsNullOrWhiteSpace(CandidateName))
                    return $"텍스트 OCR 없이 화면 글자 형태를 비교해 ‘{CandidateName}’로 판단했습니다.";
                return "아이템 이름을 읽지 못해 식별을 보류했습니다.";
            }

            if (string.IsNullOrWhiteSpace(CandidateName))
                return $"화면에서 ‘{observed}’를 읽었지만 비교할 수 있는 아이템 후보를 찾지 못했습니다.";

            var confidence = Confidence.ToString("P1");
            return Success
                ? $"화면에서 ‘{observed}’를 읽었고 ‘{CandidateName}’와 {confidence} 일치해 해당 아이템으로 판단했습니다."
                : $"화면에서 ‘{observed}’를 읽었고 가장 가까운 후보 ‘{CandidateName}’와 {confidence} 유사했지만 기준을 충족하지 않아 식별을 보류했습니다.";
        }
    }

    public string DetailText
    {
        get
        {
            var reason = Reason switch
            {
                "EXACT" => "완전 일치",
                "FUZZY" => "유사도 기준 통과",
                "FONT_VERIFIED" => "Tarkov 제목 폰트 시각 검증 통과",
                "FONT_VISUAL_VERIFIED" => "전체 공식 이름 시각 대조 통과",
                "OCR_INVALID_CHARACTERS" => "현재 공식 이름에 없는 문자 또는 한자 OCR 감지",
                "LOW_CONFIDENCE" => "유사도 또는 후보 간 차이 부족",
                "AMBIGUOUS_OFFICIAL_NAME" => "동일 이름 후보 중복",
                "EMPTY_OCR" => "텍스트 인식 실패",
                "NO_CANDIDATE" => "비교 후보 없음",
                "NO_CATALOG" => "아이템 목록 없음",
                _ => "판단 보류",
            };

            if (string.IsNullOrWhiteSpace(CandidateName))
                return $"{ModeLabel} · {reason}";

            var margin = Math.Max(0, Confidence - SecondScore);
            return $"{ModeLabel} · {reason} · 유사도 {Confidence:P1} · 1·2순위 차이 {margin:P1}";
        }
    }

    private static string NormalizeForDisplay(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var compact = string.Join(
            " / ",
            value.Split(['\r', '\n', '|'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        if (compact.Length <= 140)
            return compact;
        return compact[..137] + "...";
    }
}