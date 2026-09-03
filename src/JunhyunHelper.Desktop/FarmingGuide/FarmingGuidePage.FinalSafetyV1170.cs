using JunhyunHelper.Core.FarmingGuide;
using JunhyunHelper.Desktop.Scanner;

namespace JunhyunHelper.Desktop.FarmingGuide;

public partial class FarmingGuidePage
{
    /// <summary>
    /// v1.17 final boundary. Safety is expressed only through system invariants and the
    /// complete-state farming objective. Historical tactical-category and pairwise-victim
    /// rules are intentionally not consulted.
    /// </summary>
    private RaidRecommendation ApplyFinalRaidSafetyV1170(
        FarmingGuideLoadoutSnapshot current,
        RaidRecommendation recommendation,
        ScannerItemSnapshot decisionScan)
    {
        if (recommendation.Action == FarmingGuideInstructionAction.Indeterminate)
            return IndeterminateRaidPlanV1170(current);
        if (recommendation.Action == FarmingGuideInstructionAction.Discard)
            return recommendation;

        var proposed = recommendation.ProposedSnapshot;
        if (!PreservesExplicitLocksV1163(current, proposed) ||
            !PreservesLockedItemPlacementV1164(current, proposed))
        {
            return IndeterminateRaidPlanV1170(current);
        }

        var currentScore = ScoreRaidStateV1170(current, decisionScan);
        var proposedScore = ScoreRaidStateV1170(proposed, decisionScan);
        if (!FarmingGuideOptimizationPolicy.IsBetter(proposedScore, currentScore))
        {
            // Equal farming value chooses the current state as the deterministic stability
            // winner. This is a proven discard only after the planner has already supplied a
            // complete/safe candidate; uncertainty reaches this method as Indeterminate.
            return ProvenDiscardV1170(current);
        }

        return recommendation;
    }

    private static RaidRecommendation ProvenDiscardV1170(
        FarmingGuideLoadoutSnapshot current) =>
        new(
            "버리기",
            FarmingGuideInstructionAction.Discard,
            current);

    private static RaidRecommendation IndeterminateRaidPlanV1170(
        FarmingGuideLoadoutSnapshot current) =>
        new(
            "판단 보류",
            FarmingGuideInstructionAction.Indeterminate,
            current);
}
