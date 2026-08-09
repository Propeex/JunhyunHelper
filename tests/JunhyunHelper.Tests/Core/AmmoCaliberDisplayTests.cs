using JunhyunHelper.Core.Ammo;
using Xunit;

namespace JunhyunHelper.Tests.Core;

public sealed class AmmoCaliberDisplayTests
{
    [Theory]
    [InlineData("Caliber784x49", ".308 Marlin Express")]
    [InlineData("Caliber93x64", "9.3x64mm")]
    [InlineData("Caliber9x18PM", "9x18mm Makarov")]
    [InlineData("Caliber127x33", ".50 Action Express")]
    [InlineData("Caliber127x108", "12.7x108mm")]
    [InlineData("Caliber1143x23ACP", ".45 ACP")]
    [InlineData("Caliber762x35", ".300 Blackout")]
    [InlineData("Caliber86x70", ".338 Lapua Magnum")]
    [InlineData("Caliber366TKM", ".366 TKM")]
    [InlineData("Caliber12g", "12/70")]
    public void UsesConventionalTarkovLabels(string rawCaliber, string expected)
    {
        Assert.Equal(expected, AmmoCaliberDisplay.GetLabel(rawCaliber));
    }
}
