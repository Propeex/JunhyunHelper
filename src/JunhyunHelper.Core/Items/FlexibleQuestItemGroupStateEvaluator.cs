namespace JunhyunHelper.Core.Items;

public enum FlexibleQuestItemGroupState
{
    Needed,
    Satisfied,
}

/// <summary>
/// Derives the status of one Quest's flexible hand-in group from its objective progress.
/// This is presentation state, not separately persisted user progress: inventory remains
/// the source of truth and the state changes immediately when inventory changes.
/// </summary>
public static class FlexibleQuestItemGroupStateEvaluator
{
    public static FlexibleQuestItemGroupState Evaluate(
        IEnumerable<FlexibleQuestItemProgress> progresses)
    {
        ArgumentNullException.ThrowIfNull(progresses);

        var any = false;
        foreach (var progress in progresses)
        {
            any = true;
            if (!progress.IsFulfilled)
                return FlexibleQuestItemGroupState.Needed;
        }

        return any
            ? FlexibleQuestItemGroupState.Satisfied
            : FlexibleQuestItemGroupState.Needed;
    }
}
