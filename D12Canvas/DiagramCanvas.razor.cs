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

    // Not persisted. OnToggleSnapToGridPressed flips it directly and notifies SnapToGridChanged,
    // so a host using @bind-SnapToGrid stays in sync with the Ctrl+' shortcut.
    [Parameter]
    public bool SnapToGrid { get; set; } = true;

    [Parameter]
    public EventCallback<bool> SnapToGridChanged { get; set; }

    // Aligns what a move or resize carries with the edges and centres of the shapes on screen,
    // drawing a guide along each match. Independent of SnapToGrid: where both are on, object
    // snapping takes any axis it matches and the grid fills the other.
    [Parameter]
    public bool ObjectSnapping { get; set; }

    [Parameter]
    public EventCallback<bool> ObjectSnappingChanged { get; set; }

    [Parameter]
    public bool EnableSnapToGridShortcut { get; set; } = true;

    // Decides what a plain wheel means, whether Shift binds and whether the wheel's transform
    // eases. The library renders no control for it and remembers nothing; that is the host's.
    [Parameter]
    public WheelDeviceProfile WheelDeviceProfile { get; set; } = WheelDeviceProfile.Auto;

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter]
    public EventCallback<ZoomPanChangedEventArgs> OnZoomOrPanChanged { get; set; }
    public event EventHandler<ZoomPanChangedEventArgs>? ZoomOrPanChanged;

    // Sibling chrome such as the property panel re-renders on this, since Blazor's auto-rerender
    // only covers this component's own tree. Undo and redo raise it too: they can change what the
    // panel shows for an unchanged selection.
    public event EventHandler? SelectionChanged;

    private void NotifySelectionChanged()
    {
        _duplicateRun.SelectionChanged(_selectedInstanceIds, _selectedEdgeIds);
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler? Changed;

    private void OnHistoryChanged(object? sender, EventArgs e)
    {
        _retractableCreation = null;
        ReconcileEnteredGroups();
        _duplicateRun.BoardChanged(id =>
            Board?.GetComponent(id) is not null
            || Board?.GetGroup(id) is not null
            || Board?.GetEdge(id) is not null
        );
        Changed?.Invoke(this, EventArgs.Empty);
    }

    // Every component instance under the selection, a selected group contributing its members
    // recursively, so there is never a subset of the selection the panel would edit silently. A
    // selected edge contributes nothing here and is read through SelectedEdges. One instance is
    // the "edit exactly one" case; 2+ (same type or cross-type) is the bulk-edit case.
    public IReadOnlyList<ComponentInstance> SelectedComponents => ResolvedSelection();

    public IReadOnlyList<Edge> SelectedEdges =>
        Board is null
            ? Array.Empty<Edge>()
            : _selectedEdgeIds.Select(Board.GetEdge).OfType<Edge>().ToList();

    // Blazor's DragEventArgs.DataTransfer has no SetData/GetData, so Palette stashes the dragged
    // type here through BeginPaletteDrag for HandleDrop to read back.
    private string? _pendingPaletteDragKey;
    private bool _isDragOverBoard;

    // Edge is not a registered component type, so the Connector palette entry has no registry
    // key. This sentinel goes through the same palette plumbing, and PlaceComponent routes it to
    // PlaceConnector.
    public const string ConnectorPaletteKey = "__d12-connector__";

    public void BeginPaletteDrag(string componentTypeKey) =>
        _pendingPaletteDragKey = componentTypeKey;

    // Click-to-add places at the viewport centre, cascading each successive click so repeated
    // adds don't stack exactly. The counter is global and never resets: there is no placement
    // session to reset it on.
    private const double ClickToAddCascadeStep = 20;
    private int _clickToAddCascadeCount;

    // Selection is transient view state - it lives here, never on Board, and is never
    // serialized or tracked by undo/redo. Two parallel sets, because instance, group and edge ids are all bare Guids that only Board can
    // tell apart, and an edge id must never reach group expansion.
    private readonly HashSet<Guid> _selectedInstanceIds = new();
    private readonly HashSet<Guid> _selectedEdgeIds = new();

    // The entered group is the last id; each earlier id is the group the next one was entered
    // from, kept so the scope can fall back to the nearest one that still exists.
    private readonly List<Guid> _enteredGroupIds = new();

    private Guid? EnteredGroupId => _enteredGroupIds.Count > 0 ? _enteredGroupIds[^1] : null;

    // Anchored in container-relative pixels, not board space, so the menu doesn't pan or zoom
    // with the board.
    private sealed record ContextMenuState(
        double X,
        double Y,
        ContextMenuSet Set,
        bool OpenedFromKeyboard = false,
        PortAtPress? PortAtPress = null,
        CustomPortEndpoint? PortToRemove = null
    );

    // Read once when the listeners start, for the pointer path's Ctrl+click and for shortcut hints.
    private bool _applePlatform;
    private Dictionary<string, string>? _keyLabels;

    private ContextMenuState? _contextMenu;

    // Session-scoped, in-memory undo/redo - lives here, not on Board, for
    // the same reason selection does; never serialized, never survives a reload.
    private readonly CommandHistory _history = new();

    private readonly ZoomPanTracker _zoomPanTracker = new ZoomPanTracker();

    public ZoomPanTracker ZoomPanTracker => _zoomPanTracker;

    // Every press the browser-side listener forwards resolves to exactly one pointer gesture,
    // chosen here once the interop hop lands because the choice needs the selection, and that
    // gesture owns the pointer until its claiming button comes up. Its identity never changes
    // mid-press, only its phase. The selection and the entered group as they stood before the
    // press stepped out of anything are kept so a cancel can put both back.
    private PointerGesture? _activeGesture;
    private PointerMove? _lastPointer;
    private PressScope? _pressScope;

    private sealed record PressScope(
        IReadOnlyList<Guid> EnteredGroupIds,
        SelectionSnapshot Selection
    );

    private PressScope CurrentScope() =>
        new(
            _enteredGroupIds.ToList(),
            new SelectionSnapshot(_selectedInstanceIds, _selectedEdgeIds)
        );

    private Bounds? _marqueeBounds;
    private Board? _previousBoard;
    private readonly GesturePreview _preview = new();

    private readonly EdgeRouteCache _routes = new();

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

    // Keyboard connector attachment. The picked port is held as a real endpoint, never a
    // FloatingEndpoint, so OnEnterPressed can hand it straight to a new Edge.
    // _pendingConnectorSource is armed by the first Enter and survives the Tab navigation to the
    // target. Native Tab is never intercepted, so FocusEntity clears stale picking state, since
    // every focus change passes through it.
    private Guid? _portFocusInstanceId;
    private IEdgeEndpoint _portFocusEndpoint = new PortEndpoint(Guid.Empty, PortId.Top);
    private IEdgeEndpoint? _pendingConnectorSource;

    private ElementReference ContainerElement;
    private ElementReference CanvasElement;
    private DotNetObjectReference<DiagramCanvas>? _dotNetObjectRef;
    private List<IJSObjectReference> _cleanupHandles = new List<IJSObjectReference>();
    private IJSObjectReference? _jsModule;
    private IJSObjectReference? _pointerListener;

    // The listener that holds the live press, which is the minimap's for a MinimapPan.
    private IJSObjectReference? _pressListener;

    // How long the content's transform eases toward the viewport's last write: the wheel device's
    // ambient duration when a wheel made that write, the framing flight's for a viewport command,
    // and nothing for any other input.
    private TimeSpan _ambientTransition = TimeSpan.Zero;
    private bool _applyingWheel;
    private (double Scale, double PanX, double PanY) _lastTransform = (1, 0, 0);

    // The new group's tab stop doesn't exist in the DOM until the grouping render commits, so
    // focus waits for OnAfterRenderAsync.
    private bool _pendingGroupFocus;

    // Keyed by id rather than index: the target's index among tab stops isn't known until the
    // render that mounts its stop commits.
    private Guid? _pendingFocusId;

    // Tracked separately from the selection because inside additive traversal focus moves
    // without selecting.
    private Guid? _focusedTabStopId;

    // Additive traversal: started by Space on a focused stop, and while it is on a focus landing
    // moves focus only. Every press, Escape, Enter on a group stop, focus leaving the container
    // and a command handing focus to a target of its choosing ends it.
    private bool _additiveTraversal;

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
            _enteredGroupIds.Clear();
            _duplicateRun.End();
            _clicksAwaitingMeasurement.Clear();
            _editsEndedDuringPress.Clear();
            _initialFitPending = true;
            FitNewBoard();
            BoardReplaced?.Invoke(this, EventArgs.Empty);
        }
    }

    // Chrome wired to this canvas by reference, such as the minimap, hears of a new Board here,
    // since a swap that leaves the viewport where it was raises no other event.
    internal event EventHandler? BoardReplaced;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (Board is not null)
        {
            _routes.RetainOnly(RenderedEdges().ToList());
        }

        if (firstRender)
        {
            _jsModule = await JS.InvokeAsync<IJSObjectReference>(
                "import",
                "./_content/D12Canvas/DiagramCanvas.razor.js"
            );

            var facts = await _jsModule.InvokeAsync<InitialFacts>("initialFacts", ContainerElement);

            _zoomPanTracker.SetContainerSize((int)facts.Width, (int)facts.Height);
            FitNewBoard();
            PlaceClicksAwaitingMeasurement();
            _applePlatform = facts.ApplePlatform;
            _keyLabels = facts.KeyLabels;
            _asyncClipboard = facts.AsyncClipboard;

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

            _pointerListener = await _jsModule.InvokeAsync<IJSObjectReference>(
                "addPointerListener",
                CanvasElement,
                ContainerElement,
                _dotNetObjectRef,
                new { classify = true, dragThreshold = ScreenPixels.DragThreshold }
            );

            var wheelCleanup = await _jsModule.InvokeAsync<IJSObjectReference>(
                "addWheelListener",
                ContainerElement,
                _dotNetObjectRef
            );

            _cleanupHandles.Add(resizeCleanup);
            _cleanupHandles.Add(keyboardCleanup);
            _cleanupHandles.Add(_pointerListener);
            _cleanupHandles.Add(wheelCleanup);
            _cleanupHandles.Add(
                await _jsModule.InvokeAsync<IJSObjectReference>(
                    "addClipboardListener",
                    ContainerElement,
                    CanvasElement,
                    _dotNetObjectRef
                )
            );
            _cleanupHandles.Add(
                await _jsModule.InvokeAsync<IJSObjectReference>(
                    "addFileDropListener",
                    CanvasElement,
                    ContainerElement,
                    _dotNetObjectRef
                )
            );

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
            _additiveTraversal = false;
            await _jsModule!.InvokeVoidAsync("focusGroupTabStop", ContainerElement);
        }

        if (_pendingFocusId is { } placedId)
        {
            _pendingFocusId = null;
            _additiveTraversal = false;
            var index = FocusableTabStopIds().IndexOf(placedId);
            if (index >= 0)
            {
                await _jsModule!.InvokeVoidAsync("focusTabStopAt", ContainerElement, index);
            }
        }

        await ReturnFocusAfterEditAsync();
        BeginPendingEdit();
    }

    [JSInvokable]
    public void OnContainerResized(double width, double height)
    {
        _zoomPanTracker.SetContainerSize((int)width, (int)height);
        FitNewBoard();
        PlaceClicksAwaitingMeasurement();
        StateHasChanged();
    }

    // The four pointer entry points. The listener has already classified the press and taken the
    // synchronous decisions; what arrives here is a press whose owner this canvas chooses, moves
    // that are real drags (the listener never forwards one below the threshold), and the one
    // release or interruption that ends the press.
    [JSInvokable]
    public void OnPointerPressed(PointerPress press)
    {
        press = WithLockRead(press);
        if (_activeGesture is null && _portPlacement is not null)
        {
            EndPortPlacement();
        }

        if (_activeGesture is not null || PressToKind.Resolve(press) is not { } kind)
        {
            return;
        }

        _additiveTraversal = false;
        var scopeBeforePress = CurrentScope();
        if (press.Button == PointerPress.PrimaryButton)
        {
            StepOutForPress(press);
        }

        var snapshot = new SelectionSnapshot(_selectedInstanceIds, _selectedEdgeIds);
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
            Hold(gesture, scopeBeforePress, _pointerListener);
        }

        StateHasChanged();
    }

    private void Hold(
        PointerGesture gesture,
        PressScope scopeBeforePress,
        IJSObjectReference? listener
    )
    {
        var press = gesture.Press;
        _activeGesture = gesture;
        _pressListener = listener;
        _lastPointer = new PointerMove(
            press.PointerId,
            press.X,
            press.Y,
            press.Buttons,
            press.ShiftKey,
            press.CtrlKey,
            press.AltKey,
            press.MetaKey
        );
        _pressScope = scopeBeforePress;
        _history.Lock();
    }

    // The board has the last word on the lock the listener read from the markup. A primary press
    // on a locked entity is a press on empty canvas, so it steps out and marquees as one would.
    private PointerPress WithLockRead(PointerPress press)
    {
        var locked = press.Locked || press.EntityId is { } id && IsLocked(id);
        if (!locked)
        {
            return press;
        }

        return press.Button == PointerPress.PrimaryButton
            ? press with
            {
                Role = HitRole.Canvas,
                EntityId = null,
                Part = null,
                Locked = true,
            }
            : press with
            {
                Locked = true,
            };
    }

    [JSInvokable]
    public void OnPointerMoved(PointerMove move)
    {
        if (_activeGesture is null || move.PointerId != _activeGesture.Press.PointerId)
        {
            return;
        }

        _lastPointer = move;
        _activeGesture.Move(move);
        StateHasChanged();
    }

    [JSInvokable]
    public void OnWheel(WheelInput input)
    {
        var device = WheelMapping.DeviceFor(input, WheelDeviceProfile);
        _applyingWheel = true;
        try
        {
            _ambientTransition = WheelMapping.AmbientTransitionFor(device);
            var changed = WheelMapping.Map(input, device) switch
            {
                WheelZoom zoom => _zoomPanTracker.ZoomAbout(zoom.X, zoom.Y, zoom.Factor),
                WheelPan pan => _zoomPanTracker.Pan(pan.DeltaX, pan.DeltaY),
                _ => false,
            };
            if (changed)
            {
                StateHasChanged();
            }
        }
        finally
        {
            _applyingWheel = false;
        }
    }

    // A live gesture keeps what the pointer holds under the pointer: it runs again from the
    // pointer's last position, and a press the change promoted out of pointing starts having its
    // moves forwarded by the listener.
    private void RunGestureUnderMovedViewport()
    {
        if (
            _activeGesture is { } gesture
            && _lastPointer is { } pointer
            && gesture.ViewportMoved(pointer with { Velocity = 0 })
            && _pressListener is not null
        )
        {
            _ = _pressListener.InvokeVoidAsync("promote").AsTask();
        }
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

        if (restoreSelection && _pressScope is { } scope)
        {
            _enteredGroupIds.Clear();
            _enteredGroupIds.AddRange(scope.EnteredGroupIds);
            SetSelection(scope.Selection.InstanceIds, scope.Selection.EdgeIds);
        }

        _activeGesture.MarkCancelled();
    }

    // Callers often build the new sets out of the current ones, so both are materialised before
    // either set is cleared.
    private void SetSelection(IEnumerable<Guid> instanceIds, IEnumerable<Guid> edgeIds)
    {
        var instances = instanceIds.ToList();
        var edges = edgeIds.ToList();
        _selectedInstanceIds.Clear();
        _selectedInstanceIds.UnionWith(instances);
        _selectedEdgeIds.Clear();
        _selectedEdgeIds.UnionWith(edges);
        NotifySelectionChanged();
    }

    private void EndPress()
    {
        _activeGesture = null;
        _pressListener = null;
        _lastPointer = null;
        _pressScope = null;
        _marqueeBounds = null;
        _preview.Clear();
        _stickyParticipants.Clear();
        _history.Unlock();
        CommitEditsEndedDuringPress();
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
                && (Board.GetComponent(id) ?? PendingCopies?.GetComponent(id)) is { } instance
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

        if (PendingCopies is { } copies)
        {
            CommitClone(copies);
            return;
        }

        var commands = new List<ICommand>();
        foreach (var (id, after) in _preview.BoundsOverrides)
        {
            if (Board.GetComponent(id) is { Locked: false } instance && instance.Bounds != after)
            {
                commands.Add(new ChangeBoundsCommand(instance, instance.Bounds, after));
            }
        }

        foreach (var (end, after) in _preview.MovedEndpoints)
        {
            if (Board.GetEdge(end.EdgeId) is not { Locked: false } edge)
            {
                continue;
            }

            var before = end.IsSource ? edge.Source : edge.Target;
            if (!before.Equals(after))
            {
                commands.Add(new ChangeEdgeEndpointCommand(edge, end.IsSource, before, after));
            }
        }

        if (commands.Count > 0)
        {
            _history.Do(new CompositeCommand(commands));
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
        if (Board?.GetEdge(edgeId) is { Locked: false } edge)
        {
            var before = isSource ? edge.Source : edge.Target;
            _history.Do(new ChangeEdgeEndpointCommand(edge, isSource, before, endpoint));
        }
    }

    // The one focus write per press lands here. The keyboard's anchor goes null with it, since a
    // press invalidates whichever stop the keyboard was on and any port pick in progress there.
    private void HandleCanvasFocus()
    {
        EndPortPlacementOnFocus(null);
        _focusedTabStopId = null;
        _portFocusInstanceId = null;
        _additiveTraversal = false;
    }

    [JSInvokable]
    public void OnFocusLeftContainer()
    {
        if (_portPlacement is not null)
        {
            EndPortPlacement();
        }

        _focusedTabStopId = null;
        _additiveTraversal = false;
    }

    private sealed class CanvasGestureContext(DiagramCanvas canvas, SelectionSnapshot snapshot)
        : IGestureContext
    {
        public Board? Board => canvas.Board;
        public ZoomPanTracker ZoomPan => canvas._zoomPanTracker;
        public SelectionSnapshot SelectionSnapshot => snapshot;

        public (double X, double Y) ToBoardPoint(double containerX, double containerY) =>
            canvas.ToBoardPoint((containerX, containerY), (0, 0));

        public void CentreViewportOn(double boardX, double boardY, bool animated) =>
            canvas.CentreViewportOn(boardX, boardY, animated);

        public Guid EffectiveSelectionId(Guid entityId) => canvas.EffectiveSelectionId(entityId);

        public bool IsInScope(Guid entityId) => canvas.IsInScope(entityId);

        public bool HasHitRegion(Guid entityId) => canvas.HasHitRegion(entityId);

        public void EnterGroup(Guid groupId) => canvas.EnterGroup(groupId);

        public bool HasEnteredGroup => canvas.EnteredGroupId is not null;

        public void StepOutFor(PointerPress press) => canvas.StepOutForPress(press);

        public IReadOnlyList<HitStackEntry> HitStackOf(PointerPress press) =>
            canvas.HitStackOf(press);

        public void SelectHitStackEntry(HitStackEntry entry) => canvas.SelectHitStackEntry(entry);

        public void ToggleHitStackEntry(HitStackEntry entry) => canvas.ToggleHitStackEntry(entry);

        public bool IsSelected(Guid effectiveId) =>
            canvas._selectedInstanceIds.Contains(effectiveId);

        public bool IsEdgeSelected(Guid edgeId) => canvas._selectedEdgeIds.Contains(edgeId);

        public IReadOnlyList<ComponentInstance> SelectedInstances() => canvas.ResolvedSelection();

        public IReadOnlyList<Edge> SelectedEdges() => canvas.SelectedEdges;

        public void ReplaceSelection(IEnumerable<Guid> effectiveIds, IEnumerable<Guid> edgeIds) =>
            canvas.SetSelection(effectiveIds, edgeIds);

        public void AddToSelection(Guid effectiveId) =>
            canvas.SetSelection(
                canvas._selectedInstanceIds.Append(effectiveId),
                canvas._selectedEdgeIds
            );

        public void RemoveFromSelection(Guid effectiveId) =>
            canvas.SetSelection(
                canvas._selectedInstanceIds.Where(id => id != effectiveId),
                canvas._selectedEdgeIds
            );

        public void SelectEdge(Guid edgeId) => canvas.SetSelection([], [edgeId]);

        public void ToggleEdge(Guid edgeId) => canvas.ToggleEdge(edgeId);

        public void ClearSelection() => canvas.SetSelection([], []);

        public double? GridSpacing => canvas.SnapSpacing;

        public bool ObjectSnapping => canvas.ObjectSnapping;

        public IReadOnlyList<Bounds> SnapCandidates(IReadOnlyCollection<Guid> excluded) =>
            canvas
                .Board!.GetVisible(canvas._zoomPanTracker.Viewport)
                .Where(instance => !excluded.Contains(instance.Id))
                .Select(instance => instance.Bounds)
                .ToList();

        public void PublishGuides(IReadOnlyList<SnapGuide> guides) =>
            canvas._preview.PublishGuides(guides);

        public void ShowMarquee(Bounds? boardBounds) => canvas._marqueeBounds = boardBounds;

        public void PublishPreview(IReadOnlyDictionary<Guid, Bounds> boundsOverrides) =>
            canvas.PublishPreview(boundsOverrides);

        public void PublishMovedEndpoints(
            IReadOnlyDictionary<EdgeEnd, FloatingEndpoint> movedEndpoints
        ) => canvas._preview.PublishMovedEndpoints(movedEndpoints);

        public void CommitPreview() => canvas.CommitPreview();

        public Board? CopyOfSelection() => canvas.CopyOfSelection();

        public void PublishPendingFragment(Board? pendingFragment) =>
            canvas._preview.PublishPendingFragment(pendingFragment);

        public void PublishSelectionFrame(Bounds frame) =>
            canvas._preview.PublishSelectionFrame(frame);

        public void BeginInlineEdit(Guid instanceId) => canvas.BeginInlineEdit(instanceId);

        public void PublishPendingEdge(PendingEdge pendingEdge) =>
            canvas._preview.PublishPendingEdge(pendingEdge);

        public void AddEdge(IEdgeEndpoint source, IEdgeEndpoint target) =>
            canvas.AddEdge(source, target);

        public void ChangeEdgeEndpoint(Guid edgeId, bool isSource, IEdgeEndpoint endpoint) =>
            canvas.ChangeEdgeEndpoint(edgeId, isSource, endpoint);

        public void AddEdgeLabel(Guid edgeId) => canvas.AddEdgeLabel(edgeId);

        public void QuickCreate(IEdgeEndpoint sourcePort) => canvas.QuickCreate(sourcePort);

        public void BeginLabelEdit(Guid edgeId) => canvas.BeginLabelEdit(edgeId);

        public void OpenContextMenuAt(PointerPress press)
        {
            var set =
                press.Role != HitRole.Canvas && canvas.HasSelection
                    ? ContextMenuSet.Object
                    : ContextMenuSet.Canvas;
            canvas._contextMenu = new ContextMenuState(
                press.X,
                press.Y,
                set,
                PortAtPress: set == ContextMenuSet.Object ? canvas.PortAtBorderPress(press) : null,
                PortToRemove: set == ContextMenuSet.Object
                    ? canvas.RemovablePortAtPress(press)
                    : null
            );
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
    // a unit is one dominant grid line. Only an empty selection falls back to the arrow-key pan; a
    // selection with nothing to move (an edge whose ends are all attached) makes the key a no-op.
    // PanStep is in screen pixels, because ZoomPanTracker's pan is, so a pan press covers the same
    // screen distance at any zoom.
    private const double NudgeStep = 1;
    private const double NudgeStepCoarse = 10;
    private const double PanStep = 50;

    // Extended by the repeat keydowns of a held arrow key and cleared on keyup, so the next press
    // starts a fresh undo entry.
    private NudgeCommand? _activeNudgeCommand;

    [JSInvokable]
    public void OnArrowKeyPressed(string code, bool shiftKey)
    {
        if (_portPlacement is { } placement)
        {
            if (!PressOwnsBoard)
            {
                MovePortPlacement(placement, code, shiftKey);
            }

            return;
        }

        // A custom port is reached via Space instead (OnSpacePressed).
        if (_portFocusInstanceId is { } focusedInstanceId)
        {
            if (PortIdForArrow(code) is { } portId)
            {
                _portFocusEndpoint = new PortEndpoint(focusedInstanceId, portId);
                StateHasChanged();
            }
            return;
        }

        if (_selectedInstanceIds.Count > 0 || _selectedEdgeIds.Count > 0)
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

        var targets = UnlockedSelection();
        var edges = UnlockedSelectedEdges();
        if (NudgeAnchor(targets, edges) is not { } anchor)
        {
            return;
        }

        var (deltaX, deltaY) = SnapToGrid
            ? GridNudgeDelta(anchor, dirX, dirY, shiftKey ? NudgeStepCoarse : NudgeStep)
            : ScreenNudgeDelta(dirX, dirY, shiftKey);

        if (
            _activeNudgeCommand is not null
            && ReferenceEquals(_history.PeekUndo, _activeNudgeCommand)
            && _activeNudgeCommand.Matches(targets, edges)
        )
        {
            _activeNudgeCommand.Extend(deltaX, deltaY);
        }
        else
        {
            _activeNudgeCommand = new NudgeCommand(targets, deltaX, deltaY, edges);
            _history.Do(_activeNudgeCommand);
        }

        StateHasChanged();
    }

    private (double X, double Y) ScreenNudgeDelta(double dirX, double dirY, bool shiftKey)
    {
        var step = ScreenNudgeStep(shiftKey);
        return (dirX * step, dirY * step);
    }

    private double ScreenNudgeStep(bool coarse) =>
        (coarse ? NudgeStepCoarse : NudgeStep) / _zoomPanTracker.Scale;

    private double NudgeAlong(double coordinate, double direction, bool coarse) =>
        SnapToGrid
            ? NextGridLine(
                coordinate,
                direction,
                DominantGridSpacing(),
                coarse ? NudgeStepCoarse : NudgeStep
            )
            : coordinate + direction * ScreenNudgeStep(coarse);

    // Read off the current geometry so each press in a held burst steps from where the last one
    // landed. Null when nothing would move.
    private static Bounds? NudgeAnchor(
        IReadOnlyList<ComponentInstance> targets,
        IReadOnlyList<Edge> edges
    )
    {
        if (targets.Count > 0)
        {
            return Bounds.Union(targets.Select(target => target.Bounds));
        }

        return Bounds.Union(
            edges
                .SelectMany(edge => new[] { edge.Source, edge.Target })
                .OfType<FloatingEndpoint>()
                .Select(end => new Bounds(end.X, end.Y, 0, 0))
        );
    }

    private (double X, double Y) GridNudgeDelta(
        Bounds anchor,
        double dirX,
        double dirY,
        double lines
    )
    {
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

    // An arrow key moves the viewport in the pressed direction, so the content pans the other way.
    private void PanFor(string code)
    {
        var (dirX, dirY) = ArrowDirection(code);
        _zoomPanTracker.Pan(-dirX * PanStep, -dirY * PanStep);
        StateHasChanged();
    }

    // One screen pixel at any zoom. Shift flips the anchor rather than coarsening the step, so
    // there is no coarse value.
    private const double ResizeStep = 1;

    // Coalesces a held Alt+Arrow burst like _activeNudgeCommand.
    private ResizeStepCommand? _activeResizeCommand;

    // Single instance only: a multi-selection or a selected group has no keyboard resize and is
    // a no-op, as is a port pick in progress.
    [JSInvokable]
    public void OnAltArrowKeyPressed(string code, bool shiftKey)
    {
        if (
            Board is null
            || PressOwnsBoard
            || PlacingPort
            || _portFocusInstanceId is not null
            || _selectedInstanceIds.Count != 1
        )
        {
            return;
        }

        var instance = Board.GetComponent(_selectedInstanceIds.Single())
            is { Locked: false } unlocked
            ? unlocked
            : null;
        var direction = ResizeDirectionFor(code, shiftKey);
        if (instance is null || direction is null)
        {
            return;
        }

        var (deltaX, deltaY) = ResizeStepDeltaFor(code, _zoomPanTracker.Scale);

        // Matches also checks direction: a Shift toggle mid-burst flips the anchor, so it must
        // start a fresh entry.
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

    // The edge on the arrow's side grows outward; with Shift that edge is the anchor and the
    // opposite edge moves toward it.
    private static ResizeDirection? ResizeDirectionFor(string code, bool shiftKey) =>
        code switch
        {
            "ArrowRight" => shiftKey ? ResizeDirection.Left : ResizeDirection.Right,
            "ArrowLeft" => shiftKey ? ResizeDirection.Right : ResizeDirection.Left,
            "ArrowDown" => shiftKey ? ResizeDirection.Top : ResizeDirection.Bottom,
            "ArrowUp" => shiftKey ? ResizeDirection.Bottom : ResizeDirection.Top,
            _ => null,
        };

    // The sign is fixed per arrow key; Shift only changes which edge it applies to.
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

    private List<ComponentInstance> ResolvedSelection() =>
        Board is null
            ? new List<ComponentInstance>()
            : ExpandedSelection()
                .Select(id => Board.GetComponent(id))
                .Where(instance => instance is not null)
                .Cast<ComponentInstance>()
                .ToList();

    // Escape takes one stage per press, newest first, so one Escape never throws away more than
    // it meant to: cancel the pointer gesture that owns the press (a second Escape mid-press does
    // nothing, because the cancelled gesture still owns the pointer until its button comes up),
    // then close the context menu, then end a half-built keyboard connection, whether still
    // mid-pick or with an armed source waiting for a target, then end additive traversal, keeping
    // the selection, then step out of the entered group, selecting the group just left and
    // handing focus to its stop when the keyboard was inside it, and last clear the selection.
    [JSInvokable]
    public void OnEscapePressed()
    {
        if (_activeGesture is not null)
        {
            CancelActiveGesture(restoreSelection: true);
            StateHasChanged();
            return;
        }

        if (_contextMenu is not null)
        {
            _contextMenu = null;
        }
        else if (_portPlacement is not null)
        {
            _portPlacement = null;
        }
        else if (_portFocusInstanceId is not null || _pendingConnectorSource is not null)
        {
            _portFocusInstanceId = null;
            _pendingConnectorSource = null;
        }
        else if (_additiveTraversal)
        {
            _additiveTraversal = false;
        }
        else if (EnteredGroupId is { } left)
        {
            StepOutWhile(enteredId => enteredId == left);
            SetSelection([left], []);
            if (_focusedTabStopId is not null)
            {
                _pendingFocusId = left;
            }
        }
        else
        {
            SetSelection([], []);
        }

        StateHasChanged();
    }

    // Enter on a group's tab stop enters the group; otherwise it drives keyboard connector
    // attachment. The first Enter while picking arms the source and leaves picking so Tab can
    // reach the target; the second completes the connection like a connector drop, so landing
    // back on the source instance creates no edge. The JS listener only forwards Enter from a
    // component container or group tab stop, so a Palette button's own Enter is never taken.
    [JSInvokable]
    public void OnEnterPressed()
    {
        if (Board is null || PressOwnsBoard)
        {
            return;
        }

        if (_portPlacement is { } placement)
        {
            CommitPortPlacement(placement);
            return;
        }

        if (_portFocusInstanceId is not { } focusedInstanceId)
        {
            if (_focusedTabStopId is { } groupId && Board.GetGroup(groupId) is not null)
            {
                _additiveTraversal = false;
                EnterGroupFromKeyboard(groupId);
                return;
            }

            if (_focusedTabStopId is not { } id || Board.GetComponent(id) is not { Locked: false })
            {
                return;
            }

            _portFocusInstanceId = id;
            _portFocusEndpoint = new AutoPortEndpoint(id);
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

        // Other commands can run between arming and confirming, so the source may be gone.
        if (
            sourceEndpoint.ComponentId is { } sourceComponentId
            && Board.GetComponent(sourceComponentId) is not null
            && !(chosenEndpoint.ComponentId is { } targetId && IsLocked(targetId))
            && chosenEndpoint.ComponentId != sourceComponentId
        )
        {
            _history.Do(new AddEdgeCommand(Board, new Edge(sourceEndpoint, chosenEndpoint)));
        }

        _pendingConnectorSource = null;
        StateHasChanged();
    }

    // An armed source stays highlighted during the Tab navigation to the target, so a keyboard
    // user can still see where the connection started.
    private IEdgeEndpoint? FocusedPortEndpointFor(Guid instanceId)
    {
        if (_portFocusInstanceId == instanceId)
        {
            return _portFocusEndpoint;
        }

        return _pendingConnectorSource is { } source && source.ComponentId == instanceId
            ? source
            : null;
    }

    private PortId? FocusedStandardPortIdFor(Guid instanceId) =>
        FocusedPortEndpointFor(instanceId) is PortEndpoint port ? port.PortId : null;

    private Guid? FocusedCustomPortIdFor(Guid instanceId) =>
        FocusedPortEndpointFor(instanceId) is CustomPortEndpoint custom ? custom.PortId : null;

    // Space on a focused stop adds it to the selection, never removing it, and starts additive
    // traversal; inside the mode it toggles the focused stop through the same toggle a Shift
    // press uses. While picking a port it cycles ports instead, the only way to reach a custom
    // port.
    [JSInvokable]
    public void OnSpacePressed()
    {
        if (PressOwnsBoard || PlacingPort)
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

        if (_additiveTraversal || !IsStopSelected(id))
        {
            ToggleStop(id);
        }

        _additiveTraversal = true;
        StateHasChanged();
    }

    private bool IsStopSelected(Guid stopId) =>
        Board?.GetEdge(stopId) is not null
            ? IsEdgeSelected(stopId)
            : _selectedInstanceIds.Contains(EffectiveSelectionId(stopId));

    private void ToggleStop(Guid stopId)
    {
        if (Board?.GetEdge(stopId) is not null)
        {
            ToggleEdge(stopId);
        }
        else
        {
            SelectComponent(stopId, addToSelection: true);
        }
    }

    // IndexOf relies on the endpoints' record structural equality.
    private void CyclePortFocus(Guid instanceId)
    {
        if (Board?.GetComponent(instanceId) is not { } instance)
        {
            return;
        }

        var ports = Board
            .AllPorts(instance)
            .Select(p => p.Endpoint)
            .Prepend(new AutoPortEndpoint(instanceId))
            .ToList();
        var currentIndex = ports.IndexOf(_portFocusEndpoint);
        _portFocusEndpoint = ports[(currentIndex + 1) % ports.Count];
        StateHasChanged();
    }

    // Removes exactly what Cut would carry: the unlocked selection, every selected edge and every
    // edge between two deleted instances, with the group edits the removal forces, in one undo
    // entry. An edge with only one end on a deleted instance stays, and whatever locked part of
    // the selection is left stays selected. While port picking it acts on the highlighted port.
    [JSInvokable]
    public void OnDeletePressed()
    {
        if (PressOwnsBoard || PlacingPort)
        {
            return;
        }

        if (_portFocusInstanceId is not null)
        {
            DeleteDuringPortPicking();
            return;
        }

        if (Board is null)
        {
            SetSelection([], []);
            StateHasChanged();
            return;
        }

        if (CopiedFragment(forCut: true) is { } removable)
        {
            Remove(removable);
            return;
        }

        KeepSurvivingSelection();
        StateHasChanged();
    }

    // After a removal the selection holds what is still on the board, which is what the removal
    // left because it was locked.
    private void KeepSurvivingSelection() =>
        SetSelection(
            _selectedInstanceIds.Where(id =>
                Board?.GetComponent(id) is not null || Board?.GetGroup(id) is not null
            ),
            _selectedEdgeIds.Where(id => Board?.GetEdge(id) is not null)
        );

    // Ctrl+A selects every top-level entity, a grouped instance as its outermost group, and every
    // edge on the board. With a group entered it selects that group's direct members instead.
    // Nothing locked is taken, a fully locked group included.
    [JSInvokable]
    public void OnSelectAllPressed()
    {
        if (Board is null || PressOwnsBoard || PlacingPort)
        {
            return;
        }

        _contextMenu = null;
        if (EnteredGroupId is { } enteredId && Board.GetGroup(enteredId) is { } entered)
        {
            SetSelection(entered.MemberIds.Where(id => !IsLocked(id)), []);
        }
        else
        {
            SetSelection(
                Board
                    .Components.Select(instance => EffectiveSelectionId(instance.Id))
                    .Where(id => !IsLocked(id)),
                Board.Edges.Where(edge => !edge.Locked).Select(edge => edge.Id)
            );
        }

        StateHasChanged();
    }

    // A selected group is carried over rather than flattened, so grouping it nests it. Focus
    // moves to the new group's tab stop. A selected edge cannot be a group member and stays
    // selected beside the new group.
    [JSInvokable]
    public void OnGroupPressed()
    {
        if (Board is null || PressOwnsBoard || PlacingPort || !CanGroupSelection)
        {
            return;
        }

        var group = new Group(_selectedInstanceIds.ToList());
        _history.Do(new GroupCommand(Board, group, EnteredGroupId));

        _selectedInstanceIds.Clear();
        _selectedInstanceIds.Add(group.Id);
        _pendingGroupFocus = true;
        NotifySelectionChanged();
        StateHasChanged();
    }

    // Dissolves only the outer level: a nested group among the members stays grouped.
    [JSInvokable]
    public void OnUngroupPressed()
    {
        if (Board is null || PressOwnsBoard || PlacingPort || !CanUngroupSelection)
        {
            return;
        }

        var groups = _selectedInstanceIds
            .Select(id => Board.GetGroup(id))
            .Where(group => group is not null)
            .Cast<Group>()
            .ToList();

        var commands = groups
            .Select(group =>
                (ICommand)new UngroupCommand(Board, group, Board.FindParentGroup(group.Id)?.Id)
            )
            .ToList();
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

    // Selection is not tracked by history, but NotifySelectionChanged still fires: an undone or
    // redone edit can change the selected instance's Props under the property panel.
    [JSInvokable]
    public void OnUndoPressed()
    {
        if (PressOwnsBoard || PlacingPort)
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
        if (PressOwnsBoard || PlacingPort)
        {
            return;
        }

        _history.Redo();
        NotifySelectionChanged();
        StateHasChanged();
    }

    // A selected group's members are reordered independently, like any multi-selection.
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

    // Disabling the chord leaves OnToggleSnapToGridPressed ungated for any other caller. Port placement owns the
    // keyboard, and its step depends on snap, so the chord does nothing while placing.
    [JSInvokable]
    public void OnSnapToGridChordPressed()
    {
        if (EnableSnapToGridShortcut && !PlacingPort)
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

    // No chord reaches this: object snapping is a set-and-forget preference, and Ctrl already
    // frees a single drag from it.
    public void OnToggleObjectSnappingPressed()
    {
        ObjectSnapping = !ObjectSnapping;
        ObjectSnappingChanged.InvokeAsync(ObjectSnapping);
        StateHasChanged();
    }

    // Each selected instance gets its own consecutive value from the board's extreme, in original
    // order, so the selection's relative order survives the move.
    private void RestackSelection(bool toFront)
    {
        if (Board is null || PressOwnsBoard || PlacingPort)
        {
            return;
        }

        var selected = UnlockedSelection().OrderBy(instance => instance.ZIndex).ToList();

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

    // The rule is evaluated against the board before any of these writes, so members of a
    // multi-selection never see each other's changes.
    private void ApplyZIndexChange(Func<ComponentInstance, int?> computeNewZIndex)
    {
        if (Board is null || PressOwnsBoard || PlacingPort)
        {
            return;
        }

        var commands = new List<ICommand>();
        foreach (var id in ExpandedSelection())
        {
            var instance = Board.GetComponent(id);
            if (instance is null || instance.Locked)
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

    private bool HasSelection => _selectedInstanceIds.Count > 0 || SelectedEdges.Count > 0;

    private ContextMenuContext ContextMenuContextFor(ContextMenuState menu)
    {
        var arrangeable = ArrangeableSelection().Count;
        return new(
            menu.Set,
            CanGroup: CanGroupSelection,
            CanUngroup: CanUngroupSelection,
            CanArrange: CanArrangeSelection,
            CanSelectAll: Board is not null && (Board.Components.Any() || Board.Edges.Any()),
            SnapToGrid: SnapToGrid,
            SnapToGridChordLive: EnableSnapToGridShortcut,
            ObjectSnapping: ObjectSnapping,
            ApplePlatform: _applePlatform,
            KeyLabels: _keyLabels,
            CanCopy: CanCopySelection,
            CanCut: CanCopySelection && CanRemoveSelection,
            AsyncClipboard: _asyncClipboard,
            CanChangePicture: CanChangePicture,
            CanAlign: arrangeable >= AlignDistribute.AlignThreshold,
            CanDistribute: arrangeable >= AlignDistribute.DistributeThreshold,
            CanDelete: CanRemoveSelection,
            SelectionLocked: SelectionIsLocked,
            CanUnlockAll: HasAnythingLocked,
            CanAddPortHere: menu.PortAtPress is not null,
            CanRemovePort: menu.PortToRemove is not null,
            CanPlacePort: menu.OpenedFromKeyboard && PlaceablePortTarget() is not null
        );
    }

    // Inside an entered group, grouping every direct member would only wrap the group in itself.
    private bool CanGroupSelection =>
        _selectedInstanceIds.Count >= 2
        && !(
            EnteredGroupId is { } enteredId
            && Board?.GetGroup(enteredId) is { } entered
            && _selectedInstanceIds.IsSupersetOf(entered.MemberIds)
        );

    private bool CanUngroupSelection =>
        _selectedInstanceIds.Any(id => Board?.GetGroup(id) is not null);

    private bool CanArrangeSelection => UnlockedSelection().Count > 0;

    // Shift+F10 or the ContextMenu key, once the listener's Menu verdict gave the request to the
    // canvas. The keyboard has no press target, so the selection decides the set, and the menu
    // draws at the bottom-left of the selection's on-screen box, or at the viewport centre for the
    // canvas set.
    [JSInvokable]
    public void OnContextMenuKeyPressed()
    {
        if (Board is null || PressOwnsBoard || PlacingPort || _portFocusInstanceId is not null)
        {
            return;
        }

        var (x, y) = KeyboardMenuAnchor();
        _contextMenu = new ContextMenuState(
            x,
            y,
            HasSelection ? ContextMenuSet.Object : ContextMenuSet.Canvas,
            OpenedFromKeyboard: true
        );
        StateHasChanged();
    }

    private (double X, double Y) KeyboardMenuAnchor()
    {
        var width = _zoomPanTracker.ContainerWidth;
        var height = _zoomPanTracker.ContainerHeight;
        var selection = Bounds.Union(
            new[] { SelectedInstancesBounds() }
                .Concat(SelectedEdges.Select(EdgeBox))
                .Where(box => box is not null)
                .Select(box => box!.Value)
        );
        if (selection is not { } box)
        {
            return (width / 2, height / 2);
        }

        var scale = _zoomPanTracker.Scale;
        return (
            Math.Clamp(box.X * scale + _zoomPanTracker.PanX, 0, width),
            Math.Clamp((box.Y + box.Height) * scale + _zoomPanTracker.PanY, 0, height)
        );
    }

    private Bounds? EdgeBox(Edge edge)
    {
        if (RouteOf(edge) is not { } route)
        {
            return null;
        }

        var (left, top) = (route.Points.Min(point => point.X), route.Points.Min(point => point.Y));
        return new Bounds(
            left,
            top,
            route.Points.Max(point => point.X) - left,
            route.Points.Max(point => point.Y) - top
        );
    }

    private void CloseContextMenu()
    {
        _contextMenu = null;
        StateHasChanged();
    }

    // Closes the menu first so a command that itself calls StateHasChanged (every OnXPressed
    // does) never re-renders with a stale menu still open.
    private async Task InvokeFromContextMenu(ContextMenuCommand command)
    {
        var menu = _contextMenu;
        _contextMenu = null;
        switch (command)
        {
            case ContextMenuCommand.Cut:
                await CopyFromMenu(cut: true);
                return;
            case ContextMenuCommand.Copy:
                await CopyFromMenu(cut: false);
                return;
            case ContextMenuCommand.Paste:
                if (menu is not null)
                {
                    await PasteFromMenu(menu);
                }

                return;
            case ContextMenuCommand.ChooseImage:
                await ChoosePictureFromMenu();
                return;
            case ContextMenuCommand.RemoveImage:
                SetPictureOfSelectedImages("");
                return;
            case ContextMenuCommand.AddPortHere:
                if (menu?.PortAtPress is { } portAtPress)
                {
                    AddPortAtPress(portAtPress);
                }

                return;
            case ContextMenuCommand.PlacePort:
                BeginPortPlacement();
                return;
            case ContextMenuCommand.RemovePort:
                if (menu?.PortToRemove is { } portToRemove)
                {
                    RemoveCustomPort(portToRemove);
                }

                StateHasChanged();
                return;
        }

        Action action = command switch
        {
            ContextMenuCommand.Duplicate => OnDuplicatePressed,
            ContextMenuCommand.Delete => OnDeletePressed,
            ContextMenuCommand.Group => OnGroupPressed,
            ContextMenuCommand.Ungroup => OnUngroupPressed,
            ContextMenuCommand.AlignLeft => OnAlignLeftPressed,
            ContextMenuCommand.AlignCentre => OnAlignCentrePressed,
            ContextMenuCommand.AlignRight => OnAlignRightPressed,
            ContextMenuCommand.AlignTop => OnAlignTopPressed,
            ContextMenuCommand.AlignMiddle => OnAlignMiddlePressed,
            ContextMenuCommand.AlignBottom => OnAlignBottomPressed,
            ContextMenuCommand.DistributeHorizontally => OnDistributeHorizontallyPressed,
            ContextMenuCommand.DistributeVertically => OnDistributeVerticallyPressed,
            ContextMenuCommand.BringToFront => OnBringToFrontPressed,
            ContextMenuCommand.BringForward => OnBringForwardPressed,
            ContextMenuCommand.SendBackward => OnSendBackwardPressed,
            ContextMenuCommand.SendToBack => OnSendToBackPressed,
            ContextMenuCommand.SelectAll => OnSelectAllPressed,
            ContextMenuCommand.ToggleSnapToGrid => OnToggleSnapToGridPressed,
            ContextMenuCommand.ToggleObjectSnapping => OnToggleObjectSnappingPressed,
            ContextMenuCommand.Lock => OnLockPressed,
            ContextMenuCommand.Unlock => OnUnlockPressed,
            ContextMenuCommand.UnlockAll => OnUnlockAllPressed,
            ContextMenuCommand.ZoomToSelection => ZoomToSelection,
            ContextMenuCommand.ZoomToFit => ZoomToFit,
            ContextMenuCommand.ZoomTo100Percent => ZoomTo100Percent,
            _ => throw new ArgumentOutOfRangeException(nameof(command), command, null),
        };
        action();
    }

    // A selection entry can be a group id; flattening to instance ids lets a selected group go
    // through the same move, resize and delete paths as any multi-selection.
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

    private bool IsSelected(Guid instanceId) => ShownSelection().Contains(instanceId);

    // What draws as selected: the selection, or a clone drag's copies while they are in the hand.
    private HashSet<Guid> ShownSelection() =>
        PendingCopies is { } copies
            ? copies.Components.Select(copy => copy.Id).ToHashSet()
            : ExpandedSelection();

    // Reads the unexpanded set: ExpandedSelection never contains a group's own id.
    private bool IsGroupSelected(Guid groupId) =>
        PendingCopies is null && _selectedInstanceIds.Contains(groupId);

    // Suppresses a container's own resize handles in favour of the shared overlay's. A selected
    // group of 2+ members counts as a multi-selection.
    private bool IsMultiSelected(Guid instanceId)
    {
        var expanded = ShownSelection();
        return expanded.Count > 1 && expanded.Contains(instanceId);
    }

    private bool HasMultiMemberSelection => ShownSelection().Count > 1;

    // The ancestor of an entity, or the entity itself, that is a direct member of the entered
    // group, or the outermost one when no group is entered or the entity lies outside it.
    private Guid EffectiveSelectionId(Guid id)
    {
        if (Board is null)
        {
            return id;
        }

        var current = id;
        while (Board.FindParentGroup(current) is { } parent && parent.Id != EnteredGroupId)
        {
            current = parent.Id;
        }

        return current;
    }

    // Every instance and edge on the board is content, and content keeps its hit region at every
    // zoom, a placeholder included, unless it is locked. Primary-press participation is decided
    // here and nowhere else; keyboard reachability never reads it.
    private bool HasHitRegion(Guid entityId) =>
        (Board?.GetComponent(entityId) is not null || Board?.GetEdge(entityId) is not null)
        && !IsLocked(entityId);

    // A clone drag's copies are drawn before they are on the board and carry no hit marker; a
    // locked instance keeps its marker, so a secondary press still reaches it.
    private bool IsOnBoard(Guid instanceId) => Board?.GetComponent(instanceId) is not null;

    private bool IsInScope(Guid id) =>
        EnteredGroupId is not { } enteredId || IsInside(id, enteredId);

    private bool IsInside(Guid id, Guid groupId)
    {
        for (
            var parent = Board?.FindParentGroup(id);
            parent is not null;
            parent = Board!.FindParentGroup(parent.Id)
        )
        {
            if (parent.Id == groupId)
            {
                return true;
            }
        }

        return false;
    }

    // Top-level, or a direct member of the entered group. Content inside anything else answers a
    // press as its instance, and only an addressable entity has a tab stop of its own.
    private bool IsAddressable(Guid id) =>
        Board?.FindParentGroup(id) is not { } parent || parent.Id == EnteredGroupId;

    private void EnterGroup(Guid groupId)
    {
        if (Board?.GetGroup(groupId) is not null && IsAddressable(groupId))
        {
            _enteredGroupIds.Add(groupId);
        }
    }

    // The group's single tab stop gives way to stops for its direct members, and focus moves to
    // the first of them in reading order, which selects it.
    private void EnterGroupFromKeyboard(Guid groupId)
    {
        EnterGroup(groupId);
        if (EnteredGroupId != groupId)
        {
            return;
        }

        var members = Board!.GetGroup(groupId)!.MemberIds;
        var first = FocusableTabStopIds()
            .Where(members.Contains)
            .Select(id => (Guid?)id)
            .FirstOrDefault();
        SetSelection(first is { } firstId ? [firstId] : [], []);
        _focusedTabStopId = first;
        _pendingFocusId = first;
        StateHasChanged();
    }

    // Rule for a press: one on an entity outside the entered group steps out until the entity is
    // inside it, one on empty canvas steps out until the press point lies inside the entered
    // group's bounds, and one on an edge, which no group holds, steps all the way out.
    private void StepOutForPress(PointerPress press)
    {
        if (EnteredGroupId is null || Board is null)
        {
            return;
        }

        if (press.Role == HitRole.Canvas)
        {
            var point = ToBoardPoint((press.X, press.Y), (0, 0));
            StepOutWhile(enteredId =>
                Board.GetGroup(enteredId) is not { } entered
                || Live.GroupBounds(entered) is not { } bounds
                || !bounds.Contains(point.X, point.Y)
            );
        }
        else if (press.EntityId is { } entityId)
        {
            if (Board.GetEdge(entityId) is not null)
            {
                StepOutWhile(_ => true);
            }
            else
            {
                StepOutWhile(enteredId => !IsInside(entityId, enteredId));
            }
        }
    }

    // A locked entity is left out, as a primary press would pass it by.
    private IReadOnlyList<HitStackEntry> HitStackOf(PointerPress press)
    {
        if (Board is null || press.Hits is null)
        {
            return [];
        }

        var entries = new List<HitStackEntry>();
        foreach (var hit in press.Hits)
        {
            if (
                hit.EntityId is not { } id
                || hit.Role is HitRole.SelectionBounds or HitRole.SelectionHandle
                || IsLocked(id)
            )
            {
                continue;
            }

            HitStackEntry? entry =
                Board.GetEdge(id) is not null ? new HitStackEntry(id, IsEdge: true)
                : Board.GetComponent(id) is not null
                    ? new HitStackEntry(PressedSelectionId(id), IsEdge: false)
                : null;
            if (entry is { } resolved && !entries.Contains(resolved))
            {
                entries.Add(resolved);
            }
        }

        return entries;
    }

    // What a press on the entity selects once it has stepped out as far as it needs: the ancestor,
    // or the entity itself, whose parent is the deepest entered group holding it, or the outermost
    // one when no entered group holds it.
    private Guid PressedSelectionId(Guid id)
    {
        var current = id;
        while (
            Board!.FindParentGroup(current) is { } parent && !_enteredGroupIds.Contains(parent.Id)
        )
        {
            current = parent.Id;
        }

        return current;
    }

    private void StepOutFor(HitStackEntry entry) =>
        StepOutWhile(enteredId => entry.IsEdge || !IsInside(entry.Id, enteredId));

    private void SelectHitStackEntry(HitStackEntry entry)
    {
        StepOutFor(entry);
        SetSelection(entry.IsEdge ? [] : [entry.Id], entry.IsEdge ? [entry.Id] : []);
    }

    private void ToggleHitStackEntry(HitStackEntry entry)
    {
        StepOutFor(entry);
        var toggled = entry.IsEdge ? _selectedEdgeIds : _selectedInstanceIds;
        var after = toggled.Contains(entry.Id)
            ? toggled.Where(id => id != entry.Id)
            : toggled.Append(entry.Id);
        SetSelection(
            entry.IsEdge ? _selectedInstanceIds : after,
            entry.IsEdge ? after : _selectedEdgeIds
        );
    }

    private void StepOutWhile(Func<Guid, bool> condition)
    {
        var depth = _enteredGroupIds.Count;
        while (EnteredGroupId is { } enteredId && condition(enteredId))
        {
            _enteredGroupIds.RemoveAt(_enteredGroupIds.Count - 1);
        }

        if (_enteredGroupIds.Count < depth)
        {
            ResolveSelectionToScope();
        }
    }

    // After the scope changes level, every selected id becomes its ancestor at the new level.
    private void ResolveSelectionToScope() =>
        SetSelection(
            _selectedInstanceIds.Select(EffectiveSelectionId).Distinct(),
            _selectedEdgeIds
        );

    // The scope cannot outlive its group: an undo, a delete or an ungroup that removes an entered
    // group, or moves it out of the group it was entered from, falls back to the nearest one still
    // in place.
    private void ReconcileEnteredGroups()
    {
        if (Board is null)
        {
            return;
        }

        var intact = 0;
        while (
            intact < _enteredGroupIds.Count
            && Board.GetGroup(_enteredGroupIds[intact]) is not null
            && Board.FindParentGroup(_enteredGroupIds[intact])?.Id
                == (intact == 0 ? null : _enteredGroupIds[intact - 1])
        )
        {
            intact++;
        }

        StepOutWhile(_ => _enteredGroupIds.Count > intact);
    }

    // The toggle leaves the selected edges as they are; the collapse clears them.
    private void SelectComponent(Guid instanceId, bool addToSelection)
    {
        StepOutWhile(enteredId => !IsInside(instanceId, enteredId));
        var effectiveId = EffectiveSelectionId(instanceId);

        if (addToSelection)
        {
            if (!_selectedInstanceIds.Remove(effectiveId))
            {
                _selectedInstanceIds.Add(effectiveId);
            }
            NotifySelectionChanged();
            return;
        }

        SetSelection([effectiveId], []);
    }

    // The caller is trusted to have already skipped a no-op edit.
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

    // One undo entry for the whole bulk edit. The caller is trusted to have already skipped any
    // instance whose value wouldn't change.
    public void CommitPropsChangeBatch(IReadOnlyList<PropsChange> changes)
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

    // An edge's label lives on its edge, not in Board.Components. A locked instance, or the label
    // of a locked edge, resolves to nothing, so no props edit reaches it.
    private ComponentInstance? ResolvePropsEntity(Guid id) =>
        IsPropsEntityLocked(id) ? null : Board?.GetComponent(id) ?? Board?.FindEdgeLabel(id);

    // No panel UI calls this yet; tests exercise it directly.
    public void CommitEdgeStyleChange(Guid edgeId, EdgeStyle before, EdgeStyle after)
    {
        var edge = Board?.GetEdge(edgeId);
        if (edge is null || edge.Locked)
        {
            return;
        }

        _history.Do(new ChangeEdgeStyleCommand(edge, before, after));
        StateHasChanged();
    }

    // Registered by BuiltInComponents.RegisterAll.
    private const string DefaultEdgeLabelComponentTypeKey = "text";

    // A double-press on the line never clobbers an existing label. The label's Bounds.X/Y are
    // never read again (see EdgeLabelStyle); only its size matters.
    private void AddEdgeLabel(Guid edgeId)
    {
        var edge = Board?.GetEdge(edgeId);
        if (edge is null || edge.Locked || edge.Label is not null)
        {
            return;
        }

        if (RouteOf(edge) is not { } route)
        {
            return;
        }

        var (midX, midY) = route.LabelAnchor;
        var label = NewCenteredInstance(DefaultEdgeLabelComponentTypeKey, midX, midY);

        RecordCreation(label.Id, new ChangeEdgeLabelCommand(edge, before: null, after: label));
        StateHasChanged();
        BeginLabelEdit(edgeId);
    }

    // Positioned from the live route, so the label's own Bounds.X/Y are ignored. While a connector
    // drag carries one of this edge's ends, the label follows the pending route so it doesn't
    // detach and freeze mid-drag.
    private string? EdgeLabelStyle(Edge edge)
    {
        if (edge.Label is null)
        {
            return null;
        }

        var route = IsCarried(edge.Id) ? PendingEdgeRoute() : RouteOf(edge);
        if (route is null)
        {
            return null;
        }

        var box = LabelBox(edge.Label, route);
        return $"left: {box.X}px; top: {box.Y}px; width: {box.Width}px; height: {box.Height}px;";
    }

    private static Bounds LabelBox(ComponentInstance label, EdgeRoute route)
    {
        var (midX, midY) = route.LabelAnchor;
        var (width, height) = (label.Bounds.Width, label.Bounds.Height);
        return new Bounds(midX - width / 2, midY - height / 2, width, height);
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

    // The larger dimension, so a thin-but-wide shape such as a divider isn't placeholdered at a
    // normal zoom.
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

    // A non-addressable instance is listed so it still paints, but has no tab stop of its own.
    // Ordered by on-screen position, Y then X, so native Tab follows reading order with no
    // keyboard interception. Only what is mounted (viewport plus overscan) has a stop.
    //
    // An edge's stop comes directly after the stop its source resolves to, edges sharing that
    // anchor in their targets' reading order; a floating source sorts as a stop of its own at its
    // point. No edge has a stop while a group is entered, since an edge is never a member.
    private readonly record struct TabStop(
        ComponentInstance? Instance,
        Group? Group,
        Edge? Edge,
        Bounds Bounds
    )
    {
        public Guid Id => Instance?.Id ?? Group?.Id ?? Edge!.Id;
    }

    private readonly record struct SortedStop(TabStop Stop, double X, double Y);

    private IReadOnlyList<TabStop> OrderedTabStops()
    {
        if (Board is null)
        {
            return Array.Empty<TabStop>();
        }

        var stops = new List<SortedStop>();
        foreach (var instance in VisibleComponents)
        {
            var bounds = Live.BoundsOf(instance);
            stops.Add(
                new SortedStop(new TabStop(instance, null, null, bounds), bounds.X, bounds.Y)
            );
        }

        var mountArea = _zoomPanTracker.Viewport.ExpandedBy(Overscan);
        foreach (var group in Board.Groups.Where(HasGroupTabStop))
        {
            if (
                Board.GetBounds(group) is { } committed
                && mountArea.Intersects(committed)
                && Live.GroupBounds(group) is { } live
            )
            {
                stops.Add(new SortedStop(new TabStop(null, group, null, live), live.X, live.Y));
            }
        }

        var anchored = new Dictionary<Guid, List<SortedStop>>();
        if (EnteredGroupId is null)
        {
            var anchors = stops.Select(stop => stop.Stop.Id).ToHashSet();
            foreach (var edge in Board.Edges)
            {
                if (
                    Live.ResolveEnd(edge, isSource: true) is not { } from
                    || Live.ResolveEnd(edge, isSource: false) is not { } to
                )
                {
                    continue;
                }

                var stop = new TabStop(null, null, edge, EdgeStopBounds(from, to));
                if (EndEntityId(edge, isSource: true) is { } anchorId)
                {
                    if (anchors.Contains(anchorId))
                    {
                        if (!anchored.TryGetValue(anchorId, out var following))
                        {
                            anchored[anchorId] = following = [];
                        }
                        following.Add(new SortedStop(stop, to.X, to.Y));
                    }
                }
                else if (mountArea.Contains(from.X, from.Y))
                {
                    stops.Add(new SortedStop(stop, from.X, from.Y));
                }
            }
        }

        var ordered = new List<TabStop>();
        foreach (var sorted in InReadingOrder(stops))
        {
            ordered.Add(sorted.Stop);
            if (sorted.Stop.Edge is null && anchored.TryGetValue(sorted.Stop.Id, out var following))
            {
                ordered.AddRange(InReadingOrder(following).Select(edgeStop => edgeStop.Stop));
            }
        }

        return ordered;
    }

    private static IEnumerable<SortedStop> InReadingOrder(IEnumerable<SortedStop> stops) =>
        stops.OrderBy(stop => stop.Y).ThenBy(stop => stop.X);

    private static Bounds EdgeStopBounds((double X, double Y) from, (double X, double Y) to) =>
        new(
            Math.Min(from.X, to.X),
            Math.Min(from.Y, to.Y),
            Math.Abs(to.X - from.X),
            Math.Abs(to.Y - from.Y)
        );

    private string EdgeAccessibleLabel(Edge edge) =>
        $"Connector from {EndAccessibleName(edge, isSource: true)} to {EndAccessibleName(edge, isSource: false)}";

    // The stop an attached end resolves to, as a press on its component would select it.
    private Guid? EndEntityId(Edge edge, bool isSource) =>
        Live.EndpointOf(edge, isSource).ComponentId is { } componentId
            ? EffectiveSelectionId(componentId)
            : null;

    private string EndAccessibleName(Edge edge, bool isSource)
    {
        var id = EndEntityId(edge, isSource);
        if (id is { } groupId && Board!.GetGroup(groupId) is { } group)
        {
            return GroupAccessibleLabel(group);
        }

        return id is { } instanceId && Board!.GetComponent(instanceId) is { } instance
            ? Registry.Resolve(instance.ComponentTypeKey).AccessibleName
            : "unattached end";
    }

    // An addressable group has one stop until it is entered, when its members' stops replace it.
    private bool HasGroupTabStop(Group group) =>
        IsAddressable(group.Id) && !_enteredGroupIds.Contains(group.Id);

    // Skips what native Tab skips (a non-addressable member, an LOD placeholder), keeping
    // focusTabStopAt's index in step with the rendered stops.
    private List<Guid> FocusableTabStopIds() =>
        FocusableTabStops().Select(stop => stop.Id).ToList();

    private List<TabStop> FocusableTabStops() =>
        OrderedTabStops()
            .Where(stop =>
                stop.Instance is null
                || (IsAddressable(stop.Instance.Id) && !IsPlaceholder(stop.Instance))
            )
            .ToList();

    // A focus move like Tab: it lands through focusTabStopAt so the stop's own @onfocus decides
    // whether it selects. An edge is never a target, because its stop's box can sit far from both
    // of its ends.
    [JSInvokable]
    public async Task OnDirectionalFocusPressed(string code)
    {
        if (
            Board is null
            || _jsModule is null
            || PressOwnsBoard
            || PlacingPort
            || _portFocusInstanceId is not null
            || DirectionalFocus.DirectionFor(code) is not { } direction
        )
        {
            return;
        }

        var ring = FocusableTabStops();
        var target = DirectionalFocus.Nearest(
            DirectionalFocusOrigin(ring),
            direction,
            ring.Where(stop => stop.Edge is null && IsInScope(stop.Id))
                .Select(stop => new FocusCandidate(stop.Id, stop.Bounds))
        );
        if (target is { } targetId)
        {
            var index = ring.FindIndex(stop => stop.Id == targetId);
            await _jsModule.InvokeVoidAsync("focusTabStopAt", ContainerElement, index);
        }
    }

    private Bounds DirectionalFocusOrigin(List<TabStop> ring)
    {
        var focused = ring.FindIndex(stop => stop.Id == _focusedTabStopId);
        if (focused >= 0)
        {
            var stop = ring[focused];
            return stop.Edge is { } edge ? EdgeSourceBox(edge) ?? stop.Bounds : stop.Bounds;
        }

        var selection = Bounds.Union(
            new[] { SelectedInstancesBounds() }
                .Concat(SelectedEdges.Select(EdgeSourceBox))
                .Where(box => box is not null)
                .Select(box => box!.Value)
        );
        if (selection is { } selectionBox)
        {
            return selectionBox;
        }

        var viewport = _zoomPanTracker.Viewport;
        return new Bounds(viewport.X + viewport.Width / 2, viewport.Y + viewport.Height / 2, 0, 0);
    }

    private Bounds? EdgeSourceBox(Edge edge)
    {
        if (EndEntityId(edge, isSource: true) is { } anchorId)
        {
            return Board!.GetGroup(anchorId) is { } group ? Live.GroupBounds(group)
                : Board.GetComponent(anchorId) is { } instance ? Live.BoundsOf(instance)
                : null;
        }

        return Live.ResolveEnd(edge, isSource: true) is { } point
            ? new Bounds(point.X, point.Y, 0, 0)
            : null;
    }

    // Reached only through a tab stop's @onfocus. Landing focus selects outright, except inside
    // additive traversal, where it moves focus only.
    private void FocusEntity(Guid id)
    {
        EndPortPlacementOnFocus(id);
        _focusedTabStopId = id;

        // Port picking never moves DOM focus itself, so this never clears a pick in progress on
        // the same instance.
        _portFocusInstanceId = null;

        if (_additiveTraversal)
        {
            StepOutWhile(enteredId => !IsInside(id, enteredId));
            return;
        }

        if (Board?.GetEdge(id) is not null)
        {
            SetSelection([], [id]);
            return;
        }

        SelectComponent(id, addToSelection: false);
    }

    private const double EnteredGroupOutlineGap = 4;

    // Around the innermost entered group only, on its live bounds so it follows a member in flight.
    private string? EnteredGroupOutlineStyle()
    {
        if (
            EnteredGroupId is not { } enteredId
            || Board?.GetGroup(enteredId) is not { } entered
            || Live.GroupBounds(entered) is not { } bounds
        )
        {
            return null;
        }

        return BoxStyle(bounds.ExpandedBy(EnteredGroupOutlineGap));
    }

    private const double SpacingGuideCapHalfLength = 4;

    // An alignment guide spans the whole viewport; a spacing guide runs across its gap with a cap
    // at each end, the caps sized in screen pixels since a stroke's non-scaling width does nothing
    // for a segment's length.
    private IEnumerable<(string Kind, double X1, double Y1, double X2, double Y2)> GuideSegments()
    {
        var viewport = _zoomPanTracker.Viewport;
        var (top, bottom) = (viewport.Y + 0.0, viewport.Bottom + 0.0);
        var (left, right) = (viewport.X + 0.0, viewport.Right + 0.0);
        var cap = SpacingGuideCapHalfLength / _zoomPanTracker.Scale;
        foreach (var guide in _preview.Guides)
        {
            switch (guide)
            {
                case AlignmentGuide { Axis: SnapAxis.X } vertical:
                    yield return (
                        "alignment-guide",
                        vertical.Coordinate,
                        top,
                        vertical.Coordinate,
                        bottom
                    );
                    break;
                case AlignmentGuide horizontal:
                    yield return (
                        "alignment-guide",
                        left,
                        horizontal.Coordinate,
                        right,
                        horizontal.Coordinate
                    );
                    break;
                case SpacingGuide { Axis: SnapAxis.X } across:
                    yield return (
                        "spacing-guide",
                        across.From,
                        across.Cross,
                        across.To,
                        across.Cross
                    );
                    yield return (
                        "spacing-guide",
                        across.From,
                        across.Cross - cap,
                        across.From,
                        across.Cross + cap
                    );
                    yield return (
                        "spacing-guide",
                        across.To,
                        across.Cross - cap,
                        across.To,
                        across.Cross + cap
                    );
                    break;
                case SpacingGuide down:
                    yield return ("spacing-guide", down.Cross, down.From, down.Cross, down.To);
                    yield return (
                        "spacing-guide",
                        down.Cross - cap,
                        down.From,
                        down.Cross + cap,
                        down.From
                    );
                    yield return (
                        "spacing-guide",
                        down.Cross - cap,
                        down.To,
                        down.Cross + cap,
                        down.To
                    );
                    break;
            }
        }
    }

    private static string BoxStyle(Bounds bounds) =>
        $"left: {bounds.X}px; top: {bounds.Y}px; width: {bounds.Width}px; height: {bounds.Height}px;";

    private string GroupAccessibleLabel(Group group) =>
        $"Group ({ResolvingMemberCount(group)} items)";

    private int ResolvingMemberCount(Group group) =>
        (PendingCopies?.GetGroup(group.Id) is not null ? PendingCopies : Board) is not { } holder
            ? 0
            : group.MemberIds.Count(id =>
                holder.GetComponent(id) is not null || holder.GetGroup(id) is not null
            );

    // While a connector drag is drawing its line every port is shown and hittable, so the drop can
    // find the one under the pointer; hover cannot do that while the canvas holds the capture.
    private string CanvasCssClass =>
        "diagram-canvas"
        + (_isDragOverBoard ? " drag-over" : "")
        + (_preview.PendingEdge is not null ? " connecting" : "");

    private void HandleDragEnterOrOver(DragEventArgs e) => _isDragOverBoard = true;

    private void HandleDragLeave(DragEventArgs e) => _isDragOverBoard = false;

    private string ContentStyle =>
        $"transform: translate({_zoomPanTracker.PanX}px, {_zoomPanTracker.PanY}px) scale({_zoomPanTracker.Scale}); --d12-scale: {_zoomPanTracker.Scale}; --d12-edge-hit-band: {ScreenPixels.EdgeHitBand}px; --d12-port-target: {ScreenPixels.PortTarget}px;{AmbientTransitionStyle}";

    private string AmbientTransitionStyle =>
        _ambientTransition > TimeSpan.Zero
            ? $" transition: transform {_ambientTransition.TotalMilliseconds}ms ease-out;"
            : "";

    // In board units: layer 0's spacing, and its on-screen spacing at scale 1.0.
    internal const double GridBaseSpacing = 20;
    private const int GridSpacingStep = 10;

    private readonly record struct GridLayer(int Level, double Opacity);

    // `level` is the fractional layer whose on-screen spacing equals GridBaseSpacing at the
    // current scale, split into the lower integer layer and the upper layer's blend weight.
    private (int LowerLevel, double UpperWeight) GridLevelSplit()
    {
        var level = -Math.Log10(_zoomPanTracker.Scale);
        var lowerLevel = (int)Math.Floor(level);
        return (lowerLevel, level - lowerLevel);
    }

    // Two adjacent layers crossfade, like mip-level blending, so zooming doesn't pop between
    // 10x-apart grids.
    private IEnumerable<GridLayer> VisibleGridLayers()
    {
        var (lowerLevel, upperWeight) = GridLevelSplit();

        yield return new GridLayer(lowerLevel, 1 - upperWeight);
        if (upperWeight > 0)
        {
            yield return new GridLayer(lowerLevel + 1, upperWeight);
        }
    }

    // The more opaque of the two visible layers; a tie favours the lower one.
    private double DominantGridSpacing()
    {
        var (lowerLevel, upperWeight) = GridLevelSplit();
        var dominantLevel = upperWeight > 0.5 ? lowerLevel + 1 : lowerLevel;
        return GridBaseSpacing * Math.Pow(GridSpacingStep, dominantLevel);
    }

    private (double X, double Y) SnapPoint(double x, double y)
    {
        if (!SnapToGrid)
        {
            return (x, y);
        }

        var spacing = DominantGridSpacing();
        return (GridSnap.NearestLine(x, spacing), GridSnap.NearestLine(y, spacing));
    }

    private Bounds SnapBounds(Bounds bounds)
    {
        var (x, y) = SnapPoint(bounds.X, bounds.Y);
        return bounds with { X = x, Y = y };
    }

    // Grid layers sit outside canvas-content so they cover the whole viewport at any pan, which
    // means they don't get its transform. PositiveMod keeps the pattern's phase locked to board
    // coordinate 0.
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

    // The pointer gestures pass an origin of zero, since their coordinates arrive
    // container-relative.
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

    private Bounds? SelectedInstancesBounds()
    {
        if (Board is null)
        {
            return null;
        }

        if (_preview.SelectionFrame is { } frame)
        {
            return frame;
        }

        var shown = ShownSelection();
        return Bounds.Union(
            Board
                .Components.Concat(PendingCopies?.Components ?? [])
                .Where(instance => shown.Contains(instance.Id))
                .Select(Live.BoundsOf)
        );
    }

    // Cached on its inputs, so a render that moved nothing the edge depends on (a pan, a zoom, a
    // drag elsewhere) routes nothing.
    private EdgeRoute? RouteOf(Edge edge)
    {
        if (Board is null || Live.RouteRequestOf(edge) is not { } request)
        {
            return null;
        }

        return _routes.RouteFor(edge.Id, request);
    }

    internal int RoutesComputed => _routes.RoutesComputed;

    // The route a connector drag draws, between the end that stays put and the pointer. A new
    // edge is straight; a carried end of an existing edge routes in that edge's own style with
    // the pointer as a floating end, standing in for the edge while it is drawn.
    private EdgeRoute? PendingEdgeRoute()
    {
        if (Board is null || _preview.PendingEdge is not { } pending)
        {
            return null;
        }

        var pointer = new FloatingEndpoint(pending.Point.X, pending.Point.Y);
        var style =
            pending.EdgeId is { } edgeId && Board.GetEdge(edgeId) is { } edge
                ? edge.RoutingStyle
                : EdgeRouting.Straight;
        var request =
            style == EdgeRouting.Straight || !pending.IsSource
                ? Live.RouteRequestBetween(pending.Anchor, pointer, style)
                : Live.RouteRequestBetween(pointer, pending.Anchor, style);
        return request is { } routed ? EdgeRouter.Route(routed) : null;
    }

    private bool IsEdgeSelected(Guid edgeId) =>
        PendingCopies is { } copies
            ? copies.GetEdge(edgeId) is not null
            : _selectedEdgeIds.Contains(edgeId);

    private void ToggleEdge(Guid edgeId) =>
        SetSelection(
            _selectedInstanceIds,
            _selectedEdgeIds.Contains(edgeId)
                ? _selectedEdgeIds.Where(id => id != edgeId)
                : _selectedEdgeIds.Append(edgeId)
        );

    private bool IsCarried(Guid edgeId) => _preview.PendingEdge?.EdgeId == edgeId;

    private bool IsEndCarried(Guid edgeId, bool isSource) =>
        IsCarried(edgeId) && _preview.PendingEdge!.IsSource == isSource;

    private IEnumerable<(Edge Edge, bool IsSource, double X, double Y)> FloatingEndpoints()
    {
        if (Board is null)
        {
            yield break;
        }

        foreach (var edge in RenderedEdges())
        {
            foreach (var isSource in new[] { true, false })
            {
                if (
                    Live.EndpointOf(edge, isSource) is FloatingEndpoint floating
                    && !IsEndCarried(edge.Id, isSource)
                )
                {
                    yield return (edge, isSource, floating.X, floating.Y);
                }
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

    private string SelectionBoundingBoxStyle
    {
        get
        {
            var bounds = SelectedInstancesBounds() ?? default;
            return $"left: {bounds.X}px; top: {bounds.Y}px; width: {bounds.Width}px; height: {bounds.Height}px;";
        }
    }

    // Matches ComponentContainer's own default Width/Height.
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

        if (PlaceComponent(componentTypeKey, boardX, boardY) is { } placed)
        {
            RequestInlineEdit(placed.Id);
        }

        StateHasChanged();
    }

    // A palette button's Enter/Space also lands here. Focus moves to the placed instance because
    // a keyboard user has no other way to reach it.
    public void ClickToAdd(string componentTypeKey)
    {
        // The add would be refused, and the selection must not move to an instance that was
        // never placed.
        if (Board is null || PressOwnsBoard)
        {
            return;
        }

        // Before the container is measured the viewport centre is unknown, so the click waits for
        // the measurement rather than landing on the board origin or being lost.
        if (!_zoomPanTracker.HasKnownContainerSize)
        {
            _clicksAwaitingMeasurement.Add(componentTypeKey);
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
            _pendingFocusId = placed.Id;
            RequestInlineEdit(placed.Id);
        }

        StateHasChanged();
    }

    private readonly List<string> _clicksAwaitingMeasurement = [];

    private void PlaceClicksAwaitingMeasurement()
    {
        if (!_zoomPanTracker.HasKnownContainerSize)
        {
            return;
        }

        var keys = _clicksAwaitingMeasurement.ToList();
        _clicksAwaitingMeasurement.Clear();
        foreach (var key in keys)
        {
            ClickToAdd(key);
        }
    }

    // Callers are trusted to have already checked Board is non-null. Null for a Connector, which
    // produces an Edge instead.
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
        RecordCreation(instance.Id, new AddEntityCommand(Board!, instance));
        return instance;
    }

    // A fixed-length segment rather than a point, so the new edge is visible and grabbable.
    private const double ConnectorDefaultHalfLength = 40;

    private void PlaceConnector(double centerX, double centerY)
    {
        var source = new FloatingEndpoint(centerX - ConnectorDefaultHalfLength, centerY);
        var target = new FloatingEndpoint(centerX + ConnectorDefaultHalfLength, centerY);
        _history.Do(new AddEdgeCommand(Board!, new Edge(source, target)));
    }

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

    // A change that leaves the transform as it was, such as the container reporting the size it
    // already had, moves no content, so it ends no flight and drops no transition.
    private void OnZoomPanChanged(object? sender, ZoomPanChangedEventArgs e)
    {
        var transform = (_zoomPanTracker.Scale, _zoomPanTracker.PanX, _zoomPanTracker.PanY);
        if (transform != _lastTransform)
        {
            if (!_applyingFlight)
            {
                _inFlight = false;
            }

            if (!_applyingWheel && !_applyingFlight)
            {
                _ambientTransition = TimeSpan.Zero;
            }

            _lastTransform = transform;
            RunGestureUnderMovedViewport();
            StateHasChanged();
        }

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
