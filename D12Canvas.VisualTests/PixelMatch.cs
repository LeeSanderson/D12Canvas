namespace D12Canvas.VisualTests;

// A .NET port of mapbox/pixelmatch v7.2.0 (https://github.com/mapbox/pixelmatch), ported to
// absorb sub-pixel rendering noise between visually-identical screenshots (see ticket 79/80):
// running the exact same pinned Docker image on different physical CI hardware produces
// byte-different but visually-identical PNGs, which Verify's default byte-exact comparison can't
// tell apart from a real regression.
//
// Faithful to upstream's actual comparison decision (the OKLab/HyAB color-difference metric and
// anti-aliasing detector) but deliberately drops the parts that only exist to render a
// highlighted diff *image* (the `output`/`diffMask`/`windowSize` options) - callers here only need
// a pass/fail pixel count, not a rendered diff. Upstream's module-level color cache is replaced
// with a cache local to each Compare call, since xUnit runs test collections in parallel and a
// shared mutable cache would race across threads.
public static class PixelMatch
{
    // A struct's implicit parameterless constructor (all fields zero) takes priority over a
    // primary constructor's default arguments when called with zero args - `new Options()` would
    // NOT give Threshold=0.1 the way it looks like it should. `Default` sidesteps that gotcha.
    public readonly record struct Options(
        double Threshold,
        bool IncludeAntiAliasing,
        bool Checkerboard
    )
    {
        public static readonly Options Default = new(
            Threshold: 0.1,
            IncludeAntiAliasing: false,
            Checkerboard: true
        );
    }

    // sRGB [0..255] -> linear [0..1], padded with a 257th entry so linLUT can always interpolate.
    private static readonly double[] Lin = BuildLinearLut();

    // Premultiplied LMS matrix contributions for opaque sRGB byte values.
    private static readonly double[] LR = new double[256];
    private static readonly double[] LG = new double[256];
    private static readonly double[] LB = new double[256];
    private static readonly double[] MR = new double[256];
    private static readonly double[] MG = new double[256];
    private static readonly double[] MB = new double[256];
    private static readonly double[] SR = new double[256];
    private static readonly double[] SG = new double[256];
    private static readonly double[] SB = new double[256];

    private const int CbrtN = 4096;
    private static readonly double[] Cbrt = BuildCbrtLut();

    private const double ToeK1 = 0.206;
    private const double ToeK2 = 0.03;
    private static readonly double ToeK3 = (1 + ToeK1) / (1 + ToeK2);

    static PixelMatch()
    {
        for (var i = 0; i < 256; i++)
        {
            var lr = Lin[i];
            LR[i] = 0.4122214708 * lr;
            MR[i] = 0.2119034982 * lr;
            SR[i] = 0.0883024619 * lr;
            LG[i] = 0.5363325363 * lr;
            MG[i] = 0.6806995451 * lr;
            SG[i] = 0.2817188376 * lr;
            LB[i] = 0.0514459929 * lr;
            MB[i] = 0.1073969566 * lr;
            SB[i] = 0.6299787005 * lr;
        }
    }

    private static double[] BuildLinearLut()
    {
        var lin = new double[257];
        for (var i = 0; i < 256; i++)
        {
            var c = i / 255.0;
            lin[i] = c <= 0.04045 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
        }
        lin[256] = lin[255];
        return lin;
    }

    private static double[] BuildCbrtLut()
    {
        var cbrt = new double[CbrtN + 2];
        for (var i = 0; i <= CbrtN + 1; i++)
        {
            cbrt[i] = Math.Cbrt((double)i / CbrtN);
        }
        return cbrt;
    }

    /// <summary>Returns the number of pixels that differ by more than a rendering-noise/anti-aliasing amount.</summary>
    public static int Compare(
        byte[] img1,
        byte[] img2,
        int width,
        int height,
        Options? options = null
    )
    {
        var opts = options ?? Options.Default;

        if (img1.Length != img2.Length)
        {
            throw new ArgumentException(
                $"Image sizes do not match: {img1.Length} vs {img2.Length}."
            );
        }

        if (img1.Length != width * height * 4)
        {
            throw new ArgumentException(
                $"Image data size does not match width/height. Expected {width * height * 4}, got {img1.Length}."
            );
        }

        if (img1.AsSpan().SequenceEqual(img2))
        {
            return 0;
        }

        var len = width * height;
        var a32 = PackPixels(img1, len);
        var b32 = PackPixels(img2, len);
        var maxDelta = opts.Threshold;
        var cache = new Dictionary<int, (double L, double M, double S, double Lr)>();
        var diff = 0;

        for (var i = 0; i < len; i++)
        {
            var pos = i * 4;
            var delta =
                a32[i] == b32[i]
                    ? 0
                    : ColorDelta(img1, img2, pos, pos, opts.Checkerboard, maxDelta, cache);
            if (delta == 0)
            {
                continue;
            }

            var x = i % width;
            var y = i / width;
            var isExcludedAsAntiAliasing =
                !opts.IncludeAntiAliasing
                && (
                    Antialiased(img1, x, y, width, height, a32, b32)
                    || Antialiased(img2, x, y, width, height, b32, a32)
                );

            if (!isExcludedAsAntiAliasing)
            {
                diff++;
            }
        }

        return diff;
    }

    private static uint[] PackPixels(byte[] img, int len)
    {
        var packed = new uint[len];
        for (int i = 0, pos = 0; i < len; i++, pos += 4)
        {
            packed[i] = (uint)(
                img[pos] | (img[pos + 1] << 8) | (img[pos + 2] << 16) | (img[pos + 3] << 24)
            );
        }
        return packed;
    }

    // Based on "Anti-aliased Pixel and Intensity Slope Detector" (V. Vysniauskas, 2009): a pixel
    // is anti-aliasing if it sits on a monotonic brightness ramp between two neighbors that each
    // have 3+ identical siblings in both images (i.e. neighbors that are themselves flat regions,
    // not part of the same edge).
    private static bool Antialiased(
        byte[] img,
        int x1,
        int y1,
        int width,
        int height,
        uint[] a32,
        uint[] b32
    )
    {
        var x0 = x1 > 0 ? x1 - 1 : 0;
        var y0 = y1 > 0 ? y1 - 1 : 0;
        var x2 = x1 < width - 1 ? x1 + 1 : width - 1;
        var y2 = y1 < height - 1 ? y1 + 1 : height - 1;
        var centerPos = (y1 * width + x1) * 4;
        int cr = img[centerPos],
            cg = img[centerPos + 1],
            cb = img[centerPos + 2],
            ca = img[centerPos + 3];

        var zeroes = x1 == x0 || x1 == x2 || y1 == y0 || y1 == y2 ? 1 : 0;
        double min = 0,
            max = 0;
        int minX = 0,
            minY = 0,
            maxX = 0,
            maxY = 0;

        for (var x = x0; x <= x2; x++)
        {
            for (var y = y0; y <= y2; y++)
            {
                if (x == x1 && y == y1)
                {
                    continue;
                }

                var delta = BrightnessDelta(img, (y * width + x) * 4, cr, cg, cb, ca);
                if (delta == 0)
                {
                    zeroes++;
                    if (zeroes > 2)
                    {
                        return false;
                    }
                }
                else if (delta < min)
                {
                    min = delta;
                    minX = x;
                    minY = y;
                }
                else if (delta > max)
                {
                    max = delta;
                    maxX = x;
                    maxY = y;
                }
            }
        }

        if (min == 0 || max == 0)
        {
            return false;
        }

        return (
                HasManySiblings(a32, minX, minY, width, height)
                && HasManySiblings(b32, minX, minY, width, height)
            )
            || (
                HasManySiblings(a32, maxX, maxY, width, height)
                && HasManySiblings(b32, maxX, maxY, width, height)
            );
    }

    private static bool HasManySiblings(uint[] img, int x1, int y1, int width, int height)
    {
        var val = img[y1 * width + x1];
        var x0 = x1 > 0 ? x1 - 1 : 0;
        var y0 = y1 > 0 ? y1 - 1 : 0;
        var x2 = x1 < width - 1 ? x1 + 1 : width - 1;
        var y2 = y1 < height - 1 ? y1 + 1 : height - 1;
        var zeroes = x1 == x0 || x1 == x2 || y1 == y0 || y1 == y2 ? 1 : 0;

        for (var x = x0; x <= x2; x++)
        {
            for (var y = y0; y <= y2; y++)
            {
                if (x == x1 && y == y1)
                {
                    continue;
                }
                if (img[y * width + x] == val)
                {
                    zeroes++;
                    if (zeroes > 2)
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    // Cheap, monotonic Rec.601-luma brightness delta used only to find the intensity ramp
    // direction for AA detection - intentionally not the OKLab metric ColorDelta uses below.
    private static double BrightnessDelta(byte[] img, int m, int r1, int g1, int b1, int a1)
    {
        int r2 = img[m],
            g2 = img[m + 1],
            b2 = img[m + 2],
            a2 = img[m + 3];

        double dr = r1 - r2;
        double dg = g1 - g2;
        double db = b1 - b2;
        var da = a1 - a2;

        if (dr == 0 && dg == 0 && db == 0 && da == 0)
        {
            return 0;
        }

        if (a1 < 255 || a2 < 255)
        {
            dr = (r1 * a1 - r2 * a2 - 255.0 * da) / 255.0;
            dg = (g1 * a1 - g2 * a2 - 255.0 * da) / 255.0;
            db = (b1 * a1 - b2 * a2 - 255.0 * da) / 255.0;
            var d = dr * 0.29889531 + dg * 0.58662247 + db * 0.11448223;
            return d == 0 && da != 0 ? da / 2.0 : d;
        }

        return dr * 0.29889531 + dg * 0.58662247 + db * 0.11448223;
    }

    /// <summary>0 if the two pixels' OKLab HyAB distance is within maxDelta, otherwise ±1 (negative if the img2 pixel is darker).</summary>
    private static int ColorDelta(
        byte[] img1,
        byte[] img2,
        int k,
        int m,
        bool checkerboard,
        double maxDelta,
        Dictionary<int, (double L, double M, double S, double Lr)> cache
    )
    {
        int r1 = img1[k],
            g1 = img1[k + 1],
            b1 = img1[k + 2],
            a1 = img1[k + 3];
        int r2 = img2[m],
            g2 = img2[m + 1],
            b2 = img2[m + 2],
            a2 = img2[m + 3];

        return a1 == 255 && a2 == 255
            ? ColorDeltaOpaque(r1, g1, b1, r2, g2, b2, maxDelta, cache)
            : ColorDeltaTransparent(r1, g1, b1, a1, r2, g2, b2, a2, k, checkerboard, maxDelta);
    }

    private static int ColorDeltaOpaque(
        int r1,
        int g1,
        int b1,
        int r2,
        int g2,
        int b2,
        double maxDelta,
        Dictionary<int, (double L, double M, double S, double Lr)> cache
    )
    {
        var (l1, m1, s1, lr1) = OpaqueLms(r1, g1, b1, cache);
        var (l2, m2, s2, lr2) = OpaqueLms(r2, g2, b2, cache);
        return OklabHyabDelta(lr1 - lr2, l1 - l2, m1 - m2, s1 - s2, maxDelta);
    }

    private static (double L, double M, double S, double Lr) OpaqueLms(
        int r,
        int g,
        int b,
        Dictionary<int, (double L, double M, double S, double Lr)> cache
    )
    {
        var key = (r << 16) | (g << 8) | b;
        if (cache.TryGetValue(key, out var cached))
        {
            return cached;
        }

        var l = CbrtLut(LR[r] + LG[g] + LB[b]);
        var m = CbrtLut(MR[r] + MG[g] + MB[b]);
        var s = CbrtLut(SR[r] + SG[g] + SB[b]);
        var lr = Toe(0.2104542553 * l + 0.7936177850 * m - 0.0040720468 * s);
        var value = (l, m, s, lr);
        cache[key] = value;
        return value;
    }

    private static int ColorDeltaTransparent(
        int r1,
        int g1,
        int b1,
        int a1,
        int r2,
        int g2,
        int b2,
        int a2,
        int k,
        bool checkerboard,
        double maxDelta
    )
    {
        double rb = 255,
            gb = 255,
            bb = 255;
        if (checkerboard)
        {
            rb = 48 + 159 * (k % 2);
            gb = 48 + 159 * (((int)(k / 1.618033988749895)) % 2);
            bb = 48 + 159 * (((int)(k / 2.618033988749895)) % 2);
        }

        var fr1 = (r1 * a1 + rb * (255 - a1)) / 255.0;
        var fg1 = (g1 * a1 + gb * (255 - a1)) / 255.0;
        var fb1 = (b1 * a1 + bb * (255 - a1)) / 255.0;
        var fr2 = (r2 * a2 + rb * (255 - a2)) / 255.0;
        var fg2 = (g2 * a2 + gb * (255 - a2)) / 255.0;
        var fb2 = (b2 * a2 + bb * (255 - a2)) / 255.0;

        var lr1 = LinLut(fr1);
        var lg1 = LinLut(fg1);
        var lb1 = LinLut(fb1);
        var lr2 = LinLut(fr2);
        var lg2 = LinLut(fg2);
        var lb2 = LinLut(fb2);

        var l1 = CbrtLut(0.4122214708 * lr1 + 0.5363325363 * lg1 + 0.0514459929 * lb1);
        var m1 = CbrtLut(0.2119034982 * lr1 + 0.6806995451 * lg1 + 0.1073969566 * lb1);
        var s1 = CbrtLut(0.0883024619 * lr1 + 0.2817188376 * lg1 + 0.6299787005 * lb1);
        var l2 = CbrtLut(0.4122214708 * lr2 + 0.5363325363 * lg2 + 0.0514459929 * lb2);
        var m2 = CbrtLut(0.2119034982 * lr2 + 0.6806995451 * lg2 + 0.1073969566 * lb2);
        var s2 = CbrtLut(0.0883024619 * lr2 + 0.2817188376 * lg2 + 0.6299787005 * lb2);

        var lrr1 = Toe(0.2104542553 * l1 + 0.7936177850 * m1 - 0.0040720468 * s1);
        var lrr2 = Toe(0.2104542553 * l2 + 0.7936177850 * m2 - 0.0040720468 * s2);

        return OklabHyabDelta(lrr1 - lrr2, l1 - l2, m1 - m2, s1 - s2, maxDelta);
    }

    // HyAB distance = |dLr| + sqrt(da^2 + db^2). Avoids the sqrt: it stays below maxDelta iff
    // |dLr| <= maxDelta and da^2 + db^2 <= (maxDelta - |dLr|)^2. Only the sign of the result is
    // otherwise used (encodes whether img1's pixel is lighter or darker).
    private static int OklabHyabDelta(double dLr, double dl, double dm, double ds, double maxDelta)
    {
        var rest = maxDelta - Math.Abs(dLr);
        if (rest > 0)
        {
            var da = 1.9779984951 * dl - 2.4285922050 * dm + 0.4505937099 * ds;
            var db = 0.0259040371 * dl + 0.7827717662 * dm - 0.8086757660 * ds;
            if (da * da + db * db <= rest * rest)
            {
                return 0;
            }
        }

        return dLr > 0 ? -1 : 1;
    }

    private static double Toe(double l)
    {
        var x = ToeK3 * l - ToeK1;
        return 0.5 * (x + Math.Sqrt(x * x + 4 * ToeK2 * ToeK3 * l));
    }

    // sRGB->linear for a fractional [0..255] channel value via linear interpolation of Lin.
    private static double LinLut(double x)
    {
        var i = (int)x;
        return Lin[i] + (Lin[i + 1] - Lin[i]) * (x - i);
    }

    // Cube root over [0..1] via lookup table with linear interpolation.
    private static double CbrtLut(double x)
    {
        var t = x * CbrtN;
        var i = (int)t;
        // Clamps for floating-point x slightly outside [0,1] (e.g. underflow near black) -
        // upstream's JS would silently produce NaN from an out-of-bounds array read instead of
        // throwing, so this keeps the same "never crashes" behavior without changing normal results.
        i = Math.Clamp(i, 0, CbrtN);
        return Cbrt[i] + (Cbrt[i + 1] - Cbrt[i]) * (t - i);
    }
}
