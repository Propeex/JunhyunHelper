using JunhyunHelper.Desktop.Services;
using SkiaSharp;
using Xunit;

namespace JunhyunHelper.Tests.Desktop;

public sealed class MapArtworkImageRegistrationTests
{
    [Fact]
    public void Finds_small_canvas_scale_and_translation_between_revisions()
    {
        const int width = 420;
        const int height = 360;
        const double expectedScale = 1.04;
        const double expectedTx = 0.03;
        const double expectedTy = -0.02;

        using var baseline = DrawReference(width, height);
        using var revised = new SKBitmap(width, height);
        using (var canvas = new SKCanvas(revised))
        {
            canvas.Clear(SKColors.Black);
            canvas.Translate(
                (float)(width * (0.5 + expectedTx)),
                (float)(height * (0.5 + expectedTy)));
            canvas.Scale((float)expectedScale, (float)expectedScale);
            canvas.Translate(-width / 2f, -height / 2f);
            canvas.DrawBitmap(baseline, 0, 0);
        }

        var ok = MapArtworkImageRegistration.TryRegister(
            EncodePng(baseline),
            EncodePng(revised),
            new MapArtworkRegistrationRegion(0.12, 0.10, 0.88, 0.90),
            out var transform,
            out var score);

        Assert.True(ok, $"registration score={score:F3}");
        Assert.True(score > 0.82, $"registration score={score:F3}");
        Assert.InRange(transform.Scale, expectedScale - 0.025, expectedScale + 0.025);
        Assert.InRange(transform.TranslateX, expectedTx - 0.015, expectedTx + 0.015);
        Assert.InRange(transform.TranslateY, expectedTy - 0.015, expectedTy + 0.015);
    }

    private static SKBitmap DrawReference(int width, int height)
    {
        var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Black);
        using var light = new SKPaint { Color = new SKColor(225, 225, 225), IsAntialias = false };
        using var medium = new SKPaint { Color = new SKColor(115, 115, 115), IsAntialias = false };
        using var dark = new SKPaint { Color = new SKColor(45, 45, 45), IsAntialias = false };

        canvas.DrawRect(width * 0.18f, height * 0.16f, width * 0.25f, height * 0.13f, light);
        canvas.DrawRect(width * 0.52f, height * 0.12f, width * 0.19f, height * 0.27f, medium);
        canvas.DrawCircle(width * 0.72f, height * 0.56f, width * 0.075f, light);
        canvas.DrawRect(width * 0.26f, height * 0.55f, width * 0.34f, height * 0.08f, medium);
        canvas.DrawRect(width * 0.42f, height * 0.70f, width * 0.12f, height * 0.18f, light);
        canvas.DrawRect(width * 0.12f, height * 0.40f, width * 0.72f, height * 0.025f, dark);
        return bitmap;
    }

    private static byte[] EncodePng(SKBitmap bitmap)
    {
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }
}
