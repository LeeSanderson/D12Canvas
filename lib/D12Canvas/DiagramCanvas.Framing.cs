using System.Diagnostics.CodeAnalysis;
using D12Canvas.Model;
using Microsoft.JSInterop;

namespace D12Canvas;

// The three viewport commands and the initial fit. Each command moves the viewport in one tracker
// change and the browser animates the content's transform to it as a framing flight; nothing here
// knows about time. The listener suppresses pointer events on the container for as long as the
// flight's transition runs, unless a press is held.
public partial class DiagramCanvas
{
    private static readonly TimeSpan FramingFlightDuration = TimeSpan.FromMilliseconds(250);

    private bool _applyingFlight;

    // Whether the last transform write was a framing flight, which marks the content element so
    // the listener can tell its transition from the wheel's.
    private bool _inFlight;

    // Set when a new Board arrives and spent on the first fit attempt once the container size is
    // known, so an unrelated re-render never yanks the view back.
    private bool _initialFitPending;

    [JSInvokable]
    public void ZoomToFit() => FlyTo(Board?.ContentExtent());

    // Frames the selection, edges included. Does nothing with nothing selected, and never falls
    // back to framing everything.
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

    // Port placement owns the keyboard until it ends, and the viewport stays where it is.
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

    // The board is drawn only once the container has been measured, which is also when the
    // initial fit lands, so it never shows first at a view the fit is about to replace.
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
