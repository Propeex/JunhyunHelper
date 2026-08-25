using JunhyunHelper.Core.Scanner;
using Xunit;

namespace JunhyunHelper.Tests.Scanner;

public sealed class ScannerObservationPacingPolicyTests
{
    [Fact]
    public void NextDelay_PreservesTargetCadenceWhenCycleFinishesEarly()
    {
        var delay = ScannerObservationPacingPolicy.NextDelay(
            TimeSpan.FromMilliseconds(200),
            TimeSpan.FromMilliseconds(80));

        Assert.Equal(TimeSpan.FromMilliseconds(120), delay);
    }

    [Theory]
    [InlineData(200)]
    [InlineData(350)]
    [InlineData(1200)]
    public void NextDelay_NeverReplaysMissedTicksBackToBack(int elapsedMilliseconds)
    {
        var delay = ScannerObservationPacingPolicy.NextDelay(
            TimeSpan.FromMilliseconds(200),
            TimeSpan.FromMilliseconds(elapsedMilliseconds));

        Assert.Equal(TimeSpan.FromMilliseconds(25), delay);
    }

    [Fact]
    public void NextDelay_UsesMinimumYieldWhenRemainingBudgetIsTooSmall()
    {
        var delay = ScannerObservationPacingPolicy.NextDelay(
            TimeSpan.FromMilliseconds(200),
            TimeSpan.FromMilliseconds(190));

        Assert.Equal(TimeSpan.FromMilliseconds(25), delay);
    }

    [Fact]
    public void NextDelay_RejectsInvalidArguments()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ScannerObservationPacingPolicy.NextDelay(TimeSpan.Zero, TimeSpan.Zero));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ScannerObservationPacingPolicy.NextDelay(TimeSpan.FromMilliseconds(200), TimeSpan.FromMilliseconds(-1)));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ScannerObservationPacingPolicy.NextDelay(
                TimeSpan.FromMilliseconds(200),
                TimeSpan.Zero,
                TimeSpan.Zero));
    }
}
