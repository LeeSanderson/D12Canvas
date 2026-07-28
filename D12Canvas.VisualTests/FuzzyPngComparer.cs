using VerifyTests;

namespace D12Canvas.VisualTests;

// Registers a tolerant PNG comparer with Verify so screenshot baselines survive imperceptible
// rendering noise between hosts/runs (ticket 79/80) - the same pinned Docker image produces
// byte-different but visually-identical PNGs depending on which physical machine runs it, which
// Verify's default byte-exact comparison can't tell apart from a real regression.
public static class FuzzyPngComparer
{
    // Calibrated against 13 real "should match" pairs pulled from two separate GitHub Actions
    // runs and the pre-existing committed baselines: every one of them measured as an *exact*
    // 0.0% diff under PixelMatch's own default threshold. This tolerance is deliberately still
    // well above that observed noise floor as headroom for slightly worse noise, while remaining
    // orders of magnitude below what any real added/moved/changed visual element would produce.
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

    // Split out from Compare so tests can exercise the actual pass/fail decision directly against
    // decoded pixel buffers, without needing a PNG encoder just to round-trip a synthetic image.
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
