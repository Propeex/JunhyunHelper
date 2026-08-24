using JunhyunHelper.Infrastructure.Scanner;
using Xunit;

namespace JunhyunHelper.Tests.Scanner;

public sealed class ScannerCatalogOutcomePolicyTests
{
    [Theory]
    [InlineData("success")]
    [InlineData("fresh-cache")]
    [InlineData("cache-loaded")]
    [InlineData("not-run")]
    [InlineData(null)]
    public void IsRefreshFailure_NormalOrNonRefreshOutcome_IsFalse(string? outcome)
    {
        Assert.False(ScannerCatalogOutcomePolicy.IsRefreshFailure(outcome));
    }

    [Theory]
    [InlineData("timeout-or-shutdown")]
    [InlineData("http-failure")]
    [InlineData("io-failure")]
    [InlineData("access-failure")]
    [InlineData("json-invalid")]
    [InlineData("payload-invalid")]
    [InlineData("identity-invalid")]
    [InlineData("cache-readback-invalid")]
    public void IsRefreshFailure_KnownRefreshFailure_IsTrue(string outcome)
    {
        Assert.True(ScannerCatalogOutcomePolicy.IsRefreshFailure(outcome));
    }

    [Theory]
    [InlineData("http-failure", true)]
    [InlineData("json-invalid", true)]
    [InlineData("payload-invalid", true)]
    [InlineData("timeout-or-shutdown", false)]
    [InlineData("access-failure", false)]
    [InlineData("fresh-cache", false)]
    [InlineData("success", false)]
    public void IsRetryableFromUserUpdate_OnlyQuickTransientFailuresRetry(string outcome, bool expected)
    {
        Assert.Equal(expected, ScannerCatalogOutcomePolicy.IsRetryableFromUserUpdate(outcome));
    }
}
