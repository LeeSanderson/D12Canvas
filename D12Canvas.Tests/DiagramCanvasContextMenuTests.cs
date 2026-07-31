using AngleSharp.Dom;
using Bunit;
using D12Canvas.Model;
using D12Canvas.Registration;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace D12Canvas.Tests;

// Right-click on a selection opens a menu offering the same action set as the baseline shortcut
// table (Delete; Group/Ungroup as applicable; the four layering commands), each wired to invoke
// the exact same OnXPressed method its shortcut does. Right-click on empty canvas (no selection)
// opens no custom menu at all.
public class DiagramCanvasContextMenuTests : ComponentTestBase
{
    private const string ComponentTypeKey = "test-props";

    public DiagramCanvasContextMenuTests()
    {
        SetupDiagramCanvasJsModule();
        SetupComponentContainerJsModule();
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
        containers[0].Click();
        containers[1].Click(new MouseEventArgs { ShiftKey = true });
    }

    private static void RightClick(
        IRenderedComponent<DiagramCanvas> canvas,
        double clientX = 0,
        double clientY = 0
    ) =>
        canvas
            .Find(".diagram-canvas")
            .ContextMenu(new MouseEventArgs { ClientX = clientX, ClientY = clientY });

    [Fact]
    public void RightClickOnASelectedInstanceOpensTheMenu()
    {
        var board = new Board();
        AddInstance(board, 0);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        canvas.Find(".component-container").Click();

        RightClick(canvas);

        Assert.Single(canvas.FindAll(".d12-context-menu"));
    }

    [Fact]
    public void RightClickOnEmptyCanvasWithNoSelectionOpensNoMenu()
    {
        var board = new Board();
        AddInstance(board, 0);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        RightClick(canvas);

        Assert.Empty(canvas.FindAll(".d12-context-menu"));
    }

    [Fact]
    public void MenuOffersOnlyDeleteAndLayeringForASingleSelectedInstance()
    {
        var board = new Board();
        AddInstance(board, 0);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        canvas.Find(".component-container").Click();

        RightClick(canvas);

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
    public void MenuOffersGroupForATwoInstanceAdHocSelection()
    {
        var board = new Board();
        AddInstance(board, 0);
        AddInstance(board, 100);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        SelectBoth(canvas);

        RightClick(canvas);

        Assert.Contains(
            "Group",
            canvas.FindAll(".d12-context-menu-item").Select(item => item.TextContent)
        );
    }

    [Fact]
    public async Task MenuOffersUngroupForASelectedGroupAndNotGroup()
    {
        var board = new Board();
        AddInstance(board, 0);
        AddInstance(board, 100);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        SelectBoth(canvas);
        await canvas.InvokeAsync(() => canvas.Instance.OnGroupPressed());

        RightClick(canvas);

        var labels = canvas
            .FindAll(".d12-context-menu-item")
            .Select(item => item.TextContent)
            .ToArray();
        Assert.Contains("Ungroup", labels);
        Assert.DoesNotContain("Group", labels);
    }

    [Fact]
    public void ClickingDeleteInTheMenuRemovesTheSelectionAndClosesTheMenu()
    {
        var board = new Board();
        var instance = AddInstance(board, 0);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        canvas.Find(".component-container").Click();
        RightClick(canvas);

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
        canvas.Find(".component-container").Click();
        RightClick(canvas);
        canvas.FindAll(".d12-context-menu-item").Single(i => i.TextContent == "Delete").Click();

        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());

        Assert.NotNull(board.GetComponent(instance.Id));
    }

    [Fact]
    public void ClickingGroupInTheMenuPromotesTheSelectionIntoAGroupAndClosesTheMenu()
    {
        var board = new Board();
        AddInstance(board, 0);
        AddInstance(board, 100);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        SelectBoth(canvas);
        RightClick(canvas);

        canvas.FindAll(".d12-context-menu-item").Single(i => i.TextContent == "Group").Click();

        Assert.Single(board.Groups);
        Assert.Empty(canvas.FindAll(".d12-context-menu"));
    }

    [Fact]
    public async Task ClickingUngroupInTheMenuDissolvesTheGroupAndClosesTheMenu()
    {
        var board = new Board();
        AddInstance(board, 0);
        AddInstance(board, 100);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        SelectBoth(canvas);
        await canvas.InvokeAsync(() => canvas.Instance.OnGroupPressed());
        RightClick(canvas);

        canvas.FindAll(".d12-context-menu-item").Single(i => i.TextContent == "Ungroup").Click();

        Assert.Empty(board.Groups);
        Assert.Empty(canvas.FindAll(".d12-context-menu"));
    }

    [Theory]
    [InlineData("Bring to Front")]
    [InlineData("Bring Forward")]
    [InlineData("Send Backward")]
    [InlineData("Send to Back")]
    public void EachLayeringMenuItemChangesZIndexAndClosesTheMenu(string label)
    {
        var board = new Board();
        // target sits between a lower and a higher neighbour, so every one of the four directions
        // has somewhere to move it to (unlike a value already at one extreme, where the matching
        // "further that way" command is legitimately a no-op).
        var target = AddInstance(board, 0, zIndex: 5);
        AddInstance(board, 100, zIndex: 1);
        AddInstance(board, 200, zIndex: 9);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        canvas.Find(".component-container").Click();
        RightClick(canvas);

        canvas.FindAll(".d12-context-menu-item").Single(i => i.TextContent == label).Click();

        Assert.NotEqual(5, target.ZIndex);
        Assert.Empty(canvas.FindAll(".d12-context-menu"));
    }

    [Fact]
    public void EscapeInsideTheMenuClosesItWithoutClearingTheBoardSelection()
    {
        var board = new Board();
        AddInstance(board, 0);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        canvas.Find(".component-container").Click();
        RightClick(canvas);

        canvas.Find(".d12-context-menu").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.Empty(canvas.FindAll(".d12-context-menu"));
        Assert.Equal("true", canvas.Find(".component-container").GetAttribute("aria-selected"));
    }

    [Fact]
    public void RightClickOnASelectedEdgeOffersDeleteAndDeletingItRemovesTheEdge()
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
        canvas.Find(".edge-line").Click();

        RightClick(canvas);
        canvas.FindAll(".d12-context-menu-item").Single(i => i.TextContent == "Delete").Click();

        Assert.Null(board.GetEdge(edge.Id));
    }
}
