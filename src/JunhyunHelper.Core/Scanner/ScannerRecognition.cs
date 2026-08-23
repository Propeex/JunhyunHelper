namespace JunhyunHelper.Core.Scanner;

public sealed record ScannerMatchCandidate(
    string ItemId,
    string OfficialName,
    double Score);

public sealed record ScannerRecognition(
    bool Success,
    string Reason,
    string? ItemId = null,
    string? OfficialName = null,
    double Confidence = 0,
    double SecondScore = 0,
    IReadOnlyList<ScannerMatchCandidate>? TopCandidates = null)
{
    public static ScannerRecognition Failed(string reason) => new(false, reason);
}
