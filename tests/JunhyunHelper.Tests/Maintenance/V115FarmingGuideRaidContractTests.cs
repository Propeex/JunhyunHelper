using System.Runtime.CompilerServices;
using Xunit;

namespace JunhyunHelper.Tests.Maintenance;

public sealed class V115FarmingGuideRaidContractTests
{
    [Fact]
    public void Raid_planner_protects_locked_targets_but_allows_storage_inside_locked_carriers()
    {
        var root = FindRepositoryRoot();
        var planning = Read(root, "src", "JunhyunHelper.Desktop", "FarmingGuide", "FarmingGuidePage.RaidPlanning.cs");

        // A carrier lock protects the carrier itself from automated replacement, but its
        // internal storage remains a legal placement surface in v1.15.1.
        Assert.Contains("if (existingState is not null && _lockedCarriers.Contains(kind))", planning, StringComparison.Ordinal);
        Assert.Contains("if (!root.TryGetValue(kind, out var storage))", planning, StringComparison.Ordinal);
        Assert.DoesNotContain("_lockedCarriers.Contains(kind) || !root.TryGetValue(kind, out var storage)", planning, StringComparison.Ordinal);

        // Item/subtree locks and independently reserved empty cells still constrain
        // destructive replacement and automatic placement.
        Assert.Contains("_lockedItemInstanceIds.Contains(candidate.Stored.InstanceId)", planning, StringComparison.Ordinal);
        Assert.Contains("SubtreeContainsLockedItem(candidate.Stored.InstanceId)", planning, StringComparison.Ordinal);
        Assert.Contains("IsInsideLockedItem(stored.InstanceId)", planning, StringComparison.Ordinal);
        Assert.Contains("_reservedCells", planning, StringComparison.Ordinal);
        Assert.Contains("$\"__locked_{index}\"", planning, StringComparison.Ordinal);
    }

    [Fact]
    public void Manual_raid_edit_silently_invalidates_pending_instruction_and_clears_persistent_overlay()
    {
        var root = FindRepositoryRoot();
        var page = Read(root, "src", "JunhyunHelper.Desktop", "FarmingGuide", "FarmingGuidePage.xaml.cs");
        var bridge = Read(root, "src", "JunhyunHelper.Desktop", "FarmingGuide", "FarmingGuideRaidBridge.cs");

        Assert.Contains("_raidSession.ReplaceCurrentState(BuildSnapshot(), BuildLockState());", page, StringComparison.Ordinal);
        Assert.Contains("_raidBridge?.SetMiniScannerInstruction(null);", page, StringComparison.Ordinal);
        Assert.DoesNotContain("var pendingWasPresent = _raidSession.State.PendingInstruction is not null;", page, StringComparison.Ordinal);
        Assert.DoesNotContain("상태 변경으로 이전 파밍 지시를 취소했습니다.", page, StringComparison.Ordinal);
        Assert.DoesNotContain("normalized.StartsWith(\"상태 변경으로\", StringComparison.Ordinal)", bridge, StringComparison.Ordinal);

        // Acceptance feedback remains transient while ordinary instruction text remains persistent.
        Assert.Contains("string.Equals(normalized, \"반영 완료\", StringComparison.Ordinal)", bridge, StringComparison.Ordinal);
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
