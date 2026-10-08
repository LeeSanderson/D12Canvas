using D12Canvas.Model;
using Microsoft.AspNetCore.Components;

namespace D12Canvas;

// One edge's line and its wider hit stroke. The canvas re-renders every edge's parameters on each
// of its own renders, so this skips its own render unless something it draws has changed: a pan
// or a drag of an unrelated shape costs one parameter set here and no diff.
public partial class EdgeView
{
    [Parameter]
    public Guid EdgeId { get; set; }

    [Parameter]
    public EdgeRouting RoutingStyle { get; set; }

    [Parameter]
    public (double X, double Y) From { get; set; }

    [Parameter]
    public (double X, double Y) To { get; set; }

    [Parameter]
    public string PathData { get; set; } = "";

    [Parameter]
    public bool IsSelected { get; set; }

    [Parameter]
    public ArrowStyle SourceArrow { get; set; }

    [Parameter]
    public ArrowStyle TargetArrow { get; set; }

    private Drawn _drawn;

    private string LineCssClass => IsSelected ? "edge-line selected" : "edge-line";

    protected override void OnInitialized() => _drawn = Current();

    protected override bool ShouldRender()
    {
        var current = Current();
        if (current == _drawn)
        {
            return false;
        }

        _drawn = current;
        return true;
    }

    private Drawn Current() =>
        new(EdgeId, RoutingStyle, From, To, PathData, IsSelected, SourceArrow, TargetArrow);

    // Selected edges use the selected-colour marker so an arrowhead never mismatches its line.
    private string? MarkerUrl(ArrowStyle arrow) =>
        arrow == ArrowStyle.None ? null
        : IsSelected ? "url(#edge-arrow-selected)"
        : "url(#edge-arrow)";

    private readonly record struct Drawn(
        Guid EdgeId,
        EdgeRouting RoutingStyle,
        (double X, double Y) From,
        (double X, double Y) To,
        string PathData,
        bool IsSelected,
        ArrowStyle SourceArrow,
        ArrowStyle TargetArrow
    );
}
