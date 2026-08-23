using System.Reflection;

namespace JunhyunHelper.Desktop;

public static class ProductVersion
{
    public static string Label { get; } = ResolveLabel();

    private static string ResolveLabel()
    {
        var assembly = typeof(ProductVersion).Assembly;
        var informational = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;
        var version = string.IsNullOrWhiteSpace(informational)
            ? null
            : informational.Split('+', 2, StringSplitOptions.TrimEntries)[0];
        if (string.IsNullOrWhiteSpace(version))
        {
            var fallback = assembly.GetName().Version;
            version = fallback is null
                ? "unknown"
                : $"{fallback.Major}.{fallback.Minor}.{Math.Max(0, fallback.Build)}";
        }

        return $"v{version}";
    }
}
