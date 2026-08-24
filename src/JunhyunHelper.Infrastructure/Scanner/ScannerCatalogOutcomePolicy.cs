namespace JunhyunHelper.Infrastructure.Scanner;

/// <summary>
/// Interprets the stable diagnostic outcome names emitted by ScannerCatalogService.
/// Presentation code must not infer "download failed" merely from the fact that an
/// existing healthy catalog was used: `fresh-cache` is a normal successful outcome.
/// </summary>
public static class ScannerCatalogOutcomePolicy
{
    public static bool IsRefreshFailure(string? outcome) => outcome is
        "timeout-or-shutdown" or
        "http-failure" or
        "io-failure" or
        "access-failure" or
        "json-invalid" or
        "payload-invalid" or
        "identity-invalid" or
        "cache-readback-invalid";

    /// <summary>
    /// Quick/transient responses that are safe to retry once from the explicit user
    /// data-update flow. The service-level timeout is intentionally excluded so the UI
    /// does not repeat an already long timeout window.
    /// </summary>
    public static bool IsRetryableFromUserUpdate(string? outcome) => outcome is
        "http-failure" or
        "json-invalid" or
        "payload-invalid";
}
