using D12Canvas.Model;
using Microsoft.AspNetCore.Components;

namespace D12Canvas;

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

    [Parameter]
    public string? Color { get; set; }

    [Parameter]
    public bool Locked { get; set; }

    private Drawn _drawn;

    private string? ColorOverride =>
        InlineStyleValue.Safe(Color) is { } color ? $"--d12-edge-override: {color}" : null;

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
        new(
            EdgeId,
            RoutingStyle,
            From,
            To,
            PathData,
            IsSelected,
            SourceArrow,
            TargetArrow,
            Color,
            Locked
        );

    private static string? MarkerUrl(ArrowStyle arrow) =>
        arrow == ArrowStyle.None ? null : "url(#edge-arrow)";

    private readonly record struct Drawn(
        Guid EdgeId,
        EdgeRouting RoutingStyle,
        (double X, double Y) From,
        (double X, double Y) To,
        string PathData,
        bool IsSelected,
        ArrowStyle SourceArrow,
        ArrowStyle TargetArrow,
        string? Color,
        bool Locked
    );
}
