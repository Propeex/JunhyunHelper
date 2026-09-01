using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using JunhyunHelper.Core.FarmingGuide;
using JunhyunHelper.Core.Items;

namespace JunhyunHelper.Desktop.FarmingGuide;

public partial class FarmingGuidePage
{
    private readonly Dictionary<string, ImageSource?> _farmingGuideItemImages = new(StringComparer.Ordinal);

    internal Image CreateItemImage(GameItem item, bool rotated = false, Thickness? margin = null)
    {
        ArgumentNullException.ThrowIfNull(item);
        var url = FarmingGuideCompleteEquipmentPolicy.PreferredCompleteImageUrl(item, _itemsById);
        return CreateRemoteImage(
            $"complete-item-{item.Id}",
            url,
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

        // v1.15.2 deliberately ignores user-maintained assembly state. Equipment is
        // shown as one source-backed complete item. The smaller equipment-slot safety
        // inset lets long weapons occupy the slot similarly to Tarkov without cropping.
        var requestedMargin = margin ?? new Thickness(3);
        var effectiveMargin = requestedMargin.Left >= 8d && requestedMargin.Top >= 20d
            ? new Thickness(2, 20, 2, 2)
            : requestedMargin;

        return CreateItemImage(item, rotated, effectiveMargin);
    }

    internal void ApplyItemImageRotation(Image image, bool rotated)
    {
        ArgumentNullException.ThrowIfNull(image);
        image.LayoutTransform = rotated ? new RotateTransform(90) : Transform.Identity;
    }

    internal void ResetAssemblyImageIndex() => _farmingGuideItemImages.Clear();

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
}
