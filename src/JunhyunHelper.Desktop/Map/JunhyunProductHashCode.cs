namespace TarkovHelper.Windows;

/// <summary>
/// Local helper used only by JunhyunHelper's MiniMap product partial. Nine-value
/// signatures are intentionally supported because System.HashCode.Combine stops at eight.
/// </summary>
internal static class HashCode
{
    public static int Combine<T1, T2, T3, T4, T5, T6, T7, T8, T9>(
        T1 value1,
        T2 value2,
        T3 value3,
        T4 value4,
        T5 value5,
        T6 value6,
        T7 value7,
        T8 value8,
        T9 value9)
    {
        var hash = new System.HashCode();
        hash.Add(value1);
        hash.Add(value2);
        hash.Add(value3);
        hash.Add(value4);
        hash.Add(value5);
        hash.Add(value6);
        hash.Add(value7);
        hash.Add(value8);
        hash.Add(value9);
        return hash.ToHashCode();
    }
}
