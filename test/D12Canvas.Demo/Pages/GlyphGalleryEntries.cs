namespace D12Canvas.Demo.Pages;

public sealed record GlyphGalleryEntry(string Name, Glyph Glyph)
{
    public bool HasAccent =>
        Glyph.Parts.Any(part => part.Paint is GlyphPaint.AccentStroke or GlyphPaint.AccentFill);
}

public sealed record GlyphGallerySection(string Title, IReadOnlyList<GlyphGalleryEntry> Entries);

public static class GlyphGalleryEntries
{
    public static readonly IReadOnlyList<GlyphGallerySection> Sections =
    [
        new(
            "Component types",
            [
                new(nameof(Glyphs.Rectangle), Glyphs.Rectangle),
                new(nameof(Glyphs.StickyNote), Glyphs.StickyNote),
                new(nameof(Glyphs.Text), Glyphs.Text),
                new(nameof(Glyphs.Image), Glyphs.Image),
                new(nameof(Glyphs.Edge), Glyphs.Edge),
                new(nameof(Glyphs.Component), Glyphs.Component),
            ]
        ),
        new(
            "Property bar",
            [
                new(nameof(Glyphs.Fill), Glyphs.Fill),
                new(nameof(Glyphs.Stroke), Glyphs.Stroke),
                new(nameof(Glyphs.TextColour), Glyphs.TextColour),
                new(nameof(Glyphs.EdgeColour), Glyphs.EdgeColour),
                new(nameof(Glyphs.StrokeWidth), Glyphs.StrokeWidth),
                new(nameof(Glyphs.FontSize), Glyphs.FontSize),
                new(nameof(Glyphs.WeightNormal), Glyphs.WeightNormal),
                new(nameof(Glyphs.WeightBold), Glyphs.WeightBold),
                new(nameof(Glyphs.TextAlignLeft), Glyphs.TextAlignLeft),
                new(nameof(Glyphs.TextAlignCentre), Glyphs.TextAlignCentre),
                new(nameof(Glyphs.TextAlignRight), Glyphs.TextAlignRight),
                new(nameof(Glyphs.TextVerticalTop), Glyphs.TextVerticalTop),
                new(nameof(Glyphs.TextVerticalMiddle), Glyphs.TextVerticalMiddle),
                new(nameof(Glyphs.TextVerticalBottom), Glyphs.TextVerticalBottom),
                new(nameof(Glyphs.RoutingStraight), Glyphs.RoutingStraight),
                new(nameof(Glyphs.RoutingOrthogonal), Glyphs.RoutingOrthogonal),
                new(nameof(Glyphs.RoutingCurved), Glyphs.RoutingCurved),
                new(nameof(Glyphs.SourceArrowNone), Glyphs.SourceArrowNone),
                new(nameof(Glyphs.SourceArrowArrow), Glyphs.SourceArrowArrow),
                new(nameof(Glyphs.TargetArrowNone), Glyphs.TargetArrowNone),
                new(nameof(Glyphs.TargetArrowArrow), Glyphs.TargetArrowArrow),
                new(nameof(Glyphs.Caret), Glyphs.Caret),
            ]
        ),
        new(
            "Align and distribute strip",
            [
                new(nameof(Glyphs.AlignLeft), Glyphs.AlignLeft),
                new(nameof(Glyphs.AlignCentre), Glyphs.AlignCentre),
                new(nameof(Glyphs.AlignRight), Glyphs.AlignRight),
                new(nameof(Glyphs.AlignTop), Glyphs.AlignTop),
                new(nameof(Glyphs.AlignMiddle), Glyphs.AlignMiddle),
                new(nameof(Glyphs.AlignBottom), Glyphs.AlignBottom),
                new(nameof(Glyphs.DistributeHorizontally), Glyphs.DistributeHorizontally),
                new(nameof(Glyphs.DistributeVertically), Glyphs.DistributeVertically),
            ]
        ),
        new(
            "Menu rows and editors",
            [
                new(nameof(Glyphs.Check), Glyphs.Check),
                new(nameof(Glyphs.Cut), Glyphs.Cut),
                new(nameof(Glyphs.Copy), Glyphs.Copy),
                new(nameof(Glyphs.Paste), Glyphs.Paste),
                new(nameof(Glyphs.Duplicate), Glyphs.Duplicate),
                new(nameof(Glyphs.Delete), Glyphs.Delete),
                new(nameof(Glyphs.SelectAll), Glyphs.SelectAll),
                new(nameof(Glyphs.Group), Glyphs.Group),
                new(nameof(Glyphs.Ungroup), Glyphs.Ungroup),
                new(nameof(Glyphs.BringToFront), Glyphs.BringToFront),
                new(nameof(Glyphs.BringForward), Glyphs.BringForward),
                new(nameof(Glyphs.SendBackward), Glyphs.SendBackward),
                new(nameof(Glyphs.SendToBack), Glyphs.SendToBack),
                new(nameof(Glyphs.Lock), Glyphs.Lock),
                new(nameof(Glyphs.Unlock), Glyphs.Unlock),
                new(nameof(Glyphs.ZoomToSelection), Glyphs.ZoomToSelection),
                new(nameof(Glyphs.ZoomToFit), Glyphs.ZoomToFit),
                new(nameof(Glyphs.ZoomTo100Percent), Glyphs.ZoomTo100Percent),
                new(nameof(Glyphs.SnapToGrid), Glyphs.SnapToGrid),
                new(nameof(Glyphs.ObjectSnapping), Glyphs.ObjectSnapping),
                new(nameof(Glyphs.AddPort), Glyphs.AddPort),
                new(nameof(Glyphs.RemovePort), Glyphs.RemovePort),
                new(nameof(Glyphs.ChooseImage), Glyphs.ChooseImage),
                new(nameof(Glyphs.RemoveImage), Glyphs.RemoveImage),
            ]
        ),
    ];
}
