using D12Canvas.Model;

namespace D12Canvas.Pointer;

// A primary press on a shape's resize handle or on one of the selection box's handles. A single
// shape and a multi-selection are one gesture: every tick resizes the selection's bounding box by
// the board-space distance from the press, with the edge opposite the handle anchored, and scales
// each member exactly inside it. The edges the handle moves snap, never below the minimum size,
// which a multi-selection raises so no member shrinks past the floor: per axis, object snapping
// takes the moving edge to a neighbour's edge or centre where it matches and the grid rounds it
// where it does not. Nothing snaps while Ctrl is held, read from every move. Shift does nothing
// here, kept free for preserve-aspect-ratio. An active release commits what was last published; a
// click changes nothing.
internal sealed class ResizeSelectionGesture : PointerGesture
{
    private readonly (double X, double Y) _pressPoint;
    private readonly ResizeDirection _direction;
    private IReadOnlyList<ComponentInstance> _participants = [];
    private Bounds _startBox;
    private (double Width, double Height) _minimum;
    private HashSet<Guid> _participantIds = [];
    private AxisSnap? _heldX;
    private AxisSnap? _heldY;

    public ResizeSelectionGesture(PointerPress press, IGestureContext context)
        : base(press, context)
    {
        _pressPoint = context.ToBoardPoint(press.X, press.Y);
        _direction = DirectionOf(press.Part);
    }

    // A shape's own handles only show while it is the whole selection, so pressing one outside
    // the selection resizes that shape alone.
    protected override void OnPress()
    {
        if (Press.EntityId is { } entityId)
        {
            var effectiveId = Context.EffectiveSelectionId(entityId);
            if (!Context.IsSelected(effectiveId))
            {
                Context.ReplaceSelection([effectiveId], []);
            }
        }

        _participants = Context.SelectedInstances();
        _participantIds = _participants.Select(participant => participant.Id).ToHashSet();
        var startBounds = _participants.Select(participant => participant.Bounds).ToList();
        if (Bounds.Union(startBounds) is { } box)
        {
            _startBox = box;
            _minimum = ResizeMath.MinimumBoxSizeFor(box, startBounds);
        }

        PublishScaledTo(_startBox);
    }

    protected override void OnMove(PointerMove move)
    {
        var current = Context.ToBoardPoint(move.X, move.Y);
        var unsnapped = ResizeMath.Apply(
            _startBox,
            _direction,
            current.X - _pressPoint.X,
            current.Y - _pressPoint.Y,
            _minimum.Width,
            _minimum.Height
        );
        var resized = unsnapped;

        var guides = new List<SnapGuide>();
        if (move.CtrlKey)
        {
            _heldX = null;
            _heldY = null;
        }
        else
        {
            if (Context.GridSpacing is { } spacing)
            {
                resized = ResizeMath.SnapMovingEdges(
                    resized,
                    _startBox,
                    _direction,
                    _minimum.Width,
                    _minimum.Height,
                    spacing
                );
            }

            var candidates = ObjectSnapCandidates(move, _participantIds);
            var tolerance = ObjectSnapTolerance;
            (resized, _heldX) = SnapEdge(
                resized,
                unsnapped,
                SnapAxis.X,
                candidates,
                tolerance,
                _heldX
            );
            (resized, _heldY) = SnapEdge(
                resized,
                unsnapped,
                SnapAxis.Y,
                candidates,
                tolerance,
                _heldY
            );
            foreach (var (axis, held) in new[] { (SnapAxis.X, _heldX), (SnapAxis.Y, _heldY) })
            {
                if (held is { } snap)
                {
                    guides.AddRange(
                        ObjectSnap.GuidesForEdge(resized, snap.Anchor, candidates, axis)
                    );
                }
            }
        }

        PublishScaledTo(resized);
        Context.PublishGuides(guides);
    }

    // The edge the handle moves on this axis, matched from where the pointer put it before any grid
    // rounding, so object snapping replaces the grid on the axis where it fires. A match that would
    // take the box below its minimum size is not taken.
    private (Bounds Box, AxisSnap? Held) SnapEdge(
        Bounds box,
        Bounds unsnapped,
        SnapAxis axis,
        IReadOnlyList<Bounds> candidates,
        double tolerance,
        AxisSnap? held
    )
    {
        if (MovingEdge(axis) is not { } edge)
        {
            return (box, null);
        }

        if (ObjectSnap.ForEdge(unsnapped, edge, candidates, axis, tolerance, held) is not { } snap)
        {
            return (box, null);
        }

        var minimum = axis == SnapAxis.X ? _minimum.Width : _minimum.Height;
        var (start, end) = axis == SnapAxis.X ? (box.X, box.Right) : (box.Y, box.Bottom);
        if (edge == SnapAnchor.Start)
        {
            start = snap.Target;
        }
        else
        {
            end = snap.Target;
        }

        if (end - start < minimum)
        {
            return (box, null);
        }

        var snapped =
            axis == SnapAxis.X
                ? box with
                {
                    X = start,
                    Width = end - start,
                }
                : box with
                {
                    Y = start,
                    Height = end - start,
                };
        return (snapped, snap);
    }

    private SnapAnchor? MovingEdge(SnapAxis axis) =>
        (axis, _direction) switch
        {
            (
                SnapAxis.X,
                ResizeDirection.Left
                    or ResizeDirection.TopLeft
                    or ResizeDirection.BottomLeft
            ) => SnapAnchor.Start,
            (
                SnapAxis.X,
                ResizeDirection.Right
                    or ResizeDirection.TopRight
                    or ResizeDirection.BottomRight
            ) => SnapAnchor.End,
            (
                SnapAxis.Y,
                ResizeDirection.Top
                    or ResizeDirection.TopLeft
                    or ResizeDirection.TopRight
            ) => SnapAnchor.Start,
            (
                SnapAxis.Y,
                ResizeDirection.Bottom
                    or ResizeDirection.BottomLeft
                    or ResizeDirection.BottomRight
            ) => SnapAnchor.End,
            _ => null,
        };

    protected override void OnRelease(PointerRelease release) => Context.CommitPreview();

    protected override void OnClick(PointerRelease release) { }

    private void PublishScaledTo(Bounds box) =>
        Context.PublishPreview(
            _participants.ToDictionary(
                participant => participant.Id,
                participant => ResizeMath.ScaleWithinBox(participant.Bounds, _startBox, box)
            )
        );

    private static ResizeDirection DirectionOf(string? part) =>
        part switch
        {
            "top-left" => ResizeDirection.TopLeft,
            "top" => ResizeDirection.Top,
            "top-right" => ResizeDirection.TopRight,
            "right" => ResizeDirection.Right,
            "bottom-right" => ResizeDirection.BottomRight,
            "bottom" => ResizeDirection.Bottom,
            "bottom-left" => ResizeDirection.BottomLeft,
            "left" => ResizeDirection.Left,
            _ => throw new ArgumentException($"No resize handle is named '{part}'.", nameof(part)),
        };
}
