using D12Canvas.Model;

namespace D12Canvas.Pointer;

// A primary press on a shape's resize handle or on one of the selection box's handles. A single
// shape and a multi-selection are one gesture: every tick resizes the selection's bounding box by
// the board-space distance from the press, with the edge opposite the handle anchored, and scales
// each member exactly inside it. Under snap the edges the handle moves round to grid lines, never
// below the minimum size, which a multi-selection raises so no member shrinks past the floor. An
// active release commits what was last published; a click changes nothing.
internal sealed class ResizeSelectionGesture : PointerGesture
{
    private readonly (double X, double Y) _pressPoint;
    private readonly ResizeDirection _direction;
    private IReadOnlyList<ComponentInstance> _participants = [];
    private Bounds _startBox;
    private (double Width, double Height) _minimum;

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
                Context.ReplaceSelection([effectiveId]);
            }
        }

        _participants = Context.SelectedInstances();
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
        var resized = ResizeMath.Apply(
            _startBox,
            _direction,
            current.X - _pressPoint.X,
            current.Y - _pressPoint.Y,
            _minimum.Width,
            _minimum.Height
        );

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

        PublishScaledTo(resized);
    }

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
