namespace JunhyunHelper.Core.FarmingGuide;

public enum FarmingGuideInstructionAction
{
    Store,
    Replace,
    Equip,
    ReplaceEquip,
    Discard,
    /// <summary>
    /// The Farming Guide cannot prove a safe/optimal recommendation from the current facts
    /// and bounded solver domain. This is not a discard decision and must not mutate raid
    /// inventory state when surfaced to the user.
    /// </summary>
    Indeterminate,
}

/// <summary>
/// A recommendation is a proposal against one exact raid-state revision. Snapshot and
/// lock/reservation roles are committed atomically only after explicit user acceptance.
/// </summary>
public sealed record FarmingGuidePendingInstruction(
    string ItemId,
    string Instruction,
    FarmingGuideInstructionAction Action,
    long BaseRevision,
    FarmingGuideLoadoutSnapshot ProposedSnapshot,
    DateTimeOffset CreatedAt)
{
    public FarmingGuideLockState? ProposedLocks { get; init; }
}

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
        FarmingGuideLoadoutSnapshot proposedSnapshot,
        FarmingGuideLockState? proposedLocks = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(itemId);
        ArgumentException.ThrowIfNullOrWhiteSpace(instruction);
        ArgumentNullException.ThrowIfNull(proposedSnapshot);
        if (action == FarmingGuideInstructionAction.Indeterminate)
            throw new ArgumentException("Indeterminate advice is non-committing and cannot become pending state.", nameof(action));

        var pending = new FarmingGuidePendingInstruction(
            itemId.Trim(),
            instruction.Trim(),
            action,
            _state.Revision,
            proposedSnapshot,
            DateTimeOffset.UtcNow)
        {
            ProposedLocks = (proposedLocks ?? _state.Locks).CopyNormalized(),
        };
        _state = _state with { PendingInstruction = pending };
        return pending;
    }

    public void ClearPending() => _state = _state with { PendingInstruction = null };

    public bool TryAccept(out FarmingGuideLoadoutSnapshot acceptedSnapshot) =>
        TryAccept(out acceptedSnapshot, out _);

    public bool TryAccept(
        out FarmingGuideLoadoutSnapshot acceptedSnapshot,
        out FarmingGuideLockState acceptedLocks)
    {
        var pending = _state.PendingInstruction;
        if (pending is null || pending.BaseRevision != _state.Revision)
        {
            acceptedSnapshot = _state.Snapshot;
            acceptedLocks = _state.Locks.CopyNormalized();
            if (pending is not null)
                ClearPending();
            return false;
        }

        acceptedSnapshot = pending.ProposedSnapshot;
        acceptedLocks = (pending.ProposedLocks ?? _state.Locks).CopyNormalized();
        _state = new FarmingGuideRaidState(
            acceptedSnapshot,
            acceptedLocks,
            checked(_state.Revision + 1),
            PendingInstruction: null);
        return true;
    }
}
