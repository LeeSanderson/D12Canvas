using AngleSharp.Dom;
using Bunit;
using D12Canvas.Model;
using D12Canvas.Pointer;
using D12Canvas.Registration;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace D12Canvas.Tests;

// A secondary release from pointing opens a menu offering the same action set as the baseline
// shortcut table (Delete; Group/Ungroup as applicable; the four layering commands), each wired to
// invoke the exact same OnXPressed method its shortcut does. The release first resolves the
// selection: a press inside it preserves it, a press on something else selects that, and a press
// on empty canvas clears it and so opens no menu.
public class DiagramCanvasContextMenuTests : ComponentTestBase
{
    private const string ComponentTypeKey = "test-props";

    public DiagramCanvasContextMenuTests()
    {
        SetupDiagramCanvasJsModule();
        var contextMenuModule = JSInterop.SetupModule(
            "./_content/D12Canvas/SelectionContextMenu.razor.js"
        );
        contextMenuModule.SetupVoid("registerClickOutside", _ => true).SetVoidResult();
        contextMenuModule.SetupVoid("unregisterClickOutside").SetVoidResult();

        var registry = new ComponentRegistry();
        registry.Register(
            new ComponentRegistration(
                Key: ComponentTypeKey,
                ComponentType: typeof(TestPropsComponent),
                PropsType: typeof(TestProps),
                DisplayName: "Test Props",
                AccessibleName: "Test props component",
                DefaultProps: new TestProps(),
                Icon: null,
                Role: "group",
                DefaultSize: null,
                Category: null
            )
        );
        Services.AddSingleton<IComponentRegistry>(registry);
    }

    private static ComponentInstance AddInstance(Board board, double x, int zIndex = 0)
    {
        var instance = new ComponentInstance(
            ComponentTypeKey,
            new TestProps(),
            new Bounds(x, 0, 50, 50),
            zIndex
        );
        board.AddComponent(instance);
        return instance;
    }

    private static void SelectBoth(IRenderedComponent<DiagramCanvas> canvas)
    {
        var containers = canvas.FindAll(".component-container");
        canvas.ClickOn(containers[0]);
        canvas.ClickOn(containers[1], shift: true);
    }

    private static async Task RightClick(
        IRenderedComponent<DiagramCanvas> canvas,
        ComponentInstance instance,
        double x = 10,
        double y = 10
    )
    {
        await canvas.Press(
            x,
            y,
            PointerPress.SecondaryButton,
            role: HitRole.Instance,
            entityId: instance.Id
        );
        await canvas.Release(x, y, PointerPress.SecondaryButton);
    }

    private static async Task RightClickEdge(IRenderedComponent<DiagramCanvas> canvas, Edge edge)
    {
        await canvas.Press(
            100,
            25,
            PointerPress.SecondaryButton,
            role: HitRole.Edge,
            entityId: edge.Id
        );
        await canvas.Release(100, 25, PointerPress.SecondaryButton);
    }

    [Fact]
    public async Task RightClickOnASelectedInstanceOpensTheMenuAtThePressPoint()
    {
        var board = new Board();
        var instance = AddInstance(board, 0);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        canvas.ClickOn(canvas.Find(".component-container"));

        await RightClick(canvas, instance, x: 37, y: 19);

        var menu = canvas.Find(".d12-context-menu");
        var style = menu.GetAttribute("style");
        Assert.Contains("left: 37px", style);
        Assert.Contains("top: 19px", style);
    }

    [Fact]
    public async Task RightClickOnAnUnselectedInstanceSelectsItAndOpensTheMenu()
    {
        var board = new Board();
        var first = AddInstance(board, 0);
        var second = AddInstance(board, 100);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        canvas.ClickOn(canvas.FindAll(".component-container")[0]);

        await RightClick(canvas, second);

        var containers = canvas.FindAll(".component-container");
        Assert.Null(containers[0].GetAttribute("aria-selected"));
        Assert.Equal("true", containers[1].GetAttribute("aria-selected"));
        Assert.Single(canvas.FindAll(".d12-context-menu"));
        Assert.NotNull(board.GetComponent(first.Id));
    }

    [Fact]
    public async Task RightClickOnAMemberOfTheSelectionPreservesTheWholeSelection()
    {
        var board = new Board();
        AddInstance(board, 0);
        var second = AddInstance(board, 100);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        SelectBoth(canvas);

        await RightClick(canvas, second);

        var containers = canvas.FindAll(".component-container");
        Assert.Equal("true", containers[0].GetAttribute("aria-selected"));
        Assert.Equal("true", containers[1].GetAttribute("aria-selected"));
        Assert.Single(canvas.FindAll(".d12-context-menu"));
    }

    [Fact]
    public async Task RightClickOnEmptyCanvasClearsTheSelectionAndOpensNoMenu()
    {
        var board = new Board();
        AddInstance(board, 0);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        canvas.ClickOn(canvas.Find(".component-container"));

        await canvas.ClickCanvas(400, 400, PointerPress.SecondaryButton);

        Assert.Null(canvas.Find(".component-container").GetAttribute("aria-selected"));
        Assert.Empty(canvas.FindAll(".d12-context-menu"));
    }

    [Fact]
    public async Task ARightDragPastTheThresholdPansAndOpensNothing()
    {
        var board = new Board();
        var instance = AddInstance(board, 0);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        canvas.ClickOn(canvas.Find(".component-container"));

        await canvas.Press(
            10,
            10,
            PointerPress.SecondaryButton,
            role: HitRole.Instance,
            entityId: instance.Id
        );
        await canvas.Move(60, 40);
        await canvas.Release(60, 40, PointerPress.SecondaryButton);

        Assert.Contains(
            "translate(50px, 30px)",
            canvas.Find(".canvas-content").GetAttribute("style")
        );
        Assert.Empty(canvas.FindAll(".d12-context-menu"));
        Assert.Equal("true", canvas.Find(".component-container").GetAttribute("aria-selected"));
    }

    [Fact]
    public async Task MenuOffersOnlyDeleteAndLayeringForASingleSelectedInstance()
    {
        var board = new Board();
        var instance = AddInstance(board, 0);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        canvas.ClickOn(canvas.Find(".component-container"));

        await RightClick(canvas, instance);

        var labels = canvas
            .FindAll(".d12-context-menu-item")
            .Select(item => item.TextContent)
            .ToArray();
        Assert.Contains("Delete", labels);
        Assert.Contains("Bring to Front", labels);
        Assert.DoesNotContain("Group", labels);
        Assert.DoesNotContain("Ungroup", labels);
    }

    [Fact]
    public async Task MenuOffersGroupForATwoInstanceAdHocSelection()
    {
        var board = new Board();
        var first = AddInstance(board, 0);
        AddInstance(board, 100);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        SelectBoth(canvas);

        await RightClick(canvas, first);

        Assert.Contains(
            "Group",
            canvas.FindAll(".d12-context-menu-item").Select(item => item.TextContent)
        );
    }

    [Fact]
    public async Task MenuOffersUngroupForASelectedGroupAndNotGroup()
    {
        var board = new Board();
        var first = AddInstance(board, 0);
        AddInstance(board, 100);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        SelectBoth(canvas);
        await canvas.InvokeAsync(() => canvas.Instance.OnGroupPressed());

        await RightClick(canvas, first);

        var labels = canvas
            .FindAll(".d12-context-menu-item")
            .Select(item => item.TextContent)
            .ToArray();
        Assert.Contains("Ungroup", labels);
        Assert.DoesNotContain("Group", labels);
    }

    [Fact]
    public async Task ClickingDeleteInTheMenuRemovesTheSelectionAndClosesTheMenu()
    {
        var board = new Board();
        var instance = AddInstance(board, 0);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        canvas.ClickOn(canvas.Find(".component-container"));
        await RightClick(canvas, instance);

        canvas.FindAll(".d12-context-menu-item").Single(i => i.TextContent == "Delete").Click();

        Assert.Null(board.GetComponent(instance.Id));
        Assert.Empty(canvas.FindAll(".d12-context-menu"));
    }

    [Fact]
    public async Task DeleteInvokedFromTheMenuIsUndoableExactlyLikeTheShortcut()
    {
        var board = new Board();
        var instance = AddInstance(board, 0);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        canvas.ClickOn(canvas.Find(".component-container"));
        await RightClick(canvas, instance);
        canvas.FindAll(".d12-context-menu-item").Single(i => i.TextContent == "Delete").Click();

        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());

        Assert.NotNull(board.GetComponent(instance.Id));
    }

    [Fact]
    public async Task ClickingGroupInTheMenuPromotesTheSelectionIntoAGroupAndClosesTheMenu()
    {
        var board = new Board();
        var first = AddInstance(board, 0);
        AddInstance(board, 100);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        SelectBoth(canvas);
        await RightClick(canvas, first);

        canvas.FindAll(".d12-context-menu-item").Single(i => i.TextContent == "Group").Click();

        Assert.Single(board.Groups);
        Assert.Empty(canvas.FindAll(".d12-context-menu"));
    }

    [Fact]
    public async Task ClickingUngroupInTheMenuDissolvesTheGroupAndClosesTheMenu()
    {
        var board = new Board();
        var first = AddInstance(board, 0);
        AddInstance(board, 100);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        SelectBoth(canvas);
        await canvas.InvokeAsync(() => canvas.Instance.OnGroupPressed());
        await RightClick(canvas, first);

        canvas.FindAll(".d12-context-menu-item").Single(i => i.TextContent == "Ungroup").Click();

        Assert.Empty(board.Groups);
        Assert.Empty(canvas.FindAll(".d12-context-menu"));
    }

    [Theory]
    [InlineData("Bring to Front")]
    [InlineData("Bring Forward")]
    [InlineData("Send Backward")]
    [InlineData("Send to Back")]
    public async Task EachLayeringMenuItemChangesZIndexAndClosesTheMenu(string label)
    {
        var board = new Board();
        // target sits between a lower and a higher neighbour, so every one of the four directions
        // has somewhere to move it to (unlike a value already at one extreme, where the matching
        // "further that way" command is legitimately a no-op).
        var target = AddInstance(board, 0, zIndex: 5);
        AddInstance(board, 100, zIndex: 1);
        AddInstance(board, 200, zIndex: 9);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        canvas.ClickOn(canvas.Find(".component-container"));
        await RightClick(canvas, target);

        canvas.FindAll(".d12-context-menu-item").Single(i => i.TextContent == label).Click();

        Assert.NotEqual(5, target.ZIndex);
        Assert.Empty(canvas.FindAll(".d12-context-menu"));
    }

    [Fact]
    public async Task EscapeInsideTheMenuClosesItWithoutClearingTheBoardSelection()
    {
        var board = new Board();
        var instance = AddInstance(board, 0);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        canvas.ClickOn(canvas.Find(".component-container"));
        await RightClick(canvas, instance);

        canvas.Find(".d12-context-menu").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.Empty(canvas.FindAll(".d12-context-menu"));
        Assert.Equal("true", canvas.Find(".component-container").GetAttribute("aria-selected"));
    }

    [Fact]
    public async Task RightClickOnAnEdgeSelectsItAndOffersDeleteWhichRemovesTheEdge()
    {
        var board = new Board();
        var source = AddInstance(board, 0);
        var target = AddInstance(board, 200);
        var edge = new Edge(
            new PortEndpoint(source.Id, PortId.Right),
            new PortEndpoint(target.Id, PortId.Left)
        );
        board.AddEdge(edge);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        await RightClickEdge(canvas, edge);
        Assert.Equal("true", canvas.Find(".edge-line").GetAttribute("aria-selected"));

        canvas.FindAll(".d12-context-menu-item").Single(i => i.TextContent == "Delete").Click();

        Assert.Null(board.GetEdge(edge.Id));
    }
}
