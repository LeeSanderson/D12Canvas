namespace D12Canvas.Pointer;

// One owner per press. Identity never changes once chosen; only the phase does: pointing until the
// first move the listener forwards (which means the drag threshold was crossed) or the first
// viewport change under the press, active after, cancelled once Escape or an interruption ended its
// effects. Cancel itself is canvas-level and has no per-gesture step, so there is no method for it
// here.
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

    // The press-time half of the gesture, run once by the canvas after it has taken the selection
    // snapshot, so anything the press changes can be put back by a cancel.
    public void Begin() => OnPress();

    // False for the one gesture that takes no capture and tracks nothing, whose press ends as soon
    // as it begins.
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

    // False for a gesture whose press ends in a click or not at all, which a viewport change
    // neither promotes nor re-runs.
    protected virtual bool HasActivePhase => true;

    // The viewport moved under the press, with the pointer still where it last was. A press still
    // pointing is promoted, since what it holds would otherwise slide away from the pointer, and
    // the gesture runs again from the pointer's last position. Returns whether this promoted it.
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

    // Release from pointing is the click outcome; release from active commits. A cancelled
    // gesture's release does nothing.
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

    protected virtual void OnPress() { }

    protected abstract void OnMove(PointerMove move);

    protected abstract void OnRelease(PointerRelease release);

    protected abstract void OnClick(PointerRelease release);
}
