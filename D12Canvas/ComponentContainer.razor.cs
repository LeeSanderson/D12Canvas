using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace D12Canvas;

public partial class ComponentContainer : IAsyncDisposable
{
    [Inject]
    private IJSRuntime JavaScriptRuntime { get; set; } = null!;

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter]
    public double X { get; set; }

    [Parameter]
    public double Y { get; set; }

    [Parameter]
    public double Width { get; set; } = 200;

    [Parameter]
    public double Height { get; set; } = 150;

    [Parameter]
    public bool InitialEditMode { get; set; }

    [Parameter]
    public EventCallback<ComponentContainerStateChangedEventArgs> OnStateChanged { get; set; }

    [CascadingParameter(Name = "ParentCanvas")]
    private DiagramCanvas? ParentCanvas { get; set; }

    private bool _editMode;
    private bool _isDragging;
    private bool _isResizing;
    private ResizeDirection _currentResizeDirection;
    private MouseEventArgs? _dragStart;
    private double _startX;
    private double _startY;
    private double _startWidth;
    private double _startHeight;
    private ElementReference _containerRef;
    private DotNetObjectReference<ComponentContainer>? _dotNetRef;
    private IJSObjectReference? _jsModule;

    private string ContainerStyle =>
        $"left: {X}px; top: {Y}px; width: {Width}px; height: {Height}px;";

    protected override void OnInitialized()
    {
        _editMode = InitialEditMode;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _jsModule = await JavaScriptRuntime.InvokeAsync<IJSObjectReference>(
                "import",
                "./js/componentContainer.js"
            );
            _dotNetRef = DotNetObjectReference.Create(this);
        }
    }

    private void HandleMouseDown(MouseEventArgs e)
    {
        if (!_editMode)
            return;

        if (!_isResizing)
        {
            _isDragging = true;
            _dragStart = e;
            _startX = X;
            _startY = Y;
        }
    }

    private void HandleMouseMove(MouseEventArgs e)
    {
        if (!_editMode)
            return;

        if (_isDragging && _dragStart != null)
        {
            double deltaX = e.ClientX - _dragStart.ClientX;
            double deltaY = e.ClientY - _dragStart.ClientY;

            if (ParentCanvas != null)
            {
                // Apply canvas zoom scale
                deltaX /= ParentCanvas.ZoomPanTracker.Scale;
                deltaY /= ParentCanvas.ZoomPanTracker.Scale;
            }

            X = _startX + deltaX;
            Y = _startY + deltaY;

            NotifyStateChanged();
        }
        else if (_isResizing && _dragStart != null)
        {
            double deltaX = e.ClientX - _dragStart.ClientX;
            double deltaY = e.ClientY - _dragStart.ClientY;

            if (ParentCanvas != null)
            {
                // Apply canvas zoom scale
                deltaX /= ParentCanvas.ZoomPanTracker.Scale;
                deltaY /= ParentCanvas.ZoomPanTracker.Scale;
            }

            ApplyResize(deltaX, deltaY);
            NotifyStateChanged();
        }
    }

    private void HandleMouseUp(MouseEventArgs e)
    {
        _isDragging = false;
        _isResizing = false;
        _dragStart = null;
    }

    private void StartResize(MouseEventArgs e, ResizeDirection direction)
    {
        _isResizing = true;
        _currentResizeDirection = direction;
        _dragStart = e;
        _startX = X;
        _startY = Y;
        _startWidth = Width;
        _startHeight = Height;

        // Stop the event from propagating to prevent dragging
        // e.StopPropagation();
    }

    private void ApplyResize(double deltaX, double deltaY)
    {
        const double minWidth = 50;
        const double minHeight = 50;

        switch (_currentResizeDirection)
        {
            case ResizeDirection.TopLeft:
                double newWidthTL = Math.Max(_startWidth - deltaX, minWidth);
                double newHeightTL = Math.Max(_startHeight - deltaY, minHeight);
                X = _startX + (_startWidth - newWidthTL);
                Y = _startY + (_startHeight - newHeightTL);
                Width = newWidthTL;
                Height = newHeightTL;
                break;

            case ResizeDirection.Top:
                double newHeightT = Math.Max(_startHeight - deltaY, minHeight);
                Y = _startY + (_startHeight - newHeightT);
                Height = newHeightT;
                break;

            case ResizeDirection.TopRight:
                Width = Math.Max(_startWidth + deltaX, minWidth);
                double newHeightTR = Math.Max(_startHeight - deltaY, minHeight);
                Y = _startY + (_startHeight - newHeightTR);
                Height = newHeightTR;
                break;

            case ResizeDirection.Right:
                Width = Math.Max(_startWidth + deltaX, minWidth);
                break;

            case ResizeDirection.BottomRight:
                Width = Math.Max(_startWidth + deltaX, minWidth);
                Height = Math.Max(_startHeight + deltaY, minHeight);
                break;

            case ResizeDirection.Bottom:
                Height = Math.Max(_startHeight + deltaY, minHeight);
                break;

            case ResizeDirection.BottomLeft:
                double newWidthBL = Math.Max(_startWidth - deltaX, minWidth);
                X = _startX + (_startWidth - newWidthBL);
                Width = newWidthBL;
                Height = Math.Max(_startHeight + deltaY, minHeight);
                break;

            case ResizeDirection.Left:
                double newWidthL = Math.Max(_startWidth - deltaX, minWidth);
                X = _startX + (_startWidth - newWidthL);
                Width = newWidthL;
                break;
        }
    }

    private void SwitchToEditMode()
    {
        _editMode = true;
        StateHasChanged();
        RegisterClickOutsideHandler();
    }

    private void ExitEditMode()
    {
        if (_editMode)
        {
            _editMode = false;
            StateHasChanged();
            UnregisterClickOutsideHandler();
        }
    }

    [JSInvokable]
    public void OnClickOutside()
    {
        ExitEditMode();
    }

    private async void RegisterClickOutsideHandler()
    {
        if (_jsModule != null && _editMode)
        {
            await _jsModule.InvokeVoidAsync("registerClickOutside", _containerRef, _dotNetRef);
        }
    }

    private async void UnregisterClickOutsideHandler()
    {
        if (_jsModule != null)
        {
            await _jsModule.InvokeVoidAsync("unregisterClickOutside");
        }
    }

    public async ValueTask DisposeAsync()
    {
        UnregisterClickOutsideHandler();
        _dotNetRef?.Dispose();
        if (_jsModule != null)
        {
            await _jsModule.DisposeAsync();
        }
    }

    private void NotifyStateChanged()
    {
        OnStateChanged.InvokeAsync(
            new ComponentContainerStateChangedEventArgs
            {
                X = X,
                Y = Y,
                Width = Width,
                Height = Height,
                IsEditMode = _editMode,
            }
        );
    }
}

public enum ResizeDirection
{
    TopLeft,
    Top,
    TopRight,
    Right,
    BottomRight,
    Bottom,
    BottomLeft,
    Left,
}

public class ComponentContainerStateChangedEventArgs : EventArgs
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public bool IsEditMode { get; set; }
}
