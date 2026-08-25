namespace JunhyunHelper.Infrastructure.Validation;

public static class ContentValidationUserMessageFormatter
{
    public static string FormatFirstFatal(ContentValidationResult validation)
    {
        ArgumentNullException.ThrowIfNull(validation);

        var fatal = validation.Issues.FirstOrDefault(static issue =>
            issue.Severity == ContentValidationSeverity.Fatal);
        if (fatal is null)
            return "새 게임 데이터가 올바르지 않아 적용하지 않았습니다.";

        return fatal.Code switch
        {
            "domain.items.empty" => "아이템 데이터가 비어 있어 적용하지 않았습니다.",
            "domain.traders.empty" => "상인 데이터가 비어 있어 적용하지 않았습니다.",
            "domain.maps.empty" => "지도 데이터가 비어 있어 적용하지 않았습니다.",
            "domain.quests.empty" or "domain.quest-objectives.empty" or "domain.quest-items.empty" =>
                "퀘스트 데이터가 불완전해 적용하지 않았습니다.",
            "domain.hideout.empty" => "은신처 데이터가 비어 있어 적용하지 않았습니다.",
            "domain.ammo.empty" => "탄약 데이터가 비어 있어 적용하지 않았습니다.",
            "quest-objective.item.missing" or "quest-item.item.missing" =>
                "퀘스트가 존재하지 않는 아이템을 참조해 적용하지 않았습니다.",
            "quest-objective.quest-item.missing" =>
                "퀘스트 전용 아이템 참조가 올바르지 않아 적용하지 않았습니다.",
            "hideout-item.item.missing" =>
                "은신처가 존재하지 않는 아이템을 참조해 적용하지 않았습니다.",
            "ammo.item.missing" or "ammo.currency.missing" or "ammo.requirement-item.missing" =>
                "탄약 데이터의 아이템 참조가 올바르지 않아 적용하지 않았습니다.",
            _ when fatal.Code.StartsWith("update.", StringComparison.Ordinal) =>
                "새 데이터 일부가 비정상적으로 누락되어 적용하지 않았습니다.",
            _ => "새 게임 데이터의 무결성 검증에 실패해 적용하지 않았습니다.",
        };
    }
}
