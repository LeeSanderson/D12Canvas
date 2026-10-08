using D12Canvas.Model;

namespace D12Canvas.Pointer;

// A primary press on an instance or on the selection box. Pressing an entity outside the
// selection selects it at once, or appends it under Shift, so the drag has something to move;
// pressing a member leaves the selection alone until release, where a click collapses to that
// member or, under Shift, toggles it out. A click on the selection box does the same with the
// topmost entity beneath it, and an Alt click selects the next entity down the hit stack from the
// one selected before the press. Every tick publishes the whole selection translated by
// the board-space distance from the press point, snapped as one rigid body by the top-left of its
// instances' bounding box, straightened to one axis while Shift is held and left unsnapped while
// Ctrl is, and an active release commits exactly what was last published. A
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

    // Shift holds the axis the press-anchored delta has moved least along. That axis is never
    // snapped, since rounding it would move the selection along the axis just locked.
    protected override void OnMove(PointerMove move)
    {
        var current = Context.ToBoardPoint(move.X, move.Y);
        var deltaX = current.X - _pressPoint.X;
        var deltaY = current.Y - _pressPoint.Y;
        var xLocked = move.ShiftKey && Math.Abs(deltaY) > Math.Abs(deltaX);
        var yLocked = move.ShiftKey && !xLocked;

        var x = xLocked ? _origin.X : _origin.X + deltaX;
        var y = yLocked ? _origin.Y : _origin.Y + deltaY;
        if (!move.CtrlKey && Context.GridSpacing is { } spacing)
        {
            x = xLocked ? x : GridSnap.NearestLine(x, spacing);
            y = yLocked ? y : GridSnap.NearestLine(y, spacing);
        }

        PublishTranslatedBy(x - _origin.X, y - _origin.Y);
    }

    protected override void OnRelease(PointerRelease release) => Context.CommitPreview();

    // An Alt click cycles whatever its press count, so quick Alt clicks keep stepping down. A
    // double-click on a member of a group that is not entered, or on the selection box over one,
    // steps one level inside that group and selects the member's ancestor at the new level. On an
    // addressable instance it leaves the selection as the first click of the pair left it and asks
    // the instance to edit.
    protected override void OnClick(PointerRelease release)
    {
        if (TrySelectNextInHitStack())
        {
            return;
        }

        if (Press.PressCount > 1)
        {
            if (Press.EntityId is { } entityId)
            {
                EnterOrEdit(entityId);
            }
            else if (ParticipantUnderPress() is { } participantId)
            {
                TryEnterContainingGroup(participantId);
            }

            return;
        }

        if (Press.Role == HitRole.SelectionBounds)
        {
            ClickThroughSelectionBox();
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

    // The topmost entity beneath the box stands in for the pressed entity: a click collapses to
    // it, or under Shift toggles it. With nothing beneath, the selection stays.
    private void ClickThroughSelectionBox()
    {
        if (Context.HitStackOf(Press) is not [var beneath, ..])
        {
            return;
        }

        if (Press.ShiftKey)
        {
            Context.ToggleHitStackEntry(beneath);
        }
        else
        {
            Context.SelectHitStackEntry(beneath);
        }
    }

    // The selection box covers the members of a selected group, so a double-press there means the
    // topmost selected instance under the pointer.
    private Guid? ParticipantUnderPress() =>
        _participants
            .Where(participant => participant.Bounds.Contains(_pressPoint.X, _pressPoint.Y))
            .OrderByDescending(participant => participant.ZIndex)
            .Select(participant => (Guid?)participant.Id)
            .FirstOrDefault();

    private void EnterOrEdit(Guid entityId)
    {
        if (!TryEnterContainingGroup(entityId))
        {
            Context.BeginInlineEdit(entityId);
        }
    }

    // Enters the group that holds the entity one level below the current scope, then selects the
    // entity's ancestor at the new level, which the scope change has just made addressable.
    private bool TryEnterContainingGroup(Guid entityId)
    {
        var effectiveId = Context.EffectiveSelectionId(entityId);
        if (effectiveId == entityId)
        {
            return false;
        }

        Context.EnterGroup(effectiveId);
        Context.ReplaceSelection([Context.EffectiveSelectionId(entityId)], []);
        return true;
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
