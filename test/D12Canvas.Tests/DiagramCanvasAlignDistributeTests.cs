using AngleSharp.Dom;
using Bunit;
using D12Canvas.BuiltIns;
using D12Canvas.Model;
using D12Canvas.Pointer;
using D12Canvas.Registration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace D12Canvas.Tests;

public class DiagramCanvasAlignDistributeTests : ComponentTestBase
{
    private static readonly string[] AlignLabels =
    [
        "Align left",
        "Align centre",
        "Align right",
        "Align top",
        "Align middle",
        "Align bottom",
    ];

    private static readonly string[] AllLabels =
    [
        .. AlignLabels,
        "Distribute horizontally",
        "Distribute vertically",
    ];

    public DiagramCanvasAlignDistributeTests()
    {
        SetupDiagramCanvasJsModule();
        var options = new D12CanvasOptions();
        BuiltInComponents.RegisterAll(options);
        Services.AddSingleton(options.Registry);
    }

    private static ComponentInstance AddShape(
        Board board,
        double x,
        double y,
        double width = 100,
        double height = 50
    )
    {
        var instance = new ComponentInstance(
            "rectangle",
            new RectangleProps(null, null, 2),
            new Bounds(x, y, width, height)
        );
        board.AddComponent(instance);
        return instance;
    }

    private IRenderedComponent<DiagramCanvas> RenderCanvas(Board board, bool snapToGrid = false) =>
        Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, board).Add(p => p.SnapToGrid, snapToGrid)
        );

    private static Task SelectAll(IRenderedComponent<DiagramCanvas> canvas) =>
        canvas.InvokeAsync(() => canvas.Instance.OnSelectAllPressed());

    private static Task Undo(IRenderedComponent<DiagramCanvas> canvas) =>
        canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());

    private static async Task OpenMenuOn(
        IRenderedComponent<DiagramCanvas> canvas,
        ComponentInstance instance
    )
    {
        await canvas.Press(
            10,
            10,
            PointerPress.SecondaryButton,
            role: HitRole.Instance,
            entityId: instance.Id
        );
        await canvas.Release(10, 10, PointerPress.SecondaryButton);
    }

    private static string[] GlyphLabels(IRenderedComponent<DiagramCanvas> canvas) =>
        canvas
            .FindAll(".d12-context-menu-glyph")
            .Select(glyph => glyph.GetAttribute("title")!)
            .ToArray();

    private static IElement Glyph(IRenderedComponent<DiagramCanvas> canvas, string label) =>
        canvas
            .FindAll(".d12-context-menu-glyph")
            .Single(glyph => glyph.GetAttribute("title") == label);

    private static double[] HorizontalGaps(IEnumerable<ComponentInstance> shapes)
    {
        var ordered = shapes.Select(shape => shape.Bounds).OrderBy(bounds => bounds.X).ToArray();
        return ordered.Zip(ordered.Skip(1), (before, after) => after.X - before.Right).ToArray();
    }

    [Fact]
    public async Task AlignLeftMovesTheOthersToTheLeftmostEdgeInOneUndoableEntry()
    {
        var board = new Board();
        var leftmost = AddShape(board, 40, 0);
        var middle = AddShape(board, 90, 100);
        var right = AddShape(board, 170, 200);
        var canvas = RenderCanvas(board);
        await SelectAll(canvas);

        await canvas.InvokeAsync(() => canvas.Instance.OnAlignLeftPressed());

        Assert.All(board.Components, shape => Assert.Equal(40, shape.Bounds.X));
        Assert.Equal(new Bounds(40, 100, 100, 50), middle.Bounds);

        await Undo(canvas);

        Assert.Equal([40d, 90d, 170d], new[] { leftmost, middle, right }.Select(s => s.Bounds.X));
    }

    [Fact]
    public async Task AligningShapesThatAreAlreadyAlignedAddsNoHistoryEntry()
    {
        var board = new Board();
        var first = AddShape(board, 40, 0);
        var second = AddShape(board, 90, 100);
        var canvas = RenderCanvas(board);
        await SelectAll(canvas);
        await canvas.InvokeAsync(() => canvas.Instance.OnAlignLeftPressed());

        await canvas.InvokeAsync(() => canvas.Instance.OnAlignLeftPressed());
        await Undo(canvas);

        Assert.Equal(40, first.Bounds.X);
        Assert.Equal(90, second.Bounds.X);
    }

    [Fact]
    public async Task ASelectedGroupAlignsAsOneBodyAndKeepsItsMembersArrangement()
    {
        var board = new Board();
        var memberA = AddShape(board, 0, 0);
        var memberB = AddShape(board, 150, 80);
        var loose = AddShape(board, 400, 200);
        board.AddGroup(new Group([memberA.Id, memberB.Id]));
        var canvas = RenderCanvas(board);
        await SelectAll(canvas);

        await canvas.InvokeAsync(() => canvas.Instance.OnAlignRightPressed());

        Assert.Equal(new Bounds(250, 0, 100, 50), memberA.Bounds);
        Assert.Equal(new Bounds(400, 80, 100, 50), memberB.Bounds);
        Assert.Equal(new Bounds(400, 200, 100, 50), loose.Bounds);
    }

    [Fact]
    public async Task DistributeHorizontallyEqualisesTheGapsBetweenMixedSizes()
    {
        var board = new Board();
        AddShape(board, 0, 0, width: 40);
        AddShape(board, 50, 0, width: 100);
        AddShape(board, 230, 0, width: 20);
        AddShape(board, 280, 0, width: 60);
        var canvas = RenderCanvas(board);
        await SelectAll(canvas);

        await canvas.InvokeAsync(() => canvas.Instance.OnDistributeHorizontallyPressed());

        Assert.Equal([40d, 40d, 40d], HorizontalGaps(board.Components));
    }

    [Fact]
    public async Task UnderSnapEachDistributedGapIsAWholeGridStep()
    {
        var board = new Board();
        var first = AddShape(board, 0, 0, width: 40);
        AddShape(board, 50, 0, width: 100);
        AddShape(board, 230, 0, width: 20);
        AddShape(board, 280, 0, width: 60);
        var canvas = RenderCanvas(board, snapToGrid: true);
        await SelectAll(canvas);

        await canvas.InvokeAsync(() => canvas.Instance.OnDistributeHorizontallyPressed());

        Assert.Equal([40d, 40d, 40d], HorizontalGaps(board.Components));
        Assert.Equal(0, first.Bounds.X);
    }

    [Fact]
    public async Task UnderSnapAlignLandsOnAGridLine()
    {
        var board = new Board();
        AddShape(board, 13, 0);
        AddShape(board, 47, 100, width: 60);
        var canvas = RenderCanvas(board, snapToGrid: true);
        await SelectAll(canvas);

        await canvas.InvokeAsync(() => canvas.Instance.OnAlignRightPressed());

        Assert.All(board.Components, shape => Assert.Equal(120, shape.Bounds.Right));
    }

    [Fact]
    public async Task SelectedEdgesAreSkippedAndCountTowardNothing()
    {
        var board = new Board();
        var left = AddShape(board, 0, 0);
        var right = AddShape(board, 200, 60);
        var edge = new Edge(
            new PortEndpoint(left.Id, PortId.Right),
            new PortEndpoint(right.Id, PortId.Left)
        );
        board.AddEdge(edge);
        board.AddEdge(new Edge(new FloatingEndpoint(10, 300), new FloatingEndpoint(90, 340)));
        var canvas = RenderCanvas(board);
        await SelectAll(canvas);

        await canvas.InvokeAsync(() => canvas.Instance.OnAlignTopPressed());
        await OpenMenuOn(canvas, left);

        Assert.Equal(0, right.Bounds.Y);
        Assert.Equal(AlignLabels, GlyphLabels(canvas));
    }

    [Fact]
    public async Task OneShapeAndAnEdgeShowNoStrip()
    {
        var board = new Board();
        var shape = AddShape(board, 0, 0);
        board.AddEdge(new Edge(new FloatingEndpoint(10, 300), new FloatingEndpoint(90, 340)));
        var canvas = RenderCanvas(board);
        await SelectAll(canvas);

        await OpenMenuOn(canvas, shape);

        Assert.Empty(canvas.FindAll(".d12-context-menu-strip"));
    }

    [Fact]
    public async Task TwoShapesShowTheSixAlignGlyphsAndThreeShowAllEight()
    {
        var board = new Board();
        var first = AddShape(board, 0, 0);
        var second = AddShape(board, 200, 0);
        var canvas = RenderCanvas(board);
        await SelectAll(canvas);
        await OpenMenuOn(canvas, first);
        Assert.Equal(AlignLabels, GlyphLabels(canvas));
        await canvas.InvokeAsync(() => canvas.Instance.OnEscapePressed());

        AddShape(board, 400, 0);
        await SelectAll(canvas);
        await OpenMenuOn(canvas, second);

        Assert.Equal(AllLabels, GlyphLabels(canvas));
    }

    [Fact]
    public async Task OneSelectedGroupOfThreeIsOneEntityAndShowsNoStrip()
    {
        var board = new Board();
        var members = new[]
        {
            AddShape(board, 0, 0),
            AddShape(board, 200, 0),
            AddShape(board, 400, 0),
        };
        board.AddGroup(new Group(members.Select(member => member.Id).ToList()));
        var canvas = RenderCanvas(board);
        await SelectAll(canvas);

        await OpenMenuOn(canvas, members[0]);

        Assert.Empty(canvas.FindAll(".d12-context-menu-strip"));
    }

    [Fact]
    public async Task TheStripIsOneGroupOfGlyphMenuItemsBeforeTheZOrderRows()
    {
        var board = new Board();
        var first = AddShape(board, 0, 0);
        AddShape(board, 200, 0);
        var canvas = RenderCanvas(board);
        await SelectAll(canvas);

        await OpenMenuOn(canvas, first);

        var strip = canvas.Find(".d12-context-menu-strip");
        Assert.Equal("group", strip.GetAttribute("role"));
        Assert.All(
            strip.QuerySelectorAll(".d12-context-menu-glyph"),
            glyph =>
            {
                Assert.Equal("menuitem", glyph.GetAttribute("role"));
                Assert.Equal(glyph.GetAttribute("title"), glyph.GetAttribute("aria-label"));
                Assert.Empty(glyph.QuerySelectorAll(".d12-context-menu-hint"));
            }
        );
        var next = strip.NextElementSibling!;
        Assert.Equal("Bring to Front", next.QuerySelector(".d12-context-menu-label")!.TextContent);
        Assert.Equal("separator", strip.PreviousElementSibling!.GetAttribute("role"));
    }

    [Fact]
    public async Task ClickingAGlyphRunsItsCommandAndClosesTheMenu()
    {
        var board = new Board();
        var top = AddShape(board, 0, 30);
        var lower = AddShape(board, 200, 90);
        var canvas = RenderCanvas(board);
        await SelectAll(canvas);
        await OpenMenuOn(canvas, top);

        await Glyph(canvas, "Align bottom").ClickAsync(new());

        Assert.Equal(140, top.Bounds.Bottom);
        Assert.Equal(140, lower.Bounds.Bottom);
        Assert.Empty(canvas.FindAll(".d12-context-menu"));
    }

    [Fact]
    public async Task InsideAnEnteredGroupTheStripActsOnTheSelectedMembers()
    {
        var board = new Board();
        var first = AddShape(board, 0, 0);
        var second = AddShape(board, 60, 100);
        var third = AddShape(board, 300, 200);
        board.AddGroup(new Group([first.Id, second.Id, third.Id]));
        var canvas = RenderCanvas(board);
        canvas.DoubleClickElement(canvas.ContainerOf(first.Id), (10, 10));
        canvas.ClickOn(canvas.ContainerOf(second.Id), shift: true);

        await OpenMenuOn(canvas, second);
        Assert.Equal(AlignLabels, GlyphLabels(canvas));
        await Glyph(canvas, "Align right").ClickAsync(new());

        Assert.Equal(160, first.Bounds.Right);
        Assert.Equal(160, second.Bounds.Right);
        Assert.Equal(new Bounds(300, 200, 100, 50), third.Bounds);
    }

    [Fact]
    public async Task WithFewerThanTwoEntitiesTheCommandsDoNothing()
    {
        var board = new Board();
        var shape = AddShape(board, 13, 7);
        var canvas = RenderCanvas(board, snapToGrid: true);
        await SelectAll(canvas);

        await canvas.InvokeAsync(() =>
        {
            canvas.Instance.OnAlignLeftPressed();
            canvas.Instance.OnAlignCentrePressed();
            canvas.Instance.OnAlignMiddlePressed();
            canvas.Instance.OnDistributeVerticallyPressed();
        });

        Assert.Equal(new Bounds(13, 7, 100, 50), shape.Bounds);
    }
}
