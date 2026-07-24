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

    [Parameter]
    public Board? Board { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter]
    public EventCallback<ZoomPanChangedEventArgs> OnZoomOrPanChanged { get; set; }
    public event EventHandler<ZoomPanChangedEventArgs>? ZoomOrPanChanged;

    private readonly ZoomPanTracker _zoomPanTracker = new ZoomPanTracker();

    // Make ZoomPanTracker accessible to child components
    public ZoomPanTracker ZoomPanTracker => _zoomPanTracker;
    private bool _isPanning = false;
    private MouseEventArgs? _panStart;
    private DateTime _lastPanRender = DateTime.MinValue;
    private static readonly TimeSpan PanRenderInterval = TimeSpan.FromMilliseconds(16); // ~60fps cap
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

    // The registered TComponent's props parameter is a fixed contract (ADR 0001 addendum):
    // [Parameter] public TProps Props { get; set; }
    private const string PropsParameterName = "Props";

    private static IDictionary<string, object> GetComponentParameters(ComponentInstance instance) =>
        new Dictionary<string, object> { [PropsParameterName] = instance.Props };

    private string ContentStyle =>
        $"width: {_zoomPanTracker.CanvasWidth}px; height: {_zoomPanTracker.CanvasHeight}px; transform: translate({_zoomPanTracker.PanX}px, {_zoomPanTracker.PanY}px) scale({_zoomPanTracker.Scale});";

    private void HandleMouseDown(MouseEventArgs e)
    {
        if (e.Button == 0) // Left mouse button
        {
            _isPanning = true;
            _panStart = e;
        }
    }

    private void HandleMouseMove(MouseEventArgs e)
    {
        if (_isPanning && _panStart != null)
        {
            var deltaX = e.ClientX - _panStart.ClientX;
            var deltaY = e.ClientY - _panStart.ClientY;

            // Pan state updates every tick so no motion is lost; the render itself is
            // throttled since it's what cascades into re-rendering every mounted child.
            _zoomPanTracker.Pan(deltaX, deltaY);
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
        _isPanning = false;
        _panStart = null;
        // Flush so the view can't be left visually behind a throttled final tick.
        StateHasChanged();
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
