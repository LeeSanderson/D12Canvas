using Bunit;
using D12Canvas.BuiltIns;
using D12Canvas.Model;
using D12Canvas.Registration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace D12Canvas.Tests;

public class BoardSketchTests : ComponentTestBase
{
    private const string NoteKey = "renamed-note";
    private const string ShapeKey = "sticky-shape";

    public BoardSketchTests()
    {
        var registry = new ComponentRegistry();
        registry.Register(Registration(NoteKey, typeof(StickyNote), typeof(StickyNoteProps)));
        registry.Register(Registration(ShapeKey, typeof(Rectangle), typeof(RectangleProps)));
        Services.AddSingleton<IComponentRegistry>(registry);
    }

    private static ComponentRegistration Registration(string key, Type component, Type props) =>
        new(
            Key: key,
            ComponentType: component,
            PropsType: props,
            DisplayName: key,
            AccessibleName: key,
            DefaultProps: new object(),
            Icon: null,
            Role: "group",
            DefaultSize: null,
            Category: null
        );

    private static ComponentInstance Note(Bounds bounds) =>
        new(NoteKey, new StickyNoteProps("", "#FFEB3B", "#000000", 14), bounds);

    private static ComponentInstance Shape(Bounds bounds) =>
        new(ShapeKey, new RectangleProps(null, null, 2), bounds);

    private IRenderedComponent<BoardSketch> RenderSketch(Board board) =>
        Render<BoardSketch>(parameters => parameters.Add(p => p.Board, board));

    [Fact]
    public void DrawsOneRectanglePerComponentAndOneLinePerEdge()
    {
        var board = new Board();
        var first = Shape(new Bounds(0, 0, 100, 50));
        var second = Shape(new Bounds(300, 200, 100, 50));
        var third = Note(new Bounds(0, 300, 80, 80));
        board.AddComponent(first);
        board.AddComponent(second);
        board.AddComponent(third);
        board.AddEdge(
            new Edge(
                new PortEndpoint(first.Id, PortId.Right),
                new PortEndpoint(second.Id, PortId.Left)
            )
        );
        board.AddEdge(
            new Edge(
                new AutoPortEndpoint(second.Id),
                new CustomPortEndpoint(third.Id, Guid.NewGuid())
            )
        );

        var sketch = RenderSketch(board);

        Assert.Equal(3, sketch.FindAll("svg rect").Count);
        Assert.Equal(2, sketch.FindAll("svg line").Count);
    }

    [Fact]
    public void AnEdgeRunsBetweenTheCentresOfTheComponentsItConnects()
    {
        var board = new Board();
        var source = Shape(new Bounds(0, 0, 100, 50));
        var target = Shape(new Bounds(300, 200, 100, 60));
        board.AddComponent(source);
        board.AddComponent(target);
        board.AddEdge(
            new Edge(
                new PortEndpoint(source.Id, PortId.Right),
                new PortEndpoint(target.Id, PortId.Top)
            )
        );

        var line = RenderSketch(board).Find("svg line");

        Assert.Equal("50", line.GetAttribute("x1"));
        Assert.Equal("25", line.GetAttribute("y1"));
        Assert.Equal("350", line.GetAttribute("x2"));
        Assert.Equal("230", line.GetAttribute("y2"));
        Assert.Equal("non-scaling-stroke", line.GetAttribute("vector-effect"));
    }

    [Fact]
    public void AFloatingEndDrawsToItsOwnPoint()
    {
        var board = new Board();
        var source = Shape(new Bounds(0, 0, 100, 50));
        board.AddComponent(source);
        board.AddEdge(new Edge(new AutoPortEndpoint(source.Id), new FloatingEndpoint(12.5, 40)));

        var line = RenderSketch(board).Find("svg line");

        Assert.Equal("50", line.GetAttribute("x1"));
        Assert.Equal("25", line.GetAttribute("y1"));
        Assert.Equal("12.5", line.GetAttribute("x2"));
        Assert.Equal("40", line.GetAttribute("y2"));
    }

    [Fact]
    public void AnEdgeWhoseComponentIsGoneDrawsNothing()
    {
        var board = new Board();
        var source = Shape(new Bounds(0, 0, 100, 50));
        board.AddComponent(source);
        board.AddEdge(
            new Edge(new AutoPortEndpoint(source.Id), new AutoPortEndpoint(Guid.NewGuid()))
        );

        Assert.Empty(RenderSketch(board).FindAll("svg line"));
    }

    [Fact]
    public void StickyNotesAreFoundByComponentTypeNotByTypeKey()
    {
        var board = new Board();
        board.AddComponent(Note(new Bounds(0, 0, 80, 80)));
        board.AddComponent(Shape(new Bounds(100, 0, 80, 80)));

        var rects = RenderSketch(board).FindAll("svg rect");

        Assert.Equal(
            ["d12-board-sketch-note", "d12-board-sketch-box"],
            rects.Select(rect => rect.GetAttribute("class"))
        );
    }

    [Fact]
    public void AComponentOfAnUnregisteredTypeDrawsAsAnOutline()
    {
        var board = new Board();
        board.AddComponent(
            new ComponentInstance("unknown", new object(), new Bounds(0, 0, 10, 10))
        );

        Assert.Equal(
            "d12-board-sketch-box",
            RenderSketch(board).Find("svg rect").GetAttribute("class")
        );
    }

    [Fact]
    public void RectanglesFollowComponentBoundsInZOrder()
    {
        var board = new Board();
        board.AddComponent(
            new ComponentInstance(
                ShapeKey,
                new RectangleProps(null, null, 2),
                new Bounds(10, 20, 30, 40),
                zIndex: 5
            )
        );
        board.AddComponent(
            new ComponentInstance(
                NoteKey,
                new StickyNoteProps("", "#FFEB3B", "#000000", 14),
                new Bounds(-5, 0, 7.5, 8),
                zIndex: 1
            )
        );

        var rects = RenderSketch(board).FindAll("svg rect");

        Assert.Equal(
            ["-5 0 7.5 8", "10 20 30 40"],
            rects.Select(rect =>
                string.Join(
                    " ",
                    rect.GetAttribute("x"),
                    rect.GetAttribute("y"),
                    rect.GetAttribute("width"),
                    rect.GetAttribute("height")
                )
            )
        );
    }

    [Fact]
    public void EdgeLabelsDrawNothing()
    {
        var board = new Board();
        var source = Shape(new Bounds(0, 0, 100, 50));
        var target = Shape(new Bounds(300, 0, 100, 50));
        board.AddComponent(source);
        board.AddComponent(target);
        board.AddEdge(
            new Edge(
                new AutoPortEndpoint(source.Id),
                new AutoPortEndpoint(target.Id),
                label: Note(new Bounds(1000, 1000, 50, 20))
            )
        );

        var sketch = RenderSketch(board);

        Assert.Equal(2, sketch.FindAll("svg rect").Count);
        Assert.Equal("0 0 400 50", sketch.Find("svg").GetAttribute("viewBox"));
    }

    [Fact]
    public void TheViewBoxIsTheUnionOfTheComponentBounds()
    {
        var board = new Board();
        var source = Shape(new Bounds(-20, 10, 100, 50));
        board.AddComponent(source);
        board.AddComponent(Note(new Bounds(200, -30.5, 80, 80)));
        board.AddEdge(new Edge(new AutoPortEndpoint(source.Id), new FloatingEndpoint(900, 900)));

        var svg = RenderSketch(board).Find("svg");

        Assert.Equal("-20 -30.5 300 90.5", svg.GetAttribute("viewBox"));
    }

    [Fact]
    public void AnEmptyBoardSaysSo()
    {
        var board = new Board();
        board.AddEdge(new Edge(new FloatingEndpoint(0, 0), new FloatingEndpoint(10, 10)));

        var sketch = RenderSketch(board);

        Assert.Empty(sketch.FindAll("svg"));
        Assert.Equal("Empty board", sketch.Find(".d12-board-sketch").TextContent.Trim());
    }

    [Fact]
    public void EveryColourReadsATokenDeclaredOnTheSketchRoot()
    {
        var css = StyleBlockText(RenderSketch(new Board()));

        Assert.All(
            ColourLiterals.In(css),
            literal => Assert.True(literal.IsTokenDeclaration, literal.ToString())
        );
        Assert.DoesNotContain("--bs-", css);
        Assert.DoesNotContain(":root", css);
        Assert.Contains("var(--d12-sketch-note)", ExtractBlock(css, ".d12-board-sketch-note {"));
        Assert.Contains("var(--d12-surface)", ExtractBlock(css, ".d12-board-sketch-box {"));
    }

    [Theory]
    [InlineData(".d12-board-sketch {")]
    [InlineData("@media (prefers-color-scheme: dark)")]
    [InlineData("[data-d12-theme=light] .d12-board-sketch {")]
    [InlineData("[data-d12-theme=dark] .d12-board-sketch {")]
    public void EveryThemeBlockDeclaresTheSketchTokens(string marker)
    {
        var css = StyleBlockText(RenderSketch(new Board()));

        var block = ExtractEveryBlock(css, marker);
        foreach (var token in ThemeTokens.Concat(["--d12-sketch-note", "--d12-sketch-note-border"]))
        {
            Assert.Contains(token + ":", block);
        }
    }

    [Theory]
    [InlineData("light")]
    [InlineData("dark")]
    public void ADataThemeOverrideAppliesToTheSketchItselfAndAnyAncestor(string theme)
    {
        var css = StyleBlockText(RenderSketch(new Board()));

        Assert.Contains($".d12-board-sketch[data-d12-theme={theme}]", css);
        Assert.Contains($"[data-d12-theme={theme}] .d12-board-sketch", css);
    }

    [Fact]
    public void TheDarkNoteIsNotTheLightNote()
    {
        var css = StyleBlockText(RenderSketch(new Board()));

        Assert.NotEqual(
            NoteTokens(ExtractEveryBlock(css, "[data-d12-theme=light] .d12-board-sketch {")),
            NoteTokens(ExtractEveryBlock(css, "[data-d12-theme=dark] .d12-board-sketch {"))
        );
    }

    private static string NoteTokens(string block) =>
        string.Join(
            "\n",
            block
                .Split('\n')
                .Select(line => line.Trim())
                .Where(line => line.StartsWith("--d12-sketch-note", StringComparison.Ordinal))
        );
}
