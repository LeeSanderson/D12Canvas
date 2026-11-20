using System.Diagnostics.CodeAnalysis;
using D12Canvas.Model;
using Microsoft.JSInterop;

namespace D12Canvas;

public partial class DiagramCanvas
{
    private static readonly TimeSpan FramingFlightDuration = TimeSpan.FromMilliseconds(250);

    private bool _applyingFlight;

    private bool _inFlight;

    private bool _initialFitPending;

    [JSInvokable]
    public void ZoomToFit() => FlyTo(Board?.ContentExtent());

    [JSInvokable]
    public void ZoomToSelection() => FlyTo(Board?.ExtentOf(ResolvedSelection(), SelectedEdges));

    [JSInvokable]
    public void ZoomTo100Percent()
    {
        if (_zoomPanTracker.HasKnownContainerSize && !PlacingPort)
        {
            Fly(
                () =>
                    _zoomPanTracker.SetScaleAbout(
                        _zoomPanTracker.ContainerWidth / 2,
                        _zoomPanTracker.ContainerHeight / 2,
                        1.0
                    )
            );
        }
    }

    private void FlyTo(Bounds? target)
    {
        if (target is { } bounds && !PlacingPort)
        {
            Fly(() => _zoomPanTracker.Frame(bounds));
        }
    }

    private void Fly(Func<bool> moveViewport)
    {
        _applyingFlight = true;
        try
        {
            if (moveViewport())
            {
                _ambientTransition = FramingFlightDuration;
                _inFlight = true;
                StateHasChanged();
            }
        }
        finally
        {
            _applyingFlight = false;
        }
    }

    [MemberNotNullWhen(true, nameof(Board))]
    private bool BoardShown => Board is not null && _zoomPanTracker.HasKnownContainerSize;

    private void FitNewBoard()
    {
        if (!_initialFitPending || Board is null || !_zoomPanTracker.HasKnownContainerSize)
        {
            return;
        }

        _initialFitPending = false;
        if (Board.ContentExtent() is { } extent)
        {
            _zoomPanTracker.Frame(extent);
        }
    }
}
