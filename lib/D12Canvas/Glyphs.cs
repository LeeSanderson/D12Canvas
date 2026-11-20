using System.Globalization;

namespace D12Canvas;

public static class Glyphs
{
    public static readonly Glyph Rectangle = new(S("M2.75 3.75h10.5v8.5H2.75z"));

    public static readonly Glyph StickyNote = new(
        S("M3.75 2.75h8.5a1 1 0 0 1 1 1v5.5l-4 4h-5.5a1 1 0 0 1-1-1v-8.5a1 1 0 0 1 1-1z"),
        S("M13.25 9.25h-3a1 1 0 0 0-1 1v3")
    );

    public static readonly Glyph Text = new(S("M3.25 3.75h9 M7.75 3.75v8.5 M5.75 12.25h4"));

    public static readonly Glyph Image = new(
        S(RoundedRect(2.75, 3.25, 10.5, 9.5, 1)),
        S("M3.25 11.75l3-3 2.5 2.5 1.25-1.25 2.75 2.75"),
        S(Circle(10.25, 6.25, 1))
    );

    public static readonly Glyph Edge = new(
        S("M5 11 12.25 3.75 M8.25 3.75h4v4"),
        F(Circle(3.75, 12.25, 1.75))
    );

    public static readonly Glyph Component = new(
        S(RoundedRect(2.75, 2.75, 10.5, 10.5, 2)),
        S("M8.75 10.75h2v-2")
    );

    public static readonly Glyph Fill = new(
        AF(RoundedRect(2.75, 2.75, 10.5, 10.5, 2)),
        S(RoundedRect(2.75, 2.75, 10.5, 10.5, 2))
    );

    public static readonly Glyph Stroke = new(
        AS(RoundedRect(2.75, 2.75, 10.5, 10.5, 2)),
        AS(RoundedRect(5.75, 5.75, 4.5, 4.5, 0.5))
    );

    public static readonly Glyph TextColour = new(
        S("M4.5 9.75 8 2.75l3.5 7 M5.75 7.25h4.5"),
        AF(RoundedRect(2, 12, 12, 3, 1))
    );

    public static readonly Glyph EdgeColour = new(
        AS("M3.25 12.75 12.75 3.25"),
        AS("M2.75 9.25 9.25 2.75")
    );

    public static readonly Glyph StrokeWidth = new(
        F(RoundedRect(2, 3, 12, 1, 0.5)),
        F(RoundedRect(2, 6, 12, 2, 1)),
        F(RoundedRect(2, 11, 12, 3, 1.5))
    );

    public static readonly Glyph FontSize = new(
        S("M2.25 3.75h7 M5.75 3.75v8.5 M9.75 7.75h4 M11.75 7.75v4.5")
    );

    public static readonly Glyph WeightNormal = new(
        S("M5.25 2.75h3.5a2.5 2.5 0 0 1 0 5h-3.5z M5.25 7.75h4a2.75 2.75 0 0 1 0 5.5h-4z")
    );

    public static readonly Glyph WeightBold = new(
        F(
            "M4 2h4.75a3.25 3.25 0 0 1 2.4 5.45A3.4 3.4 0 0 1 9.25 14H4z M6.5 4.25v2.5h2.25a1.25 1.25 0 0 0 0-2.5z M6.5 9v2.75h2.75a1.375 1.375 0 0 0 0-2.75z"
        )
    );

    public static readonly Glyph TextAlignLeft = new(
        S("M2.75 2.75h10.5 M2.75 6.25h7 M2.75 9.75h10.5 M2.75 13.25h7")
    );

    public static readonly Glyph TextAlignCentre = new(
        S("M2.75 2.75h10.5 M4.5 6.25h7 M2.75 9.75h10.5 M4.5 13.25h7")
    );

    public static readonly Glyph TextAlignRight = new(
        S("M2.75 2.75h10.5 M6.25 6.25h7 M2.75 9.75h10.5 M6.25 13.25h7")
    );

    public static readonly Glyph TextVerticalTop = new(
        S(RoundedRect(2.75, 2.75, 10.5, 10.5, 1)),
        S("M5.25 5.25h5.5 M5.25 7.75h3.5")
    );

    public static readonly Glyph TextVerticalMiddle = new(
        S(RoundedRect(2.75, 2.75, 10.5, 10.5, 1)),
        S("M5.25 6.75h5.5 M5.25 9.25h3.5")
    );

    public static readonly Glyph TextVerticalBottom = new(
        S(RoundedRect(2.75, 2.75, 10.5, 10.5, 1)),
        S("M5.25 8.25h5.5 M5.25 10.75h3.5")
    );

    public static readonly Glyph RoutingStraight = new(S("M3.25 12.75 12.75 3.25"));

    public static readonly Glyph RoutingOrthogonal = new(S("M2.75 12.75h5.5v-9.5h5"));

    public static readonly Glyph RoutingCurved = new(S("M2.75 12.75c6 0 4.5-9.5 10.5-9.5"));

    public static readonly Glyph SourceArrowNone = new(S("M2.75 7.75h10.5"));

    public static readonly Glyph SourceArrowArrow = new(
        S("M2.75 7.75h10.5 M6.25 4.25 2.75 7.75l3.5 3.5")
    );

    public static readonly Glyph TargetArrowNone = new(S("M2.75 7.75h10.5"));

    public static readonly Glyph TargetArrowArrow = new(
        S("M2.75 7.75h10.5 M9.75 4.25l3.5 3.5-3.5 3.5")
    );

    public static readonly Glyph Caret = new(S("M4.75 6.25 8 9.5l3.25-3.25"));

    public static readonly Glyph AlignLeft = new(
        S("M2.25 1.75v12.5"),
        F(RoundedRect(4, 3, 9, 3, 0.5)),
        F(RoundedRect(4, 10, 6, 3, 0.5))
    );

    public static readonly Glyph AlignCentre = new(
        S("M7.75 1.75v12.5"),
        F(RoundedRect(3.25, 3, 9, 3, 0.5)),
        F(RoundedRect(4.75, 10, 6, 3, 0.5))
    );

    public static readonly Glyph AlignRight = new(
        S("M13.75 1.75v12.5"),
        F(RoundedRect(3, 3, 9, 3, 0.5)),
        F(RoundedRect(6, 10, 6, 3, 0.5))
    );

    public static readonly Glyph AlignTop = new(
        S("M1.75 2.25h12.5"),
        F(RoundedRect(3, 4, 3, 9, 0.5)),
        F(RoundedRect(10, 4, 3, 6, 0.5))
    );

    public static readonly Glyph AlignMiddle = new(
        S("M1.75 7.75h12.5"),
        F(RoundedRect(3, 3.25, 3, 9, 0.5)),
        F(RoundedRect(10, 4.75, 3, 6, 0.5))
    );

    public static readonly Glyph AlignBottom = new(
        S("M1.75 13.75h12.5"),
        F(RoundedRect(3, 3, 3, 9, 0.5)),
        F(RoundedRect(10, 6, 3, 6, 0.5))
    );

    public static readonly Glyph DistributeHorizontally = new(
        F(RoundedRect(2, 5, 2, 6, 0.5)),
        F(RoundedRect(7, 3, 2, 10, 0.5)),
        F(RoundedRect(12, 5, 2, 6, 0.5))
    );

    public static readonly Glyph DistributeVertically = new(
        F(RoundedRect(5, 2, 6, 2, 0.5)),
        F(RoundedRect(3, 7, 10, 2, 0.5)),
        F(RoundedRect(5, 12, 6, 2, 0.5))
    );

    public static readonly Glyph Check = new(S("M3.25 8.25 6.5 11.5l6.25-6.75"));

    public static readonly Glyph Cut = new(
        S(Circle(4.5, 11.5, 1.75)),
        S(Circle(11.5, 11.5, 1.75)),
        S("M5.75 10.25 11.25 2.75 M10.25 10.25 4.75 2.75")
    );

    public static readonly Glyph Copy = new(
        S(RoundedRect(5.75, 5.75, 7.5, 7.5, 1)),
        S("M3.75 10.25a1 1 0 0 1-1-1v-5.5a1 1 0 0 1 1-1h5.5a1 1 0 0 1 1 1")
    );

    public static readonly Glyph Paste = new(
        S("M5.75 3.25h-2a1 1 0 0 0-1 1v8.5a1 1 0 0 0 1 1h8.5a1 1 0 0 0 1-1v-8.5a1 1 0 0 0-1-1h-2"),
        S(RoundedRect(5.75, 1.75, 4.5, 2.5, 0.5))
    );

    public static readonly Glyph Duplicate = new(
        S(RoundedRect(5.75, 5.75, 7.5, 7.5, 1)),
        S("M3.75 10.25a1 1 0 0 1-1-1v-5.5a1 1 0 0 1 1-1h5.5a1 1 0 0 1 1 1"),
        S("M9.25 7.75v3 M7.75 9.25h3")
    );

    public static readonly Glyph Delete = new(
        S("M2.75 4.25h10.5 M6.25 4.25v-1.5h3.5v1.5 M4.25 4.25l.75 9h6l.75-9"),
        S("M6.75 7.25v3.5 M9.25 7.25v3.5")
    );

    public static readonly Glyph SelectAll = new(
        S("M2.25 4.25v-2h2 M11.75 2.25h2v2 M13.75 11.75v2h-2 M4.25 13.75h-2v-2"),
        S("M7 2.25h2 M7 13.75h2 M2.25 7v2 M13.75 7v2"),
        S("M6.25 6.25h3.5v3.5h-3.5z")
    );

    public static readonly Glyph Group = new(
        S("M2.25 5.25v-3h3 M10.75 2.25h3v3 M13.75 10.75v3h-3 M5.25 13.75h-3v-3"),
        S("M4.75 4.75h4.5v2.5h-4.5z M6.75 8.75h4.5v2.5h-4.5z")
    );

    public static readonly Glyph Ungroup = new(S("M2.75 2.75h5.5v4h-5.5z M7.75 9.25h5.5v4h-5.5z"));

    public static readonly Glyph BringToFront = new(
        S("M3.75 8.25h-1v-5.5h5.5v1"),
        S("M12.25 7.75h1v5.5h-5.5v-1"),
        F(RoundedRect(5, 5, 6, 6, 1))
    );

    public static readonly Glyph BringForward = new(
        S("M4.75 9.25h-2v-6.5h6.5v2"),
        F(RoundedRect(6, 6, 8, 8, 1))
    );

    public static readonly Glyph SendBackward = new(
        F("M3 2h6a1 1 0 0 1 1 1v2H6a1 1 0 0 0-1 1v4H3a1 1 0 0 1-1-1V3a1 1 0 0 1 1-1z"),
        S(RoundedRect(6.75, 6.75, 6.5, 6.5, 1))
    );

    public static readonly Glyph SendToBack = new(
        F("M3 2h4a1 1 0 0 1 1 1v1H5a1 1 0 0 0-1 1v3H3a1 1 0 0 1-1-1V3a1 1 0 0 1 1-1z"),
        F("M13 14H9a1 1 0 0 1-1-1v-1h3a1 1 0 0 0 1-1V8h1a1 1 0 0 1 1 1v4a1 1 0 0 1-1 1z"),
        S(RoundedRect(5.75, 5.75, 4.5, 4.5, 1))
    );

    public static readonly Glyph Lock = new(
        S(RoundedRect(3.75, 7.25, 8.5, 6, 1)),
        S("M5.75 7.25v-2a2.25 2.25 0 0 1 4.5 0v2")
    );

    public static readonly Glyph Unlock = new(
        S(RoundedRect(3.75, 7.25, 8.5, 6, 1)),
        S("M5.75 7.25v-2a2.25 2.25 0 0 1 4.5 0")
    );

    public static readonly Glyph ZoomToSelection = new(
        S(Circle(6.75, 6.75, 4.5)),
        S("M10 10l3.25 3.25"),
        S("M5.25 5.25h3v3h-3z")
    );

    public static readonly Glyph ZoomToFit = new(
        S("M2.25 5.25v-3h3 M10.75 2.25h3v3 M13.75 10.75v3h-3 M5.25 13.75h-3v-3"),
        S("M5.75 5.75l1.5 1.5 M10.25 5.75l-1.5 1.5 M10.25 10.25l-1.5-1.5 M5.75 10.25l1.5-1.5")
    );

    public static readonly Glyph ZoomTo100Percent = new(
        S("M2.75 5.25l1.5-1v7.5 M11.75 5.25l1.5-1v7.5"),
        S("M8 6.25v.01 M8 9.75v.01")
    );

    public static readonly Glyph SnapToGrid = new(
        S("M5.75 2.75v10.5 M10.25 2.75v10.5 M2.75 5.75h10.5 M2.75 10.25h10.5"),
        F(Circle(10.25, 10.25, 2.25))
    );

    public static readonly Glyph ObjectSnapping = new(
        S("M2.75 3.25h4v7.5h-4z M9.25 6.25h4v4.5h-4z"),
        S("M2.25 13.75h.01 M4.75 13.75h.01 M7.25 13.75h.01 M9.75 13.75h.01 M12.25 13.75h.01")
    );

    public static readonly Glyph AddPort = new(
        S("M4.75 2.75v10.5"),
        F(Circle(4.75, 7.75, 2.25)),
        S("M11.25 5.5v4.5 M9 7.75h4.5")
    );

    public static readonly Glyph RemovePort = new(
        S("M4.75 2.75v10.5"),
        F(Circle(4.75, 7.75, 2.25)),
        S("M9 7.75h4.5")
    );

    public static readonly Glyph ChooseImage = new(
        S("M8.25 12.25h-4.5a1 1 0 0 1-1-1v-7.5a1 1 0 0 1 1-1h8.5a1 1 0 0 1 1 1v3.5"),
        S("M2.75 10.75l3-3 2.25 2.25"),
        S(Circle(9.75, 5.75, 1)),
        S("M12.25 9.5v4.5 M10 11.75h4.5")
    );

    public static readonly Glyph RemoveImage = new(
        S("M8.25 12.25h-4.5a1 1 0 0 1-1-1v-7.5a1 1 0 0 1 1-1h8.5a1 1 0 0 1 1 1v3.5"),
        S("M2.75 10.75l3-3 2.25 2.25"),
        S(Circle(9.75, 5.75, 1)),
        S("M10 11.75h4.5")
    );

    private static GlyphPart S(string data) => new(data);

    private static GlyphPart F(string data) => new(data, GlyphPaint.Fill);

    private static GlyphPart AS(string data) => new(data, GlyphPaint.AccentStroke);

    private static GlyphPart AF(string data) => new(data, GlyphPaint.AccentFill);

    private static string RoundedRect(
        double x,
        double y,
        double width,
        double height,
        double radius
    )
    {
        var across = width - 2 * radius;
        var down = height - 2 * radius;
        return $"M{N(x + radius)} {N(y)}h{N(across)}{Corner(radius, radius, radius)}"
            + $"v{N(down)}{Corner(radius, -radius, radius)}"
            + $"h{N(-across)}{Corner(radius, -radius, -radius)}"
            + $"v{N(-down)}{Corner(radius, radius, -radius)}z";
    }

    private static string Corner(double radius, double dx, double dy) =>
        $"a{N(radius)} {N(radius)} 0 0 1 {N(dx)} {N(dy)}";

    private static string Circle(double centreX, double centreY, double radius) =>
        $"M{N(centreX)} {N(centreY - radius)}a{N(radius)} {N(radius)} 0 1 1 0 {N(2 * radius)}"
        + $"a{N(radius)} {N(radius)} 0 1 1 0 {N(-2 * radius)}z";

    private static string N(double value) => value.ToString(CultureInfo.InvariantCulture);
}
