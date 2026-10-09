using D12Canvas.BuiltIns;
using D12Canvas.History;
using D12Canvas.Model;
using D12Canvas.Panel;
using D12Canvas.Registration;
using Xunit;

namespace D12Canvas.Tests;

public class PropertyBarRowsTests
{
    private readonly IComponentRegistry _registry;
    private readonly List<
        IReadOnlyList<(Guid InstanceId, object Before, object After)>
    > _propsCommits = [];
    private readonly List<
        IReadOnlyList<(Guid EdgeId, EdgeStyle Before, EdgeStyle After)>
    > _edgeCommits = [];

    public PropertyBarRowsTests()
    {
        var options = new D12CanvasOptions();
        BuiltInComponents.RegisterAll(options);
        _registry = options.Registry;
    }

    private static ComponentInstance Rectangle(string? fill = "#ff0000", double strokeWidth = 2) =>
        new("rectangle", new RectangleProps(fill, null, strokeWidth), new Bounds(0, 0, 100, 60));

    private static ComponentInstance StickyNote(string color = "#FFEB3B") =>
        new(
            "sticky-note",
            new StickyNoteProps("", color, "#000000", 14),
            new Bounds(0, 0, 100, 60)
        );

    private static Edge Edge(
        EdgeRouting routing = EdgeRouting.Orthogonal,
        ArrowStyle targetArrow = ArrowStyle.Arrow,
        string? color = null
    ) =>
        new(
            new FloatingEndpoint(0, 0),
            new FloatingEndpoint(100, 0),
            routingStyle: routing,
            targetArrow: targetArrow,
            color: color
        );

    private IReadOnlyList<PropertyBarRow> RowsFor(
        IReadOnlyList<ComponentInstance> instances,
        IReadOnlyList<Edge> edges
    ) => PropertyBarRows.For(instances, edges, _registry, _propsCommits.Add, _edgeCommits.Add);

    private static PropertyBarRow RowOf(IReadOnlyList<PropertyBarRow> rows, PropertyRole role) =>
        Assert.Single(rows, row => row.Role == role);

    [Fact]
    public void ARectangleOffersItsFillStrokeAndStrokeWidthInRoleOrder()
    {
        var rows = RowsFor([Rectangle()], []);

        Assert.Equal(
            [PropertyRole.Fill, PropertyRole.Stroke, PropertyRole.StrokeWidth],
            rows.Select(row => row.Role)
        );
        Assert.Equal("#ff0000", RowOf(rows, PropertyRole.Fill).Value);
    }

    [Fact]
    public void AcrossTypesOnlyTheRolesEveryTypeDeclaresAreOffered()
    {
        var rows = RowsFor([Rectangle(), StickyNote()], []);

        Assert.Equal([PropertyRole.Fill], rows.Select(row => row.Role));
    }

    [Fact]
    public void AnEdgeOffersRoutingBothArrowsAndColour()
    {
        var rows = RowsFor([], [Edge()]);

        Assert.Equal(
            [
                PropertyRole.EdgeRouting,
                PropertyRole.EdgeSourceArrow,
                PropertyRole.EdgeTargetArrow,
                PropertyRole.EdgeColour,
            ],
            rows.Select(row => row.Role)
        );
        Assert.Equal(EdgeRouting.Orthogonal, RowOf(rows, PropertyRole.EdgeRouting).Value);
        Assert.Equal(
            ["Straight", "Orthogonal", "Curved"],
            RowOf(rows, PropertyRole.EdgeRouting).Options
        );
        Assert.True(RowOf(rows, PropertyRole.EdgeColour).IsThemed);
    }

    [Fact]
    public void AnInstanceBesideAnEdgeSharesNoRoleSoNothingIsOffered()
    {
        Assert.Empty(RowsFor([Rectangle()], [Edge()]));
    }

    [Fact]
    public void DisagreeingTargetsMakeAMixedRowWithNoValue()
    {
        var rows = RowsFor([Rectangle("#ff0000"), Rectangle("#00ff00")], []);

        var fill = RowOf(rows, PropertyRole.Fill);
        Assert.True(fill.IsMixed);
        Assert.Null(fill.Value);
        Assert.False(fill.IsThemed);
        Assert.False(RowOf(rows, PropertyRole.StrokeWidth).IsMixed);
    }

    [Fact]
    public void AnInstanceCommitWritesEveryTargetThatDiffersInOneBatch()
    {
        var holder = Rectangle("#00FF00");
        var other = Rectangle("#ff0000");
        var rows = RowsFor([holder, other], []);

        RowOf(rows, PropertyRole.Fill).Commit("#00ff00");

        var batch = Assert.Single(_propsCommits);
        var (instanceId, _, after) = Assert.Single(batch);
        Assert.Equal(other.Id, instanceId);
        Assert.Equal("#00ff00", ((RectangleProps)after).FillColor);
    }

    [Fact]
    public void ANumberThatDoesNotParseCommitsNothing()
    {
        var rows = RowsFor([Rectangle()], []);

        RowOf(rows, PropertyRole.StrokeWidth).Commit("wide");

        Assert.Empty(_propsCommits);
    }

    [Fact]
    public void ANumberCommitsAsTheRolesType()
    {
        var rows = RowsFor([Rectangle(strokeWidth: 2)], []);

        RowOf(rows, PropertyRole.StrokeWidth).Commit("4.5");

        var (_, _, after) = Assert.Single(Assert.Single(_propsCommits));
        Assert.Equal(4.5, ((RectangleProps)after).StrokeWidth);
    }

    [Fact]
    public void AnEdgeCommitChangesOnlyItsOwnFieldAndKeepsAnAuthoredColour()
    {
        var edge = Edge(EdgeRouting.Curved, ArrowStyle.None, color: "#e5246b");
        var rows = RowsFor([], [edge]);

        RowOf(rows, PropertyRole.EdgeTargetArrow).Commit("Arrow");

        var (edgeId, before, after) = Assert.Single(Assert.Single(_edgeCommits));
        Assert.Equal(edge.Id, edgeId);
        Assert.Equal(
            new EdgeStyle(EdgeRouting.Curved, ArrowStyle.None, ArrowStyle.None, "#e5246b"),
            before
        );
        Assert.Equal(before with { TargetArrow = ArrowStyle.Arrow }, after);
    }

    [Fact]
    public void AnEdgeColourCommitSkipsAnEdgeAlreadyHoldingItWithoutRegardToCase()
    {
        var holder = Edge(color: "#E5246B");
        var other = Edge(color: null);
        var rows = RowsFor([], [holder, other]);

        Assert.True(RowOf(rows, PropertyRole.EdgeColour).IsMixed);
        RowOf(rows, PropertyRole.EdgeColour).Commit("#e5246b");

        var (edgeId, _, after) = Assert.Single(Assert.Single(_edgeCommits));
        Assert.Equal(other.Id, edgeId);
        Assert.Equal("#e5246b", after.Color);
    }

    [Fact]
    public void AnUnknownOptionCommitsNothing()
    {
        var rows = RowsFor([], [Edge()]);

        RowOf(rows, PropertyRole.EdgeRouting).Commit("Diagonal");
        RowOf(rows, PropertyRole.EdgeRouting).Commit("7");

        Assert.Empty(_edgeCommits);
    }
}
