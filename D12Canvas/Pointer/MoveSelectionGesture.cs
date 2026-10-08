using D12Canvas.Model;

namespace D12Canvas.Pointer;

// A primary press on an instance or on the selection box. Pressing an entity outside the
// selection selects it at once, or appends it under Shift, so the drag has something to move;
// pressing a member leaves the selection alone until release, where a click collapses to that
// member or, under Shift, toggles it out. Every tick publishes the whole selection translated by
// the board-space distance from the press point, snapped as one rigid body by the top-left of its
// instances' bounding box, and an active release commits exactly what was last published. A
// selected edge's floating ends take the same delta; its attached ends follow their components
// and have nothing of their own to publish.
internal sealed class MoveSelectionGesture : PointerGesture
{
    private readonly (double X, double Y) _pressPoint;
    private Guid? _pressedMember;
    private IReadOnlyList<ComponentInstance> _participants = [];
    private IReadOnlyList<(EdgeEnd End, FloatingEndpoint Start)> _floatingEnds = [];
    private (double X, double Y) _origin;

    public MoveSelectionGesture(PointerPress press, IGestureContext context)
        : base(press, context)
    {
        _pressPoint = context.ToBoardPoint(press.X, press.Y);
    }

    protected override void OnPress()
    {
        if (Press.EntityId is { } entityId)
        {
            var effectiveId = Context.EffectiveSelectionId(entityId);
            if (Context.IsSelected(effectiveId))
            {
                _pressedMember = effectiveId;
            }
            else if (Press.ShiftKey)
            {
                Context.AddToSelection(effectiveId);
            }
            else
            {
                Context.ReplaceSelection([effectiveId], []);
            }
        }

        _participants = Context.SelectedInstances();
        _floatingEnds = Context.SelectedEdges().SelectMany(FloatingEndsOf).ToList();
        if (Bounds.Union(_participants.Select(participant => participant.Bounds)) is { } box)
        {
            _origin = (box.X, box.Y);
        }

        PublishTranslatedBy(0, 0);
    }

    protected override void OnMove(PointerMove move)
    {
        var current = Context.ToBoardPoint(move.X, move.Y);
        var (x, y) = Context.SnapToGrid(
            _origin.X + current.X - _pressPoint.X,
            _origin.Y + current.Y - _pressPoint.Y
        );
        PublishTranslatedBy(x - _origin.X, y - _origin.Y);
    }

    protected override void OnRelease(PointerRelease release) => Context.CommitPreview();

    // A double-click leaves the selection as the first click of the pair left it and asks the
    // pressed instance to edit, which the canvas refuses for a grouped member.
    protected override void OnClick(PointerRelease release)
    {
        if (Press.PressCount > 1)
        {
            if (Press.EntityId is { } entityId)
            {
                Context.BeginInlineEdit(entityId);
            }

            return;
        }

        if (_pressedMember is not { } member)
        {
            return;
        }

        if (Press.ShiftKey)
        {
            Context.RemoveFromSelection(member);
        }
        else
        {
            Context.ReplaceSelection([member], []);
        }
    }

    private static IEnumerable<(EdgeEnd End, FloatingEndpoint Start)> FloatingEndsOf(Edge edge)
    {
        if (edge.Source is FloatingEndpoint source)
        {
            yield return (new EdgeEnd(edge.Id, IsSource: true), source);
        }

        if (edge.Target is FloatingEndpoint target)
        {
            yield return (new EdgeEnd(edge.Id, IsSource: false), target);
        }
    }

    private void PublishTranslatedBy(double deltaX, double deltaY)
    {
        Context.PublishPreview(
            _participants.ToDictionary(
                participant => participant.Id,
                participant =>
                    participant.Bounds with
                    {
                        X = participant.Bounds.X + deltaX,
                        Y = participant.Bounds.Y + deltaY,
                    }
            )
        );
        Context.PublishMovedEndpoints(
            _floatingEnds.ToDictionary(
                floating => floating.End,
                floating => new FloatingEndpoint(
                    floating.Start.X + deltaX,
                    floating.Start.Y + deltaY
                )
            )
        );
    }
}
