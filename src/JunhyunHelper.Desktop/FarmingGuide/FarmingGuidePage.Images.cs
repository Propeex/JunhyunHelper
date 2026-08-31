using System.Windows.Controls;
using System.Windows.Media;
using JunhyunHelper.Core.Items;

namespace JunhyunHelper.Desktop.FarmingGuide;

public partial class FarmingGuidePage
{
    private readonly Dictionary<string, ImageSource?> _farmingGuideItemImages = new(StringComparer.Ordinal);

    internal Image CreateItemImage(GameItem item, bool rotated = false, Thickness? margin = null)
    {
        ArgumentNullException.ThrowIfNull(item);

        var image = new Image
        {
            Stretch = Stretch.Uniform,
            Margin = margin ?? new Thickness(3),
            IsHitTestVisible = false,
            RenderTransformOrigin = new Point(0.5, 0.5),
            RenderTransform = rotated ? new RotateTransform(90) : Transform.Identity,
        };

        if (_farmingGuideItemImages.TryGetValue(item.Id, out var cached))
        {
            image.Source = cached;
        }
        else
        {
            _ = LoadItemImageIntoAsync(item, image);
        }

        return image;
    }

    internal void ApplyItemImageRotation(Image image, bool rotated)
    {
        ArgumentNullException.ThrowIfNull(image);
        image.RenderTransformOrigin = new Point(0.5, 0.5);
        image.RenderTransform = rotated ? new RotateTransform(90) : Transform.Identity;
    }

    private async Task LoadItemImageIntoAsync(GameItem item, Image target)
    {
        if (_images is null)
            return;

        if (!_farmingGuideItemImages.TryGetValue(item.Id, out var source))
        {
            source = await _images.LoadAsync($"item-{item.Id}", item.IconUrl);
            _farmingGuideItemImages[item.Id] = source;
        }

        if (target.Dispatcher.HasShutdownStarted || target.Dispatcher.HasShutdownFinished)
            return;

        target.Source = source;
    }
}
