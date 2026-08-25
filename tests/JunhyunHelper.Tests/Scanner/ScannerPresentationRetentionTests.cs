using JunhyunHelper.Core.Scanner;

namespace JunhyunHelper.Tests.Scanner;

public sealed class ScannerPresentationRetentionTests
{
    [Fact]
    public void Confirm_HoldsItemAcrossTwoMisses_AndHidesOnThird()
    {
        var retention = new ScannerPresentationRetention();

        retention.Confirm("item-a");

        Assert.False(retention.ReportMiss());
        Assert.Equal("item-a", retention.ItemId);
        Assert.Equal(1, retention.ConsecutiveMisses);

        Assert.False(retention.ReportMiss());
        Assert.Equal("item-a", retention.ItemId);
        Assert.Equal(2, retention.ConsecutiveMisses);

        Assert.True(retention.ReportMiss());
        Assert.False(retention.HasItem);
        Assert.Null(retention.ItemId);
        Assert.Equal(0, retention.ConsecutiveMisses);
    }

    [Fact]
    public void Confirm_SameItemResetsMissBudget()
    {
        var retention = new ScannerPresentationRetention();
        retention.Confirm("item-a");
        Assert.False(retention.ReportMiss());
        Assert.False(retention.ReportMiss());

        var changed = retention.Confirm("item-a");

        Assert.False(changed);
        Assert.Equal(0, retention.ConsecutiveMisses);
        Assert.False(retention.ReportMiss());
        Assert.Equal("item-a", retention.ItemId);
    }

    [Fact]
    public void Confirm_DifferentItemReplacesImmediately_AndResetsMissBudget()
    {
        var retention = new ScannerPresentationRetention();
        retention.Confirm("item-a");
        Assert.False(retention.ReportMiss());
        Assert.False(retention.ReportMiss());

        var changed = retention.Confirm("item-b");

        Assert.True(changed);
        Assert.Equal("item-b", retention.ItemId);
        Assert.Equal(0, retention.ConsecutiveMisses);
    }

    [Fact]
    public void Reset_ClearsHeldItemImmediately()
    {
        var retention = new ScannerPresentationRetention();
        retention.Confirm("item-a");
        Assert.False(retention.ReportMiss());

        retention.Reset();

        Assert.False(retention.HasItem);
        Assert.Null(retention.ItemId);
        Assert.Equal(0, retention.ConsecutiveMisses);
        Assert.False(retention.ReportMiss());
    }

    [Fact]
    public void Constructor_RejectsInvalidMissLimit()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ScannerPresentationRetention(0));
    }
}
