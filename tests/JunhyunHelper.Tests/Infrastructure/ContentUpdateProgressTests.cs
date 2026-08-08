using JunhyunHelper.Infrastructure.Content;
using Xunit;

namespace JunhyunHelper.Tests.Infrastructure;

public sealed class ContentUpdateProgressTests
{
    [Fact]
    public void DownloadProgressAdvancesFromRealCompletedSourceCount()
    {
        var first = ContentUpdateProgress.ForDownloadedSource("아이템", 1, 8);
        var middle = ContentUpdateProgress.ForDownloadedSource("퀘스트", 4, 8);
        var final = ContentUpdateProgress.ForDownloadedSource("에디션 규칙", 8, 8);

        Assert.Equal(ContentUpdateStage.Downloading, first.Stage);
        Assert.Equal(1, first.CompletedUnits);
        Assert.Equal(8, first.TotalUnits);
        Assert.True(first.Percent >= 5);
        Assert.True(first.Percent < middle.Percent);
        Assert.True(middle.Percent < final.Percent);
        Assert.Equal(60, final.Percent);
    }

    [Theory]
    [InlineData(-1, 8)]
    [InlineData(9, 8)]
    [InlineData(0, 0)]
    public void DownloadProgressRejectsImpossibleCounts(int completed, int total)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ContentUpdateProgress.ForDownloadedSource("test", completed, total));
    }
}
