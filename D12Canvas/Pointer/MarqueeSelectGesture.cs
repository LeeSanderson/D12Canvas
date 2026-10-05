using D12Canvas.Model;

namespace D12Canvas.Pointer;

// A plain primary drag on empty canvas draws the selection band. Every tick replaces the
// selection with what the band intersects, resolved outward to groups; with Shift held at press
// the band's contents are unioned into the press-time selection instead, so a selection can be
// collected across several sweeps. A release below the threshold is the click on empty canvas:
// it clears the selection and nothing else.
internal sealed class MarqueeSelectGesture : PointerGesture
{
    private readonly (double X, double Y) _anchor;

    public MarqueeSelectGesture(PointerPress press, IGestureContext context)
        : base(press, context)
    {
        _anchor = context.ToBoardPoint(press.X, press.Y);
    }

    protected override void OnMove(PointerMove move)
    {
        var current = Context.ToBoardPoint(move.X, move.Y);
        var band = BandBetween(_anchor, current);
        Context.ShowMarquee(band);

        var swept = (Context.Board?.Components ?? [])
            .Where(instance => instance.Bounds.Intersects(band))
            .Select(instance => Context.EffectiveSelectionId(instance.Id));

        Context.ReplaceSelection(
            Press.ShiftKey ? Context.SelectionSnapshot.InstanceIds.Union(swept) : swept
        );
    }

    protected override void OnRelease(PointerRelease release) => Context.ShowMarquee(null);

    protected override void OnClick(PointerRelease release) => Context.ClearSelection();

    private static Bounds BandBetween((double X, double Y) a, (double X, double Y) b) =>
        new(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y), Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y));
}
