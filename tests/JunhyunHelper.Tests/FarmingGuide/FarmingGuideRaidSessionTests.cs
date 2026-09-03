using JunhyunHelper.Core.FarmingGuide;
using Xunit;

namespace JunhyunHelper.Tests.FarmingGuide;

public sealed class FarmingGuideRaidSessionTests
{
    [Fact]
    public void Constructor_normalizes_locks_and_preserves_raid_start_baseline()
    {
        var snapshot = SnapshotWith("baseline-item");
        var cell = new FarmingGuideLockedCell(FarmingGuideStorageKind.Backpack, 0, 1, 2);
        var locks = new FarmingGuideLockState(
            [FarmingGuideEquipmentSlot.Helmet, FarmingGuideEquipmentSlot.Helmet],
            [FarmingGuideStorageKind.Backpack, FarmingGuideStorageKind.Backpack],
            ["item-a", "item-a", "", "  "],
            [cell, cell]);

        var session = new FarmingGuideRaidSession(snapshot, locks);

        Assert.Same(snapshot, session.BaselineSnapshot);
        Assert.Single(session.BaselineLocks.EquipmentSlots);
        Assert.Single(session.BaselineLocks.Carriers);
        Assert.Single(session.BaselineLocks.ItemInstanceIds);
        Assert.Single(session.BaselineLocks.ReservedCells);
        Assert.Equal(0, session.State.Revision);
        Assert.Null(session.State.PendingInstruction);
    }

    [Fact]
    public void Accept_commits_pending_snapshot_and_advances_revision()
    {
        var baseline = SnapshotWith("baseline-item");
        var proposed = SnapshotWith("accepted-item");
        var session = new FarmingGuideRaidSession(baseline);

        var pending = session.SetPending(
            "accepted-item",
            "backpack에 보관",
            FarmingGuideInstructionAction.Store,
            proposed);

        Assert.Equal(0, pending.BaseRevision);
        Assert.True(session.TryAccept(out var accepted));
        Assert.Same(proposed, accepted);
        Assert.Same(proposed, session.State.Snapshot);
        Assert.Equal(1, session.State.Revision);
        Assert.Null(session.State.PendingInstruction);
        Assert.Same(baseline, session.BaselineSnapshot);
    }

    [Fact]
    public void Indeterminate_advice_cannot_become_a_pending_transaction()
    {
        var baseline = SnapshotWith("baseline-item");
        var session = new FarmingGuideRaidSession(baseline);

        var error = Assert.Throws<ArgumentException>(() => session.SetPending(
            "incoming",
            "판단 보류",
            FarmingGuideInstructionAction.Indeterminate,
            baseline));

        Assert.Contains("non-committing", error.Message, StringComparison.Ordinal);
        Assert.Null(session.State.PendingInstruction);
        Assert.Equal(0, session.State.Revision);
    }

    [Fact]
    public void New_scan_can_reject_pending_and_replace_it_without_mutating_revision()
    {
        var baseline = SnapshotWith("baseline-item");
        var ignoredProposal = SnapshotWith("ignored-item");
        var acceptedProposal = SnapshotWith("next-item");
        var session = new FarmingGuideRaidSession(baseline);

        session.SetPending(
            "ignored-item",
            "가방에 보관",
            FarmingGuideInstructionAction.Store,
            ignoredProposal);
        session.ClearPending();
        var replacement = session.SetPending(
            "next-item",
            "헬멧에 장착",
            FarmingGuideInstructionAction.Equip,
            acceptedProposal);

        Assert.Equal(0, session.State.Revision);
        Assert.Equal("next-item", replacement.ItemId);
        Assert.Equal(FarmingGuideInstructionAction.Equip, replacement.Action);
        Assert.True(session.TryAccept(out var accepted));
        Assert.Same(acceptedProposal, accepted);
        Assert.DoesNotContain(accepted.StoredItems, item => item.Item.ItemId == "ignored-item");
    }

    [Theory]
    [InlineData(FarmingGuideInstructionAction.Equip)]
    [InlineData(FarmingGuideInstructionAction.ReplaceEquip)]
    public void Equip_actions_are_first_class_pending_transactions(FarmingGuideInstructionAction action)
    {
        var baseline = SnapshotWith("baseline-item");
        var proposed = SnapshotWith("equipped-item");
        var session = new FarmingGuideRaidSession(baseline);

        var pending = session.SetPending("equipped-item", "헬멧에 장착", action, proposed);

        Assert.Equal(action, pending.Action);
        Assert.True(session.TryAccept(out var accepted));
        Assert.Same(proposed, accepted);
        Assert.Equal(1, session.State.Revision);
    }

    [Fact]
    public void Manual_state_change_invalidates_pending_instruction_before_acceptance()
    {
        var baseline = SnapshotWith("baseline-item");
        var staleProposal = SnapshotWith("stale-item");
        var manual = SnapshotWith("manual-item");
        var session = new FarmingGuideRaidSession(baseline);

        session.SetPending(
            "stale-item",
            "stale instruction",
            FarmingGuideInstructionAction.Store,
            staleProposal);
        session.ReplaceCurrentState(manual);

        Assert.Equal(1, session.State.Revision);
        Assert.Null(session.State.PendingInstruction);
        Assert.False(session.TryAccept(out var accepted));
        Assert.Same(manual, accepted);
        Assert.Same(manual, session.State.Snapshot);
    }

    [Fact]
    public void Lock_change_invalidates_pending_without_changing_inventory_snapshot()
    {
        var baseline = SnapshotWith("baseline-item");
        var proposal = SnapshotWith("proposal-item");
        var session = new FarmingGuideRaidSession(baseline);
        session.SetPending(
            "proposal-item",
            "proposal instruction",
            FarmingGuideInstructionAction.Store,
            proposal);

        var newLocks = new FarmingGuideLockState(
            [],
            [FarmingGuideStorageKind.SecureContainer],
            [],
            []);
        session.ReplaceLocks(newLocks);

        Assert.Equal(1, session.State.Revision);
        Assert.Same(baseline, session.State.Snapshot);
        Assert.Null(session.State.PendingInstruction);
        Assert.Contains(FarmingGuideStorageKind.SecureContainer, session.State.Locks.Carriers);
    }

    [Fact]
    public void Needed_item_outranks_unneeded_item_regardless_of_market_value()
    {
        var needed = new FarmingGuideLootMetrics(1, 1, 1, 4);
        var expensive = new FarmingGuideLootMetrics(0, 1_000_000, 1_000_000, 1);

        Assert.True(FarmingGuideLootPriorityPolicy.Compare(needed, expensive) > 0);
        Assert.True(FarmingGuideLootPriorityPolicy.ShouldReplace(needed, expensive));
    }

    [Fact]
    public void Higher_total_flea_value_wins_when_need_priority_is_equal()
    {
        var expensiveBulky = new FarmingGuideLootMetrics(0, null, 100_000, 4);
        var cheaperDense = new FarmingGuideLootMetrics(0, null, 80_000, 2);

        Assert.True(FarmingGuideLootPriorityPolicy.Compare(expensiveBulky, cheaperDense) > 0);
    }

    [Fact]
    public void Equal_flea_value_does_not_create_value_per_slot_farming_priority()
    {
        var larger = new FarmingGuideLootMetrics(0, null, 40_000, 2);
        var compact = new FarmingGuideLootMetrics(0, null, 40_000, 1);

        Assert.Equal(0, FarmingGuideLootPriorityPolicy.Compare(compact, larger));
        Assert.Equal(0, FarmingGuideLootPriorityPolicy.Compare(larger, compact));
        Assert.False(FarmingGuideLootPriorityPolicy.ShouldReplace(compact, larger));
        Assert.False(FarmingGuideLootPriorityPolicy.ShouldReplace(larger, compact));
    }

    [Fact]
    public void Footprint_is_system_geometry_not_final_farming_tie_breaker()
    {
        var compact = new FarmingGuideLootMetrics(0, 0, 0, 1);
        var bulky = new FarmingGuideLootMetrics(0, 0, 0, 4);

        Assert.Equal(0, FarmingGuideLootPriorityPolicy.Compare(compact, bulky));
        Assert.Equal(0, FarmingGuideLootPriorityPolicy.Compare(bulky, compact));
    }

    private static FarmingGuideLoadoutSnapshot SnapshotWith(string itemId) =>
        FarmingGuideLoadoutSnapshot.Empty with
        {
            StoredItems =
            [
                new FarmingGuideStoredItemState(
                    "instance-" + itemId,
                    FarmingGuideItemState.Create(itemId),
                    FarmingGuideStorageKind.Backpack,
                    0,
                    0,
                    0,
                    false),
            ],
        };
}
