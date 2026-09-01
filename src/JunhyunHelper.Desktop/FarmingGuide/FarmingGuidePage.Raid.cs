using System.Windows;
using JunhyunHelper.Core.FarmingGuide;
using JunhyunHelper.Desktop.Scanner;

namespace JunhyunHelper.Desktop.FarmingGuide;

public partial class FarmingGuidePage
{
    private readonly Dictionary<string, int> _acceptedRaidItemCounts = new(StringComparer.Ordinal);

    private FarmingGuideRaidBridge? _raidBridge;
    private FarmingGuideRaidSession? _raidSession;
    private Func<string>? _acceptHotkeyTextProvider;
    private string? _raidStartSelectedPresetName;

    public bool IsRaidActive => _raidSession is not null;

    public void ConfigureRaid(
        FarmingGuideRaidBridge bridge,
        Func<string>? acceptHotkeyTextProvider = null)
    {
        _raidBridge = bridge ?? throw new ArgumentNullException(nameof(bridge));
        _acceptHotkeyTextProvider = acceptHotkeyTextProvider;
        bridge.Bind(HandleScannedItem, TryAcceptPendingInstruction, bridge.ShowMiniScannerStatus);
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
            ? _raidSession!.State.PendingInstruction is { } pending
                ? pending.Instruction
                : "레이드 진행 중 · 아이템을 스캔하세요."
            : string.Empty;
    }

    private void HandleScannedItem(ScannerItemSnapshot scanned)
    {
        if (_raidSession is null || _content is null)
            return;

        // Scanning another item is an implicit rejection of the previous recommendation.
        // No inventory state was committed yet, so the new item is evaluated against the
        // same current raid snapshot.
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

        var recommendation = PlanScannedItemEquipmentAware(scanned, item);
        _raidSession.SetPending(
            scanned.ItemId,
            recommendation.Instruction,
            recommendation.Action,
            recommendation.ProposedSnapshot);
        RefreshRaidUi();
        _raidBridge?.ShowMiniScannerStatus(
            $"{recommendation.Instruction}\n수락 [{AcceptHotkeyText()}]");
    }

    private string AcceptHotkeyText()
    {
        var value = _acceptHotkeyTextProvider?.Invoke();
        return string.IsNullOrWhiteSpace(value) ? "사용 안 함" : value.Trim();
    }

    private bool TryAcceptPendingInstruction()
    {
        if (_raidSession?.State.PendingInstruction is not { } pending)
            return false;

        var acceptedAddsItem = pending.Action is
            FarmingGuideInstructionAction.Store or
            FarmingGuideInstructionAction.Replace or
            FarmingGuideInstructionAction.Equip or
            FarmingGuideInstructionAction.ReplaceEquip;
        if (!_raidSession.TryAccept(out var snapshot))
            return false;

        ApplySnapshot(snapshot);
        if (acceptedAddsItem)
            _acceptedRaidItemCounts[pending.ItemId] = _acceptedRaidItemCounts.GetValueOrDefault(pending.ItemId) + 1;
        RefreshAll();
        RefreshRaidUi();
        _raidBridge?.ShowMiniScannerStatus("반영 완료");
        return true;
    }
}
