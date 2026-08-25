using JunhyunHelper.Infrastructure.Validation;
using Xunit;

namespace JunhyunHelper.Tests.Validation;

public sealed class ContentValidationUserMessageFormatterTests
{
    [Fact]
    public void UsesFirstFatalIssueRatherThanWarningOrLaterFatal()
    {
        var validation = new ContentValidationResult(
        [
            new ContentValidationIssue(ContentValidationSeverity.Warning, "warning", "warning"),
            new ContentValidationIssue(
                ContentValidationSeverity.Fatal,
                "quest-objective.item.missing",
                "missing quest item"),
            new ContentValidationIssue(ContentValidationSeverity.Fatal, "domain.ammo.empty", "ammo empty"),
        ]);

        var message = ContentValidationUserMessageFormatter.FormatFirstFatal(validation);

        Assert.Equal("퀘스트가 존재하지 않는 아이템을 참조해 적용하지 않았습니다.", message);
    }

    [Fact]
    public void SuspiciousPartialUpdateHasSimpleUserFacingReason()
    {
        var validation = new ContentValidationResult(
        [
            new ContentValidationIssue(
                ContentValidationSeverity.Fatal,
                "update.items.suspicious-shrink",
                "internal detail"),
        ]);

        var message = ContentValidationUserMessageFormatter.FormatFirstFatal(validation);

        Assert.Equal("새 데이터 일부가 비정상적으로 누락되어 적용하지 않았습니다.", message);
    }
}
