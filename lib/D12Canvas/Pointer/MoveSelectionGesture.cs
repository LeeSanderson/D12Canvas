using D12Canvas.Model;

namespace D12Canvas.Pointer;

internal sealed class MoveSelectionGesture : PointerGesture
{
    private readonly (double X, double Y) _pressPoint;
    private Guid? _pressedMember;
    private IReadOnlyList<ComponentInstance> _participants = [];
    private IReadOnlyList<(EdgeEnd End, FloatingEndpoint Start)> _floatingEnds = [];
    private Bounds _movedBox;
    private Bounds _wholeBox;
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

        var selected = Context.SelectedInstances();
        _participants = selected.Where(instance => !instance.Locked).ToList();
        _participantIds = _participants.Select(participant => participant.Id).ToHashSet();
        _floatingEnds = Context
            .SelectedEdges()
            .Where(edge => !edge.Locked)
            .SelectMany(FloatingEndsOf)
            .ToList();
        _wholeBox = Bounds.Union(selected.Select(instance => instance.Bounds)) ?? default;
        _movedBox =
            Bounds.Union(_participants.Select(participant => participant.Bounds)) ?? default;

        PublishTranslatedBy(0, 0);
    }

    private Bounds Box => _carriedCopies is null ? _movedBox : _wholeBox;

    private (double X, double Y) Origin => (Box.X, Box.Y);

    protected override void OnMove(PointerMove move)
    {
        var current = Context.ToBoardPoint(move.X, move.Y);
        _carriedCopies = move.AltKey ? Copies() : null;
        var deltaX = current.X - _pressPoint.X;
        var deltaY = current.Y - _pressPoint.Y;
        var xLocked = move.ShiftKey && Math.Abs(deltaY) > Math.Abs(deltaX);
        var yLocked = move.ShiftKey && !xLocked;

        var x = xLocked ? Origin.X : Origin.X + deltaX;
        var y = yLocked ? Origin.Y : Origin.Y + deltaY;
        var guides = new List<SnapGuide>();
        if (!move.CtrlKey)
        {
            var candidates = ObjectSnapCandidates(move, CarriedIds());
            var raw = Box with { X = x, Y = y };
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

        PublishTranslatedBy(x - Origin.X, y - Origin.Y);
        Context.PublishGuides(guides);
    }

    private IReadOnlyList<SnapGuide> GuidesAlong(
        SnapAxis axis,
        AxisSnap? held,
        double x,
        double y,
        IReadOnlyList<Bounds> candidates
    ) =>
        held is null
            ? []
            : ObjectSnap.GuidesForMove(Box with { X = x, Y = y }, candidates, axis).ToList();

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
