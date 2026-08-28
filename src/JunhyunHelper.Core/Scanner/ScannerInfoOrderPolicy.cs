namespace JunhyunHelper.Core.Scanner;

/// <summary>
/// Preserves a user's existing Mini Scanner information-row order while normalizing
/// unknown/duplicate keys and appending newly introduced product fields exactly once.
/// </summary>
public static class ScannerInfoOrderPolicy
{
    public static List<string> Normalize(
        IEnumerable<string>? values,
        IReadOnlyList<string> knownFields)
    {
        ArgumentNullException.ThrowIfNull(knownFields);

        var known = new HashSet<string>(knownFields, StringComparer.Ordinal);
        var result = new List<string>(knownFields.Count);
        foreach (var value in values ?? [])
        {
            if (known.Contains(value) && !result.Contains(value, StringComparer.Ordinal))
                result.Add(value);
        }

        foreach (var value in knownFields)
        {
            if (!result.Contains(value, StringComparer.Ordinal))
                result.Add(value);
        }

        return result;
    }
}
