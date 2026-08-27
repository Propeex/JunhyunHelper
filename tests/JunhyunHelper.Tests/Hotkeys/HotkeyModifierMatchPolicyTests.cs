using JunhyunHelper.Core.Hotkeys;
using Xunit;

namespace JunhyunHelper.Tests.Hotkeys;

public sealed class HotkeyModifierMatchPolicyTests
{
    [Fact]
    public void RequiredModifiersAllowAdditionalCtrlAltShift()
    {
        Assert.True(HotkeyModifierMatchPolicy.IsCompatible(
            HotkeyModifierMask.Control,
            HotkeyModifierMask.Control | HotkeyModifierMask.Shift));
        Assert.True(HotkeyModifierMatchPolicy.IsCompatible(
            HotkeyModifierMask.None,
            HotkeyModifierMask.Control | HotkeyModifierMask.Alt | HotkeyModifierMask.Shift));
        Assert.False(HotkeyModifierMatchPolicy.IsCompatible(
            HotkeyModifierMask.Control | HotkeyModifierMask.Alt,
            HotkeyModifierMask.Control | HotkeyModifierMask.Shift));
    }

    [Fact]
    public void SpecificityCountsRequiredModifiersForConflictResolution()
    {
        Assert.Equal(0, HotkeyModifierMatchPolicy.Specificity(HotkeyModifierMask.None));
        Assert.Equal(1, HotkeyModifierMatchPolicy.Specificity(HotkeyModifierMask.Shift));
        Assert.Equal(2, HotkeyModifierMatchPolicy.Specificity(
            HotkeyModifierMask.Control | HotkeyModifierMask.Shift));
        Assert.Equal(3, HotkeyModifierMatchPolicy.Specificity(HotkeyModifierMask.All));
    }
}
