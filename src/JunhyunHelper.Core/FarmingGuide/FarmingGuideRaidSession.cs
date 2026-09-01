namespace JunhyunHelper.Core.FarmingGuide;

public enum FarmingGuideInstructionAction
{
    Store,
    Replace,
    Discard,
}

/// <summary>
/// A recommendation is a proposal against one exact raid-state revision. The proposal
/// is committed only after an explicit user acceptance and only while that revision is
/// still current.
/// </summary>
public sealed record FarmingGuidePendingInstruction(
    string ItemId,
    string Instruction,
    FarmingGuideInstructionAction Action,
    long BaseRevision,
    FarmingGuideLoadoutSnapshot ProposedSnapshot,
    DateTimeOffset CreatedAt);

public sealed record FarmingGuideRaidState(
    FarmingGuideLoadoutSnapshot Snapshot,
    FarmingGuideLockState Locks,
    long Revision,
    FarmingGuidePendingInstruction? PendingInstruction);

/// <summary>
/// Owns the ephemeral live-raid state. The baseline snapshot/locks are immutable for the
/// lifetime of the session so End always restores the exact raid-start state.
/// </summary>
public sealed class FarmingGuideRaidSession
{
    private readonly FarmingGuideLoadoutSnapshot _baselineSnapshot;
    private readonly FarmingGuideLockState _baselineLocks;
    private FarmingGuideRaidState _state;

    public FarmingGuideRaidSession(
        FarmingGuideLoadoutSnapshot baselineSnapshot,
        FarmingGuideLockState? baselineLocks = null)
    {
        ArgumentNullException.ThrowIfNull(baselineSnapshot);
        _baselineSnapshot = baselineSnapshot;
        _baselineLocks = (baselineLocks ?? FarmingGuideLockState.Empty).CopyNormalized();
        _state = new FarmingGuideRaidState(
            baselineSnapshot,
            _baselineLocks.CopyNormalized(),
            Revision: 0,
            PendingInstruction: null);
    }

    public FarmingGuideRaidState State => _state;

    public FarmingGuideLoadoutSnapshot BaselineSnapshot => _baselineSnapshot;

    public FarmingGuideLockState BaselineLocks => _baselineLocks.CopyNormalized();

    public void ReplaceCurrentState(
        FarmingGuideLoadoutSnapshot snapshot,
        FarmingGuideLockState? locks = null)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        _state = new FarmingGuideRaidState(
            snapshot,
            (locks ?? _state.Locks).CopyNormalized(),
            checked(_state.Revision + 1),
            PendingInstruction: null);
    }

    public void ReplaceLocks(FarmingGuideLockState locks)
    {
        ArgumentNullException.ThrowIfNull(locks);
        _state = _state with
        {
            Locks = locks.CopyNormalized(),
            Revision = checked(_state.Revision + 1),
            PendingInstruction = null,
        };
    }

    public FarmingGuidePendingInstruction SetPending(
        string itemId,
        string instruction,
        FarmingGuideInstructionAction action,
        FarmingGuideLoadoutSnapshot proposedSnapshot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(itemId);
        ArgumentException.ThrowIfNullOrWhiteSpace(instruction);
        ArgumentNullException.ThrowIfNull(proposedSnapshot);

        var pending = new FarmingGuidePendingInstruction(
            itemId.Trim(),
            instruction.Trim(),
            action,
            _state.Revision,
            proposedSnapshot,
            DateTimeOffset.UtcNow);
        _state = _state with { PendingInstruction = pending };
        return pending;
    }

    public void ClearPending() => _state = _state with { PendingInstruction = null };

    public bool TryAccept(out FarmingGuideLoadoutSnapshot acceptedSnapshot)
    {
        var pending = _state.PendingInstruction;
        if (pending is null || pending.BaseRevision != _state.Revision)
        {
            acceptedSnapshot = _state.Snapshot;
            if (pending is not null)
                ClearPending();
            return false;
        }

        acceptedSnapshot = pending.ProposedSnapshot;
        _state = new FarmingGuideRaidState(
            acceptedSnapshot,
            _state.Locks.CopyNormalized(),
            checked(_state.Revision + 1),
            PendingInstruction: null);
        return true;
    }
}
