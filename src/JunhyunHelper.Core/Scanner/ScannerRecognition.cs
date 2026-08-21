namespace JunhyunHelper.Core.Scanner;

public sealed record ScannerRecognition(
    bool Success,
    string Reason,
    string? ItemId = null,
    string? OfficialName = null,
    double Confidence = 0,
    double SecondScore = 0)
{
    public static ScannerRecognition Failed(string reason) => new(false, reason);
}
