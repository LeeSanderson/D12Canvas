namespace D12Canvas.Pointer;

// One owner per press. Identity never changes once chosen; only the phase does: pointing until the
// first move the listener forwards (which means the drag threshold was crossed), active after,
// cancelled once Escape or an interruption ended its effects. Cancel itself is canvas-level and
// has no per-gesture step, so there is no method for it here.
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
