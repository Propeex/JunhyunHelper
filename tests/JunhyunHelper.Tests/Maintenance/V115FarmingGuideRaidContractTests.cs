using System.Runtime.CompilerServices;
using Xunit;

namespace JunhyunHelper.Tests.Maintenance;

public sealed class V115FarmingGuideRaidContractTests
{
    [Fact]
    public void Raid_planner_honors_carrier_item_and_reserved_cell_locks()
    {
        var root = FindRepositoryRoot();
        var raid = Read(root, "src", "JunhyunHelper.Desktop", "FarmingGuide", "FarmingGuidePage.Raid.cs");

        Assert.Contains("if (_lockedCarriers.Contains(kind) || !root.TryGetValue(kind, out var storage))", raid, StringComparison.Ordinal);
        Assert.Contains("if (_lockedCarriers.Contains(stored.Storage) || IsInsideLockedItem(stored.InstanceId))", raid, StringComparison.Ordinal);
        Assert.Contains("_lockedItemInstanceIds.Contains(candidate.Stored.InstanceId)", raid, StringComparison.Ordinal);
        Assert.Contains("SubtreeContainsLockedItem(candidate.Stored.InstanceId)", raid, StringComparison.Ordinal);
        Assert.Contains("_reservedCells", raid, StringComparison.Ordinal);
        Assert.Contains("$\"__locked_{index}\"", raid, StringComparison.Ordinal);
    }

    [Fact]
    public void Manual_raid_edit_cancels_pending_instruction_and_clears_persistent_overlay_instruction()
    {
        var root = FindRepositoryRoot();
        var page = Read(root, "src", "JunhyunHelper.Desktop", "FarmingGuide", "FarmingGuidePage.xaml.cs");
        var bridge = Read(root, "src", "JunhyunHelper.Desktop", "FarmingGuide", "FarmingGuideRaidBridge.cs");

        Assert.Contains("var pendingWasPresent = _raidSession.State.PendingInstruction is not null;", page, StringComparison.Ordinal);
        Assert.Contains("_raidSession.ReplaceCurrentState(BuildSnapshot(), BuildLockState());", page, StringComparison.Ordinal);
        Assert.Contains("_raidBridge?.ShowMiniScannerStatus(\"상태 변경으로 이전 파밍 지시를 취소했습니다.\");", page, StringComparison.Ordinal);
        Assert.Contains("normalized.StartsWith(\"상태 변경으로\", StringComparison.Ordinal)", bridge, StringComparison.Ordinal);
        Assert.Contains("SetMiniScannerInstruction(null);", bridge, StringComparison.Ordinal);
        Assert.Contains("ShowTransientStatus(normalized);", bridge, StringComparison.Ordinal);
    }

    [Fact]
    public void Raid_end_data_change_and_scanner_worker_callbacks_keep_the_ui_boundary_safe()
    {
        var root = FindRepositoryRoot();
        var raid = Read(root, "src", "JunhyunHelper.Desktop", "FarmingGuide", "FarmingGuidePage.Raid.cs");
        var bridge = Read(root, "src", "JunhyunHelper.Desktop", "FarmingGuide", "FarmingGuideRaidBridge.cs");

        Assert.Contains("_raidBridge?.ResetScannerIdentity();", raid, StringComparison.Ordinal);
        Assert.Contains("public void ResetScannerIdentity()", bridge, StringComparison.Ordinal);
        Assert.Contains("SetMiniScannerInstruction(null);", bridge, StringComparison.Ordinal);
        Assert.Contains("private readonly Dispatcher _dispatcher;", bridge, StringComparison.Ordinal);
        Assert.Contains("InvokePageCallback(() => handler(snapshot));", bridge, StringComparison.Ordinal);
        Assert.Contains("if (_dispatcher.CheckAccess())", bridge, StringComparison.Ordinal);
        Assert.Contains("_dispatcher.BeginInvoke(callback, DispatcherPriority.Normal)", bridge, StringComparison.Ordinal);
        Assert.Contains("return _dispatcher.Invoke(handler);", bridge, StringComparison.Ordinal);
    }

    private static string Read(string root, params string[] path) =>
        File.ReadAllText(Path.Combine([root, .. path]));

    private static string FindRepositoryRoot([CallerFilePath] string sourcePath = "")
    {
        var directory = new DirectoryInfo(Path.GetDirectoryName(sourcePath)
            ?? throw new InvalidOperationException("Test source path is unavailable."));

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "AGENTS.md")) &&
                Directory.Exists(Path.Combine(directory.FullName, "src")) &&
                Directory.Exists(Path.Combine(directory.FullName, "tests")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the JunhyunHelper repository root.");
    }
}
