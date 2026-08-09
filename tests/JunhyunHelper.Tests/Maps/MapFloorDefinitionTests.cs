using JunhyunHelper.Core.Maps;
using Xunit;

namespace JunhyunHelper.Tests.Maps;

public sealed class MapFloorDefinitionTests
{
    [Fact]
    public void ToString_returns_floor_display_name_for_selection_boxes()
    {
        var floor = new MapFloorDefinition(
            "main",
            "기본층",
            "Ground_Level",
            -1000,
            1000,
            true);

        Assert.Equal("기본층", floor.ToString());
    }
}
