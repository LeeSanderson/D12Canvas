using D12Canvas.Model;

namespace D12Canvas.Pointer;

// A primary press on a shape's resize handle or on one of the selection box's handles. A single
// shape and a multi-selection are one gesture: every tick resizes the selection's bounding box by
// the board-space distance from the press, with the edge opposite the handle anchored, and scales
// each member exactly inside it. While Alt is held the box's centre is anchored instead and the
// opposite edge mirrors the handle; Alt is read from every move and each move recomputes from the
// start box, so a toggle keeps the handle under the pointer and the opposite edge jumps. The edges
// that move snap, never below the minimum size, which a multi-selection raises so no member
// shrinks past the floor: per axis, object snapping takes a moving edge to a neighbour's edge or
// centre where it matches and the grid rounds it where it does not, and under a centre resize
// whichever of the two edges needs the smaller correction wins. Nothing snaps while Ctrl is held,
// read from every move. Shift does nothing here, kept free for preserve-aspect-ratio. An active
// release commits what was last published; a click changes nothing.
internal sealed class ResizeSelectionGesture : PointerGesture
{
    private readonly (double X, double Y) _pressPoint;
    private readonly ResizeDirection _direction;
    private IReadOnlyList<ComponentInstance> _participants = [];
    private Bounds _startBox;
    private bool _holdsLocked;
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

        var selected = Context.SelectedInstances();
        _participants = selected.Where(instance => !instance.Locked).ToList();
        _holdsLocked = _participants.Count < selected.Count;
        _participantIds = _participants.Select(participant => participant.Id).ToHashSet();
        var startBounds = _participants.Select(participant => participant.Bounds).ToList();
        if (Bounds.Union(selected.Select(instance => instance.Bounds)) is { } box)
        {
            _startBox = box;
            _minimum = ResizeMath.MinimumBoxSizeFor(box, startBounds);
        }

        PublishScaledTo(_startBox);
    }

    protected override void OnMove(PointerMove move)
    {
        var current = Context.ToBoardPoint(move.X, move.Y);
        var centred = move.AltKey;
        var (deltaX, deltaY) = (current.X - _pressPoint.X, current.Y - _pressPoint.Y);
        var unsnapped = centred
            ? ResizeMath.ApplyAboutCentre(
                _startBox,
                _direction,
                deltaX,
                deltaY,
                _minimum.Width,
                _minimum.Height
            )
            : ResizeMath.Apply(
                _startBox,
                _direction,
                deltaX,
                deltaY,
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
                resized = centred
                    ? ResizeMath.SnapAboutCentre(
                        resized,
                        _direction,
                        _minimum.Width,
                        _minimum.Height,
                        spacing
                    )
                    : ResizeMath.SnapMovingEdges(
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
            (resized, _heldX) = SnapEdges(
                resized,
                unsnapped,
                SnapAxis.X,
                centred,
                candidates,
                tolerance,
                _heldX
            );
            (resized, _heldY) = SnapEdges(
                resized,
                unsnapped,
                SnapAxis.Y,
                centred,
                candidates,
                tolerance,
                _heldY
            );
            foreach (var (axis, held) in new[] { (SnapAxis.X, _heldX), (SnapAxis.Y, _heldY) })
            {
                if (held is not null)
                {
                    guides.AddRange(
                        ObjectSnap.GuidesForEdges(
                            resized,
                            MovingEdges(axis, centred),
                            candidates,
                            axis
                        )
                    );
                }
            }
        }

        PublishScaledTo(resized);
        Context.PublishGuides(guides);
    }

    // The edges moving on this axis, matched from where the pointer put them before any grid
    // rounding, so object snapping replaces the grid on the axis where it fires. The nearest match
    // wins; under a centre resize the other edge moves the same amount the opposite way. A match
    // that would take the box below its minimum size is not taken.
    private (Bounds Box, AxisSnap? Held) SnapEdges(
        Bounds box,
        Bounds unsnapped,
        SnapAxis axis,
        bool centred,
        IReadOnlyList<Bounds> candidates,
        double tolerance,
        AxisSnap? held
    )
    {
        var edges = MovingEdges(axis, centred);
        if (
            edges.Count == 0
            || ObjectSnap.ForEdges(unsnapped, edges, candidates, axis, tolerance, held)
                is not { } snap
        )
        {
            return (box, null);
        }

        var minimum = axis == SnapAxis.X ? _minimum.Width : _minimum.Height;
        var (start, end) =
            axis == SnapAxis.X ? (unsnapped.X, unsnapped.Right) : (unsnapped.Y, unsnapped.Bottom);
        var correction = ObjectSnap.Offset(unsnapped, axis, snap);
        if (snap.Anchor == SnapAnchor.Start)
        {
            start += correction;
            end -= centred ? correction : 0;
        }
        else
        {
            end += correction;
            start -= centred ? correction : 0;
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

    private IReadOnlyList<SnapAnchor> MovingEdges(SnapAxis axis, bool centred) =>
        ResizeMath.HandleEdge(_direction, axis) switch
        {
            null => [],
            _ when centred => [SnapAnchor.Start, SnapAnchor.End],
            { } edge => [edge],
        };

    protected override void OnRelease(PointerRelease release) => Context.CommitPreview();

    protected override void OnClick(PointerRelease release) { }

    // With a locked member in the selection the box under the pointer is the frame being dragged,
    // which a locked member left at its size may stick out of until the release re-derives it.
    private void PublishScaledTo(Bounds box)
    {
        Context.PublishPreview(
            _participants.ToDictionary(
                participant => participant.Id,
                participant => ResizeMath.ScaleWithinBox(participant.Bounds, _startBox, box)
            )
        );
        if (_holdsLocked)
        {
            Context.PublishSelectionFrame(box);
        }
    }

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
