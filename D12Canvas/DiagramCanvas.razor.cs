using System.Globalization;
using D12Canvas.History;
using D12Canvas.Model;
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

    // Sized to absorb the pan-render throttle's worst case: at PanRenderInterval (16ms) and a
    // fast drag, the pointer can move well past this before the next windowed re-render lands,
    // but a much larger margin just inflates mount/unmount cost for no visible benefit.
    public const double DefaultOverscan = 200;

    [Parameter]
    public Board? Board { get; set; }

    [Parameter]
    public double Overscan { get; set; } = DefaultOverscan;

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter]
    public EventCallback<ZoomPanChangedEventArgs> OnZoomOrPanChanged { get; set; }
    public event EventHandler<ZoomPanChangedEventArgs>? ZoomOrPanChanged;

    // A palette entry has no compile-time-typed payload it can hand across the native HTML5 drag
    // session (Blazor's DragEventArgs.DataTransfer exposes no SetData/GetData) - Palette instead
    // calls this directly (via its explicit Canvas reference, ADR 0002) to stash which type is
    // being dragged, for HandleDrop to read back once the gesture completes.
    private string? _pendingPaletteDragKey;
    private bool _isDragOverBoard;

    public void BeginPaletteDrag(string componentTypeKey) =>
        _pendingPaletteDragKey = componentTypeKey;

    // ADR 0009: click-to-add's default position is the viewport center (not a fixed board-space
    // origin, which could land off-screen once the user has panned), with a small cascading
    // offset per successive click so repeated adds don't stack in a perfectly overlapping pile.
    // The counter is global (not per component type) - it only needs to keep successive placements
    // visually apart, and never resets, matching this app's momentary-gesture-only model (there's
    // no "start a new placement session" event to reset it on).
    private const double ClickToAddCascadeStep = 20;
    private int _clickToAddCascadeCount;

    // Selection is transient view state (ADR 0006) - it lives here, never on Board, and is never
    // serialized or tracked by undo/redo. Ticket 32: ad-hoc multi-select via marquee/shift-click.
    private readonly HashSet<Guid> _selectedInstanceIds = new();

    // Ticket 50: an edge's own exclusive selection slot - edges don't participate in multi-select,
    // grouping, or move/resize as a unit (ADR 0006 only covers component instances), so an edge id
    // is never mixed into _selectedInstanceIds. Selecting an edge always clears any instance
    // selection, and selecting an instance (or starting a marquee) always clears this.
    private Guid? _selectedEdgeId;

    // Ticket 37: session-scoped, in-memory undo/redo (ADR 0007) - lives here, not on Board, for
    // the same reason selection does; never serialized, never survives a reload.
    private readonly CommandHistory _history = new();

    private readonly ZoomPanTracker _zoomPanTracker = new ZoomPanTracker();

    // Make ZoomPanTracker accessible to child components
    public ZoomPanTracker ZoomPanTracker => _zoomPanTracker;

    // Ticket 32: a plain drag on empty canvas still pans (pre-existing behaviour, unchanged);
    // Shift+drag draws an intersection-based marquee instead, pairing with Shift-click's existing
    // "multi-select gesture" meaning. A drag starting inside the current selection's own combined
    // bounding box does neither of those - it's ticket 33's group-move instead (see _isGroupMoving
    // below). Whichever of these a gesture turns out to be, it starts and ends with mousedown/
    // mouseup on the same element, so the browser's native click fires right after it - without the
    // _dragMoved guard that click would immediately clear the selection the drag just established
    // (or leave alone), the same wrinkle ticket 29's original pan guard existed to solve.
    private bool _isPanning;
    private bool _isMarqueeSelecting;
    private bool _dragMoved;
    private MouseEventArgs? _panStart;
    private DateTime _lastPanRender = DateTime.MinValue;
    private static readonly TimeSpan PanRenderInterval = TimeSpan.FromMilliseconds(16); // ~60fps cap
    private (double Left, double Top) _marqueeContainerOrigin;
    private (double X, double Y) _marqueeAnchor;
    private (double X, double Y) _marqueeCurrent;

    // Ticket 33: a multi-selection (2+) moves and resizes as a single bounding-box unit (ADR 0006).
    // Move can start two ways - dragging empty space inside the combined bounding box (tracked here,
    // live-previewed every tick since DiagramCanvas owns the whole gesture) or dragging one of the
    // selected members directly (ComponentContainer's own existing _isMoving already tracks that
    // member smoothly; MoveComponent below turns its single OnMoved delta into a one-shot update of
    // every other member once the gesture commits, rather than routing it through here). Either way
    // Board is only ever written once, on release, matching every other gesture's discipline.
    private bool _isGroupMoving;
    private (double X, double Y) _groupMoveAnchor;
    private double _groupMoveDeltaX;
    private double _groupMoveDeltaY;

    // Group resize is driven entirely by the selection bounding box's own handles (rendered by
    // DiagramCanvas, not any individual ComponentContainer - see IsMultiSelected). Members' bounds
    // are scaled proportionally relative to the bbox they started at, previewed live via
    // EffectiveBounds and committed once, on release.
    private bool _isGroupResizing;
    private ResizeDirection _groupResizeDirection;
    private MouseEventArgs? _groupResizeAnchor;
    private Bounds _groupResizeStartBounds;
    private Bounds _groupResizeCurrentBounds;
    private Dictionary<Guid, Bounds> _groupResizeMemberStartBounds = new();
    private double _groupResizeMinBboxWidth;
    private double _groupResizeMinBboxHeight;

    // Ticket 48: connector drag-in-progress state (ADR 0005/0009) - lives here, not on Board,
    // the same reasoning as every other momentary gesture tracked in this file. Owned centrally
    // (rather than by the source ComponentContainer) because a completed connection spans two
    // different instances.
    private bool _isConnectingPort;
    private Guid _connectSourceComponentId;
    private PortId _connectSourcePortId;
    private (double Left, double Top) _connectContainerOrigin;
    private (double X, double Y) _connectCurrentPoint;

    // Ticket 49: set when this drag is repositioning an EXISTING edge's endpoint - grabbed from a
    // port that already anchors an edge (StartPortDrag), or from a floating endpoint's own marker
    // (StartFloatingEndpointDrag) - rather than creating a brand new one. Null means "creating a
    // new edge from a bare port" (ticket 48's original path). While set, the edge being edited is
    // rendered via the drag preview instead of its own normal line (see IsBeingEdited).
    private Guid? _connectEditingEdgeId;
    private bool _connectEditingEdgeIsSource;

    // Board-space radius a connector-drag release must land within one of an instance's own
    // standard ports to attach - matches that port affordance's own authored radius
    // (ComponentContainer.razor: a 20px-diameter circle in the same, zoom-independent local
    // coordinate space Bounds itself uses; the ancestor .canvas-content's CSS scale transform
    // only changes its *painted* on-screen footprint, not this board-space size). dropPoint
    // (from ToBoardPoint) and FindPortNear's own port positions are both already in that same
    // board space, so this needs no further scaling by zoom.
    private const double PortHitRadius = 10;

    // Read by ComponentContainer (via the ParentCanvas cascading parameter) so every instance's
    // own mousemove/mouseup can forward to this gesture instead of running its own drag logic.
    public bool IsConnectingPort => _isConnectingPort;

    private ElementReference ContainerElement;
    private DotNetObjectReference<DiagramCanvas>? _dotNetObjectRef;
    private List<Action> _cleanupFunctions = new List<Action>();
    private IJSObjectReference? _jsModule;

    protected override void OnInitialized()
    {
        _zoomPanTracker.Changed += OnZoomPanChanged;
        _dotNetObjectRef = DotNetObjectReference.Create(this);
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
            _zoomPanTracker.SetCanvasSize(3000, 3000);

            // Set up resize listener
            var resizeCleanup = await _jsModule.InvokeAsync<Action>(
                "addResizeListener",
                ContainerElement,
                _dotNetObjectRef
            );

            // Set up keyboard listener
            var keyboardCleanup = await _jsModule.InvokeAsync<Action>(
                "addKeyboardListener",
                ContainerElement,
                _dotNetObjectRef
            );

            // Store cleanup functions for disposal
            _cleanupFunctions.Add(resizeCleanup);
            _cleanupFunctions.Add(keyboardCleanup);

            StateHasChanged();
        }
    }

    [JSInvokable]
    public void OnContainerResized(double width, double height)
    {
        _zoomPanTracker.SetContainerSize((int)width, (int)height);
        StateHasChanged();
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

    [JSInvokable]
    public void OnPanLeft()
    {
        _zoomPanTracker.Pan(50, 0);
        StateHasChanged();
    }

    [JSInvokable]
    public void OnPanRight()
    {
        _zoomPanTracker.Pan(-50, 0);
        StateHasChanged();
    }

    [JSInvokable]
    public void OnPanUp()
    {
        _zoomPanTracker.Pan(0, 50);
        StateHasChanged();
    }

    [JSInvokable]
    public void OnPanDown()
    {
        _zoomPanTracker.Pan(0, -50);
        StateHasChanged();
    }

    // ADR 0009: Escape clears the selection, and (ticket 48) cancels an in-progress connector
    // drag rather than letting it resolve against wherever the pointer happens to be.
    [JSInvokable]
    public void OnEscapePressed()
    {
        if (_isConnectingPort)
        {
            CancelPortDrag();
        }

        _selectedInstanceIds.Clear();
        _selectedEdgeId = null;
        StateHasChanged();
    }

    // ADR 0009: Delete/Backspace removes every currently selected instance from Board and clears
    // the selection - single and multi-selection are the same code path here, since (unlike
    // move/resize) deletion has no "move as one unit" delta to apply, just N independent removals.
    // Ticket 38: wrapped in one CompositeCommand of RemoveEntityCommands (ADR 0007), so a
    // multi-selection delete undoes as a single atomic entry and every deleted instance is
    // restored with its identity, bounds, and props intact.
    // Ticket 44: reads through ExpandedSelection so a selected Group's members are what actually
    // get deleted (the Group entity itself, now referencing missing members, is left for a future
    // ticket to decide how to handle - not exercised by this one).
    // Ticket 50: a selected edge takes a separate branch - it's never mixed into
    // _selectedInstanceIds (see _selectedEdgeId), and there's no multi-select or "as one unit"
    // delta to apply, just the one RemoveEdgeCommand.
    [JSInvokable]
    public void OnDeletePressed()
    {
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
                var commands = new List<ICommand>();
                foreach (var id in ExpandedSelection())
                {
                    var instance = Board.GetComponent(id);
                    if (instance is not null)
                    {
                        commands.Add(new RemoveEntityCommand(Board, instance));
                    }
                }

                if (commands.Count > 0)
                {
                    _history.Do(new CompositeCommand(commands));
                }
            }
        }

        _selectedInstanceIds.Clear();
        _selectedEdgeId = null;
        StateHasChanged();
    }

    // ADR 0006/0007: Ctrl+G promotes the current 2+ top-level selection into a persistent Group
    // entity - the group becomes the new selection. A selection entry that is already a Group's
    // own id (from a prior grouping) is carried over by reference rather than flattened to its
    // members, so grouping a selection that already contains a group nests it.
    [JSInvokable]
    public void OnGroupPressed()
    {
        if (Board is null || _selectedInstanceIds.Count < 2)
        {
            return;
        }

        var group = new Group(_selectedInstanceIds.ToList());
        _history.Do(new GroupCommand(Board, group));

        _selectedInstanceIds.Clear();
        _selectedInstanceIds.Add(group.Id);
        StateHasChanged();
    }

    // ADR 0006/0007: Ctrl+Shift+G dissolves every currently-selected Group back into its
    // immediate members, which become independently selectable again - a member that is itself a
    // nested Group stays grouped (only the outer group is dissolved). Non-group entries already in
    // the selection are left untouched.
    [JSInvokable]
    public void OnUngroupPressed()
    {
        if (Board is null)
        {
            return;
        }

        var groups = _selectedInstanceIds
            .Select(id => Board.GetGroup(id))
            .Where(group => group is not null)
            .Cast<Group>()
            .ToList();

        if (groups.Count == 0)
        {
            return;
        }

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

        StateHasChanged();
    }

    // ADR 0007/0009: Ctrl+Z / Ctrl+Shift+Z. Selection is untouched either way - it's not tracked
    // by History (ADR 0006).
    [JSInvokable]
    public void OnUndoPressed()
    {
        _history.Undo();
        StateHasChanged();
    }

    [JSInvokable]
    public void OnRedoPressed()
    {
        _history.Redo();
        StateHasChanged();
    }

    // Ticket 44: a top-level entry in _selectedInstanceIds can be either a component instance id
    // or a Group id (grouping/clicking a member collapses selection onto the Group - ADR 0006).
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

    // Ticket 33: distinguishes "selected" from "selected as part of a group of 2+" - only the
    // latter suppresses a ComponentContainer's own resize handles in favour of the shared overlay's.
    // Ticket 44: reads the expanded (flattened) selection, so a single selected Group of 2+
    // members counts the same as an ad-hoc 2+ multi-selection.
    private bool IsMultiSelected(Guid instanceId)
    {
        var expanded = ExpandedSelection();
        return expanded.Count > 1 && expanded.Contains(instanceId);
    }

    // Ticket 44: the shared bounding-box overlay (and its resize handles) must show for a
    // selected Group of 2+ members too, not only an ad-hoc multi-selection.
    private bool HasMultiMemberSelection => ExpandedSelection().Count > 1;

    // Ticket 44: an entity id's outermost containing group id, if it has one, else the id itself -
    // shared by SelectComponent (a click) and UpdateMarqueeSelection (a marquee drag), so
    // whichever gesture picks an entity up, selection converges onto its group the same way.
    private Guid EffectiveSelectionId(Guid id) => Board?.FindContainingGroup(id)?.Id ?? id;

    // Ticket 32: a shift-click toggles the clicked instance's membership without disturbing the
    // rest of the selection; a plain click always collapses the selection down to just this one.
    // Ticket 44: clicking any member of a Group selects the whole group instead of just that one
    // instance - selection and group membership converge (ADR 0006).
    // Ticket 50: selecting a component always clears any edge selection - the two slots are
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
            return;
        }

        _selectedInstanceIds.Clear();
        _selectedInstanceIds.Add(effectiveId);
    }

    // Ticket 30: fired once by ComponentContainer's OnMoved, on release - the whole
    // press-to-release drag is one gesture, so Board is only ever mutated with the final Bounds,
    // never per intermediate mousemove tick.
    // Ticket 33: when the dragged instance is part of a 2+ multi-selection, the delta between its
    // own before/after Bounds is applied to every selected member instead of just this one -
    // preserving relative offsets without needing ComponentContainer itself to know anything about
    // multi-selection (its own local drag-tracking is unchanged; only this receiving end differs).
    private void MoveComponent(Guid instanceId, Bounds bounds)
    {
        var instance = Board?.GetComponent(instanceId);
        if (instance is null)
        {
            return;
        }

        if (IsMultiSelected(instanceId))
        {
            CommitGroupMove(bounds.X - instance.Bounds.X, bounds.Y - instance.Bounds.Y);
        }
        else
        {
            _history.Do(new ChangeBoundsCommand(instance, instance.Bounds, bounds));
        }

        StateHasChanged();
    }

    // Shared by MoveComponent (member-drag trigger) and HandleMouseUp (empty-space-in-bbox
    // trigger) - applies the same board-space delta to every selected member in one write.
    // Ticket 37: the whole gesture is one CompositeCommand (ADR 0007), so a single undo reverts
    // every member together rather than one at a time.
    private void CommitGroupMove(double deltaX, double deltaY)
    {
        if (Board is null)
        {
            return;
        }

        var commands = new List<ICommand>();
        foreach (var id in ExpandedSelection())
        {
            var member = Board.GetComponent(id);
            if (member is null)
            {
                continue;
            }

            var before = member.Bounds;
            var after = new Bounds(
                before.X + deltaX,
                before.Y + deltaY,
                before.Width,
                before.Height
            );
            commands.Add(new ChangeBoundsCommand(member, before, after));
        }

        if (commands.Count > 0)
        {
            _history.Do(new CompositeCommand(commands));
        }
    }

    // Ticket 31: same shape as MoveComponent, fired once by ComponentContainer's OnResized. A
    // multi-selected instance never reaches here for resize (its own handles are suppressed while
    // IsMultiSelected - see ComponentContainer), so unlike MoveComponent this needs no group branch.
    private void ResizeComponent(Guid instanceId, Bounds bounds)
    {
        var instance = Board?.GetComponent(instanceId);
        if (instance is null)
        {
            return;
        }

        _history.Do(new ChangeBoundsCommand(instance, instance.Bounds, bounds));
        StateHasChanged();
    }

    // Ticket 48: fired by a ComponentContainer's own port mousedown (OnPortDragStart).
    // _isConnectingPort must flip synchronously, in this same call - a real browser's very next
    // mousemove/mouseup can arrive before an awaited JS round-trip resolves (unlike bUnit's mock,
    // which completes synchronously), and ComponentContainer's own forwarding check would race
    // against it, silently dropping the gesture's start. The container's page position is instead
    // refreshed in the background by RefreshConnectContainerOrigin below - a stale origin only
    // costs a barely-perceptible one-frame offset on the drag-preview's very first paint, self-
    // correcting on the next mousemove, which is a far cheaper price than the race.
    private void StartPortDrag(Guid instanceId, PortDragStartEventArgs args)
    {
        if (Board is null)
        {
            return;
        }

        // Ticket 49: a port that already anchors an edge starts a "reposition this edge's
        // endpoint" gesture instead of creating a new edge - matches common diagramming-tool UX
        // (grabbing a connected point moves the connection; grabbing a bare port starts a new one).
        var attachedEdge = Board.FindEdgeAttachedTo(new PortEndpoint(instanceId, args.PortId));

        _isConnectingPort = true;
        _connectSourceComponentId = instanceId;
        _connectSourcePortId = args.PortId;
        _connectEditingEdgeId = attachedEdge?.EdgeId;
        _connectEditingEdgeIsSource = attachedEdge?.IsSource ?? false;
        _connectCurrentPoint = ToBoardPoint((args.ClientX, args.ClientY), _connectContainerOrigin);
        StateHasChanged();

        _ = RefreshConnectContainerOrigin();
    }

    // Ticket 49: grabbing a floating endpoint's own marker starts the same connector-drag gesture
    // as StartPortDrag, but always originates from an existing Edge - CompletePortDrag mutates
    // that edge's Source/Target in place rather than creating a new one. Unlike a port press
    // (nested inside a ComponentContainer, which needs the mousedown-bubbles-then-gate trick), this
    // marker sits directly on the canvas, so stopping propagation at the marker itself (see the
    // .razor markup) is enough to keep the canvas's own pan/marquee logic from also engaging.
    private void StartFloatingEndpointDrag(Guid edgeId, bool isSource, MouseEventArgs e)
    {
        if (Board is null)
        {
            return;
        }

        _isConnectingPort = true;
        _connectEditingEdgeId = edgeId;
        _connectEditingEdgeIsSource = isSource;
        _connectCurrentPoint = ToBoardPoint(e, _connectContainerOrigin);
        StateHasChanged();

        _ = RefreshConnectContainerOrigin();
    }

    private async Task RefreshConnectContainerOrigin()
    {
        var containerRect = await _jsModule!.InvokeAsync<Dictionary<string, double>>(
            "getContainerDimensions",
            ContainerElement
        );
        _connectContainerOrigin = (containerRect["left"], containerRect["top"]);
    }

    // Ticket 48: called both by this class's own HandleMouseMove (pointer over empty canvas) and
    // by any ComponentContainer forwarding its own mousemove (pointer over an instance's body) -
    // either way, DiagramCanvas is the single owner of the gesture's live preview.
    public void UpdatePortDrag(double clientX, double clientY)
    {
        if (!_isConnectingPort)
        {
            return;
        }

        _connectCurrentPoint = ToBoardPoint((clientX, clientY), _connectContainerOrigin);
        StateHasChanged();
    }

    // Ticket 48/49: resolves the drop point to a port within PortHitRadius, falling back to a
    // FloatingEndpoint at the drop point itself when nothing is within tolerance - so a connector
    // drag always produces a valid endpoint, attached or not. Either creates a brand new Edge
    // (bare-port origin) or writes the resolved endpoint onto whichever side of an existing Edge
    // is being repositioned (ApplyEdgeEndpointEdit).
    public void CompletePortDrag(double clientX, double clientY)
    {
        if (!_isConnectingPort || Board is null)
        {
            CancelPortDrag();
            return;
        }

        var dropPoint = ToBoardPoint((clientX, clientY), _connectContainerOrigin);
        var hitPort = Board.FindPortNear(dropPoint, PortHitRadius);
        IEdgeEndpoint resolved = hitPort is { } port
            ? port
            : new FloatingEndpoint(dropPoint.X, dropPoint.Y);

        if (_connectEditingEdgeId is { } editingEdgeId)
        {
            ApplyEdgeEndpointEdit(editingEdgeId, _connectEditingEdgeIsSource, resolved);
        }
        else
        {
            var source = new PortEndpoint(_connectSourceComponentId, _connectSourcePortId);

            // Dropping back on the exact port the drag started from creates no edge - a real
            // gesture always connects two distinct points.
            // Ticket 50: routed through AddEdgeCommand (ADR 0007) rather than a direct
            // Board.AddEdge call, so undo removes the created edge and redo restores it with the
            // same attachments.
            if (!(resolved is PortEndpoint resolvedPort && resolvedPort == source))
            {
                _history.Do(new AddEdgeCommand(Board, new Edge(source, resolved)));
            }
        }

        CancelPortDrag();
    }

    // Ticket 49: writes the drag's resolved endpoint onto whichever side of the edge is being
    // edited - unless doing so would collapse the edge onto a single point (both ends resolving to
    // the same port, or both left floating at the same coordinate), in which case the edge is left
    // exactly as it was before this drag. IEdgeEndpoint's implementations are records, so structural
    // equality already covers both shapes without a type-specific comparison.
    private void ApplyEdgeEndpointEdit(Guid edgeId, bool editingSource, IEdgeEndpoint resolved)
    {
        var edge = Board!.GetEdge(edgeId);
        if (edge is null)
        {
            return;
        }

        var other = editingSource ? edge.Target : edge.Source;
        if (resolved.Equals(other))
        {
            return;
        }

        if (editingSource)
        {
            edge.Source = resolved;
        }
        else
        {
            edge.Target = resolved;
        }
    }

    private void CancelPortDrag()
    {
        _isConnectingPort = false;
        _connectSourceComponentId = Guid.Empty;
        _connectEditingEdgeId = null;
        _connectEditingEdgeIsSource = false;
        StateHasChanged();
    }

    // Ticket 43: generic commit point for a built-in's own inline WYSIWYG text edit (or any future
    // opaque Props edit) - Sticky Note and Text call this from their own editor on blur, via the
    // ParentCanvas cascading parameter every built-in already has access to. MutateEntityCommand
    // treats Props as opaque (ADR 0007), so this works without DiagramCanvas knowing any TProps
    // shape. The caller is trusted to have already skipped a no-op (unchanged) edit.
    public void CommitPropsChange(Guid instanceId, object before, object after)
    {
        var instance = Board?.GetComponent(instanceId);
        if (instance is null)
        {
            return;
        }

        _history.Do(new MutateEntityCommand(instance, before, after));
        StateHasChanged();
    }

    // Ticket 52: the commit point for a routing-style/arrowhead change on a specific edge - the
    // Edge counterpart to CommitPropsChange. No panel UI calls this yet (ticket 56 is still
    // unbuilt), but the command/undo plumbing is independent of any UI and is exercised directly.
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

    // Ticket 33: armed by one of the group bounding-box overlay's own 8 handles (never an
    // individual instance's handles - those are suppressed while multi-selected). Snapshots each
    // member's own Bounds plus the bbox they currently form, both taken before this gesture flips
    // _isGroupResizing on, so EffectiveBounds still reads raw Board state for that one snapshot.
    private void StartGroupResize(MouseEventArgs e, ResizeDirection direction)
    {
        if (Board is null)
        {
            return;
        }

        var bbox = SelectedInstancesBounds();
        if (bbox is null)
        {
            return;
        }

        _groupResizeMemberStartBounds = ExpandedSelection()
            .ToDictionary(id => id, id => Board.GetComponent(id)!.Bounds);
        _groupResizeStartBounds = bbox.Value;
        _groupResizeCurrentBounds = bbox.Value;
        _groupResizeDirection = direction;
        _groupResizeAnchor = e;
        (_groupResizeMinBboxWidth, _groupResizeMinBboxHeight) = MinBoundingBoxSizeFor(
            bbox.Value,
            _groupResizeMemberStartBounds.Values
        );
        _isGroupResizing = true;
    }

    // The smallest the bbox's width/height can shrink to while every member's own proportionally-
    // scaled size stays at or above ResizeMath's per-instance floor - so a group resize can never
    // shrink an individual member smaller than that same member's own handles ever could (ticket
    // 31's invariant, extended to the group case). Derived per member (width/height independently,
    // since they scale independently) and taken as the most restrictive (largest) requirement
    // across the whole selection.
    private static (double MinWidth, double MinHeight) MinBoundingBoxSizeFor(
        Bounds bbox,
        IEnumerable<Bounds> members
    )
    {
        var minWidth = ResizeMath.DefaultMinWidth;
        var minHeight = ResizeMath.DefaultMinHeight;

        foreach (var member in members)
        {
            if (member.Width > 0)
            {
                minWidth = Math.Max(
                    minWidth,
                    ResizeMath.DefaultMinWidth * bbox.Width / member.Width
                );
            }

            if (member.Height > 0)
            {
                minHeight = Math.Max(
                    minHeight,
                    ResizeMath.DefaultMinHeight * bbox.Height / member.Height
                );
            }
        }

        return (minWidth, minHeight);
    }

    private void ApplyGroupResize(MouseEventArgs e)
    {
        var (deltaX, deltaY) = ScaledDelta(_groupResizeAnchor!, e);
        _groupResizeCurrentBounds = ResizeMath.Apply(
            _groupResizeStartBounds,
            _groupResizeDirection,
            deltaX,
            deltaY,
            _groupResizeMinBboxWidth,
            _groupResizeMinBboxHeight
        );
    }

    // Ticket 37: same one-CompositeCommand-per-gesture treatment as CommitGroupMove.
    private void CommitGroupResize()
    {
        if (Board is null)
        {
            return;
        }

        var commands = new List<ICommand>();
        foreach (var (id, startBounds) in _groupResizeMemberStartBounds)
        {
            var member = Board.GetComponent(id);
            if (member is null)
            {
                continue;
            }

            var after = ScaleWithinBoundingBox(
                startBounds,
                _groupResizeStartBounds,
                _groupResizeCurrentBounds
            );
            commands.Add(new ChangeBoundsCommand(member, startBounds, after));
        }

        if (commands.Count > 0)
        {
            _history.Do(new CompositeCommand(commands));
        }
    }

    // Pan cancels out of a screen-space delta - only the canvas's current zoom scale matters.
    // Same reasoning as ComponentContainer's own ScaledDelta, duplicated rather than shared since
    // that one also accounts for a ParentCanvas cascading parameter DiagramCanvas doesn't need.
    private (double DeltaX, double DeltaY) ScaledDelta(MouseEventArgs from, MouseEventArgs to) =>
        (
            (to.ClientX - from.ClientX) / _zoomPanTracker.Scale,
            (to.ClientY - from.ClientY) / _zoomPanTracker.Scale
        );

    // Bound directly to the canvas background's own click, so it never fires for a click that
    // landed on a ComponentContainer (that element stops the click from propagating here).
    private void HandleCanvasClick()
    {
        if (_dragMoved)
        {
            _dragMoved = false;
            return;
        }

        _selectedInstanceIds.Clear();
        _selectedEdgeId = null;
    }

    // The registered TComponent's props parameter is a fixed contract (ADR 0001 addendum):
    // [Parameter] public TProps Props { get; set; }
    private const string PropsParameterName = "Props";

    private static IDictionary<string, object> GetComponentParameters(ComponentInstance instance) =>
        new Dictionary<string, object> { [PropsParameterName] = instance.Props };

    // Recomputed whenever DiagramCanvas re-renders. That's driven entirely by the pan/zoom/
    // resize events that already call StateHasChanged (throttled for pan, see HandleMouseMove) -
    // never by a per-frame timer - so the mounted window follows the same cadence.
    private IReadOnlyCollection<ComponentInstance> VisibleComponents =>
        Board?.GetVisible(_zoomPanTracker.Viewport, Overscan) ?? Array.Empty<ComponentInstance>();

    private string CanvasCssClass =>
        _isDragOverBoard ? "diagram-canvas drag-over" : "diagram-canvas";

    // Same handler for both events - dragenter and dragover mark the same "still hovering" state.
    private void HandleDragEnterOrOver(DragEventArgs e) => _isDragOverBoard = true;

    private void HandleDragLeave(DragEventArgs e) => _isDragOverBoard = false;

    private string ContentStyle =>
        $"width: {_zoomPanTracker.CanvasWidth}px; height: {_zoomPanTracker.CanvasHeight}px; transform: translate({_zoomPanTracker.PanX}px, {_zoomPanTracker.PanY}px) scale({_zoomPanTracker.Scale});";

    private async Task HandleMouseDown(MouseEventArgs e)
    {
        if (e.Button != 0) // Left mouse button only
        {
            return;
        }

        _dragMoved = false;

        // Fetched fresh rather than reused from first render - same reasoning as HandleDrop: the
        // container can move on the page (scroll, sibling layout changes) between renders.
        var containerRect = await _jsModule!.InvokeAsync<Dictionary<string, double>>(
            "getContainerDimensions",
            ContainerElement
        );
        _marqueeContainerOrigin = (containerRect["left"], containerRect["top"]);
        var boardPoint = ToBoardPoint(e, _marqueeContainerOrigin);

        if (e.ShiftKey)
        {
            _isMarqueeSelecting = true;
            _marqueeAnchor = boardPoint;
            _marqueeCurrent = boardPoint;
            return;
        }

        if (PointIsWithinSelectionBounds(boardPoint))
        {
            // Ticket 33: a drag starting on empty space inside the multi-selection's own combined
            // bounding box moves the whole selection - no pan underneath it, no new marquee.
            _isGroupMoving = true;
            _groupMoveAnchor = boardPoint;
            _groupMoveDeltaX = 0;
            _groupMoveDeltaY = 0;
            return;
        }

        _isPanning = true;
        _panStart = e;
    }

    private void HandleMouseMove(MouseEventArgs e)
    {
        // Ticket 48: reached when the pointer is directly over empty canvas mid-connector-drag
        // (over an instance's own body, ComponentContainer forwards here instead - see
        // UpdatePortDrag's other caller).
        if (_isConnectingPort)
        {
            UpdatePortDrag(e.ClientX, e.ClientY);
            return;
        }

        if (_isMarqueeSelecting)
        {
            _marqueeCurrent = ToBoardPoint(e, _marqueeContainerOrigin);
            _dragMoved = true;
            UpdateMarqueeSelection();
            StateHasChanged();
            return;
        }

        if (_isGroupResizing)
        {
            ApplyGroupResize(e);
            _dragMoved = true;
            StateHasChanged();
            return;
        }

        if (_isGroupMoving)
        {
            var current = ToBoardPoint(e, _marqueeContainerOrigin);
            _groupMoveDeltaX = current.X - _groupMoveAnchor.X;
            _groupMoveDeltaY = current.Y - _groupMoveAnchor.Y;
            _dragMoved = true;
            StateHasChanged();
            return;
        }

        if (_isPanning && _panStart != null)
        {
            var deltaX = e.ClientX - _panStart.ClientX;
            var deltaY = e.ClientY - _panStart.ClientY;

            // Pan state updates every tick so no motion is lost; the render itself is
            // throttled since it's what cascades into re-rendering every mounted child.
            _zoomPanTracker.Pan(deltaX, deltaY);
            _dragMoved = true;
            _panStart = e;

            var now = DateTime.UtcNow;
            if (now - _lastPanRender >= PanRenderInterval)
            {
                _lastPanRender = now;
                StateHasChanged();
            }
        }
    }

    private void HandleMouseUp(MouseEventArgs e)
    {
        // Ticket 48: same reasoning as the top of HandleMouseMove above - a drop landing
        // directly on empty canvas reaches this handler natively.
        if (_isConnectingPort)
        {
            CompletePortDrag(e.ClientX, e.ClientY);
            return;
        }

        // Single-commit gestures (ticket 33): only write to Board once, here, and only if the
        // drag actually moved anything - a plain click that happened to land inside the bbox or on
        // a handle is a no-op, matching every other gesture's press-release-with-no-movement rule.
        if (_isGroupMoving && (_groupMoveDeltaX != 0 || _groupMoveDeltaY != 0))
        {
            CommitGroupMove(_groupMoveDeltaX, _groupMoveDeltaY);
        }

        if (_isGroupResizing && !_groupResizeCurrentBounds.Equals(_groupResizeStartBounds))
        {
            CommitGroupResize();
        }

        _isPanning = false;
        _isMarqueeSelecting = false;
        _isGroupMoving = false;
        _isGroupResizing = false;
        _panStart = null;
        _groupResizeAnchor = null;
        _groupMoveDeltaX = 0;
        _groupMoveDeltaY = 0;
        // Flush so the view can't be left visually behind a throttled final pan tick.
        StateHasChanged();
    }

    // Screen (client) coordinates to board space, given the container's own page position -
    // shared by the marquee gesture, HandleDrop below, and (ticket 48) the connector-drag
    // gesture, whose coordinates arrive as raw doubles rather than a MouseEventArgs when
    // forwarded from a ComponentContainer.
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

    private static Bounds MarqueeBoundsFrom((double X, double Y) a, (double X, double Y) b) =>
        new(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y), Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y));

    // Replaces the selection outright with whatever the marquee currently intersects (ADR 0006:
    // intersection semantics, not full-containment) - not additive, so a marquee drag that ends up
    // over nothing empties the selection, the same as clicking empty canvas.
    // Ticket 44: an intersected instance that belongs to a Group is added by that group's id, not
    // its own - same convergence as a plain click - so _selectedInstanceIds never ends up holding
    // a "naked" grouped member id (which would let a later Ctrl+G create a second, overlapping
    // group over members already grouped).
    private void UpdateMarqueeSelection()
    {
        if (Board is null)
        {
            return;
        }

        var marquee = MarqueeBoundsFrom(_marqueeAnchor, _marqueeCurrent);
        _selectedInstanceIds.Clear();
        _selectedEdgeId = null;
        foreach (var instance in Board.Components)
        {
            if (instance.Bounds.Intersects(marquee))
            {
                _selectedInstanceIds.Add(EffectiveSelectionId(instance.Id));
            }
        }
    }

    // The combined bounding box of the current selection, or null when nothing is selected - used
    // both to keep a plain drag starting there from panning underneath it, and (ticket 33) to
    // position the group bounding-box overlay. Reads through EffectiveBounds rather than each
    // instance's raw Bounds, so it live-tracks during an active group move/resize instead of only
    // updating once the gesture commits.
    private Bounds? SelectedInstancesBounds()
    {
        if (Board is null)
        {
            return null;
        }

        var expanded = ExpandedSelection();
        return Bounds.Union(
            Board
                .Components.Where(instance => expanded.Contains(instance.Id))
                .Select(EffectiveBounds)
        );
    }

    // Ticket 33: an instance's Bounds as they should currently render - offset by the live
    // in-progress group-move delta, or scaled proportionally within an in-progress group-resize.
    // Board itself is never touched until the gesture commits (single-write discipline, matching
    // every other drag), so this is the only place mid-gesture visual feedback comes from.
    private Bounds EffectiveBounds(ComponentInstance instance)
    {
        if (_isGroupMoving && ExpandedSelection().Contains(instance.Id))
        {
            return new Bounds(
                instance.Bounds.X + _groupMoveDeltaX,
                instance.Bounds.Y + _groupMoveDeltaY,
                instance.Bounds.Width,
                instance.Bounds.Height
            );
        }

        if (
            _isGroupResizing
            && _groupResizeMemberStartBounds.TryGetValue(instance.Id, out var startBounds)
        )
        {
            return ScaleWithinBoundingBox(
                startBounds,
                _groupResizeStartBounds,
                _groupResizeCurrentBounds
            );
        }

        return instance.Bounds;
    }

    // A member's start-of-gesture Bounds, re-expressed as the same relative position/size within
    // the bbox's current (possibly live-preview) extent - the core of "resize handles on the
    // selection's bounding box scale all members proportionally".
    private static Bounds ScaleWithinBoundingBox(
        Bounds memberStart,
        Bounds bboxStart,
        Bounds bboxCurrent
    )
    {
        var scaleX = bboxStart.Width > 0 ? bboxCurrent.Width / bboxStart.Width : 1;
        var scaleY = bboxStart.Height > 0 ? bboxCurrent.Height / bboxStart.Height : 1;
        var relativeX = memberStart.X - bboxStart.X;
        var relativeY = memberStart.Y - bboxStart.Y;

        return new Bounds(
            bboxCurrent.X + relativeX * scaleX,
            bboxCurrent.Y + relativeY * scaleY,
            memberStart.Width * scaleX,
            memberStart.Height * scaleY
        );
    }

    // Ticket 33's group-move-as-a-unit only applies once 2+ instances are selected - a lone
    // selected instance's own bounds already are its bounding box, so a drag just outside it (if
    // reachable at all) should pan like before, not move "a group of one".
    private bool PointIsWithinSelectionBounds((double X, double Y) point) =>
        HasMultiMemberSelection
        && (SelectedInstancesBounds()?.Intersects(new Bounds(point.X, point.Y, 0, 0)) ?? false);

    // Ticket 48: an edge's rendered endpoints, resolved fresh from Board on every render - this
    // is what lets an attached edge track its instances through move/resize with no separate
    // update path. Null (skip rendering) if either endpoint's instance no longer exists.
    private ((double X, double Y) From, (double X, double Y) To)? EdgeLine(Edge edge)
    {
        var from = Board?.ResolveEndpoint(edge.Source);
        var to = Board?.ResolveEndpoint(edge.Target);

        return from is null || to is null ? null : (from.Value, to.Value);
    }

    // The in-progress connector drag-preview: from a fixed origin to wherever the pointer
    // currently is in board space. Ticket 49: while repositioning an existing edge's endpoint,
    // the fixed origin is the edge's OTHER endpoint (not the one being dragged) - that edge's own
    // normal line is suppressed for the duration (see IsBeingEdited), so this preview is the only
    // thing representing it. Otherwise (creating a brand new edge) the origin is the bare port
    // the drag started from, same as ticket 48.
    private ((double X, double Y) From, (double X, double Y) To)? ConnectPreviewLine()
    {
        if (!_isConnectingPort || Board is null)
        {
            return null;
        }

        var from = _connectEditingEdgeId is { } editingEdgeId
            ? ResolveOtherEndpoint(editingEdgeId, _connectEditingEdgeIsSource)
            : Board.ResolveEndpoint(
                new PortEndpoint(_connectSourceComponentId, _connectSourcePortId)
            );

        return from is null ? null : (from.Value, _connectCurrentPoint);
    }

    private (double X, double Y)? ResolveOtherEndpoint(Guid edgeId, bool draggingSource)
    {
        var edge = Board!.GetEdge(edgeId);
        if (edge is null)
        {
            return null;
        }

        var other = draggingSource ? edge.Target : edge.Source;
        return Board.ResolveEndpoint(other);
    }

    // Ticket 50: an end user clicks an edge to select it - the edge counterpart to
    // SelectComponent, but kept as its own exclusive slot (see _selectedEdgeId) since edges don't
    // participate in multi-select, grouping, or move/resize as a unit.
    private void SelectEdge(Guid edgeId)
    {
        _selectedInstanceIds.Clear();
        _selectedEdgeId = edgeId;
    }

    private bool IsEdgeSelected(Guid edgeId) => _selectedEdgeId == edgeId;

    private string EdgeLineCssClass(Guid edgeId) =>
        IsEdgeSelected(edgeId) ? "edge-line selected" : "edge-line";

    // Ticket 52: Orthogonal/Curved routing needs an SVG <path> (a <line> can only ever be
    // straight), rendered via a computed `d`. Straight itself stays a plain <line> - see the
    // markup - so every pre-52 test asserting x1/y1/x2/y2 on a default edge is untouched.
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

    // Ticket 52: which <marker> (if any) an edge endpoint's ArrowStyle resolves to - null omits
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

    // Ticket 49: true while the given edge is being repositioned mid-drag (either endpoint) - its
    // normal line is suppressed for the duration in favour of the drag preview, since one <line>
    // element represents both ends together.
    private bool IsBeingEdited(Guid edgeId) => _isConnectingPort && _connectEditingEdgeId == edgeId;

    // Ticket 49: true only while THIS SPECIFIC SIDE of the edge is the one being dragged - unlike
    // IsBeingEdited, this doesn't suppress the untouched side's own floating marker (each marker is
    // independent, so an edge with one attached and one floating end shouldn't hide the floating
    // one while the attached end is what's being re-dragged).
    private bool IsEndpointBeingEdited(Guid edgeId, bool isSource) =>
        IsBeingEdited(edgeId) && _connectEditingEdgeIsSource == isSource;

    // Ticket 49: every persisted (not currently being dragged) floating endpoint across the whole
    // Board, each with a stable render key - an edge can have zero, one, or both ends floating.
    private IEnumerable<(Edge Edge, bool IsSource, double X, double Y)> FloatingEndpoints()
    {
        if (Board is null)
        {
            yield break;
        }

        foreach (var edge in Board.Edges)
        {
            if (edge.Source is FloatingEndpoint source && !IsEndpointBeingEdited(edge.Id, true))
            {
                yield return (edge, true, source.X, source.Y);
            }

            if (edge.Target is FloatingEndpoint target && !IsEndpointBeingEdited(edge.Id, false))
            {
                yield return (edge, false, target.X, target.Y);
            }
        }
    }

    private string MarqueeStyle
    {
        get
        {
            var bounds = MarqueeBoundsFrom(_marqueeAnchor, _marqueeCurrent);
            return $"left: {bounds.X}px; top: {bounds.Y}px; width: {bounds.Width}px; height: {bounds.Height}px;";
        }
    }

    // Ticket 33: positions the group bounding-box overlay - reads through SelectedInstancesBounds,
    // so it live-tracks an in-progress group move/resize the same way the members themselves do.
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

        if (componentTypeKey is null || Board is null)
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

    public void ClickToAdd(string componentTypeKey)
    {
        if (Board is null)
        {
            return;
        }

        var offset = _clickToAddCascadeCount * ClickToAddCascadeStep;
        _clickToAddCascadeCount++;

        var viewport = _zoomPanTracker.Viewport;
        PlaceComponent(
            componentTypeKey,
            viewport.X + viewport.Width / 2 + offset,
            viewport.Y + viewport.Height / 2 + offset
        );

        StateHasChanged();
    }

    // Shared by HandleDrop and ClickToAdd - both place a new instance centered on a board-space
    // point, differing only in how that point is derived. Callers are trusted to have already
    // checked Board is non-null (both do, before computing their center point).
    // Ticket 38: routed through AddEntityCommand (ADR 0007) rather than a direct Board.AddComponent
    // call, so undo removes the placed instance and redo restores it with the same Id.
    private void PlaceComponent(string componentTypeKey, double centerX, double centerY)
    {
        var registration = Registry.Resolve(componentTypeKey);
        var size = registration.DefaultSize ?? new ComponentSize(FallbackWidth, FallbackHeight);

        var instance = new ComponentInstance(
            registration.Key,
            registration.DefaultProps,
            new Bounds(centerX - size.Width / 2, centerY - size.Height / 2, size.Width, size.Height)
        );

        _history.Do(new AddEntityCommand(Board!, instance));
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
        _cleanupFunctions.ForEach(f => f());
        _cleanupFunctions.Clear();

        _zoomPanTracker.Changed -= OnZoomPanChanged;
        _dotNetObjectRef?.Dispose();

        if (_jsModule != null)
        {
            await _jsModule.DisposeAsync();
        }
    }
}
