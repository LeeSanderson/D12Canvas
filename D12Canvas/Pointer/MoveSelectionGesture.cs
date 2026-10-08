using D12Canvas.Model;

namespace D12Canvas.Pointer;

// A primary press on an instance or on the selection box. Pressing an entity outside the
// selection selects it at once, or appends it under Shift, so the drag has something to move;
// pressing a member leaves the selection alone until release, where a click collapses to that
// member or, under Shift, toggles it out. A click on the selection box does the same with the
// topmost entity beneath it, and an Alt click selects the next entity down the hit stack from the
// one selected before the press. Every tick publishes the whole selection translated by
// the board-space distance from the press point, snapped as one rigid body: object snapping aligns
// its instances' bounding box with the shapes around it where it matches, the grid rounds the
// box's top-left on any axis it did not, the motion is straightened to one axis while Shift is
// held and left unsnapped while Ctrl is, and an active release commits exactly what was last
// published. A selected edge's floating ends take the same delta; its attached ends follow their
// components and have nothing of their own to publish.
//
// Holding Alt makes it a clone drag: the delta is published onto a copy of the selection, held in
// the preview's pending fragment, and the originals stay where they are as snap candidates. Alt is
// read on every move, so letting go of it puts the delta back on the originals. The copy is built
// on the first move that wants it, so an Alt click builds nothing, and is kept for the rest of the
// press, so toggling Alt shows the same copy each time.
internal sealed class MoveSelectionGesture : PointerGesture
{
    private readonly (double X, double Y) _pressPoint;
    private Guid? _pressedMember;
    private IReadOnlyList<ComponentInstance> _participants = [];
    private IReadOnlyList<(EdgeEnd End, FloatingEndpoint Start)> _floatingEnds = [];
    private (double X, double Y) _origin;
    private Bounds _box;
    private HashSet<Guid> _participantIds = [];
    private AxisSnap? _heldX;
    private AxisSnap? _heldY;
    private Board? _copies;
    private Board? _carriedCopies;

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
        _participantIds = _participants.Select(participant => participant.Id).ToHashSet();
        _floatingEnds = Context.SelectedEdges().SelectMany(FloatingEndsOf).ToList();
        if (Bounds.Union(_participants.Select(participant => participant.Bounds)) is { } box)
        {
            _origin = (box.X, box.Y);
            _box = box;
        }

        PublishTranslatedBy(0, 0);
    }

    // Shift holds the axis the press-anchored delta has moved least along. That axis is never
    // snapped, since rounding it would move the selection along the axis just locked. On each free
    // axis object snapping takes the axis where it matches and the grid fills it where it does not;
    // Ctrl suppresses both, and a fast pointer stands object snapping down and drops what it held.
    protected override void OnMove(PointerMove move)
    {
        var current = Context.ToBoardPoint(move.X, move.Y);
        _carriedCopies = move.AltKey ? Copies() : null;
        var deltaX = current.X - _pressPoint.X;
        var deltaY = current.Y - _pressPoint.Y;
        var xLocked = move.ShiftKey && Math.Abs(deltaY) > Math.Abs(deltaX);
        var yLocked = move.ShiftKey && !xLocked;

        var x = xLocked ? _origin.X : _origin.X + deltaX;
        var y = yLocked ? _origin.Y : _origin.Y + deltaY;
        var guides = new List<SnapGuide>();
        if (!move.CtrlKey)
        {
            var candidates = ObjectSnapCandidates(move, CarriedIds());
            var raw = _box with { X = x, Y = y };
            var tolerance = ObjectSnapTolerance;
            _heldX = xLocked
                ? null
                : ObjectSnap.ForMove(raw, candidates, SnapAxis.X, tolerance, _heldX);
            _heldY = yLocked
                ? null
                : ObjectSnap.ForMove(raw, candidates, SnapAxis.Y, tolerance, _heldY);

            var snappedX = SnapAxisCoordinate(x, xLocked, _heldX, raw, SnapAxis.X);
            var snappedY = SnapAxisCoordinate(y, yLocked, _heldY, raw, SnapAxis.Y);
            var guidesX = GuidesAlong(SnapAxis.X, _heldX, snappedX, snappedY, candidates);
            var guidesY = GuidesAlong(SnapAxis.Y, _heldY, snappedX, snappedY, candidates);

            if (_heldX is not null && guidesX.Count == 0)
            {
                _heldX = null;
                snappedX = SnapAxisCoordinate(x, xLocked, null, raw, SnapAxis.X);
                guidesY = GuidesAlong(SnapAxis.Y, _heldY, snappedX, snappedY, candidates);
            }

            if (_heldY is not null && guidesY.Count == 0)
            {
                _heldY = null;
                snappedY = SnapAxisCoordinate(y, yLocked, null, raw, SnapAxis.Y);
                guidesX = GuidesAlong(SnapAxis.X, _heldX, snappedX, snappedY, candidates);
            }

            (x, y) = (snappedX, snappedY);
            guides.AddRange(guidesX);
            guides.AddRange(guidesY);
        }
        else
        {
            _heldX = null;
            _heldY = null;
        }

        PublishTranslatedBy(x - _origin.X, y - _origin.Y);
        Context.PublishGuides(guides);
    }

    // The second pass, from where the selection now stands. An axis whose match leaves nothing to
    // draw there, which happens when the other axis's snap carried the selection out of the row an
    // equal-spacing match was found in, gives the axis back to the grid rather than correcting it
    // silently.
    private IReadOnlyList<SnapGuide> GuidesAlong(
        SnapAxis axis,
        AxisSnap? held,
        double x,
        double y,
        IReadOnlyList<Bounds> candidates
    ) =>
        held is null
            ? []
            : ObjectSnap.GuidesForMove(_box with { X = x, Y = y }, candidates, axis).ToList();

    private double SnapAxisCoordinate(
        double coordinate,
        bool locked,
        AxisSnap? held,
        Bounds raw,
        SnapAxis axis
    )
    {
        if (locked)
        {
            return coordinate;
        }

        if (held is { } snap)
        {
            return coordinate + ObjectSnap.Offset(raw, axis, snap);
        }

        return Context.GridSpacing is { } spacing
            ? GridSnap.NearestLine(coordinate, spacing)
            : coordinate;
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

    private Board? Copies() => _copies ??= Context.CopyOfSelection();

    // A clone's originals are not carried, so they stay on the board as candidates; its copies are
    // not on the board at all, so they never are.
    private IReadOnlyCollection<Guid> CarriedIds() =>
        _carriedCopies is { } copies
            ? copies.Components.Select(copy => copy.Id).ToList()
            : _participantIds;

    private void PublishTranslatedBy(double deltaX, double deltaY)
    {
        if (_carriedCopies is { } copies)
        {
            PublishCopiesTranslatedBy(copies, deltaX, deltaY);
            return;
        }

        Context.PublishPendingFragment(null);
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

    private void PublishCopiesTranslatedBy(Board copies, double deltaX, double deltaY)
    {
        Context.PublishPendingFragment(copies);
        Context.PublishPreview(
            copies.Components.ToDictionary(
                copy => copy.Id,
                copy => copy.Bounds with { X = copy.Bounds.X + deltaX, Y = copy.Bounds.Y + deltaY }
            )
        );
        Context.PublishMovedEndpoints(
            copies
                .Edges.SelectMany(FloatingEndsOf)
                .ToDictionary(
                    floating => floating.End,
                    floating => new FloatingEndpoint(
                        floating.Start.X + deltaX,
                        floating.Start.Y + deltaY
                    )
                )
        );
    }
}
