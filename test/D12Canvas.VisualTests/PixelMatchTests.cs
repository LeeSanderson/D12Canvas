using System.Runtime.CompilerServices;
using Xunit;

namespace D12Canvas.VisualTests;

// Correctness coverage for the PixelMatch/Png port - not a Playwright visual test itself, just
// unit-level checks on the ported algorithm's own decisions, since a subtly wrong OKLab/HyAB port
// would silently make the fuzzy comparer either too strict (defeating the point) or too lenient
// (masking real regressions).
public class PixelMatchTests
{
    private static byte[] SolidImage(int width, int height, byte r, byte g, byte b, byte a = 255)
    {
        var pixels = new byte[width * height * 4];
        for (var i = 0; i < pixels.Length; i += 4)
        {
            pixels[i] = r;
            pixels[i + 1] = g;
            pixels[i + 2] = b;
            pixels[i + 3] = a;
        }
        return pixels;
    }

    [Fact]
    public void IdenticalImages_ReturnZeroDiff()
    {
        var image = SolidImage(20, 20, 128, 64, 200);
        var copy = (byte[])image.Clone();

        Assert.Equal(0, PixelMatch.Compare(image, copy, 20, 20));
    }

    [Fact]
    public void TinyUniformColorShift_IsWithinDefaultThreshold()
    {
        // A ±1 sRGB nudge across a whole flat image is imperceptible rendering noise, exactly the
        // class of difference the fuzzy comparer exists to absorb.
        var image1 = SolidImage(20, 20, 128, 128, 128);
        var image2 = SolidImage(20, 20, 129, 127, 129);

        Assert.Equal(0, PixelMatch.Compare(image1, image2, 20, 20));
    }

    [Fact]
    public void LargeUniformColorChange_CountsEveryPixel()
    {
        // Flat black vs. flat white: no internal contrast in either image, so the anti-aliasing
        // detector (which needs both a darker AND a brighter neighbor to suspect a ramp) can't
        // exclude any of it - every pixel should count as a real difference.
        var black = SolidImage(10, 10, 0, 0, 0);
        var white = SolidImage(10, 10, 255, 255, 255);

        Assert.Equal(100, PixelMatch.Compare(black, white, 10, 10));
    }

    [Fact]
    public void LocalizedHardEdgeChange_CountsExactlyTheChangedRegion()
    {
        // A 4x4 white block dropped into an all-black image, away from the border. Both images
        // individually have only a hard 2-tone edge (no gradient ramp), which the AA detector
        // requires evidence of a ramp on *both* sides of the center pixel to exclude - a flat
        // block edge only ever darkens (or only ever lightens) in one direction, so none of these
        // pixels should be excluded.
        const int width = 10;
        const int height = 10;
        var black = SolidImage(width, height, 0, 0, 0);
        var withPatch = (byte[])black.Clone();

        for (var y = 3; y < 7; y++)
        {
            for (var x = 3; x < 7; x++)
            {
                var pos = (y * width + x) * 4;
                withPatch[pos] = 255;
                withPatch[pos + 1] = 255;
                withPatch[pos + 2] = 255;
                withPatch[pos + 3] = 255;
            }
        }

        Assert.Equal(16, PixelMatch.Compare(black, withPatch, width, height));
    }

    [Fact]
    public void MismatchedImageLengths_Throws()
    {
        var image1 = SolidImage(10, 10, 0, 0, 0);
        var image2 = SolidImage(5, 5, 0, 0, 0);

        Assert.Throws<ArgumentException>(() => PixelMatch.Compare(image1, image2, 10, 10));
    }

    private static string SiblingPath(string fileName, [CallerFilePath] string sourceFile = "") =>
        Path.Combine(Path.GetDirectoryName(sourceFile)!, fileName);

    [Fact]
    public void Decode_RealChromiumScreenshot_ProducesPlausibleRgbaBuffer()
    {
        var path = SiblingPath("PaletteVisualTests.RenderedPalette_MatchesBaseline.verified.png");
        var image = Png.Decode(File.ReadAllBytes(path));

        Assert.True(image.Width > 0);
        Assert.True(image.Height > 0);
        Assert.Equal(image.Width * image.Height * 4, image.Rgba.Length);
    }

    [Fact]
    public void Decode_IsDeterministic()
    {
        var path = SiblingPath("PaletteVisualTests.RenderedPalette_MatchesBaseline.verified.png");
        var bytes = File.ReadAllBytes(path);

        var first = Png.Decode(bytes);
        var second = Png.Decode(bytes);

        Assert.Equal(first.Width, second.Width);
        Assert.Equal(first.Height, second.Height);
        Assert.Equal(first.Rgba, second.Rgba);
    }

    [Fact]
    public void Decode_RealScreenshot_ComparesEqualToItself()
    {
        var path = SiblingPath("PaletteVisualTests.RenderedPalette_MatchesBaseline.verified.png");
        var image = Png.Decode(File.ReadAllBytes(path));
        var copy = (byte[])image.Rgba.Clone();

        Assert.Equal(0, PixelMatch.Compare(image.Rgba, copy, image.Width, image.Height));
    }

    [Fact]
    public void Evaluate_IdenticalRealScreenshot_IsEqual()
    {
        var path = SiblingPath("PaletteVisualTests.RenderedPalette_MatchesBaseline.verified.png");
        var image = Png.Decode(File.ReadAllBytes(path));
        var copy = new Png.Image((byte[])image.Rgba.Clone(), image.Width, image.Height);

        Assert.True(FuzzyPngComparer.Evaluate(copy, image).IsEqual);
    }

    [Fact]
    public void Evaluate_SmallButRealChangeOnActualScreenshot_IsNotEqual()
    {
        // A 40x40 solid-magenta patch dropped onto a real committed baseline - visually small (a
        // few percent of the frame at most) but a real, deliberate content change, not rendering
        // noise. Guards against the fuzzy comparer's tolerance being calibrated so loose that it
        // also swallows genuine regressions, not just the sub-pixel noise it was built for.
        var path = SiblingPath("PaletteVisualTests.RenderedPalette_MatchesBaseline.verified.png");
        var original = Png.Decode(File.ReadAllBytes(path));
        var modified = WithSolidPatch(original, x: 20, y: 20, size: 40, r: 255, g: 0, b: 255);

        var result = FuzzyPngComparer.Evaluate(modified, original);

        Assert.False(result.IsEqual);
    }

    private static Png.Image WithSolidPatch(
        Png.Image image,
        int x,
        int y,
        int size,
        byte r,
        byte g,
        byte b
    )
    {
        var rgba = (byte[])image.Rgba.Clone();
        for (var dy = 0; dy < size && y + dy < image.Height; dy++)
        {
            for (var dx = 0; dx < size && x + dx < image.Width; dx++)
            {
                var pos = ((y + dy) * image.Width + (x + dx)) * 4;
                rgba[pos] = r;
                rgba[pos + 1] = g;
                rgba[pos + 2] = b;
                rgba[pos + 3] = 255;
            }
        }
        return new Png.Image(rgba, image.Width, image.Height);
    }
}
