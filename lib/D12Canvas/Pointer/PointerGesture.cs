using D12Canvas.Model;

namespace D12Canvas.Pointer;

internal abstract class PointerGesture
{
    protected PointerGesture(PointerPress press, IGestureContext context)
    {
        Press = press;
        Context = context;
    }

    public PointerPress Press { get; }
    public GesturePhase Phase { get; private set; } = GesturePhase.Pointing;
    protected IGestureContext Context { get; }

    public void Begin() => OnPress();

    public virtual bool HoldsPress => true;

    public bool Owns(int pointerId, int button) =>
        pointerId == Press.PointerId && button == Press.Button;

    public void Move(PointerMove move)
    {
        if (Phase == GesturePhase.Cancelled)
        {
            return;
        }

        Phase = GesturePhase.Active;
        OnMove(move);
    }

    protected virtual bool HasActivePhase => true;

    public bool ViewportMoved(PointerMove pointer)
    {
        if (Phase == GesturePhase.Cancelled || !HasActivePhase)
        {
            return false;
        }

        var promoted = Phase == GesturePhase.Pointing;
        Phase = GesturePhase.Active;
        OnViewportMoved(pointer);
        return promoted;
    }

    protected virtual void OnViewportMoved(PointerMove pointer) => OnMove(pointer);

    public void Release(PointerRelease release)
    {
        switch (Phase)
        {
            case GesturePhase.Pointing:
                OnClick(release);
                break;
            case GesturePhase.Active:
                OnRelease(release);
                break;
        }
    }

    public void MarkCancelled() => Phase = GesturePhase.Cancelled;

    protected bool TrySelectNextInHitStack()
    {
        if (
            !Press.AltKey
            || Press.CtrlKey
            || HitStack.NextBelow(
                Context.HitStackOf(Press),
                Context.SelectionSnapshot,
                Context.EffectiveSelectionId
            )
                is not { } next
        )
        {
            return false;
        }

        Context.SelectHitStackEntry(next);
        return true;
    }

    protected IReadOnlyList<Bounds> ObjectSnapCandidates(
        PointerMove move,
        IReadOnlyCollection<Guid> carried
    ) =>
        carried.Count > 0 && Context.ObjectSnapping && move.Velocity <= ObjectSnap.FastPointerSpeed
            ? Context.SnapCandidates(carried)
            : [];

    protected double ObjectSnapTolerance =>
        ScreenPixels.ObjectSnapTolerance / Context.ZoomPan.Scale;

    protected virtual void OnPress() { }

    protected abstract void OnMove(PointerMove move);

    protected abstract void OnRelease(PointerRelease release);

    protected abstract void OnClick(PointerRelease release);
}
