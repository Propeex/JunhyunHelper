namespace JunhyunHelper.Infrastructure.Content;

public enum ContentUpdateStage
{
    Preparing,
    Downloading,
    Importing,
    Validating,
    WritingCandidate,
    Activating,
    Completed,
    Failed,
}

public sealed record ContentUpdateProgress(
    ContentUpdateStage Stage,
    string Message,
    int Percent,
    int? CompletedUnits = null,
    int? TotalUnits = null)
{
    public static ContentUpdateProgress ForDownloadedSource(
        string sourceName,
        int completedUnits,
        int totalUnits)
    {
        if (totalUnits <= 0)
            throw new ArgumentOutOfRangeException(nameof(totalUnits));
        if (completedUnits < 0 || completedUnits > totalUnits)
            throw new ArgumentOutOfRangeException(nameof(completedUnits));

        const int startPercent = 5;
        const int endPercent = 60;
        var percent = startPercent +
                      (int)Math.Round(
                          (endPercent - startPercent) * (completedUnits / (double)totalUnits),
                          MidpointRounding.AwayFromZero);

        return new ContentUpdateProgress(
            ContentUpdateStage.Downloading,
            $"온라인 데이터 다운로드 {completedUnits}/{totalUnits} · {sourceName}",
            percent,
            completedUnits,
            totalUnits);
    }
}
