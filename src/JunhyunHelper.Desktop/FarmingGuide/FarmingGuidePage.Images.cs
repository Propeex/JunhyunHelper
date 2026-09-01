using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using JunhyunHelper.Core.FarmingGuide;
using JunhyunHelper.Core.Items;

namespace JunhyunHelper.Desktop.FarmingGuide;

public partial class FarmingGuidePage
{
    private readonly Dictionary<string, ImageSource?> _farmingGuideItemImages = new(StringComparer.Ordinal);
    private Dictionary<string, (string ItemId, string ImageUrl)>? _authoritativeAssemblyImagesBySignature;

    internal Image CreateItemImage(GameItem item, bool rotated = false, Thickness? margin = null)
    {
        ArgumentNullException.ThrowIfNull(item);
        return CreateRemoteImage(
            $"item-{item.Id}",
            item.IconUrl,
            rotated,
            margin ?? new Thickness(3));
    }

    internal FrameworkElement CreateAssemblyVisual(
        FarmingGuideItemState state,
        GameItem item,
        bool rotated = false,
        Thickness? margin = null)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(item);

        var host = new Grid
        {
            Margin = margin ?? new Thickness(3),
            IsHitTestVisible = false,
            LayoutTransform = rotated ? new RotateTransform(90) : Transform.Identity,
            ClipToBounds = true,
        };

        if (TryResolveAuthoritativeAssemblyImage(state, item, out var cacheKey, out var imageUrl))
        {
            host.Children.Add(CreateRemoteImage(cacheKey, imageUrl, rotated: false, margin: new Thickness(0)));
            return host;
        }

        host.Children.Add(CreateRemoteImage(
            $"item-{item.Id}",
            item.IconUrl,
            rotated: false,
            margin: new Thickness(0)));

        var installed = FarmingGuideAssemblyPolicy.EnumerateStates(state)
            .Skip(1)
            .Select(ResolveItem)
            .Where(static value => value is not null)
            .Cast<GameItem>()
            .Take(4)
            .ToArray();
        if (installed.Length == 0)
            return host;

        var strip = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(2),
            IsHitTestVisible = false,
        };
        foreach (var part in installed)
        {
            var tile = new Border
            {
                Width = 25,
                Height = 25,
                Margin = new Thickness(1, 0, 0, 0),
                Padding = new Thickness(1),
                CornerRadius = new CornerRadius(3),
                BorderThickness = new Thickness(1),
                BorderBrush = (Brush)FindResource("BorderBrush"),
                Background = (Brush)FindResource("BackgroundDarkBrush"),
                IsHitTestVisible = false,
                Child = CreateRemoteImage(
                    $"assembly-part-{part.Id}",
                    part.FarmingGuideAssembly?.GridImageUrl ?? part.IconUrl,
                    rotated: false,
                    margin: new Thickness(0)),
            };
            strip.Children.Add(tile);
        }
        Panel.SetZIndex(strip, 5);
        host.Children.Add(strip);
        return host;
    }

    internal void ApplyItemImageRotation(Image image, bool rotated)
    {
        ArgumentNullException.ThrowIfNull(image);
        image.LayoutTransform = rotated ? new RotateTransform(90) : Transform.Identity;
    }

    internal void ResetAssemblyImageIndex() => _authoritativeAssemblyImagesBySignature = null;

    private Image CreateRemoteImage(string cacheKey, string? url, bool rotated, Thickness margin)
    {
        var image = new Image
        {
            Stretch = Stretch.Uniform,
            Margin = margin,
            IsHitTestVisible = false,
            LayoutTransform = rotated ? new RotateTransform(90) : Transform.Identity,
        };

        if (_farmingGuideItemImages.TryGetValue(cacheKey, out var cached))
            image.Source = cached;
        else
            _ = LoadImageIntoAsync(cacheKey, url, image);
        return image;
    }

    private async Task LoadImageIntoAsync(string cacheKey, string? url, Image target)
    {
        if (_images is null || string.IsNullOrWhiteSpace(url))
            return;

        if (!_farmingGuideItemImages.TryGetValue(cacheKey, out var source))
        {
            source = await _images.LoadAsync(cacheKey, url);
            _farmingGuideItemImages[cacheKey] = source;
        }

        if (target.Dispatcher.HasShutdownStarted || target.Dispatcher.HasShutdownFinished)
            return;
        target.Source = source;
    }

    private bool TryResolveAuthoritativeAssemblyImage(
        FarmingGuideItemState state,
        GameItem item,
        out string cacheKey,
        out string? imageUrl)
    {
        if (TryResolveDefaultPresetImage(state, item, out cacheKey, out imageUrl))
            return true;

        var signature = AssemblySignature(FarmingGuideAssemblyPolicy.EnumerateStates(state)
            .Select(static value => value.ItemId));
        if (signature.Length == 0)
        {
            cacheKey = string.Empty;
            imageUrl = null;
            return false;
        }

        var index = GetAuthoritativeAssemblyImageIndex();
        if (!index.TryGetValue(signature, out var source))
        {
            cacheKey = string.Empty;
            imageUrl = null;
            return false;
        }

        cacheKey = $"assembly-source-{source.ItemId}";
        imageUrl = source.ImageUrl;
        return true;
    }

    private bool TryResolveDefaultPresetImage(
        FarmingGuideItemState state,
        GameItem item,
        out string cacheKey,
        out string? imageUrl)
    {
        cacheKey = string.Empty;
        imageUrl = null;
        var presetId = item.FarmingGuideAssembly?.DefaultPresetItemId;
        if (string.IsNullOrWhiteSpace(presetId) || !_itemsById.TryGetValue(presetId, out var preset))
            return false;
        var presetSource = preset.FarmingGuideAssembly;
        if (presetSource is null || presetSource.ContainedItemIds.Count == 0)
            return false;

        var current = FarmingGuideAssemblyPolicy.EnumerateStates(state)
            .Select(static value => value.ItemId)
            .ToList();
        var expected = presetSource.ContainedItemIds.ToList();
        RemoveOne(current, item.Id);
        RemoveOne(expected, item.Id);
        current.Sort(StringComparer.Ordinal);
        expected.Sort(StringComparer.Ordinal);
        if (!current.SequenceEqual(expected, StringComparer.Ordinal))
            return false;

        imageUrl = presetSource.Image512Url ?? presetSource.GridImageUrl;
        if (string.IsNullOrWhiteSpace(imageUrl))
            return false;
        cacheKey = $"assembly-preset-{preset.Id}";
        return true;
    }

    private IReadOnlyDictionary<string, (string ItemId, string ImageUrl)> GetAuthoritativeAssemblyImageIndex()
    {
        if (_authoritativeAssemblyImagesBySignature is not null)
            return _authoritativeAssemblyImagesBySignature;

        var index = new Dictionary<string, (string ItemId, string ImageUrl)>(StringComparer.Ordinal);
        foreach (var candidate in _itemsById.Values)
        {
            var source = candidate.FarmingGuideAssembly;
            if (source is null || source.ContainedItemIds.Count == 0)
                continue;
            var imageUrl = source.Image512Url ?? source.GridImageUrl;
            if (string.IsNullOrWhiteSpace(imageUrl))
                continue;
            var signature = AssemblySignature(source.ContainedItemIds);
            if (signature.Length > 0)
                index.TryAdd(signature, (candidate.Id, imageUrl));
        }
        _authoritativeAssemblyImagesBySignature = index;
        return index;
    }

    private static string AssemblySignature(IEnumerable<string> itemIds) =>
        string.Join('\u001f', itemIds
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .OrderBy(static value => value, StringComparer.Ordinal));

    private static void RemoveOne(List<string> values, string id)
    {
        var index = values.FindIndex(value => string.Equals(value, id, StringComparison.Ordinal));
        if (index >= 0)
            values.RemoveAt(index);
    }
}
