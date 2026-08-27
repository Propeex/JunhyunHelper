namespace JunhyunHelper.Core.Hotkeys;

[Flags]
public enum HotkeyModifierMask
{
    None = 0,
    Control = 1,
    Alt = 2,
    Shift = 4,
    All = Control | Alt | Shift,
}

/// <summary>
/// Product-wide modifier matching policy for configurable hotkeys.
/// A registered gesture remains compatible when Ctrl/Alt/Shift modifiers are added,
/// while the most specific compatible registered gesture wins.
/// </summary>
public static class HotkeyModifierMatchPolicy
{
    public static bool IsCompatible(HotkeyModifierMask required, HotkeyModifierMask pressed)
    {
        required &= HotkeyModifierMask.All;
        pressed &= HotkeyModifierMask.All;
        return (pressed & required) == required;
    }

    public static int Specificity(HotkeyModifierMask modifiers)
    {
        modifiers &= HotkeyModifierMask.All;
        var count = 0;
        if ((modifiers & HotkeyModifierMask.Control) != 0)
            count++;
        if ((modifiers & HotkeyModifierMask.Alt) != 0)
            count++;
        if ((modifiers & HotkeyModifierMask.Shift) != 0)
            count++;
        return count;
    }
}
