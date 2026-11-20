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

    public const double DefaultOverscan = 200;

    [Parameter]
    public Board? Board { get; set; }

    [Parameter]
    public double Overscan { get; set; } = DefaultOverscan;

    [Parameter]
    public double? MinZoom { get; set; }

    [Parameter]
    public double? MaxZoom { get; set; }

    public const double DefaultLodSizeThreshold = 32;

    [Parameter]
    public double LodSizeThreshold { get; set; } = DefaultLodSizeThreshold;

    [Parameter]
    public bool SnapToGrid { get; set; } = true;

    [Parameter]
    public EventCallback<bool> SnapToGridChanged { get; set; }

    [Parameter]
    public bool ObjectSnapping { get; set; }

    [Parameter]
    public EventCallback<bool> ObjectSnappingChanged { get; set; }

    [Parameter]
    public bool EnableSnapToGridShortcut { get; set; } = true;

    [Parameter]
    public WheelDeviceProfile WheelDeviceProfile { get; set; } = WheelDeviceProfile.Auto;

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter]
    public EventCallback<ZoomPanChangedEventArgs> OnZoomOrPanChanged { get; set; }
    public event EventHandler<ZoomPanChangedEventArgs>? ZoomOrPanChanged;

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

    public IReadOnlyList<ComponentInstance> SelectedComponents => ResolvedSelection();

    public IReadOnlyList<Edge> SelectedEdges =>
        Board is null
            ? Array.Empty<Edge>()
            : _selectedEdgeIds.Select(Board.GetEdge).OfType<Edge>().ToList();

    private string? _pendingPaletteDragKey;
    private bool _isDragOverBoard;

    internal const string EdgePaletteKey = "__d12-edge__";

    public void BeginPaletteDrag(string componentTypeKey) =>
        _pendingPaletteDragKey = componentTypeKey;

    private const double ClickToAddCascadeStep = 20;
    private int _clickToAddCascadeCount;

    private readonly HashSet<Guid> _selectedInstanceIds = new();
    private readonly HashSet<Guid> _selectedEdgeIds = new();

    private readonly List<Guid> _enteredGroupIds = new();

    private Guid? EnteredGroupId => _enteredGroupIds.Count > 0 ? _enteredGroupIds[^1] : null;

    private sealed record ContextMenuState(
        double X,
        double Y,
        ContextMenuSet Set,
        bool OpenedFromKeyboard = false,
        PortAtPress? PortAtPress = null,
        CustomPortEndpoint? PortToRemove = null
    );

    private bool _applePlatform;
    private Dictionary<string, string>? _keyLabels;

    private ContextMenuState? _contextMenu;

    private readonly CommandHistory _history = new();

    private readonly ZoomPanTracker _zoomPanTracker = new ZoomPanTracker();

    public ZoomPanTracker ZoomPanTracker => _zoomPanTracker;

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

    private readonly Dictionary<Guid, bool> _stickyParticipants = new();

    private readonly Dictionary<Guid, DynamicComponent> _mountedComponents = new();

    private readonly Dictionary<Guid, DynamicComponent> _mountedLabels = new();

    private LiveGeometry Live => new(Board!, _preview);

    private Guid? _portFocusInstanceId;
    private IEdgeEndpoint _portFocusEndpoint = new PortEndpoint(Guid.Empty, PortId.Top);
    private IEdgeEndpoint? _pendingConnectorSource;

    private ElementReference ContainerElement;
    private ElementReference CanvasElement;
    private DotNetObjectReference<DiagramCanvas>? _dotNetObjectRef;
    private List<IJSObjectReference> _cleanupHandles = new List<IJSObjectReference>();
    private IJSObjectReference? _jsModule;
    private IJSObjectReference? _pointerListener;

    private IJSObjectReference? _pressListener;

    private TimeSpan _ambientTransition = TimeSpan.Zero;
    private bool _applyingWheel;
    private (double Scale, double PanX, double PanY) _lastTransform = (1, 0, 0);

    private bool _pendingGroupFocus;

    private Guid? _pendingFocusId;

    private Guid? _focusedTabStopId;

    private bool _additiveTraversal;

    protected override void OnInitialized()
    {
        _zoomPanTracker.Changed += OnZoomPanChanged;
        _history.Changed += OnHistoryChanged;
        _dotNetObjectRef = DotNetObjectReference.Create(this);
    }

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

    private bool PressOwnsBoard => _activeGesture is not null;

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

    private const double NudgeStep = 1;
    private const double NudgeStepCoarse = 10;
    private const double PanStep = 50;

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

    private void PanFor(string code)
    {
        var (dirX, dirY) = ArrowDirection(code);
        _zoomPanTracker.Pan(-dirX * PanStep, -dirY * PanStep);
        StateHasChanged();
    }

    private const double ResizeStep = 1;

    private ResizeStepCommand? _activeResizeCommand;

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

    private static ResizeDirection? ResizeDirectionFor(string code, bool shiftKey) =>
        code switch
        {
            "ArrowRight" => shiftKey ? ResizeDirection.Left : ResizeDirection.Right,
            "ArrowLeft" => shiftKey ? ResizeDirection.Right : ResizeDirection.Left,
            "ArrowDown" => shiftKey ? ResizeDirection.Top : ResizeDirection.Bottom,
            "ArrowUp" => shiftKey ? ResizeDirection.Bottom : ResizeDirection.Top,
            _ => null,
        };

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

    private void KeepSurvivingSelection() =>
        SetSelection(
            _selectedInstanceIds.Where(id =>
                Board?.GetComponent(id) is not null || Board?.GetGroup(id) is not null
            ),
            _selectedEdgeIds.Where(id => Board?.GetEdge(id) is not null)
        );

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

    public void OnToggleObjectSnappingPressed()
    {
        ObjectSnapping = !ObjectSnapping;
        ObjectSnappingChanged.InvokeAsync(ObjectSnapping);
        StateHasChanged();
    }

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

    private HashSet<Guid> ShownSelection() =>
        PendingCopies is { } copies
            ? copies.Components.Select(copy => copy.Id).ToHashSet()
            : ExpandedSelection();

    private bool IsGroupSelected(Guid groupId) =>
        PendingCopies is null && _selectedInstanceIds.Contains(groupId);

    private bool IsMultiSelected(Guid instanceId)
    {
        var expanded = ShownSelection();
        return expanded.Count > 1 && expanded.Contains(instanceId);
    }

    private bool HasMultiMemberSelection => ShownSelection().Count > 1;

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

    private bool HasHitRegion(Guid entityId) =>
        (Board?.GetComponent(entityId) is not null || Board?.GetEdge(entityId) is not null)
        && !IsLocked(entityId);

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

    private bool IsAddressable(Guid id) =>
        Board?.FindParentGroup(id) is not { } parent || parent.Id == EnteredGroupId;

    private void EnterGroup(Guid groupId)
    {
        if (Board?.GetGroup(groupId) is not null && IsAddressable(groupId))
        {
            _enteredGroupIds.Add(groupId);
        }
    }

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

    private void ResolveSelectionToScope() =>
        SetSelection(
            _selectedInstanceIds.Select(EffectiveSelectionId).Distinct(),
            _selectedEdgeIds
        );

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

    private ComponentInstance? ResolvePropsEntity(Guid id) =>
        IsPropsEntityLocked(id) ? null : Board?.GetComponent(id) ?? Board?.FindEdgeLabel(id);

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

    private const string DefaultEdgeLabelComponentTypeKey = "text";

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

    private const string PropsParameterName = "Props";

    private readonly AssetReferenceResolver _assetReferenceResolver = new();

    private object BoundProps(ComponentInstance instance, ComponentRegistration registration) =>
        _assetReferenceResolver.Resolve(instance, registration, Board!);

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

    private bool IsBelowLodThreshold(Bounds bounds) =>
        Math.Max(bounds.Width, bounds.Height) * _zoomPanTracker.Scale < LodSizeThreshold;

    private bool IsPlaceholder(ComponentInstance instance) =>
        _stickyParticipants.TryGetValue(instance.Id, out var frozen)
            ? frozen
            : IsBelowLodThreshold(instance.Bounds);

    private static string LodPlaceholderStyle(Bounds bounds, int zIndex) =>
        $"left: {bounds.X}px; top: {bounds.Y}px; width: {bounds.Width}px; height: {bounds.Height}px; z-index: {zIndex};";

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

    private bool HasGroupTabStop(Group group) =>
        IsAddressable(group.Id) && !_enteredGroupIds.Contains(group.Id);

    private List<Guid> FocusableTabStopIds() =>
        FocusableTabStops().Select(stop => stop.Id).ToList();

    private List<TabStop> FocusableTabStops() =>
        OrderedTabStops()
            .Where(stop =>
                stop.Instance is null
                || (IsAddressable(stop.Instance.Id) && !IsPlaceholder(stop.Instance))
            )
            .ToList();

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

    private void FocusEntity(Guid id)
    {
        EndPortPlacementOnFocus(id);
        _focusedTabStopId = id;

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

    internal const double GridBaseSpacing = 20;
    private const int GridSpacingStep = 10;

    private readonly record struct GridLayer(int Level, double Opacity);

    private (int LowerLevel, double UpperWeight) GridLevelSplit()
    {
        var level = -Math.Log10(_zoomPanTracker.Scale);
        var lowerLevel = (int)Math.Floor(level);
        return (lowerLevel, level - lowerLevel);
    }

    private IEnumerable<GridLayer> VisibleGridLayers()
    {
        var (lowerLevel, upperWeight) = GridLevelSplit();

        yield return new GridLayer(lowerLevel, 1 - upperWeight);
        if (upperWeight > 0)
        {
            yield return new GridLayer(lowerLevel + 1, upperWeight);
        }
    }

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

    private EdgeRoute? RouteOf(Edge edge)
    {
        if (Board is null || Live.RouteRequestOf(edge) is not { } request)
        {
            return null;
        }

        return _routes.RouteFor(edge.Id, request);
    }

    internal int RoutesComputed => _routes.RoutesComputed;

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

    public void ClickToAdd(string componentTypeKey)
    {
        if (Board is null || PressOwnsBoard)
        {
            return;
        }

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

    private ComponentInstance? PlaceComponent(
        string componentTypeKey,
        double centerX,
        double centerY
    )
    {
        if (componentTypeKey == EdgePaletteKey)
        {
            PlaceConnector(centerX, centerY);
            return null;
        }

        var instance = NewCenteredInstance(componentTypeKey, centerX, centerY);
        RecordCreation(instance.Id, new AddEntityCommand(Board!, instance));
        return instance;
    }

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
