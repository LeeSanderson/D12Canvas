using D12Canvas.Model;
using Microsoft.AspNetCore.Components;

namespace D12Canvas;

// The minimap's boxes, in board units under the minimap's one transformed wrapper, so a pan or a
// reframe moves the wrapper and never re-renders a box. They render again only for a new board or
// a new board revision.
public partial class MinimapBoxes
{
    private (Board? Board, int Revision) _rendered;

    [Parameter]
    public Board? Board { get; set; }

    [Parameter]
    public int Revision { get; set; }

    protected override void OnInitialized() => _rendered = (Board, Revision);

    protected override bool ShouldRender()
    {
        if (_rendered == (Board, Revision))
        {
            return false;
        }

        _rendered = (Board, Revision);
        return true;
    }

    private static string BoxStyle(Bounds bounds) =>
        FormattableString.Invariant(
            $"left: {bounds.X}px; top: {bounds.Y}px; width: {bounds.Width}px; height: {bounds.Height}px"
        );
}
