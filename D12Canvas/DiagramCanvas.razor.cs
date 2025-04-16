using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace D12Canvas;

public partial class DiagramCanvas
{
    [Inject]
    private IJSRuntime JS { get; set; } = null!;

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter]
    public EventCallback<ZoomPanChangedEventArgs> OnZoomOrPanChanged { get; set; }
    public event EventHandler<ZoomPanChangedEventArgs>? ZoomOrPanChanged;

    private ZoomPanTracker _zoomPanTracker = new ZoomPanTracker();
    private bool _isPanning = false;
    private MouseEventArgs? _panStart;
    private ElementReference ContainerElement;
    private DotNetObjectReference<DiagramCanvas>? _dotNetObjectRef;
    private List<Action> _cleanupFunctions = new List<Action>();

    protected override void OnInitialized()
    {
        _zoomPanTracker.Changed += OnZoomPanChanged;
        _dotNetObjectRef = DotNetObjectReference.Create(this);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            var dimensions = await JS.InvokeAsync<Dictionary<string, double>>(
                "DiagramCanvas.getContainerDimensions",
                ContainerElement
            );

            _zoomPanTracker.SetContainerSize((int)dimensions["width"], (int)dimensions["height"]);
            _zoomPanTracker.SetCanvasSize(3000, 3000);

            // Set up resize listener
            var resizeCleanup = await JS.InvokeAsync<Action>(
                "DiagramCanvas.addResizeListener",
                ContainerElement,
                _dotNetObjectRef
            );

            // Set up keyboard listener
            var keyboardCleanup = await JS.InvokeAsync<Action>(
                "DiagramCanvas.addKeyboardListener",
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

            _zoomPanTracker.Pan(deltaX, deltaY);

            _panStart = e;
            StateHasChanged();
        }
    }

    private void HandleMouseUp(MouseEventArgs e)
    {
        _isPanning = false;
        _panStart = null;
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

    public void Dispose()
    {
        _cleanupFunctions.ForEach(f => f());
        _cleanupFunctions.Clear();

        _zoomPanTracker.Changed -= OnZoomPanChanged;
        _dotNetObjectRef?.Dispose();
    }
}
