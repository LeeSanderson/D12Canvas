using VerifyTests;

namespace D12Canvas.VisualTests;

public static class FuzzyPngComparer
{
    public const double MaxDiffPixelRatio = 0.001;

    public static void Register()
    {
        VerifierSettings.RegisterStreamComparer(
            "png",
            (received, verified, _) => Task.FromResult(Compare(received, verified))
        );
    }

    private static CompareResult Compare(Stream received, Stream verified) =>
        Evaluate(Png.Decode(received), Png.Decode(verified));

    internal static CompareResult Evaluate(Png.Image received, Png.Image verified)
    {
        if (received.Width != verified.Width || received.Height != verified.Height)
        {
            return CompareResult.NotEqual(
                $"Image dimensions differ: {received.Width}x{received.Height} vs {verified.Width}x{verified.Height}."
            );
        }

        var diffPixels = PixelMatch.Compare(
            received.Rgba,
            verified.Rgba,
            received.Width,
            received.Height
        );
        var totalPixels = received.Width * received.Height;
        var ratio = (double)diffPixels / totalPixels;

        return ratio <= MaxDiffPixelRatio
            ? CompareResult.Equal
            : CompareResult.NotEqual(
                $"{diffPixels} of {totalPixels} pixels differ ({ratio:P3}), exceeding the {MaxDiffPixelRatio:P3} tolerance for rendering noise (see ticket 79)."
            );
    }
}
