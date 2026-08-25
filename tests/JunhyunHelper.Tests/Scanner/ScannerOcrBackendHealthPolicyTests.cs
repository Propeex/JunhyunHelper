using JunhyunHelper.Core.Scanner;
using Xunit;

namespace JunhyunHelper.Tests.Scanner;

public sealed class ScannerOcrBackendHealthPolicyTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 25, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void FastEmptyResultDoesNotDegrade()
    {
        var policy = new ScannerOcrBackendHealthPolicy();

        var degraded = policy.RecordResult(Now, TimeSpan.FromMilliseconds(200), hasUsableText: false);

        Assert.False(degraded);
        Assert.True(policy.ShouldAttempt(Now));
    }

    [Fact]
    public void SlowSuccessfulResultDoesNotDegrade()
    {
        var policy = new ScannerOcrBackendHealthPolicy();

        var degraded = policy.RecordResult(Now, TimeSpan.FromSeconds(2), hasUsableText: true);

        Assert.False(degraded);
        Assert.True(policy.ShouldAttempt(Now));
    }

    [Fact]
    public void SlowEmptyResultDegradesForCooldown()
    {
        var policy = new ScannerOcrBackendHealthPolicy();

        var degraded = policy.RecordResult(Now, TimeSpan.FromMilliseconds(900), hasUsableText: false);

        Assert.True(degraded);
        Assert.False(policy.ShouldAttempt(Now + TimeSpan.FromSeconds(29)));
        Assert.True(policy.ShouldAttempt(Now + TimeSpan.FromSeconds(30)));
    }

    [Fact]
    public void SuccessfulProbeClearsDegradedState()
    {
        var policy = new ScannerOcrBackendHealthPolicy();
        policy.RecordResult(Now, TimeSpan.FromSeconds(1), hasUsableText: false);
        var probeAt = Now + TimeSpan.FromSeconds(30);
        Assert.True(policy.ShouldAttempt(probeAt));

        policy.RecordResult(probeAt, TimeSpan.FromSeconds(1), hasUsableText: true);

        Assert.True(policy.ShouldAttempt(probeAt));
        Assert.Equal(DateTimeOffset.MinValue, policy.DegradedUntilUtc);
    }

    [Fact]
    public void InvalidConfigurationIsRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ScannerOcrBackendHealthPolicy(TimeSpan.Zero, TimeSpan.FromSeconds(1)));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ScannerOcrBackendHealthPolicy(TimeSpan.FromSeconds(1), TimeSpan.Zero));
    }
}
