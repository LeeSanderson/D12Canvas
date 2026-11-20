using D12Canvas.Pointer;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace D12Canvas;

public partial class DiagramCanvas
{
    internal ElementReference MinimapFocusTarget => CanvasElement;

    internal bool BeginMinimapPan(
        PointerPress press,
        Func<double, double, (double X, double Y)> toBoardPoint,
        IJSObjectReference? listener
    )
    {
        if (_activeGesture is not null)
        {
            return false;
        }

        if (_portPlacement is not null)
        {
            EndPortPlacement();
        }

        _additiveTraversal = false;
        var scope = CurrentScope();
        var gesture = new MinimapPanGesture(
            press,
            new CanvasGestureContext(this, scope.Selection),
            toBoardPoint
        );
        gesture.Begin();
        Hold(gesture, scope, listener);
        StateHasChanged();
        return true;
    }

    private void CentreViewportOn(double boardX, double boardY, bool animated)
    {
        bool Centre() =>
            _zoomPanTracker.SetPanPosition(
                _zoomPanTracker.ContainerWidth / 2 - boardX * _zoomPanTracker.Scale,
                _zoomPanTracker.ContainerHeight / 2 - boardY * _zoomPanTracker.Scale
            );

        if (animated)
        {
            Fly(Centre);
        }
        else
        {
            Centre();
        }
    }
}
