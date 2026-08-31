namespace JunhyunHelper.Core.FarmingGuide;

public sealed record FarmingGuideGridPlacement(
    string InstanceId,
    int X,
    int Y,
    int Width,
    int Height);

public static class FarmingGuidePlacementEngine
{
    public static (int Width, int Height) Footprint(int itemWidth, int itemHeight, bool rotated)
    {
        var width = Math.Max(1, itemWidth);
        var height = Math.Max(1, itemHeight);
        return rotated ? (height, width) : (width, height);
    }

    public static bool CanPlace(
        int gridWidth,
        int gridHeight,
        int x,
        int y,
        int itemWidth,
        int itemHeight,
        bool rotated,
        IEnumerable<FarmingGuideGridPlacement> existing,
        string? ignoredInstanceId = null)
    {
        var (width, height) = Footprint(itemWidth, itemHeight, rotated);
        if (x < 0 || y < 0 || x + width > gridWidth || y + height > gridHeight)
            return false;

        var left = x;
        var top = y;
        var right = x + width;
        var bottom = y + height;

        foreach (var placement in existing)
        {
            if (!string.IsNullOrWhiteSpace(ignoredInstanceId) &&
                string.Equals(placement.InstanceId, ignoredInstanceId, StringComparison.Ordinal))
            {
                continue;
            }

            var overlap = left < placement.X + placement.Width &&
                          right > placement.X &&
                          top < placement.Y + placement.Height &&
                          bottom > placement.Y;
            if (overlap)
                return false;
        }

        return true;
    }

    public static (int X, int Y)? FindFirstFit(
        int gridWidth,
        int gridHeight,
        int itemWidth,
        int itemHeight,
        bool rotated,
        IEnumerable<FarmingGuideGridPlacement> existing,
        string? ignoredInstanceId = null)
    {
        for (var y = 0; y < gridHeight; y++)
        {
            for (var x = 0; x < gridWidth; x++)
            {
                if (CanPlace(
                        gridWidth,
                        gridHeight,
                        x,
                        y,
                        itemWidth,
                        itemHeight,
                        rotated,
                        existing,
                        ignoredInstanceId))
                {
                    return (x, y);
                }
            }
        }

        return null;
    }
}
