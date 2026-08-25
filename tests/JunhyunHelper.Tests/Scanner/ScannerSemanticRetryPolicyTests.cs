using JunhyunHelper.Core.Scanner;
using Xunit;

namespace JunhyunHelper.Tests.Scanner;

public sealed class ScannerSemanticRetryPolicyTests
{
    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, 0)]
    [InlineData(1, 250)]
    [InlineData(2, 500)]
    [InlineData(3, 800)]
    [InlineData(4, 1200)]
    [InlineData(20, 1200)]
    public void DelayAfterFailure_UsesFastRetryThenPreviousCeiling(int failures, int expectedMilliseconds)
    {
        Assert.Equal(
            TimeSpan.FromMilliseconds(expectedMilliseconds),
            ScannerSemanticRetryPolicy.DelayAfterFailure(failures));
    }
}
