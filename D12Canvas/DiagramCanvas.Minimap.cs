using D12Canvas.Pointer;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace D12Canvas;

// The canvas owns the minimap's press as it owns every other: the minimap's listener reports it
// here, and its moves, release and cancel arrive through the same entry points a press on the
// board uses. The minimap's press never steps out of a group or touches the selection.
public partial class DiagramCanvas
{
    // Where a press on the minimap sends focus, so Escape reaches the canvas while it pans.
    internal ElementReference MinimapFocusTarget => CanvasElement;

    // False when another press already owns the pointer, in which case the minimap's press is
    // dropped as any second press is.
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
        var snapshot = new SelectionSnapshot(_selectedInstanceIds, _selectedEdgeIds);
        var gesture = new MinimapPanGesture(
            press,
            new CanvasGestureContext(this, snapshot),
            toBoardPoint
        );
        gesture.Begin();
        Hold(gesture, snapshot, listener);
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
