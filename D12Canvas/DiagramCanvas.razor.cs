using System.Globalization;
using D12Canvas.History;
using D12Canvas.Model;
using D12Canvas.Pointer;
using D12Canvas.Registration;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace D12Canvas;

public partial class DiagramCanvas : IAsyncDisposable
{
    [Inject]
    private IJSRuntime JS { get; set; } = null!;

    [Inject]
    private IComponentRegistry Registry { get; set; } = null!;

    // Sized to absorb a fast pan: moves reach the canvas once per animation frame, so the pointer
    // can travel well past this before the next windowed re-render lands, but a much larger
    // margin just inflates mount/unmount cost for no visible benefit.
    public const double DefaultOverscan = 200;

    [Parameter]
    public Board? Board { get; set; }

    [Parameter]
    public double Overscan { get; set; } = DefaultOverscan;

    // Both default to unbounded - a host sets either independently to cap zoom in, zoom out, or
    // both.
    [Parameter]
    public double? MinZoom { get; set; }

    [Parameter]
    public double? MaxZoom { get; set; }

    // Small enough that no built-in's default size ever crosses it at a normal (near-1.0) zoom
    // level - it only bites once zooming out far enough actually shrinks an instance past
    // legibility.
    public const double DefaultLodSizeThreshold = 32;

    [Parameter]
    public double LodSizeThreshold { get; set; } = DefaultLodSizeThreshold;

    // Off by default - an editing preference, not a board or persisted setting (see
    // CONTEXT.md's Snap-to-grid term). Component-owned like an <InputBase>'s own bindable value:
    // OnToggleSnapToGridPressed below flips it directly and notifies SnapToGridChanged, so a host
    // using @bind-SnapToGrid stays in sync with the built-in Ctrl+' shortcut too.
    [Parameter]
    public bool SnapToGrid { get; set; }

    [Parameter]
    public EventCallback<bool> SnapToGridChanged { get; set; }

    // Lets a host disable the built-in Ctrl+' chord independently of the bindable SnapToGrid
    // parameter itself, in case it conflicts with the host's own keybindings.
    [Parameter]
    public bool EnableSnapToGridShortcut { get; set; } = true;

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter]
    public EventCallback<ZoomPanChangedEventArgs> OnZoomOrPanChanged { get; set; }
    public event EventHandler<ZoomPanChangedEventArgs>? ZoomOrPanChanged;

    // The property panel (a standalone chrome sibling, same wiring as Palette) subscribes to
    // this to know when to re-render. Blazor's own auto-rerender after an event handler only
    // covers DiagramCanvas's own render tree, never a sibling component's. Also raised by
    // undo/redo (see OnUndoPressed/OnRedoPressed) even though selection identity itself is
    // untouched there - either can change what the panel should be showing for the current
    // selection (a newly selected instance, or the currently selected instance's Props
    // reverting/reapplying).
    public event EventHandler? SelectionChanged;

    private void NotifySelectionChanged() => SelectionChanged?.Invoke(this, EventArgs.Empty);

    // Re-exposes CommandHistory.Changed for host/chrome code (e.g. a Save button's
    // disabled-when-clean state) - same "internal signal re-raised as a public event" shape as
    // SelectionChanged, wired via subscribe/unsubscribe the same way OnZoomPanChanged is below.
    public event EventHandler? Changed;

    private void OnHistoryChanged(object? sender, EventArgs e) =>
        Changed?.Invoke(this, EventArgs.Empty);

    // The property panel's selection surface - every top-level selected id resolved
    // to a ComponentInstance, deliberately NOT expanded through group membership (unlike
    // ExpandedSelection, which move/resize/delete use). A selected Group's own id lives in Board's
    // separate group dictionary, so Board.GetComponent returns null for it - and, since a Group has
    // no Props of its own to edit, ANY unresolvable id in the selection empties the whole result
    // rather than silently dropping just that one entry (a shift-click can mix a grouped member,
    // i.e. its group's id per EffectiveSelectionId, with a standalone instance in the
    // same selection - that must read as "nothing to edit", the same as a lone selected Group,
    // rather than collapsing to "edit the one standalone instance"). Empty whenever an edge is
    // selected instead (edges have no Props reachable via this panel). A single resolved instance
    // is the "edit exactly one instance" case; 2+ (same type or cross-type) is the bulk-edit case.
    public IReadOnlyList<ComponentInstance> SelectedComponents
    {
        get
        {
            if (Board is null || _selectedEdgeId is not null)
            {
                return Array.Empty<ComponentInstance>();
            }

            var resolved = new List<ComponentInstance>();
            foreach (var id in _selectedInstanceIds)
            {
                var instance = Board.GetComponent(id);
                if (instance is null)
                {
                    return Array.Empty<ComponentInstance>();
                }

                resolved.Add(instance);
            }

            return resolved;
        }
    }

    // A palette entry has no compile-time-typed payload it can hand across the native HTML5 drag
    // session (Blazor's DragEventArgs.DataTransfer exposes no SetData/GetData) - Palette instead
    // calls this directly (via its explicit Canvas reference) to stash which type is
    // being dragged, for HandleDrop to read back once the gesture completes.
    private string? _pendingPaletteDragKey;
    private bool _isDragOverBoard;

    // The "Connector" palette entry isn't a registered component type - Edge is
    // its own entity, not something RegisterComponent covers - so it has no
    // real registry key. This sentinel flows through the exact same BeginPaletteDrag/ClickToAdd
    // gesture plumbing every other palette entry uses; PlaceComponent below recognizes it and routes
    // to PlaceConnector instead of resolving it through Registry.
    public const string ConnectorPaletteKey = "__d12-connector__";

    public void BeginPaletteDrag(string componentTypeKey) =>
        _pendingPaletteDragKey = componentTypeKey;

    // Click-to-add's default position is the viewport center (not a fixed board-space
    // origin, which could land off-screen once the user has panned), with a small cascading
    // offset per successive click so repeated adds don't stack in a perfectly overlapping pile.
    // The counter is global (not per component type) - it only needs to keep successive placements
    // visually apart, and never resets, matching this app's momentary-gesture-only model (there's
    // no "start a new placement session" event to reset it on).
    private const double ClickToAddCascadeStep = 20;
    private int _clickToAddCascadeCount;

    // Selection is transient view state - it lives here, never on Board, and is never
    // serialized or tracked by undo/redo. Ad-hoc multi-select via marquee/shift-click.
    private readonly HashSet<Guid> _selectedInstanceIds = new();

    // An edge's own exclusive selection slot - edges don't participate in multi-select,
    // grouping, or move/resize as a unit the way component instances do, so an edge id
    // is never mixed into _selectedInstanceIds. Selecting an edge always clears any instance
    // selection, and selecting an instance (or starting a marquee) always clears this.
    private Guid? _selectedEdgeId;

    // The open selection context menu, if any - null means none is open. Its
    // anchor point is plain container-relative pixels (not board space), since the menu is canvas
    // chrome (CONTEXT.md) that must not pan/zoom with the board content.
    private sealed record ContextMenuState(double X, double Y);

    private ContextMenuState? _contextMenu;

    // Session-scoped, in-memory undo/redo - lives here, not on Board, for
    // the same reason selection does; never serialized, never survives a reload.
    private readonly CommandHistory _history = new();

    private readonly ZoomPanTracker _zoomPanTracker = new ZoomPanTracker();

    public ZoomPanTracker ZoomPanTracker => _zoomPanTracker;

    // Every press the browser-side listener forwards resolves to exactly one pointer gesture,
    // chosen here once the interop hop lands because the choice needs the selection, and that
    // gesture owns the pointer until its claiming button comes up. Its identity never changes
    // mid-press, only its phase. The selection as it stood at the press is kept so a cancel can
    // put it back. The marquee band is drawn from its own field, and every other piece of
    // in-flight geometry is in the gesture preview, read through live geometry.
    private PointerGesture? _activeGesture;
    private SelectionSnapshot? _pressSelection;
    private Bounds? _marqueeBounds;
    private Board? _previousBoard;
    private readonly GesturePreview _preview = new();

    // The participants of the live gesture that have been mounted at some point during it, each
    // with the placeholder state it was mounted with. Mounting only grows until release, so an
    // instance carried past the viewport edge stays in the user's hand, and a placeholder never
    // swaps for the full component mid-gesture or back.
    private readonly Dictionary<Guid, bool> _stickyParticipants = new();

    // The full component mounted for each instance, kept so the canvas can ask one to begin an
    // inline edit. A remount overwrites its entry, and an entry goes once its instance leaves the
    // board.
    private readonly Dictionary<Guid, DynamicComponent> _mountedComponents = new();

    // The label component mounted for each labelled edge, keyed by edge id, kept so a double-press
    // on a label can open its editor.
    private readonly Dictionary<Guid, DynamicComponent> _mountedLabels = new();

    private LiveGeometry Live => new(Board!, _preview);

    // Keyboard-driven connector attachment - the keyboard equivalent of a connector drag, built
    // entirely from Enter/arrow-key/Space focus
    // navigation rather than a continuous pointer gesture. _portFocusInstanceId/_portFocusEndpoint
    // is the port currently being picked on whichever instance Enter was most recently pressed on
    // (entered/exited by Enter alone - see OnEnterPressed - defaulting each entry to that instance's
    // Top port; an arrow key jumps directly to one of the four standard ports, Space instead steps
    // to the next port in Board.AllPorts's own order so a custom port is reachable too - see
    // OnSpacePressed). Represented as a real PortEndpoint/CustomPortEndpoint (never a
    // FloatingEndpoint) rather than a bare PortId, so both port kinds share one representation and
    // OnEnterPressed can hand it straight to a new Edge with no reconstruction step.
    // _pendingConnectorSource is the already-armed source, set by the FIRST Enter while picking and
    // persisting across the Tab/Shift+Tab navigation a keyboard user relies on to reach the target
    // instance - native Tab is never intercepted (see DiagramCanvas.razor.js), so there is no
    // keydown hook to clear stale picking state on an ordinary Tab press; FocusEntity does that
    // instead, the one place every focus-changing navigation (Tab, click, Ctrl+Tab) already passes
    // through.
    private Guid? _portFocusInstanceId;
    private IEdgeEndpoint _portFocusEndpoint = new PortEndpoint(Guid.Empty, PortId.Top);
    private IEdgeEndpoint? _pendingConnectorSource;

    private ElementReference ContainerElement;
    private ElementReference CanvasElement;
    private DotNetObjectReference<DiagramCanvas>? _dotNetObjectRef;
    private List<IJSObjectReference> _cleanupHandles = new List<IJSObjectReference>();
    private IJSObjectReference? _jsModule;

    // Set by OnGroupPressed - the new group's own tab stop doesn't exist in the DOM until the
    // render its grouping triggers actually commits, so the focus call has to wait for
    // OnAfterRenderAsync (guaranteed to run after that render lands) rather than firing inline.
    private bool _pendingGroupFocus;

    // Set by ClickToAdd - same reasoning as _pendingGroupFocus, but keyed by id rather
    // than a flag: the newly placed instance's own index among tab stops isn't known until the
    // render that adds it to the Board actually commits (its on-screen position, and therefore its
    // reading-order slot, can depend on what else is already on the board).
    private Guid? _pendingPlacementFocusId;

    // Whichever entity currently has real DOM focus - kept in sync by FocusEntity (every native
    // Tab/Shift+Tab landing), cleared when a press focuses the canvas, and advanced without
    // selecting by OnCtrlTabPressed. Tracked separately from _selectedInstanceIds because Ctrl+Tab
    // must move focus without touching selection at all.
    private Guid? _focusedTabStopId;

    // Set immediately before OnCtrlTabPressed's own JS focus() call - consumed by the very next
    // FocusEntity invocation that call triggers (the native onfocus round-trip a real .focus() call
    // fires), so that one focus move skips the hard-select every other focus arrival performs.
    private bool _suppressFocusSelect;

    protected override void OnInitialized()
    {
        _zoomPanTracker.Changed += OnZoomPanChanged;
        _history.Changed += OnHistoryChanged;
        _dotNetObjectRef = DotNetObjectReference.Create(this);
    }

    // A host replacing the Board reference mid-press cancels the gesture without restoring the
    // selection snapshot, which names entities in a model that is gone.
    protected override void OnParametersSet()
    {
        _zoomPanTracker.SetZoomLimits(MinZoom, MaxZoom);

        if (!ReferenceEquals(Board, _previousBoard))
        {
            _previousBoard = Board;
            CancelActiveGesture(restoreSelection: false);
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _jsModule = await JS.InvokeAsync<IJSObjectReference>(
                "import",
                "./_content/D12Canvas/DiagramCanvas.razor.js"
            );

            var dimensions = await _jsModule.InvokeAsync<Dictionary<string, double>>(
                "getContainerDimensions",
                ContainerElement
            );

            _zoomPanTracker.SetContainerSize((int)dimensions["width"], (int)dimensions["height"]);

            var resizeCleanup = await _jsModule.InvokeAsync<IJSObjectReference>(
                "addResizeListener",
                ContainerElement,
                _dotNetObjectRef
            );

            var keyboardCleanup = await _jsModule.InvokeAsync<IJSObjectReference>(
                "addKeyboardListener",
                ContainerElement,
                _dotNetObjectRef
            );

            var pointerCleanup = await _jsModule.InvokeAsync<IJSObjectReference>(
                "addPointerListener",
                CanvasElement,
                ContainerElement,
                _dotNetObjectRef,
                new { classify = true }
            );

            _cleanupHandles.Add(resizeCleanup);
            _cleanupHandles.Add(keyboardCleanup);
            _cleanupHandles.Add(pointerCleanup);

            StateHasChanged();
        }

        foreach (var id in _mountedComponents.Keys.Where(id => Board?.GetComponent(id) is null))
        {
            _mountedComponents.Remove(id);
        }

        foreach (var id in _mountedLabels.Keys.Where(id => Board?.GetEdge(id)?.Label is null))
        {
            _mountedLabels.Remove(id);
        }

        if (_pendingGroupFocus)
        {
            _pendingGroupFocus = false;
            await _jsModule!.InvokeVoidAsync("focusGroupTabStop", ContainerElement);
        }

        if (_pendingPlacementFocusId is { } placedId)
        {
            _pendingPlacementFocusId = null;
            var index = FocusableTabStopIds().IndexOf(placedId);
            if (index >= 0)
            {
                await _jsModule!.InvokeVoidAsync("focusTabStopAt", ContainerElement, index);
            }
        }
    }

    [JSInvokable]
    public void OnContainerResized(double width, double height)
    {
        _zoomPanTracker.SetContainerSize((int)width, (int)height);
        StateHasChanged();
    }

    // The four pointer entry points. The listener has already classified the press and taken the
    // synchronous decisions; what arrives here is a press whose owner this canvas chooses, moves
    // that are real drags (the listener never forwards one below the threshold), and the one
    // release or interruption that ends the press.
    [JSInvokable]
    public void OnPointerPressed(PointerPress press)
    {
        if (_activeGesture is not null || PressToKind.Resolve(press) is not { } kind)
        {
            return;
        }

        var snapshot = new SelectionSnapshot(_selectedInstanceIds, _selectedEdgeId);
        var context = new CanvasGestureContext(this, snapshot);
        PointerGesture gesture = kind switch
        {
            GestureKind.Pan => new PanGesture(press, context),
            GestureKind.MarqueeSelect => new MarqueeSelectGesture(press, context),
            GestureKind.MoveSelection => new MoveSelectionGesture(press, context),
            GestureKind.ResizeSelection => new ResizeSelectionGesture(press, context),
            GestureKind.DragEdgeEnd => new DragEdgeEndGesture(press, context),
            GestureKind.SelectEdge => new SelectEdgeGesture(press, context),
            GestureKind.Native => new NativeGesture(press, context),
            _ => throw new InvalidOperationException($"No gesture is built for {kind}."),
        };
        gesture.Begin();

        if (gesture.HoldsPress)
        {
            _activeGesture = gesture;
            _pressSelection = snapshot;
            _history.Lock();
        }

        StateHasChanged();
    }

    [JSInvokable]
    public void OnPointerMoved(PointerMove move)
    {
        if (_activeGesture is null || move.PointerId != _activeGesture.Press.PointerId)
        {
            return;
        }

        _activeGesture.Move(move);
        StateHasChanged();
    }

    // History unlocks before the gesture acts on its release, so the release is the one write the
    // board accepts while the gesture still owns it, and the preview it commits is dropped after.
    [JSInvokable]
    public void OnPointerReleased(PointerRelease release)
    {
        if (_activeGesture is null || !_activeGesture.Owns(release.PointerId, release.Button))
        {
            return;
        }

        _history.Unlock();
        _activeGesture.Release(release);
        EndPress();
        StateHasChanged();
    }

    // pointercancel, a lost capture and a window blur all mean the claiming release will never
    // arrive on this page, so the gesture is cancelled and the press ends with it.
    [JSInvokable]
    public void OnPointerCancelled(string reason)
    {
        if (_activeGesture is null)
        {
            return;
        }

        CancelActiveGesture(restoreSelection: true);
        EndPress();
        StateHasChanged();
    }

    // While any gesture owns the press, in any phase, nothing writes the board except that
    // gesture's release and no keyboard command changes the selection.
    private bool PressOwnsBoard => _activeGesture is not null;

    // Cancel is three canvas-level steps and no gesture implements any of them: drop the preview,
    // restore the selection taken at press, mark the gesture cancelled. The viewport is never
    // restored. A cancelled gesture keeps owning the press until its button comes up.
    private void CancelActiveGesture(bool restoreSelection)
    {
        if (_activeGesture is null || _activeGesture.Phase == GesturePhase.Cancelled)
        {
            return;
        }

        _marqueeBounds = null;
        _preview.Clear();

        if (restoreSelection && _pressSelection is { } snapshot)
        {
            SetSelection(snapshot.InstanceIds, snapshot.EdgeId);
        }

        _activeGesture.MarkCancelled();
    }

    private void SetSelection(IEnumerable<Guid> instanceIds, Guid? edgeId)
    {
        _selectedInstanceIds.Clear();
        _selectedInstanceIds.UnionWith(instanceIds);
        _selectedEdgeId = edgeId;
        NotifySelectionChanged();
    }

    private void EndPress()
    {
        _activeGesture = null;
        _pressSelection = null;
        _marqueeBounds = null;
        _preview.Clear();
        _stickyParticipants.Clear();
        _history.Unlock();
    }

    // A participant enters the sticky set the first time its live bounds are inside the viewport
    // plus overscan, which for one already on screen is the press itself, and its placeholder
    // state is resolved then from its committed bounds and held until release.
    private void PublishPreview(IReadOnlyDictionary<Guid, Bounds> boundsOverrides)
    {
        _preview.Publish(boundsOverrides);
        if (Board is null)
        {
            return;
        }

        var mountArea = _zoomPanTracker.Viewport.ExpandedBy(Overscan);
        foreach (var (id, bounds) in boundsOverrides)
        {
            if (
                !_stickyParticipants.ContainsKey(id)
                && mountArea.Intersects(bounds)
                && Board.GetComponent(id) is { } instance
            )
            {
                _stickyParticipants[id] = IsBelowLodThreshold(instance.Bounds);
            }
        }
    }

    // Committed bounds are still the press-time values here, since nothing writes Board
    // mid-gesture, so each command's before-value comes straight off the field.
    private void CommitPreview()
    {
        if (Board is null)
        {
            return;
        }

        var commands = new List<ICommand>();
        foreach (var (id, after) in _preview.BoundsOverrides)
        {
            if (Board.GetComponent(id) is { } instance && instance.Bounds != after)
            {
                commands.Add(new ChangeBoundsCommand(instance, instance.Bounds, after));
            }
        }

        if (commands.Count > 0)
        {
            _history.Do(new CompositeCommand(commands));
        }
    }

    private void BeginInlineEdit(Guid instanceId)
    {
        if (
            Board?.GetComponent(instanceId) is not { } instance
            || IsGrouped(instanceId)
            || IsPlaceholder(instance)
            || !Registry.Resolve(instance.ComponentTypeKey).IsInlineEditable
        )
        {
            return;
        }

        if (
            _mountedComponents.TryGetValue(instanceId, out var mounted)
            && mounted.Instance is IInlineEditable editable
        )
        {
            editable.BeginEdit();
        }
    }

    private void BeginLabelEdit(Guid edgeId)
    {
        if (
            _mountedLabels.TryGetValue(edgeId, out var mounted)
            && mounted.Instance is IInlineEditable editable
        )
        {
            editable.BeginEdit();
        }
    }

    private void AddEdge(IEdgeEndpoint source, IEdgeEndpoint target)
    {
        if (Board is not null)
        {
            _history.Do(new AddEdgeCommand(Board, new Edge(source, target)));
        }
    }

    private void ChangeEdgeEndpoint(Guid edgeId, bool isSource, IEdgeEndpoint endpoint)
    {
        if (Board?.GetEdge(edgeId) is { } edge)
        {
            var before = isSource ? edge.Source : edge.Target;
            _history.Do(new ChangeEdgeEndpointCommand(edge, isSource, before, endpoint));
        }
    }

    // The one focus write per press lands here. The keyboard's anchor goes null with it, since a
    // press invalidates whichever stop the keyboard was on and any port pick in progress there.
    private void HandleCanvasFocus()
    {
        _focusedTabStopId = null;
        _portFocusInstanceId = null;
    }

    // What a gesture may reach, bound to this canvas for the duration of one press.
    private sealed class CanvasGestureContext(DiagramCanvas canvas, SelectionSnapshot snapshot)
        : IGestureContext
    {
        public Board? Board => canvas.Board;
        public ZoomPanTracker ZoomPan => canvas._zoomPanTracker;
        public SelectionSnapshot SelectionSnapshot => snapshot;

        public (double X, double Y) ToBoardPoint(double containerX, double containerY) =>
            canvas.ToBoardPoint((containerX, containerY), (0, 0));

        public Guid EffectiveSelectionId(Guid entityId) => canvas.EffectiveSelectionId(entityId);

        public bool IsSelected(Guid effectiveId) =>
            canvas._selectedInstanceIds.Contains(effectiveId);

        public IReadOnlyList<ComponentInstance> SelectedInstances() => canvas.ResolvedSelection();

        public void ReplaceSelection(IEnumerable<Guid> effectiveIds) =>
            canvas.SetSelection(effectiveIds, null);

        public void AddToSelection(Guid effectiveId) =>
            canvas.SetSelection(canvas._selectedInstanceIds.Append(effectiveId).ToList(), null);

        public void RemoveFromSelection(Guid effectiveId) =>
            canvas.SetSelection(
                canvas._selectedInstanceIds.Where(id => id != effectiveId).ToList(),
                canvas._selectedEdgeId
            );

        public void SelectEdge(Guid edgeId) => canvas.SelectEdge(edgeId);

        public void ClearSelection() => canvas.SetSelection([], null);

        public (double X, double Y) SnapToGrid(double x, double y) => canvas.SnapPoint(x, y);

        public double? GridSpacing => canvas.SnapToGrid ? canvas.DominantGridSpacing() : null;

        public void ShowMarquee(Bounds? boardBounds) => canvas._marqueeBounds = boardBounds;

        public void PublishPreview(IReadOnlyDictionary<Guid, Bounds> boundsOverrides) =>
            canvas.PublishPreview(boundsOverrides);

        public void CommitPreview() => canvas.CommitPreview();

        public void BeginInlineEdit(Guid instanceId) => canvas.BeginInlineEdit(instanceId);

        public void PublishPendingEdge(PendingEdge pendingEdge) =>
            canvas._preview.PublishPendingEdge(pendingEdge);

        public void AddEdge(IEdgeEndpoint source, IEdgeEndpoint target) =>
            canvas.AddEdge(source, target);

        public void ChangeEdgeEndpoint(Guid edgeId, bool isSource, IEdgeEndpoint endpoint) =>
            canvas.ChangeEdgeEndpoint(edgeId, isSource, endpoint);

        public void AddCustomPort(Guid instanceId, PortDef port) =>
            canvas.AddCustomPort(instanceId, port);

        public void AddEdgeLabel(Guid edgeId) => canvas.AddEdgeLabel(edgeId);

        public void BeginLabelEdit(Guid edgeId) => canvas.BeginLabelEdit(edgeId);

        public void OpenContextMenuAt(double containerX, double containerY)
        {
            if (canvas.HasContextMenuEligibleSelection)
            {
                canvas._contextMenu = new ContextMenuState(containerX, containerY);
            }
        }
    }

    [JSInvokable]
    public void OnZoomIn()
    {
        _zoomPanTracker.ZoomIn();
        StateHasChanged();
    }

    [JSInvokable]
    public void OnZoomOut()
    {
        _zoomPanTracker.ZoomOut();
        StateHasChanged();
    }

    // With snap-to-grid off a nudge unit is one screen pixel (1 / Scale board units); with it on
    // a unit is one dominant grid line. With nothing selected there's no instance to nudge, so this falls back to the arrow-key
    // pan instead (an edge-only selection - _selectedEdgeId, never mixed into _selectedInstanceIds
    // - has no Bounds either, so it takes the same pan fallback). PanStep is in screen pixels,
    // because ZoomPanTracker's pan is, so a pan press covers the same screen distance at any zoom.
    private const double NudgeStep = 1;
    private const double NudgeStepCoarse = 10;
    private const double PanStep = 50;

    // The most recent nudge's own command, kept only long enough to extend it while a burst of
    // repeat keydowns (a held arrow key) is still in progress - OnArrowKeyReleased clears it once
    // the matching keyup arrives, so the NEXT press starts a fresh undoable entry rather than
    // resuming a burst that already ended.
    private NudgeCommand? _activeNudgeCommand;

    [JSInvokable]
    public void OnArrowKeyPressed(string code, bool shiftKey)
    {
        // While picking a port (see OnEnterPressed), arrow keys jump directly to one of the
        // focused instance's four standard ports instead of nudging/panning - Top/Right/Bottom/Left
        // already read as Up/Right/Down/Left, so this takes over the same keys rather than adding
        // new ones. A custom port is reached via Space instead (OnSpacePressed).
        if (_portFocusInstanceId is { } focusedInstanceId)
        {
            if (PortIdForArrow(code) is { } portId)
            {
                _portFocusEndpoint = new PortEndpoint(focusedInstanceId, portId);
                StateHasChanged();
            }
            return;
        }

        if (_selectedInstanceIds.Count > 0)
        {
            if (!PressOwnsBoard)
            {
                NudgeSelection(code, shiftKey);
            }
        }
        else
        {
            PanFor(code);
        }
    }

    private static PortId? PortIdForArrow(string code) =>
        code switch
        {
            "ArrowUp" => PortId.Top,
            "ArrowRight" => PortId.Right,
            "ArrowDown" => PortId.Bottom,
            "ArrowLeft" => PortId.Left,
            _ => null,
        };

    [JSInvokable]
    public void OnArrowKeyReleased()
    {
        _activeNudgeCommand = null;
        _activeResizeCommand = null;
    }

    private void NudgeSelection(string code, bool shiftKey)
    {
        if (Board is null)
        {
            return;
        }

        var (dirX, dirY) = ArrowDirection(code);
        if (dirX == 0 && dirY == 0)
        {
            return;
        }

        var targets = ResolvedSelection();
        if (targets.Count == 0)
        {
            return;
        }

        var (deltaX, deltaY) = SnapToGrid
            ? GridNudgeDelta(targets, dirX, dirY, shiftKey ? NudgeStepCoarse : NudgeStep)
            : ScreenNudgeDelta(dirX, dirY, shiftKey);

        // Extending in place only when this is still the top of the undo stack (nothing else was
        // pushed or undone since) and the selection hasn't changed - either failing means this
        // press starts a new gesture rather than silently reusing a stale one.
        if (
            _activeNudgeCommand is not null
            && ReferenceEquals(_history.PeekUndo, _activeNudgeCommand)
            && _activeNudgeCommand.Matches(targets)
        )
        {
            _activeNudgeCommand.Extend(deltaX, deltaY);
        }
        else
        {
            _activeNudgeCommand = new NudgeCommand(targets, deltaX, deltaY);
            _history.Do(_activeNudgeCommand);
        }

        StateHasChanged();
    }

    private (double X, double Y) ScreenNudgeDelta(double dirX, double dirY, bool shiftKey)
    {
        var step = (shiftKey ? NudgeStepCoarse : NudgeStep) / _zoomPanTracker.Scale;
        return (dirX * step, dirY * step);
    }

    // Measured from the top-left of the selection's bounding box, the point snap-to-grid anchors,
    // and read off the current bounds so each press in a held burst steps from where the last
    // one landed.
    private (double X, double Y) GridNudgeDelta(
        IReadOnlyList<ComponentInstance> targets,
        double dirX,
        double dirY,
        double lines
    )
    {
        var anchor = Bounds.Union(targets.Select(target => target.Bounds))!.Value;
        var spacing = DominantGridSpacing();
        var deltaX = dirX == 0 ? 0 : NextGridLine(anchor.X, dirX, spacing, lines) - anchor.X;
        var deltaY = dirY == 0 ? 0 : NextGridLine(anchor.Y, dirY, spacing, lines) - anchor.Y;
        return (deltaX, deltaY);
    }

    private const double GridLineTolerance = 1e-9;

    // A coordinate already on a line counts as on it, within a tolerance that absorbs
    // floating-point drift, so it moves a whole spacing rather than landing back where it started.
    private static double NextGridLine(
        double coordinate,
        double direction,
        double spacing,
        double lines
    )
    {
        var cell = coordinate / spacing;
        var nearest = Math.Round(cell);
        var onLine = Math.Abs(cell - nearest) < GridLineTolerance;
        var first =
            direction > 0
                ? (onLine ? nearest + 1 : Math.Ceiling(cell))
                : (onLine ? nearest - 1 : Math.Floor(cell));
        return (first + direction * (lines - 1)) * spacing;
    }

    private static (double X, double Y) ArrowDirection(string code) =>
        code switch
        {
            "ArrowLeft" => (-1, 0),
            "ArrowRight" => (1, 0),
            "ArrowUp" => (0, -1),
            "ArrowDown" => (0, 1),
            _ => (0, 0),
        };

    // Pans the opposite way from a nudge's own direction vector - an arrow key "moves the
    // viewport" in the pressed direction, which is the same as translating its content the
    // other way.
    private void PanFor(string code)
    {
        var (dirX, dirY) = ArrowDirection(code);
        _zoomPanTracker.Pan(-dirX * PanStep, -dirY * PanStep);
        StateHasChanged();
    }

    // Zoom-relative resize step - always 1 screen pixel regardless of zoom, same as
    // NudgeStep. Unlike nudge, Shift is fully repurposed as the anchor-flip modifier (see
    // ResizeDirectionFor) rather than a coarser-step modifier, so there is no separate coarse value.
    private const double ResizeStep = 1;

    // The most recent Alt+Arrow resize's own command, kept only long enough to extend it while a
    // burst of repeat keydowns (a held key) is still in progress - cleared by OnArrowKeyReleased
    // exactly like _activeNudgeCommand, so the next press starts a fresh undoable entry.
    private ResizeStepCommand? _activeResizeCommand;

    // Alt+Arrow resizes the single selected instance instead of nudging - single-instance
    // only: an ad-hoc multi-selection or a selected Group (whose id never resolves through
    // Board.GetComponent) has no keyboard resize, that stays mouse-only, so both cases fall
    // through as a no-op here rather than falling back to any other behaviour.
    [JSInvokable]
    public void OnAltArrowKeyPressed(string code, bool shiftKey)
    {
        // Also a no-op while picking a port (see OnEnterPressed) - Alt+Arrow must not resize
        // whichever instance is currently being navigated for its connector attachment mid-gesture.
        if (
            Board is null
            || PressOwnsBoard
            || _portFocusInstanceId is not null
            || _selectedInstanceIds.Count != 1
        )
        {
            return;
        }

        var instance = Board.GetComponent(_selectedInstanceIds.Single());
        var direction = ResizeDirectionFor(code, shiftKey);
        if (instance is null || direction is null)
        {
            return;
        }

        var (deltaX, deltaY) = ResizeStepDeltaFor(code, _zoomPanTracker.Scale);

        // Same burst-coalescing shape as NudgeSelection, kept separate rather than shared - a
        // resize's Match also has to agree on direction (a Shift toggle mid-burst flips the anchor,
        // so it must start a fresh gesture rather than resuming one anchored to the wrong edge),
        // where a nudge's Match only ever cares about the target set.
        if (
            _activeResizeCommand is not null
            && ReferenceEquals(_history.PeekUndo, _activeResizeCommand)
            && _activeResizeCommand.Matches(instance, direction.Value)
        )
        {
            _activeResizeCommand.Extend(deltaX, deltaY);
        }
        else
        {
            _activeResizeCommand = new ResizeStepCommand(instance, direction.Value, deltaX, deltaY);
            _history.Do(_activeResizeCommand);
        }

        StateHasChanged();
    }

    // Per axis, the edge matching the arrow's direction moves outward (growing) with the opposite
    // edge anchored - Shift flips that: the edge matching the arrow's direction becomes the anchor
    // and the opposite edge moves toward it (shrinking) instead. Null for any code that isn't an
    // arrow key.
    private static ResizeDirection? ResizeDirectionFor(string code, bool shiftKey) =>
        code switch
        {
            "ArrowRight" => shiftKey ? ResizeDirection.Left : ResizeDirection.Right,
            "ArrowLeft" => shiftKey ? ResizeDirection.Right : ResizeDirection.Left,
            "ArrowDown" => shiftKey ? ResizeDirection.Top : ResizeDirection.Bottom,
            "ArrowUp" => shiftKey ? ResizeDirection.Bottom : ResizeDirection.Top,
            _ => null,
        };

    // The delta handed to ResizeMath.Apply - its sign is fixed per arrow key regardless of Shift
    // (Shift only changes which direction/anchor that delta is applied against, above).
    private static (double X, double Y) ResizeStepDeltaFor(string code, double scale)
    {
        var step = ResizeStep / scale;
        return code switch
        {
            "ArrowRight" => (step, 0),
            "ArrowLeft" => (-step, 0),
            "ArrowDown" => (0, step),
            "ArrowUp" => (0, -step),
            _ => (0, 0),
        };
    }

    // Shared by NudgeSelection and RestackSelection - resolves ExpandedSelection's ids to live
    // ComponentInstances, dropping any that no longer resolve.
    private List<ComponentInstance> ResolvedSelection() =>
        Board is null
            ? new List<ComponentInstance>()
            : ExpandedSelection()
                .Select(id => Board.GetComponent(id))
                .Where(instance => instance is not null)
                .Cast<ComponentInstance>()
                .ToList();

    // Escape's first rung is the pointer gesture that owns the press: cancel it and stop, so one
    // Escape never throws away more than it meant to and a second Escape mid-press does nothing,
    // because the cancelled gesture still owns the pointer until its button comes up. Below that
    // rung it clears the selection, and cancels a half-built KEYBOARD connection in one press,
    // whether it's still mid-pick (_portFocusInstanceId) or already has an armed source waiting
    // for a target (_pendingConnectorSource).
    [JSInvokable]
    public void OnEscapePressed()
    {
        if (_activeGesture is not null)
        {
            CancelActiveGesture(restoreSelection: true);
            StateHasChanged();
            return;
        }

        _portFocusInstanceId = null;
        _pendingConnectorSource = null;

        _selectedInstanceIds.Clear();
        _selectedEdgeId = null;
        _contextMenu = null;
        NotifySelectionChanged();
        StateHasChanged();
    }

    // Enter enters/advances the keyboard connector-attachment gesture. Not currently picking
    // a port: enters port-focus mode on whichever instance currently has keyboard focus, defaulting
    // the pick to its Top port - a no-op for anything that isn't a real ComponentInstance (nothing
    // focused yet, or focus is on a Group's own tab stop; groups have no ports). Already picking:
    // the FIRST Enter arms the currently-highlighted port as this connection's source (mirroring
    // a connector drag's press) and exits port-focus mode so Tab/Shift+Tab can reach the target
    // instance; a SECOND Enter (reached once a source is already armed) instead completes the connection
    // exactly like a connector drag dropped on a port - including its same "landing back on
    // the exact port the drag started from creates no edge" rule. Only ever reached with the DOM
    // focus actually on a `.component-container` (see the target-scoped guard in
    // DiagramCanvas.razor.js), so it never fires while a Palette button's own native
    // Enter-to-activate is what the user meant.
    [JSInvokable]
    public void OnEnterPressed()
    {
        if (Board is null || PressOwnsBoard)
        {
            return;
        }

        if (_portFocusInstanceId is not { } focusedInstanceId)
        {
            if (_focusedTabStopId is not { } id || Board.GetComponent(id) is null)
            {
                return;
            }

            _portFocusInstanceId = id;
            _portFocusEndpoint = new PortEndpoint(id, PortId.Top);
            StateHasChanged();
            return;
        }

        var chosenEndpoint = _portFocusEndpoint;
        _portFocusInstanceId = null;

        if (_pendingConnectorSource is not { } sourceEndpoint)
        {
            _pendingConnectorSource = chosenEndpoint;
            StateHasChanged();
            return;
        }

        // The source instance may have been deleted in the (arbitrarily long) span between
        // arming it and confirming a target - a keyboard gesture, unlike a continuous mouse drag,
        // spans multiple discrete key presses with other commands possible in between.
        if (
            EndpointComponentId(sourceEndpoint) is { } sourceComponentId
            && Board.GetComponent(sourceComponentId) is not null
            && !chosenEndpoint.Equals(sourceEndpoint)
        )
        {
            _history.Do(new AddEdgeCommand(Board, new Edge(sourceEndpoint, chosenEndpoint)));
        }

        _pendingConnectorSource = null;
        StateHasChanged();
    }

    // Shared by OnEnterPressed (the armed source's own instance may since have been deleted)
    // and CyclePortFocus (Board.AllPorts needs a live ComponentInstance) - a PortEndpoint/
    // CustomPortEndpoint's ComponentId, pulled out via the same pattern-match idiom
    // Board.ResolveEndpoint already uses. Never a FloatingEndpoint - _portFocusEndpoint and
    // _pendingConnectorSource are never assigned one.
    private static Guid? EndpointComponentId(IEdgeEndpoint endpoint) =>
        endpoint switch
        {
            PortEndpoint port => port.ComponentId,
            CustomPortEndpoint custom => custom.ComponentId,
            _ => null,
        };

    // The port currently highlighted for a given instance - either it's mid-pick
    // (_portFocusInstanceId) or it's the already-armed connector source waiting for a target to be
    // picked on some other instance (_pendingConnectorSource), which stays highlighted across the
    // Tab navigation between the two so a keyboard user can still see where the connection started.
    // Null (no highlight) the rest of the time.
    private IEdgeEndpoint? FocusedPortEndpointFor(Guid instanceId)
    {
        if (_portFocusInstanceId == instanceId)
        {
            return _portFocusEndpoint;
        }

        return _pendingConnectorSource is { } source && EndpointComponentId(source) == instanceId
            ? source
            : null;
    }

    // Fed to ComponentContainer's own FocusedPortId/FocusedCustomPortId parameters - split from
    // FocusedPortEndpointFor's single IEdgeEndpoint? since a standard and a custom port render as
    // two distinct markup shapes (four fixed border-center divs vs. one div per CustomPorts entry).
    private PortId? FocusedStandardPortIdFor(Guid instanceId) =>
        FocusedPortEndpointFor(instanceId) is PortEndpoint port ? port.PortId : null;

    private Guid? FocusedCustomPortIdFor(Guid instanceId) =>
        FocusedPortEndpointFor(instanceId) is CustomPortEndpoint custom ? custom.PortId : null;

    // Ctrl+Tab moves DOM focus to the next entity in reading order without selecting it - a
    // one-off suspension of focus-follows-selection for this chord only, so Space (see
    // OnSpacePressed below) has a target to toggle that's independent of the current selection.
    // Starts over from the first tab stop whenever _focusedTabStopId is null or no longer among
    // the current stops (nothing focused yet, or the previously-focused entity was deleted or
    // scrolled out of the windowed-mounting viewport) - IndexOf's -1 plus one wraps to 0 for free.
    // A no-op when the only stop available is the one already focused (nowhere else to move to) -
    // calling .focus() on an already-focused element fires no new onfocus in a real browser, which
    // would otherwise leave _suppressFocusSelect set and silently swallow the hard-select an
    // unrelated, later focus arrival should have performed.
    [JSInvokable]
    public async Task OnCtrlTabPressed()
    {
        var stops = FocusableTabStopIds();
        if (stops.Count == 0)
        {
            return;
        }

        var currentIndex = _focusedTabStopId is { } focused ? stops.IndexOf(focused) : -1;
        var nextIndex = (currentIndex + 1) % stops.Count;
        var nextId = stops[nextIndex];

        if (nextId == _focusedTabStopId)
        {
            return;
        }

        _focusedTabStopId = nextId;
        _suppressFocusSelect = true;
        await _jsModule!.InvokeVoidAsync("focusTabStopAt", ContainerElement, nextIndex);
    }

    // Space toggles the currently-focused entity's membership in the ad-hoc selection - the
    // keyboard equivalent of a shift-click, reusing SelectComponent's own toggle branch so the
    // resulting multi-selection is indistinguishable from a pointer-built one. A no-op once nothing
    // is focused, or the focused id's own tab stop is gone (deleted, or newly grouped into a member
    // with no tab stop of its own).
    // While picking a port (see OnEnterPressed), Space means something else entirely - it
    // steps to the next port in Board.AllPorts's own order instead, the only way to reach a custom
    // port (arrow keys only ever jump to one of the four standard ones).
    [JSInvokable]
    public void OnSpacePressed()
    {
        if (PressOwnsBoard)
        {
            return;
        }

        if (_portFocusInstanceId is { } focusedInstanceId)
        {
            CyclePortFocus(focusedInstanceId);
            return;
        }

        if (_focusedTabStopId is not { } id || !FocusableTabStopIds().Contains(id))
        {
            return;
        }

        SelectComponent(id, addToSelection: true);
        StateHasChanged();
    }

    // Advances _portFocusEndpoint to the next port in Board.AllPorts's own order (every standard
    // port, then every custom one), wrapping back to the first - a no-op if the instance has
    // meanwhile been deleted. IndexOf relies on PortEndpoint/CustomPortEndpoint's record structural
    // equality (via IEdgeEndpoint, same as Board.FindEdgeAttachedTo's own edge.Source.Equals(...)
    // check) to find the currently-picked port's position in that list.
    private void CyclePortFocus(Guid instanceId)
    {
        if (Board?.GetComponent(instanceId) is not { } instance)
        {
            return;
        }

        var ports = Board.AllPorts(instance).Select(p => p.Endpoint).ToList();
        var currentIndex = ports.IndexOf(_portFocusEndpoint);
        _portFocusEndpoint = ports[(currentIndex + 1) % ports.Count];
        StateHasChanged();
    }

    // Delete/Backspace removes every currently selected instance from Board and clears
    // the selection - single and multi-selection are the same code path here, since (unlike
    // move/resize) deletion has no "move as one unit" delta to apply, just N independent removals.
    // Reads through ExpandedSelection so a selected Group's members are what actually get
    // deleted; the group membership edits those removals force (a group dissolving at one
    // member, disappearing at none) ride in the same CompositeCommand, so one undo restores
    // every deleted instance and every group exactly as they were.
    // A selected edge takes a separate branch - it's never mixed into
    // _selectedInstanceIds (see _selectedEdgeId), and there's no multi-select or "as one unit"
    // delta to apply, just the one RemoveEdgeCommand.
    [JSInvokable]
    public void OnDeletePressed()
    {
        if (PressOwnsBoard)
        {
            return;
        }

        if (Board is not null)
        {
            if (_selectedEdgeId is { } edgeId)
            {
                var edge = Board.GetEdge(edgeId);
                if (edge is not null)
                {
                    _history.Do(new RemoveEdgeCommand(Board, edge));
                }
            }
            else
            {
                var commands = InstanceRemoval.Compose(Board, ExpandedSelection());
                if (commands.Count > 0)
                {
                    _history.Do(new CompositeCommand(commands));
                }
            }
        }

        _selectedInstanceIds.Clear();
        _selectedEdgeId = null;
        NotifySelectionChanged();
        StateHasChanged();
    }

    // Ctrl+G promotes the current 2+ top-level selection into a persistent Group
    // entity - the group becomes the new selection. A selection entry that is already a Group's
    // own id (from a prior grouping) is carried over by reference rather than flattened to its
    // members, so grouping a selection that already contains a group nests it.
    // Unlike a marquee or a shift-click (which can leave a multi-selection with no single element
    // to hand real DOM focus to), grouping always resolves to exactly one new focusable target -
    // the group's own tab stop - so this moves focus there too, once it exists in the DOM (see
    // _pendingGroupFocus/OnAfterRenderAsync).
    [JSInvokable]
    public void OnGroupPressed()
    {
        if (Board is null || PressOwnsBoard || !CanGroupSelection)
        {
            return;
        }

        var group = new Group(_selectedInstanceIds.ToList());
        _history.Do(new GroupCommand(Board, group));

        _selectedInstanceIds.Clear();
        _selectedInstanceIds.Add(group.Id);
        _pendingGroupFocus = true;
        NotifySelectionChanged();
        StateHasChanged();
    }

    // Ctrl+Shift+G dissolves every currently-selected Group back into its
    // immediate members, which become independently selectable again - a member that is itself a
    // nested Group stays grouped (only the outer group is dissolved). Non-group entries already in
    // the selection are left untouched.
    [JSInvokable]
    public void OnUngroupPressed()
    {
        if (Board is null || PressOwnsBoard || !CanUngroupSelection)
        {
            return;
        }

        var groups = _selectedInstanceIds
            .Select(id => Board.GetGroup(id))
            .Where(group => group is not null)
            .Cast<Group>()
            .ToList();

        var commands = groups.Select(group => (ICommand)new UngroupCommand(Board, group)).ToList();
        _history.Do(new CompositeCommand(commands));

        foreach (var group in groups)
        {
            _selectedInstanceIds.Remove(group.Id);
            foreach (var memberId in group.MemberIds)
            {
                _selectedInstanceIds.Add(memberId);
            }
        }

        NotifySelectionChanged();
        StateHasChanged();
    }

    // Ctrl+Z / Ctrl+Shift+Z. Selection is untouched either way - it's not tracked
    // by History. NotifySelectionChanged still fires: an undone/redone MutateEntityCommand
    // can change the currently-selected instance's Props out from under the property panel.
    [JSInvokable]
    public void OnUndoPressed()
    {
        if (PressOwnsBoard)
        {
            return;
        }

        _history.Undo();
        NotifySelectionChanged();
        StateHasChanged();
    }

    [JSInvokable]
    public void OnRedoPressed()
    {
        if (PressOwnsBoard)
        {
            return;
        }

        _history.Redo();
        NotifySelectionChanged();
        StateHasChanged();
    }

    // Ctrl+]/Ctrl+Shift+] and Ctrl+[/Ctrl+Shift+[. Layering reads through
    // ExpandedSelection, so a selected Group's members are reordered independently,
    // same as any other multi-selection - a Group's own "bulk-write, preserve relative member
    // order" layering behaviour is handled elsewhere, not here.
    [JSInvokable]
    public void OnBringToFrontPressed() => RestackSelection(toFront: true);

    [JSInvokable]
    public void OnSendToBackPressed() => RestackSelection(toFront: false);

    [JSInvokable]
    public void OnBringForwardPressed() =>
        ApplyZIndexChange(instance => Board!.ZIndexAbove(instance.ZIndex));

    [JSInvokable]
    public void OnSendBackwardPressed() =>
        ApplyZIndexChange(instance => Board!.ZIndexBelow(instance.ZIndex));

    // The Ctrl+' keydown lands here. A host that disables the chord disables only the chord, so
    // OnToggleSnapToGridPressed stays ungated for any other caller.
    [JSInvokable]
    public void OnSnapToGridChordPressed()
    {
        if (EnableSnapToGridShortcut)
        {
            OnToggleSnapToGridPressed();
        }
    }

    public void OnToggleSnapToGridPressed()
    {
        SnapToGrid = !SnapToGrid;
        SnapToGridChanged.InvokeAsync(SnapToGrid);
        StateHasChanged();
    }

    // Bring to Front / Send to Back move the whole selection above/below every other entity in
    // one gesture - unlike Forward/Backward, every selected instance needs to land on its own
    // distinct value (not all tied at the same Board.NextZIndex()/PreviousZIndex()), so their
    // relative order to each other survives the move: consecutive values starting from the
    // board's current extreme, assigned in ascending original-ZIndex order.
    private void RestackSelection(bool toFront)
    {
        if (Board is null || PressOwnsBoard)
        {
            return;
        }

        var selected = ResolvedSelection().OrderBy(instance => instance.ZIndex).ToList();

        if (selected.Count == 0)
        {
            return;
        }

        var start = toFront ? Board.NextZIndex() : Board.PreviousZIndex() - selected.Count + 1;
        var commands = new List<ICommand>();
        for (var i = 0; i < selected.Count; i++)
        {
            var instance = selected[i];
            var newZIndex = start + i;
            if (newZIndex == instance.ZIndex)
            {
                continue;
            }

            commands.Add(new ChangeZIndexCommand(instance, instance.ZIndex, newZIndex));
        }

        if (commands.Count == 0)
        {
            return;
        }

        _history.Do(new CompositeCommand(commands));
        StateHasChanged();
    }

    // Shared by Forward/Backward: resolves the current selection to live ComponentInstances,
    // computes each one's new ZIndex via the supplied rule - evaluated against the board's state
    // before any of this gesture's own writes, so a multi-selection's members never see each
    // other's in-progress changes - skips any that wouldn't actually change, and commits the rest
    // as one undoable gesture: one ChangeZIndexCommand per changed instance, wrapped in
    // a CompositeCommand, mirroring OnDeletePressed/OnUngroupPressed's own "build a list, wrap
    // once" shape.
    private void ApplyZIndexChange(Func<ComponentInstance, int?> computeNewZIndex)
    {
        if (Board is null || PressOwnsBoard)
        {
            return;
        }

        var commands = new List<ICommand>();
        foreach (var id in ExpandedSelection())
        {
            var instance = Board.GetComponent(id);
            if (instance is null)
            {
                continue;
            }

            var newZIndex = computeNewZIndex(instance);
            if (newZIndex is null || newZIndex == instance.ZIndex)
            {
                continue;
            }

            commands.Add(new ChangeZIndexCommand(instance, instance.ZIndex, newZIndex.Value));
        }

        if (commands.Count == 0)
        {
            return;
        }

        _history.Do(new CompositeCommand(commands));
        StateHasChanged();
    }

    // Right-click on a selection opens the menu; right-click on empty canvas
    // (nothing selected) is a no-op here, leaving the @oncontextmenu:preventDefault binding false
    // for that render so the browser's own default menu shows instead.
    private bool HasContextMenuEligibleSelection =>
        _selectedInstanceIds.Count > 0 || _selectedEdgeId is not null;

    // Same eligibility OnGroupPressed itself already guards on (2+ top-level entries) - kept as its
    // own property so the menu's own "should Group show" question reads independently of invoking it.
    private bool CanGroupSelection => _selectedInstanceIds.Count >= 2;

    // Same eligibility OnUngroupPressed itself already computes - true whenever at least one
    // top-level entry resolves to a persisted Group.
    private bool CanUngroupSelection =>
        _selectedInstanceIds.Any(id => Board?.GetGroup(id) is not null);

    private void CloseContextMenu()
    {
        _contextMenu = null;
        StateHasChanged();
    }

    // Shared by every menu item's own click callback (see the markup) - closes the menu first so a
    // command that itself calls StateHasChanged (every OnXPressed does) never re-renders with a
    // stale menu still open.
    private void InvokeFromContextMenu(Action action)
    {
        _contextMenu = null;
        action();
    }

    // A top-level entry in _selectedInstanceIds can be either a component instance id
    // or a Group id (grouping/clicking a member collapses selection onto the Group).
    // This recursively flattens every entry down to the underlying component instance ids, so the
    // rest of DiagramCanvas's existing ad-hoc multi-select machinery (bounds/move/resize/delete)
    // handles a selected Group exactly like any other 2+ multi-selection, with no separate code
    // path needed.
    private HashSet<Guid> ExpandedSelection()
    {
        var result = new HashSet<Guid>();
        foreach (var id in _selectedInstanceIds)
        {
            ExpandInto(id, result);
        }
        return result;
    }

    private void ExpandInto(Guid id, HashSet<Guid> result)
    {
        var group = Board?.GetGroup(id);
        if (group is null)
        {
            result.Add(id);
            return;
        }

        foreach (var memberId in group.MemberIds)
        {
            ExpandInto(memberId, result);
        }
    }

    private bool IsSelected(Guid instanceId) => ExpandedSelection().Contains(instanceId);

    // A top-level Group's own selection state - unlike IsSelected, this must NOT read through
    // ExpandedSelection (which flattens a selected group down to its member ids, so the group's
    // own id is never "contained" in it). The raw, unexpanded selection set is exactly the group's
    // own aria-selected state.
    private bool IsGroupSelected(Guid groupId) => _selectedInstanceIds.Contains(groupId);

    // Distinguishes "selected" from "selected as part of a group of 2+" - only the
    // latter suppresses a ComponentContainer's own resize handles in favour of the shared overlay's.
    // Reads the expanded (flattened) selection, so a single selected Group of 2+
    // members counts the same as an ad-hoc 2+ multi-selection.
    private bool IsMultiSelected(Guid instanceId)
    {
        var expanded = ExpandedSelection();
        return expanded.Count > 1 && expanded.Contains(instanceId);
    }

    // The shared bounding-box overlay (and its resize handles) must show for a
    // selected Group of 2+ members too, not only an ad-hoc multi-selection.
    private bool HasMultiMemberSelection => ExpandedSelection().Count > 1;

    // An entity id's outermost containing group id, if it has one, else the id itself -
    // shared by SelectComponent (a click) and the pointer gestures through their context, so
    // whichever gesture picks an entity up, selection converges onto its group the same way.
    private Guid EffectiveSelectionId(Guid id) => Board?.FindContainingGroup(id)?.Id ?? id;

    // A shift-click toggles the clicked instance's membership without disturbing the
    // rest of the selection; a plain click always collapses the selection down to just this one.
    // Clicking any member of a Group selects the whole group instead of just that one
    // instance - selection and group membership converge.
    // Selecting a component always clears any edge selection - the two slots are
    // mutually exclusive.
    private void SelectComponent(Guid instanceId, bool addToSelection)
    {
        var effectiveId = EffectiveSelectionId(instanceId);
        _selectedEdgeId = null;

        if (addToSelection)
        {
            if (!_selectedInstanceIds.Remove(effectiveId))
            {
                _selectedInstanceIds.Add(effectiveId);
            }
            NotifySelectionChanged();
            return;
        }

        _selectedInstanceIds.Clear();
        _selectedInstanceIds.Add(effectiveId);
        NotifySelectionChanged();
    }

    // Generic commit point for a built-in's own inline WYSIWYG text edit (or any future
    // opaque Props edit) - Sticky Note and Text call this from their own editor on blur, via the
    // ParentCanvas cascading parameter every built-in already has access to. MutateEntityCommand
    // treats Props as opaque, so this works without DiagramCanvas knowing any TProps
    // shape. The caller is trusted to have already skipped a no-op (unchanged) edit.
    // Falls back to Board.FindEdgeLabel when the id isn't an ordinary Board.Components
    // entry - an edge's Label is the same kind of editable built-in (Text, by default) but lives
    // only on its owning Edge, so its own inline edit reaches this exact commit point
    // via the same cascaded InstanceId, just resolved through a different lookup.
    public void CommitPropsChange(Guid instanceId, object before, object after)
    {
        var instance = ResolvePropsEntity(instanceId);
        if (instance is null)
        {
            return;
        }

        var (committedBefore, unresolvedAfter) = UnresolvedForCommit(instance, before, after);
        _history.Do(new MutateEntityCommand(instance, committedBefore, unresolvedAfter));
        StateHasChanged();
    }

    // The multi-instance counterpart to CommitPropsChange - a same-type or
    // cross-type bulk property edit resolves to one MutateEntityCommand per instance that actually
    // changed, wrapped in a single CompositeCommand so the whole gesture undoes/redoes
    // as one atomic entry regardless of how many instances it touched. The caller is trusted to
    // have already skipped any instance whose value wouldn't actually change.
    public void CommitPropsChangeBatch(
        IReadOnlyList<(Guid InstanceId, object Before, object After)> changes
    )
    {
        var commands = new List<ICommand>();
        foreach (var (instanceId, before, after) in changes)
        {
            var instance = ResolvePropsEntity(instanceId);
            if (instance is not null)
            {
                var (committedBefore, unresolvedAfter) = UnresolvedForCommit(
                    instance,
                    before,
                    after
                );
                commands.Add(new MutateEntityCommand(instance, committedBefore, unresolvedAfter));
            }
        }

        if (commands.Count == 0)
        {
            return;
        }

        _history.Do(new CompositeCommand(commands));
        StateHasChanged();
    }

    // Shared by CommitPropsChange and CommitPropsChangeBatch - an id is either an
    // ordinary Board.Components entry or an edge's Label, which lives only on its
    // owning Edge rather than in Board's own component dictionary.
    private ComponentInstance? ResolvePropsEntity(Guid id) =>
        Board?.GetComponent(id) ?? Board?.FindEdgeLabel(id);

    // The commit point for a routing-style/arrowhead change on a specific edge - the
    // Edge counterpart to CommitPropsChange. No panel UI calls this yet, but the command/undo
    // plumbing is independent of any UI and is exercised directly.
    public void CommitEdgeStyleChange(Guid edgeId, EdgeStyle before, EdgeStyle after)
    {
        var edge = Board?.GetEdge(edgeId);
        if (edge is null)
        {
            return;
        }

        _history.Do(new ChangeEdgeStyleCommand(edge, before, after));
        StateHasChanged();
    }

    // The commit point for a double-press on a port strip adding a custom port - routed
    // through AddCustomPortCommand so undo removes exactly the port that was added.
    private void AddCustomPort(Guid instanceId, PortDef port)
    {
        var instance = Board?.GetComponent(instanceId);
        if (instance is null)
        {
            return;
        }

        _history.Do(new AddCustomPortCommand(instance, port));
        StateHasChanged();
    }

    // The component type a brand-new edge label defaults to - a plain Text
    // instance, edited in place exactly like any board Text/Sticky Note. Any host using
    // D12Canvas's built-ins (BuiltInComponents.RegisterAll) always has this key registered.
    private const string DefaultEdgeLabelComponentTypeKey = "text";

    // An end user double-presses an edge's line to add a label - a no-op if it already
    // has one (a double-press elsewhere on the line never clobbers an existing label; editing it
    // further goes through a double-press on the label itself, not this) or if the edge's own line
    // can't currently be resolved (a dangling endpoint). The new label is a default (empty) Text
    // instance centered on the edge's current midpoint - Bounds.X/Y are never read again afterwards
    // (see EdgeLabelStyle), only Width/Height matter, the same "position is always live-derived,
    // never persisted" trick ports and floating endpoints already rely on.
    private void AddEdgeLabel(Guid edgeId)
    {
        var edge = Board?.GetEdge(edgeId);
        if (edge is null || edge.Label is not null)
        {
            return;
        }

        var line = EdgeLine(edge);
        if (line is null)
        {
            return;
        }

        var (midX, midY) = Midpoint(line.Value.From, line.Value.To);
        var label = NewCenteredInstance(DefaultEdgeLabelComponentTypeKey, midX, midY);

        _history.Do(new ChangeEdgeLabelCommand(edge, before: null, after: label));
        StateHasChanged();
    }

    private static (double X, double Y) Midpoint(
        (double X, double Y) from,
        (double X, double Y) to
    ) => ((from.X + to.X) / 2, (from.Y + to.Y) / 2);

    // An edge label's own rendered box - null when the edge has no label, or (like
    // EdgeLine) when either endpoint can't currently be resolved. Recomputed every render from the
    // edge's CURRENT endpoints rather than anything stored on the label itself, so it rides along
    // for free as either endpoint moves or resizes - the label's own Bounds.X/Y are ignored for
    // positioning, only Width/Height are read. The anchor is the straight-line midpoint between the
    // two endpoints regardless of the edge's own RoutingStyle - a reasonable approximation for
    // Orthogonal/Curved too, not full on-path placement.
    // While a connector drag carries one of this edge's ends, the label follows the pending line
    // rather than the edge's committed endpoints, so it doesn't detach and freeze mid-drag.
    private string? EdgeLabelStyle(Edge edge)
    {
        if (edge.Label is null)
        {
            return null;
        }

        var line = IsCarried(edge.Id) ? PendingEdgeLine() : EdgeLine(edge);
        if (line is null)
        {
            return null;
        }

        var (midX, midY) = Midpoint(line.Value.From, line.Value.To);
        var (width, height) = (edge.Label.Bounds.Width, edge.Label.Bounds.Height);

        return $"left: {midX - width / 2}px; top: {midY - height / 2}px; width: {width}px; height: {height}px;";
    }

    // The registered TComponent's props parameter is a fixed contract:
    // [Parameter] public TProps Props { get; set; }
    private const string PropsParameterName = "Props";

    private readonly AssetReferenceResolver _assetReferenceResolver = new();

    // The props object a component actually binds, with any asset reference already swapped for
    // its data: URI. The same object goes to the ComponentContainer's Props parameter, so an
    // asset arriving after the first render changes what the container compares and it re-renders
    // its child; an unchanged, already-resolved props object is the same cached copy both times.
    private object BoundProps(ComponentInstance instance, ComponentRegistration registration) =>
        _assetReferenceResolver.Resolve(instance, registration, Board!);

    // The inverse at the commit point: a component edits the props it was bound with, so a
    // declared reference comes back as a data URI unless it is put back here.
    private (object Before, object After) UnresolvedForCommit(
        ComponentInstance instance,
        object before,
        object after
    ) =>
        _assetReferenceResolver.Unresolve(
            instance,
            Registry.Resolve(instance.ComponentTypeKey),
            Board!,
            before,
            after
        );

    private static IDictionary<string, object> ComponentParameters(object boundProps) =>
        new Dictionary<string, object> { [PropsParameterName] = boundProps };

    // Recomputed whenever DiagramCanvas re-renders, which pointer moves drive at most once per
    // animation frame - never a per-frame timer - so the mounted window follows the same cadence.
    // Windowing reads committed bounds, so a non-participant never mounts or unmounts because of
    // a gesture, and the live gesture's sticky participants are added on top.
    private IReadOnlyCollection<ComponentInstance> VisibleComponents
    {
        get
        {
            if (Board is null)
            {
                return Array.Empty<ComponentInstance>();
            }

            var visible = Board.GetVisible(_zoomPanTracker.Viewport, Overscan);
            if (_stickyParticipants.Count == 0)
            {
                return visible;
            }

            var visibleIds = visible.Select(instance => instance.Id).ToHashSet();
            return visible
                .Concat(
                    _stickyParticipants
                        .Keys.Where(id => !visibleIds.Contains(id))
                        .Select(Board.GetComponent)
                        .OfType<ComponentInstance>()
                )
                .ToList();
        }
    }

    private bool IsGrouped(Guid instanceId) => Board?.FindContainingGroup(instanceId) is not null;

    // An instance's on-screen size, taken as its larger dimension rather than area or the
    // smaller dimension - so a naturally thin-but-wide shape (e.g. a divider bar) isn't
    // perpetually placeholdered at a normal zoom just because one axis is small.
    private bool IsBelowLodThreshold(Bounds bounds) =>
        Math.Max(bounds.Width, bounds.Height) * _zoomPanTracker.Scale < LodSizeThreshold;

    // Decided from committed bounds, and frozen for a participant of the live gesture, so the
    // author's component tree is never mounted or unmounted while the user is holding it.
    private bool IsPlaceholder(ComponentInstance instance) =>
        _stickyParticipants.TryGetValue(instance.Id, out var frozen)
            ? frozen
            : IsBelowLodThreshold(instance.Bounds);

    private static string LodPlaceholderStyle(Bounds bounds, int zIndex) =>
        $"left: {bounds.X}px; top: {bounds.Y}px; width: {bounds.Width}px; height: {bounds.Height}px; z-index: {zIndex};";

    // One entry per keyboard tab stop: either a rendered ComponentInstance or a top-level Group's
    // own single stop - never both for a grouped member, which has no tab stop of its own (see
    // IsGrouped/ComponentContainer.Focusable). Ordered by current on-screen position (top-left to
    // bottom-right, i.e. Y then X) rather than creation order or ZIndex, so native Tab/Shift+Tab
    // traversal follows reading order for free once each stop's own tabindex="0" and DOM position
    // (this list's own order) are in place - no keyboard interception needed for Tab itself.
    // Drawn from the same windowed-mounting viewport query as VisibleComponents/GetVisibleGroups,
    // so an instance or group entirely outside the current viewport (+ overscan) has no tab stop
    // at all - reachability is bounded by what's currently mounted, not the whole Board.
    private readonly record struct TabStop(
        ComponentInstance? Instance,
        Group? Group,
        Bounds Bounds
    );

    private IReadOnlyList<TabStop> OrderedTabStops()
    {
        if (Board is null)
        {
            return Array.Empty<TabStop>();
        }

        var stops = new List<TabStop>();
        foreach (var instance in VisibleComponents)
        {
            stops.Add(new TabStop(instance, null, Live.BoundsOf(instance)));
        }

        foreach (var group in Board.GetVisibleGroups(_zoomPanTracker.Viewport, Overscan))
        {
            stops.Add(new TabStop(null, group, Live.GroupBounds(group)!.Value));
        }

        return stops.OrderBy(s => s.Bounds.Y).ThenBy(s => s.Bounds.X).ToList();
    }

    // The ids a keyboard user can actually land on, in the same order OrderedTabStops renders
    // them - a grouped member has an entry in that list too (so it still paints inside its group)
    // but no tabindex of its own (see IsGrouped/ComponentContainer.Focusable), so Ctrl+Tab must
    // skip it exactly the way native Tab already does. An LOD-placeholdered instance (see
    // IsPlaceholder) is excluded the same way - it renders as a plain div with no tabindex of its
    // own either.
    private List<Guid> FocusableTabStopIds() =>
        OrderedTabStops()
            .Where(stop =>
                stop.Instance is null
                || (!IsGrouped(stop.Instance.Id) && !IsPlaceholder(stop.Instance))
            )
            .Select(stop => stop.Instance?.Id ?? stop.Group!.Id)
            .ToList();

    // The sole entry point for the "focusing selects" half of focus-follows-selection - reached
    // only via a tab stop's own @onfocus (native Tab/Shift+Tab navigation, or a command handing
    // focus to a new instance or group), never wired to any keyboard shortcut directly. Always a hard
    // single-select, matching "landing focus on an entity selects it outright - there is no
    // separate commit step"; a grouped member is never the id passed here (it has no tab stop of
    // its own), and a top-level Group's own id needs no EffectiveSelectionId resolution
    // (SelectComponent already handles that uniformly for both cases). Ctrl+Tab's own focus move
    // (OnCtrlTabPressed) is the one exception - it sets _suppressFocusSelect first so its resulting
    // onfocus round-trip only updates _focusedTabStopId here, without the hard-select.
    private void FocusEntity(Guid id)
    {
        _focusedTabStopId = id;

        // Any genuine focus-changing navigation (Tab, Shift+Tab, Ctrl+Tab) invalidates an
        // in-progress port pick (see OnEnterPressed) - it only
        // makes sense for whichever instance real DOM focus is currently on. Enter/arrow-key port
        // picking never itself moves DOM focus, so this is never cleared out from under a pick still
        // in progress on the same instance.
        _portFocusInstanceId = null;

        if (_suppressFocusSelect)
        {
            _suppressFocusSelect = false;
            return;
        }

        SelectComponent(id, addToSelection: false);
    }

    private static string GroupTabStopStyle(Bounds bounds) =>
        $"left: {bounds.X}px; top: {bounds.Y}px; width: {bounds.Width}px; height: {bounds.Height}px;";

    private string GroupAccessibleLabel(Group group) =>
        $"Group ({ResolvingMemberCount(group)} items)";

    private int ResolvingMemberCount(Group group) =>
        Board is null
            ? 0
            : group.MemberIds.Count(id =>
                Board.GetComponent(id) is not null || Board.GetGroup(id) is not null
            );

    // While a connector drag is drawing its line every port is shown and hittable, so the drop can
    // find the one under the pointer; hover cannot do that while the canvas holds the capture.
    private string CanvasCssClass =>
        "diagram-canvas"
        + (_isDragOverBoard ? " drag-over" : "")
        + (_preview.PendingEdge is not null ? " connecting" : "");

    // Same handler for both events - dragenter and dragover mark the same "still hovering" state.
    private void HandleDragEnterOrOver(DragEventArgs e) => _isDragOverBoard = true;

    private void HandleDragLeave(DragEventArgs e) => _isDragOverBoard = false;

    private string ContentStyle =>
        $"transform: translate({_zoomPanTracker.PanX}px, {_zoomPanTracker.PanY}px) scale({_zoomPanTracker.Scale}); --d12-scale: {_zoomPanTracker.Scale};";

    // board units - layer 0's spacing, and (at scale 1.0) also its on-screen px spacing, matching
    // the legacy fixed grid's look at the default zoom level.
    private const double GridBaseSpacing = 20;
    private const int GridSpacingStep = 10;

    private readonly record struct GridLayer(int Level, double Opacity);

    // Shared by VisibleGridLayers and DominantGridSpacing - `level` is the (fractional) layer
    // index whose on-screen spacing would exactly equal GridBaseSpacing at the current scale;
    // lowerLevel/upperWeight split it into the two neighbouring integer layers and how much of the
    // blend belongs to the upper one.
    private (int LowerLevel, double UpperWeight) GridLevelSplit()
    {
        var level = -Math.Log10(_zoomPanTracker.Scale);
        var lowerLevel = (int)Math.Floor(level);
        return (lowerLevel, level - lowerLevel);
    }

    // At most two layers are ever rendered: the same "blend between two adjacent mip levels"
    // technique continuous LOD texture filtering uses, so panning/zooming crossfades smoothly
    // between 10x-apart spacings rather than popping between discrete grids.
    private IEnumerable<GridLayer> VisibleGridLayers()
    {
        var (lowerLevel, upperWeight) = GridLevelSplit();

        yield return new GridLayer(lowerLevel, 1 - upperWeight);
        if (upperWeight > 0)
        {
            yield return new GridLayer(lowerLevel + 1, upperWeight);
        }
    }

    // Snap-to-grid always targets whichever single layer is currently the more opaque (visually
    // dominant) of the two VisibleGridLayers ever renders - an exact tie (upperWeight == 0.5)
    // deterministically favours the lower layer.
    private double DominantGridSpacing()
    {
        var (lowerLevel, upperWeight) = GridLevelSplit();
        var dominantLevel = upperWeight > 0.5 ? lowerLevel + 1 : lowerLevel;
        return GridBaseSpacing * Math.Pow(GridSpacingStep, dominantLevel);
    }

    // A no-op (returns the point unchanged) whenever SnapToGrid is off - callers apply this
    // unconditionally rather than branching themselves.
    private (double X, double Y) SnapPoint(double x, double y)
    {
        if (!SnapToGrid)
        {
            return (x, y);
        }

        var spacing = DominantGridSpacing();
        return (Math.Round(x / spacing) * spacing, Math.Round(y / spacing) * spacing);
    }

    private Bounds SnapBounds(Bounds bounds)
    {
        var (x, y) = SnapPoint(bounds.X, bounds.Y);
        return bounds with { X = x, Y = y };
    }

    // background-size/position are computed here (rather than relying on canvas-content's own CSS
    // transform, the way the old single-layer grid did) because grid layers live outside
    // canvas-content - they must cover the whole viewport, not just its finite backdrop box, to read
    // correctly at arbitrary pan distance. PositiveMod keeps the pattern's phase locked to board
    // coordinate 0 (where component Bounds are anchored) regardless of how far PanX/PanY have moved.
    private string GridLayerStyle(GridLayer layer)
    {
        var boardSpacing = GridBaseSpacing * Math.Pow(GridSpacingStep, layer.Level);
        var onScreenSpacing = boardSpacing * _zoomPanTracker.Scale;
        var offsetX = PositiveMod(_zoomPanTracker.PanX, onScreenSpacing);
        var offsetY = PositiveMod(_zoomPanTracker.PanY, onScreenSpacing);
        return $"background-size: {onScreenSpacing}px {onScreenSpacing}px; background-position: {offsetX}px {offsetY}px; opacity: {layer.Opacity};";
    }

    private static double PositiveMod(double value, double modulus) =>
        ((value % modulus) + modulus) % modulus;

    // Screen (client) coordinates to board space, given the container's own page position. The
    // pointer gestures pass an origin of zero, since their coordinates arrive container-relative.
    private (double X, double Y) ToBoardPoint(
        (double ClientX, double ClientY) client,
        (double Left, double Top) containerOrigin
    ) =>
        (
            (client.ClientX - containerOrigin.Left - _zoomPanTracker.PanX) / _zoomPanTracker.Scale,
            (client.ClientY - containerOrigin.Top - _zoomPanTracker.PanY) / _zoomPanTracker.Scale
        );

    private (double X, double Y) ToBoardPoint(
        MouseEventArgs e,
        (double Left, double Top) containerOrigin
    ) => ToBoardPoint((e.ClientX, e.ClientY), containerOrigin);

    // The combined bounding box of the current selection, or null when nothing is selected - what
    // positions the selection box, which is itself the hit target for a press inside the
    // multi-selection's bounds. Reads live geometry, so it tracks a move or resize in flight
    // rather than only updating once the gesture commits.
    private Bounds? SelectedInstancesBounds()
    {
        if (Board is null)
        {
            return null;
        }

        var expanded = ExpandedSelection();
        return Bounds.Union(
            Board.Components.Where(instance => expanded.Contains(instance.Id)).Select(Live.BoundsOf)
        );
    }

    // An edge's rendered endpoints, resolved through live geometry on every render - this is
    // what lets an attached edge follow its instances through a move or resize in flight with no
    // separate update path. Null (skip rendering) if either endpoint's instance no longer exists.
    private ((double X, double Y) From, (double X, double Y) To)? EdgeLine(Edge edge)
    {
        if (Board is null)
        {
            return null;
        }

        var from = Live.ResolveEndpoint(edge.Source);
        var to = Live.ResolveEndpoint(edge.Target);

        return from is null || to is null ? null : (from.Value, to.Value);
    }

    // The pending edge line a connector drag publishes, from the end that stays put to the
    // pointer. While it carries an existing edge's end it is the only thing drawing that edge.
    private ((double X, double Y) From, (double X, double Y) To)? PendingEdgeLine()
    {
        if (Board is null || _preview.PendingEdge is not { } pending)
        {
            return null;
        }

        var from = Live.ResolveEndpoint(pending.Anchor);
        return from is null ? null : (from.Value, pending.Point);
    }

    // A click on an edge selects it - the edge counterpart to
    // SelectComponent, but kept as its own exclusive slot (see _selectedEdgeId) since edges don't
    // participate in multi-select, grouping, or move/resize as a unit.
    private void SelectEdge(Guid edgeId)
    {
        _selectedInstanceIds.Clear();
        _selectedEdgeId = edgeId;
        NotifySelectionChanged();
    }

    private bool IsEdgeSelected(Guid edgeId) => _selectedEdgeId == edgeId;

    private string EdgeLineCssClass(Guid edgeId) =>
        IsEdgeSelected(edgeId) ? "edge-line selected" : "edge-line";

    // Orthogonal/Curved routing needs an SVG <path> (a <line> can only ever be
    // straight), rendered via a computed `d`. Straight itself stays a plain <line> - see the
    // markup - so every existing test asserting x1/y1/x2/y2 on a default edge is untouched.
    private static string EdgePathD(
        EdgeRouting routing,
        (double X, double Y) from,
        (double X, double Y) to
    )
    {
        var f = InvariantPoint(from);
        var t = InvariantPoint(to);
        var midX = ((from.X + to.X) / 2).ToString(CultureInfo.InvariantCulture);

        return routing switch
        {
            EdgeRouting.Orthogonal => $"M {f.X} {f.Y} L {midX} {f.Y} L {midX} {t.Y} L {t.X} {t.Y}",
            EdgeRouting.Curved => $"M {f.X} {f.Y} C {midX} {f.Y} {midX} {t.Y} {t.X} {t.Y}",
            _ => $"M {f.X} {f.Y} L {t.X} {t.Y}",
        };
    }

    private static (string X, string Y) InvariantPoint((double X, double Y) point) =>
        (
            point.X.ToString(CultureInfo.InvariantCulture),
            point.Y.ToString(CultureInfo.InvariantCulture)
        );

    // Which <marker> (if any) an edge endpoint's ArrowStyle resolves to - null omits
    // the marker-start/marker-end attribute entirely (Blazor's usual null-means-absent attribute
    // convention, same as aria-selected above). Selected edges use the selected-color marker so an
    // arrowhead never reads as a mismatched color against its own (now-blue) line.
    private static string? EdgeMarkerUrl(ArrowStyle arrow, bool selected)
    {
        if (arrow == ArrowStyle.None)
        {
            return null;
        }

        return selected ? "url(#edge-arrow-selected)" : "url(#edge-arrow)";
    }

    private bool IsCarried(Guid edgeId) => _preview.PendingEdge?.EdgeId == edgeId;

    private bool IsEndCarried(Guid edgeId, bool isSource) =>
        IsCarried(edgeId) && _preview.PendingEdge!.IsSource == isSource;

    // Every persisted (not currently being dragged) floating endpoint across the whole
    // Board, each with a stable render key - an edge can have zero, one, or both ends floating.
    private IEnumerable<(Edge Edge, bool IsSource, double X, double Y)> FloatingEndpoints()
    {
        if (Board is null)
        {
            yield break;
        }

        foreach (var edge in Board.Edges)
        {
            if (edge.Source is FloatingEndpoint source && !IsEndCarried(edge.Id, true))
            {
                yield return (edge, true, source.X, source.Y);
            }

            if (edge.Target is FloatingEndpoint target && !IsEndCarried(edge.Id, false))
            {
                yield return (edge, false, target.X, target.Y);
            }
        }
    }

    private string MarqueeStyle
    {
        get
        {
            var bounds = _marqueeBounds ?? default;
            return $"left: {bounds.X}px; top: {bounds.Y}px; width: {bounds.Width}px; height: {bounds.Height}px;";
        }
    }

    // Positions the group bounding-box overlay - reads through SelectedInstancesBounds,
    // so it tracks a move or resize in flight the same way the members themselves do.
    private string SelectionBoundingBoxStyle
    {
        get
        {
            var bounds = SelectedInstancesBounds() ?? default;
            return $"left: {bounds.X}px; top: {bounds.Y}px; width: {bounds.Width}px; height: {bounds.Height}px;";
        }
    }

    // Matches ComponentContainer's own default Width/Height parameter values - used only when a
    // registration was declared without a DefaultSize (ComponentSize? is optional).
    private const double FallbackWidth = 200;
    private const double FallbackHeight = 150;

    private async Task HandleDrop(DragEventArgs e)
    {
        var componentTypeKey = _pendingPaletteDragKey;
        _pendingPaletteDragKey = null;
        _isDragOverBoard = false;

        if (componentTypeKey is null || Board is null || PressOwnsBoard)
        {
            StateHasChanged();
            return;
        }

        // Fetched fresh rather than reused from first render: the container can move on the page
        // (scroll, sibling layout changes) without firing the resize listener that only tracks size.
        var containerRect = await _jsModule!.InvokeAsync<Dictionary<string, double>>(
            "getContainerDimensions",
            ContainerElement
        );

        var (boardX, boardY) = ToBoardPoint(e, (containerRect["left"], containerRect["top"]));

        // The drop point is the center of the placed instance, not its top-left corner - matching
        // where the user's cursor (and the browser's default drag ghost) actually is on release.
        PlaceComponent(componentTypeKey, boardX, boardY);

        StateHasChanged();
    }

    // Also the sole entry point a keyboard user's Enter/Space on a palette button reaches -
    // native button semantics synthesize the same click event a pointer would, so there is no
    // separate keyboard code path to wire. What a keyboard user needs that a mouse click-to-add
    // doesn't: the newly placed instance is selected, and real DOM focus moves to it (once its tab
    // stop exists post-render, see _pendingPlacementFocusId/OnAfterRenderAsync) - a keyboard user
    // has no other way to reach what they just placed, since there's no cursor already sitting on
    // it the way a mouse click leaves one.
    public void ClickToAdd(string componentTypeKey)
    {
        // Also a no-op before the first-render container-size JS round trip has resolved, which
        // would otherwise center the new instance on the board origin instead of the viewport,
        // and while a pointer gesture owns the board, since the add would be refused and the
        // selection must not move to an instance that was never placed.
        if (Board is null || PressOwnsBoard || !_zoomPanTracker.HasKnownContainerSize)
        {
            return;
        }

        var offset = _clickToAddCascadeCount * ClickToAddCascadeStep;
        _clickToAddCascadeCount++;

        var viewport = _zoomPanTracker.Viewport;
        var placed = PlaceComponent(
            componentTypeKey,
            viewport.X + viewport.Width / 2 + offset,
            viewport.Y + viewport.Height / 2 + offset
        );

        if (placed is not null)
        {
            SelectComponent(placed.Id, addToSelection: false);
            _pendingPlacementFocusId = placed.Id;
        }

        StateHasChanged();
    }

    // Shared by HandleDrop and ClickToAdd - both place a new instance centered on a board-space
    // point, differing only in how that point is derived. Callers are trusted to have already
    // checked Board is non-null (both do, before computing their center point).
    // Routed through AddEntityCommand rather than a direct Board.AddComponent
    // call, so undo removes the placed instance and redo restores it with the same Id.
    // The Connector sentinel key never reaches NewCenteredInstance/Registry.Resolve -
    // there's no registration for it to resolve. Returns the placed instance (null for a
    // Connector, which produces an Edge instead) so ClickToAdd can select/focus it - HandleDrop
    // discards the return value, leaving drag-and-drop placement's own (lack of) selection
    // behaviour unchanged.
    private ComponentInstance? PlaceComponent(
        string componentTypeKey,
        double centerX,
        double centerY
    )
    {
        if (componentTypeKey == ConnectorPaletteKey)
        {
            PlaceConnector(centerX, centerY);
            return null;
        }

        var instance = NewCenteredInstance(componentTypeKey, centerX, centerY);
        _history.Do(new AddEntityCommand(Board!, instance));
        return instance;
    }

    // The palette's own way of dropping a new Edge, both ends floating, centered on the
    // same drop point / viewport-center-plus-cascade point every other placement gesture uses
    // - a fixed-length horizontal segment rather than a single point, so the new edge is
    // immediately visible and grabbable rather than a zero-length line. Routed through AddEdgeCommand
    // exactly like a connector drag's own edge creation, so undo/redo
    // treats it identically.
    private const double ConnectorDefaultHalfLength = 40;

    private void PlaceConnector(double centerX, double centerY)
    {
        var source = new FloatingEndpoint(centerX - ConnectorDefaultHalfLength, centerY);
        var target = new FloatingEndpoint(centerX + ConnectorDefaultHalfLength, centerY);
        _history.Do(new AddEdgeCommand(Board!, new Edge(source, target)));
    }

    // Extracted from PlaceComponent so AddEdgeLabel (which centers a label the same way,
    // but embeds it on an Edge instead of adding it to Board) doesn't duplicate the same
    // resolve-registration/fallback-size/center-on-a-point construction.
    // A newly-placed instance always defaults to Board.NextZIndex(), i.e.
    // above every existing entity - never a fixed baseline that would bury it behind them.
    private ComponentInstance NewCenteredInstance(
        string componentTypeKey,
        double centerX,
        double centerY
    )
    {
        var registration = Registry.Resolve(componentTypeKey);
        var size = registration.DefaultSize ?? new ComponentSize(FallbackWidth, FallbackHeight);

        return new ComponentInstance(
            registration.Key,
            registration.DefaultProps,
            SnapBounds(
                new Bounds(
                    centerX - size.Width / 2,
                    centerY - size.Height / 2,
                    size.Width,
                    size.Height
                )
            ),
            Board!.NextZIndex()
        );
    }

    private void HandleMouseWheel(WheelEventArgs e)
    {
        var zoomIn = e.DeltaY < 0;
        var zoomed = _zoomPanTracker.Zoom(zoomIn);
        if (zoomed)
        {
            StateHasChanged();
        }
    }

    private void OnZoomPanChanged(object? sender, ZoomPanChangedEventArgs e)
    {
        OnZoomOrPanChanged.InvokeAsync(e);
        ZoomOrPanChanged?.Invoke(this, e);
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var handle in _cleanupHandles)
        {
            await handle.InvokeVoidAsync("dispose");
            await handle.DisposeAsync();
        }
        _cleanupHandles.Clear();

        _zoomPanTracker.Changed -= OnZoomPanChanged;
        _history.Changed -= OnHistoryChanged;
        _dotNetObjectRef?.Dispose();

        if (_jsModule != null)
        {
            await _jsModule.DisposeAsync();
        }
    }
}
