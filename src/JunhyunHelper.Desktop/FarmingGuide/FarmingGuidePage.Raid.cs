using System.Windows;
using JunhyunHelper.Core.FarmingGuide;
using JunhyunHelper.Core.Items;
using JunhyunHelper.Desktop.Scanner;

namespace JunhyunHelper.Desktop.FarmingGuide;

public partial class FarmingGuidePage
{
    private readonly Dictionary<string, int> _acceptedRaidItemCounts = new(StringComparer.Ordinal);

    private FarmingGuideRaidBridge? _raidBridge;
    private FarmingGuideRaidSession? _raidSession;
    private Func<string>? _acceptHotkeyTextProvider;
    private string? _raidStartSelectedPresetName;
    private ScannerItemSnapshot? _quantityPendingSnapshot;
    private FarmingGuideLockState? _plannedLocksOverrideV1160;

    public bool IsRaidActive => _raidSession is not null;

    public void ConfigureRaid(
        FarmingGuideRaidBridge bridge,
        Func<string>? acceptHotkeyTextProvider = null)
    {
        _raidBridge = bridge ?? throw new ArgumentNullException(nameof(bridge));
        _acceptHotkeyTextProvider = acceptHotkeyTextProvider;
        bridge.Bind(HandleScannedItem, TryAcceptPendingInstruction, HandleScannedQuantity);
        RefreshRaidUi();
    }

    private void RaidToggleButton_Click(object sender, RoutedEventArgs e)
    {
        if (_raidSession is null)
            StartRaid();
        else
            EndRaid();
    }

    private void StartRaid()
    {
        if (_content is null || string.IsNullOrWhiteSpace(_profileId))
            return;

        var snapshot = BuildSnapshot();
        var locks = BuildLockState();
        _raidSession = new FarmingGuideRaidSession(snapshot, locks);
        _raidStartSelectedPresetName = _selectedPresetName;
        _acceptedRaidItemCounts.Clear();
        _quantityPendingSnapshot = null;
        _plannedLocksOverrideV1160 = null;
        _raidBridge?.ResetScannerIdentity();
        CloseWorkbench();
        RefreshPresetChoices();
        RefreshRaidUi();
        RefreshAll();
    }

    private void EndRaid()
    {
        if (_raidSession is null)
            return;

        var baselineSnapshot = _raidSession.BaselineSnapshot;
        var baselineLocks = _raidSession.BaselineLocks;
        _raidSession = null;
        ApplySnapshot(baselineSnapshot);
        ApplyLockState(baselineLocks);
        _selectedPresetName = _raidStartSelectedPresetName;
        _raidStartSelectedPresetName = null;
        _acceptedRaidItemCounts.Clear();
        _quantityPendingSnapshot = null;
        _plannedLocksOverrideV1160 = null;
        _raidBridge?.ResetScannerIdentity();
        CloseWorkbench();
        RefreshPresetChoices();
        RefreshRaidUi();
        RefreshAll();
    }

    private void ResetRaidForDataChange()
    {
        _raidSession = null;
        _raidStartSelectedPresetName = null;
        _acceptedRaidItemCounts.Clear();
        _quantityPendingSnapshot = null;
        _plannedLocksOverrideV1160 = null;
        _raidBridge?.ResetScannerIdentity();
    }

    private void RefreshRaidUi()
    {
        if (RaidToggleButton is null || RaidStatusText is null)
            return;

        var active = _raidSession is not null;
        RaidToggleButton.Content = active ? "레이드 종료" : "레이드 시작";
        RaidStatusText.Visibility = active ? Visibility.Visible : Visibility.Collapsed;
        RaidStatusText.Text = active
            ? _quantityPendingSnapshot is not null
                ? "개수 입력 대기 중"
                : _raidSession!.State.PendingInstruction is { } pending
                    ? pending.Instruction
                    : "레이드 진행 중 · 아이템을 스캔하세요."
            : string.Empty;
    }

    private void HandleScannedItem(ScannerItemSnapshot scanned)
    {
        if (_raidSession is null || _content is null)
            return;

        _quantityPendingSnapshot = null;
        _plannedLocksOverrideV1160 = null;
        _raidBridge?.CancelMiniScannerQuantity();
        if (_raidSession.State.PendingInstruction is not null)
        {
            _raidSession.ClearPending();
            _raidBridge?.SetMiniScannerInstruction(null);
        }

        var item = ResolveItem(scanned.ItemId);
        if (item is null || !FarmingGuideSearchPolicy.IsDraggableInventoryItem(item))
        {
            RefreshRaidUi();
            return;
        }

        if (FarmingGuideStackQuantityPolicy.RequiresQuantity(item))
        {
            _quantityPendingSnapshot = scanned;
            _raidBridge?.SetMiniScannerInstruction(null);
            _raidBridge?.RequestMiniScannerQuantity();
            RefreshRaidUi();
            return;
        }

        PlanConfirmedScannedItem(scanned with { Quantity = 1 }, item);
    }

    private void HandleScannedQuantity(int quantity)
    {
        if (_raidSession is null || _content is null || _quantityPendingSnapshot is not { } pending)
            return;

        var item = ResolveItem(pending.ItemId);
        _quantityPendingSnapshot = null;
        _raidBridge?.CancelMiniScannerQuantity();
        if (item is null || !FarmingGuideSearchPolicy.IsDraggableInventoryItem(item))
        {
            RefreshRaidUi();
            return;
        }

        PlanConfirmedScannedItem(
            pending with { Quantity = FarmingGuideStackQuantityPolicy.NormalizeQuantity(quantity) },
            item);
    }

    private void PlanConfirmedScannedItem(ScannerItemSnapshot scanned, GameItem item)
    {
        if (_raidSession is null)
            return;

        var current = BuildSnapshot();
        RefreshRaidAcquiredCounts(current);

        var quantity = Math.Max(1, scanned.Quantity);
        int? totalFlea = scanned.FleaAveragePrice is { } flea
            ? checked(flea * quantity)
            : null;
        var decisionScan = scanned with
        {
            CurrentNeeded = scanned.CurrentNeededFir,
            FleaAveragePrice = totalFlea,
            Quantity = quantity,
        };

        _plannedLocksOverrideV1160 = null;
        var planned = PlanScannedItemRulebookV1160(current, decisionScan, item);
        var transitioned = ApplyRaidStateTransitionsV1155(current, planned, decisionScan, item);
        var optimized = OptimizeDestructiveRaidPlanV1155(current, transitioned, decisionScan, item);
        var quantityApplied = ApplyIncomingQuantityV1160(current, optimized, item.Id, quantity);
        var weightChecked = ApplyRaidWeightConstraintV1160(current, quantityApplied);
        var recommendation = ApplyRaidInstructionPresentationV1155(current, weightChecked, item);
        _raidSession.SetPending(
            scanned.ItemId,
            recommendation.Instruction,
            recommendation.Action,
            recommendation.ProposedSnapshot,
            _plannedLocksOverrideV1160 ?? BuildLockState());
        RefreshRaidUi();
        _raidBridge?.ShowMiniScannerStatus(
            $"{recommendation.Instruction}\n수락 [{AcceptHotkeyText()}]");
    }

    private static RaidRecommendation ApplyIncomingQuantityV1160(
        FarmingGuideLoadoutSnapshot current,
        RaidRecommendation recommendation,
        string incomingItemId,
        int quantity)
    {
        if (quantity <= 1)
            return recommendation;

        var existingIds = current.StoredItems
            .Select(value => value.InstanceId)
            .ToHashSet(StringComparer.Ordinal);
        var changed = false;
        var stored = recommendation.ProposedSnapshot.StoredItems
            .Select(value =>
            {
                if (changed || existingIds.Contains(value.InstanceId) ||
                    !string.Equals(value.Item.ItemId, incomingItemId, StringComparison.Ordinal))
                {
                    return value;
                }

                changed = true;
                return value with { Quantity = quantity };
            })
            .ToArray();
        return changed
            ? recommendation with { ProposedSnapshot = recommendation.ProposedSnapshot with { StoredItems = stored } }
            : recommendation;
    }

    private void RefreshRaidAcquiredCounts(FarmingGuideLoadoutSnapshot current)
    {
        _acceptedRaidItemCounts.Clear();
        if (_raidSession is null)
            return;

        foreach (var pair in FarmingGuideSnapshotInventoryCounter.AcquiredSinceAll(
                     _raidSession.BaselineSnapshot,
                     current))
        {
            _acceptedRaidItemCounts[pair.Key] = pair.Value;
        }
    }

    private string AcceptHotkeyText()
    {
        var value = _acceptHotkeyTextProvider?.Invoke();
        return string.IsNullOrWhiteSpace(value) ? "사용 안 함" : value.Trim();
    }

    private bool TryAcceptPendingInstruction()
    {
        if (_quantityPendingSnapshot is not null ||
            _raidSession?.State.PendingInstruction is not { })
        {
            return false;
        }

        if (!_raidSession.TryAccept(out var snapshot, out var locks))
            return false;

        ApplySnapshot(snapshot);
        ApplyLockState(locks);
        _plannedLocksOverrideV1160 = null;
        RefreshAll();
        RefreshRaidUi();
        _raidBridge?.ShowMiniScannerStatus("반영 완료");
        return true;
    }
}
