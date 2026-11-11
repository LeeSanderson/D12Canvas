using System.Globalization;
using D12Canvas.BuiltIns;
using D12Canvas.Model;
using D12Canvas.Registration;
using Microsoft.AspNetCore.Components;

namespace D12Canvas;

public partial class BoardSketch
{
    private Bounds? _extent;
    private IReadOnlyList<(Bounds Bounds, bool IsStickyNote)> _boxes = [];
    private IReadOnlyList<((double X, double Y) From, (double X, double Y) To)> _lines = [];

    [Parameter, EditorRequired]
    public Board Board { get; set; } = null!;

    [Inject]
    private IComponentRegistry Registry { get; set; } = null!;

    protected override void OnParametersSet()
    {
        var stickyNoteKeys = Registry
            .All.Where(registration => registration.ComponentType == typeof(StickyNote))
            .Select(registration => registration.Key)
            .ToHashSet(StringComparer.Ordinal);

        _extent = Bounds.Union(Board.Components.Select(instance => instance.Bounds));
        _boxes = Board
            .Components.OrderBy(instance => instance.ZIndex)
            .Select(instance =>
                (instance.Bounds, stickyNoteKeys.Contains(instance.ComponentTypeKey))
            )
            .ToList();
        _lines = Board
            .Edges.Select(edge => (From: PointOf(edge.Source), To: PointOf(edge.Target)))
            .Where(ends => ends.From is not null && ends.To is not null)
            .Select(ends => (ends.From!.Value, ends.To!.Value))
            .ToList();
    }

    private (double X, double Y)? PointOf(IEdgeEndpoint end) =>
        end is FloatingEndpoint floating ? (floating.X, floating.Y)
        : end.ComponentId is { } id && Board.GetComponent(id) is { } instance
            ? instance.Bounds.PointAtFraction(0.5, 0.5)
        : null;

    private static string Invariant(params double[] values) =>
        string.Join(" ", values.Select(value => value.ToString(CultureInfo.InvariantCulture)));
}
