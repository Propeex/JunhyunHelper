using JunhyunHelper.Core.FarmingGuide;
using JunhyunHelper.Desktop.Scanner;

namespace JunhyunHelper.Desktop.FarmingGuide;

public partial class FarmingGuidePage
{
    /// <summary>
    /// v1.17 final boundary. Safety is now expressed only through system invariants and the
    /// complete-state farming objective. Historical tactical-category and pairwise-victim
    /// rules are intentionally not consulted.
    /// </summary>
    private RaidRecommendation ApplyFinalRaidSafetyV1170(
        FarmingGuideLoadoutSnapshot current,
        RaidRecommendation recommendation,
        ScannerItemSnapshot decisionScan)
    {
        if (recommendation.Action == FarmingGuideInstructionAction.Discard)
            return recommendation;

        var proposed = recommendation.ProposedSnapshot;
        if (!PreservesExplicitLocksV1163(current, proposed) ||
            !PreservesLockedItemPlacementV1164(current, proposed))
        {
            return RejectUnsafeRaidPlanV1163(current);
        }

        var currentScore = ScoreRaidStateV1170(current, decisionScan);
        var proposedScore = ScoreRaidStateV1170(proposed, decisionScan);
        if (!FarmingGuideOptimizationPolicy.IsBetter(proposedScore, currentScore))
        {
            // Equal farming value is not a reason to issue a destructive/rearranging action.
            // The current state is the deterministic stability winner after the actual
            // product objective ties.
            return RejectUnsafeRaidPlanV1163(current);
        }

        return recommendation;
    }
}
