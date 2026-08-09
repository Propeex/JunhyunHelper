namespace JunhyunHelper.Desktop.Map;

/// <summary>
/// Explicit WPF runtime bridge for Map/MiniMap code.
/// JunhyunHelper also has a sibling namespace named Application, so generated WPF
/// temporary projects can otherwise bind an unqualified Application.Current to that
/// namespace instead of System.Windows.Application.
/// </summary>
internal static class Application
{
    public static System.Windows.Application? Current => System.Windows.Application.Current;
}
