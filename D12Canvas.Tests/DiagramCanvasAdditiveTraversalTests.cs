using AngleSharp.Dom;
using Bunit;
using D12Canvas.Model;
using D12Canvas.Registration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace D12Canvas.Tests;

// Focus is established with .Focus(), a real AngleSharp focus event routed through the stop's own
// @onfocus, which is where a native Tab lands. Space, Enter and Escape are the entry points the
// keydown table calls. None of this proves the keys arrive; AdditiveTraversalProbes does that.
public class DiagramCanvasAdditiveTraversalTests : ComponentTestBase
{
    private const string ComponentTypeKey = "test-props";

    private readonly Board _board = new();
    private readonly ComponentInstance _first;
    private readonly ComponentInstance _second;
    private readonly ComponentInstance _third;

    public DiagramCanvasAdditiveTraversalTests()
    {
        SetupDiagramCanvasJsModule();

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

        _first = AddInstance(0, 0);
        _second = AddInstance(100, 0);
        _third = AddInstance(200, 0);
    }

    private ComponentInstance AddInstance(double x, double y)
    {
        var instance = new ComponentInstance(
            ComponentTypeKey,
            new TestProps(),
            new Bounds(x, y, 50, 50)
        );
        _board.AddComponent(instance);
        return instance;
    }

    private IRenderedComponent<DiagramCanvas> RenderCanvas() =>
        Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, _board));

    private static IElement ContainerOf(IRenderedComponent<DiagramCanvas> canvas, Guid id) =>
        canvas.Find($".component-container[data-d12-entity='{id}']");

    private static void Focus(
        IRenderedComponent<DiagramCanvas> canvas,
        ComponentInstance instance
    ) => ContainerOf(canvas, instance.Id).Focus();

    private static HashSet<Guid> Selected(IRenderedComponent<DiagramCanvas> canvas) =>
        canvas.Instance.SelectedComponents.Select(instance => instance.Id).ToHashSet();

    private static Task PressSpace(IRenderedComponent<DiagramCanvas> canvas) =>
        canvas.InvokeAsync(() => canvas.Instance.OnSpacePressed());

    private static Task PressEscape(IRenderedComponent<DiagramCanvas> canvas) =>
        canvas.InvokeAsync(() => canvas.Instance.OnEscapePressed());

    private static Task PressEnter(IRenderedComponent<DiagramCanvas> canvas) =>
        canvas.InvokeAsync(() => canvas.Instance.OnEnterPressed());

    private async Task<IRenderedComponent<DiagramCanvas>> RenderInTheModeWithFirstSelected()
    {
        var canvas = RenderCanvas();
        Focus(canvas, _first);
        await PressSpace(canvas);
        return canvas;
    }

    [Fact]
    public async Task SpaceOnAFocusedSelectedShapeKeepsItSelectedAndTabThenMovesFocusOnly()
    {
        var canvas = RenderCanvas();
        Focus(canvas, _first);

        await PressSpace(canvas);

        Assert.Equal([_first.Id], Selected(canvas));

        Focus(canvas, _second);

        Assert.Equal([_first.Id], Selected(canvas));
        Assert.Null(ContainerOf(canvas, _second.Id).GetAttribute("aria-selected"));
    }

    [Fact]
    public async Task SpaceInsideTheModeAddsTheFocusedStopAndSpaceAgainRemovesIt()
    {
        var canvas = await RenderInTheModeWithFirstSelected();
        Focus(canvas, _second);
        Focus(canvas, _third);

        await PressSpace(canvas);

        Assert.Equal([_first.Id, _third.Id], Selected(canvas));

        await PressSpace(canvas);

        Assert.Equal([_first.Id], Selected(canvas));
    }

    [Fact]
    public async Task SpaceOutsideTheModeAddsAnUnselectedFocusedStopWithoutRemovingAnything()
    {
        var canvas = await RenderInTheModeWithFirstSelected();
        Focus(canvas, _third);
        await PressSpace(canvas);
        Focus(canvas, _second);
        await PressEscape(canvas);

        await PressSpace(canvas);

        Assert.Equal([_first.Id, _second.Id, _third.Id], Selected(canvas));
    }

    [Fact]
    public async Task TogglingTheLastMemberOutKeepsTheModeOn()
    {
        var canvas = await RenderInTheModeWithFirstSelected();

        await PressSpace(canvas);
        Focus(canvas, _second);

        Assert.Empty(Selected(canvas));
    }

    [Fact]
    public async Task SpaceWithNothingFocusedIsANoOp()
    {
        var canvas = RenderCanvas();

        await PressSpace(canvas);

        Assert.Empty(Selected(canvas));
    }

    [Fact]
    public async Task EscapeEndsTheModeAndKeepsTheSelection()
    {
        var canvas = await RenderInTheModeWithFirstSelected();
        Focus(canvas, _second);
        await PressSpace(canvas);

        await PressEscape(canvas);

        Assert.Equal([_first.Id, _second.Id], Selected(canvas));

        Focus(canvas, _third);

        Assert.Equal([_third.Id], Selected(canvas));
    }

    [Fact]
    public async Task APointerPressEndsTheMode()
    {
        var canvas = await RenderInTheModeWithFirstSelected();

        canvas.ClickElement(ContainerOf(canvas, _first.Id), at: (10, 10));
        Focus(canvas, _second);

        Assert.Equal([_second.Id], Selected(canvas));
    }

    [Fact]
    public async Task FocusLandingOnTheCanvasEndsTheModeAndDropsSpacesTarget()
    {
        var canvas = await RenderInTheModeWithFirstSelected();

        canvas.Find(".diagram-canvas").Focus();
        await PressSpace(canvas);
        Focus(canvas, _second);

        Assert.Equal([_second.Id], Selected(canvas));
    }

    [Fact]
    public async Task FocusLeavingTheContainerEndsTheMode()
    {
        var canvas = await RenderInTheModeWithFirstSelected();

        await canvas.InvokeAsync(() => canvas.Instance.OnFocusLeftContainer());
        Focus(canvas, _second);

        Assert.Equal([_second.Id], Selected(canvas));
    }

    [Fact]
    public async Task EnterOnAGroupStopEndsTheModeAndEntersTheGroupAtItsFirstMember()
    {
        _board.AddGroup(new Group([_first.Id, _second.Id]));
        var canvas = RenderCanvas();
        Focus(canvas, _third);
        await PressSpace(canvas);
        canvas.Find(".group-tab-stop").Focus();

        await PressEnter(canvas);

        Assert.Equal([_first.Id], Selected(canvas));
        Assert.NotNull(canvas.Find(".entered-group-outline"));

        Focus(canvas, _second);

        Assert.Equal([_second.Id], Selected(canvas));
    }

    [Fact]
    public async Task FocusLandingOutsideTheEnteredGroupStepsOutWithoutSelectingTheStop()
    {
        _board.AddGroup(new Group([_first.Id, _second.Id]));
        var canvas = RenderCanvas();
        canvas.Find(".group-tab-stop").Focus();
        await PressEnter(canvas);
        await PressSpace(canvas);

        Focus(canvas, _third);

        Assert.Empty(canvas.FindAll(".entered-group-outline"));
        Assert.Equal([_first.Id, _second.Id], Selected(canvas));
    }

    [Fact]
    public async Task GroupingHandsFocusToTheNewGroupAndEndsTheMode()
    {
        var canvas = await RenderInTheModeWithFirstSelected();
        Focus(canvas, _second);
        await PressSpace(canvas);

        await canvas.InvokeAsync(() => canvas.Instance.OnGroupPressed());

        Assert.Single(JSInterop.Invocations["focusGroupTabStop"]);
        Focus(canvas, _third);

        Assert.Equal([_third.Id], Selected(canvas));
    }

    [Fact]
    public async Task ClickToAddHandsFocusToThePlacedShapeAndEndsTheMode()
    {
        var canvas = await RenderInTheModeWithFirstSelected();

        await canvas.InvokeAsync(() => canvas.Instance.ClickToAdd(ComponentTypeKey));

        Assert.Single(JSInterop.Invocations["focusTabStopAt"]);
        Focus(canvas, _third);

        Assert.Equal([_third.Id], Selected(canvas));
    }

    [Fact]
    public async Task SpaceOnAGroupStopInsideTheModeAddsTheWholeGroup()
    {
        _board.AddGroup(new Group([_second.Id, _third.Id]));
        var canvas = await RenderInTheModeWithFirstSelected();

        canvas.Find(".group-tab-stop").Focus();
        await PressSpace(canvas);

        Assert.Equal("true", canvas.Find(".group-tab-stop").GetAttribute("aria-selected"));
        Assert.Equal([_first.Id, _second.Id, _third.Id], Selected(canvas));
    }

    [Fact]
    public async Task SpaceOnAnEdgeStopStartsTheModeAndKeepsTheEdgeSelected()
    {
        var edge = new Edge(
            new PortEndpoint(_first.Id, PortId.Right),
            new PortEndpoint(_second.Id, PortId.Left)
        );
        _board.AddEdge(edge);
        var canvas = RenderCanvas();
        canvas.Find(".edge-tab-stop").Focus();

        await PressSpace(canvas);
        Focus(canvas, _second);
        await PressSpace(canvas);

        Assert.Equal([edge.Id], canvas.Instance.SelectedEdges.Select(selected => selected.Id));
        Assert.Equal([_second.Id], Selected(canvas));
    }

    [Fact]
    public async Task SpaceWhilePickingAPortCyclesThePortAndLeavesTheSelectionAlone()
    {
        var canvas = await RenderInTheModeWithFirstSelected();
        Focus(canvas, _second);
        await PressEnter(canvas);
        var focusedPortsBefore = canvas.FindAll(".port-focused").Count;

        await PressSpace(canvas);

        Assert.NotEqual(focusedPortsBefore, canvas.FindAll(".port-focused").Count);
        Assert.Equal([_first.Id], Selected(canvas));
    }

    [Fact]
    public async Task ABuiltMultiSelectionGroupsExactlyLikeAPointerBuiltOne()
    {
        var canvas = await RenderInTheModeWithFirstSelected();
        Focus(canvas, _second);
        await PressSpace(canvas);

        await canvas.InvokeAsync(() => canvas.Instance.OnGroupPressed());

        var group = Assert.Single(_board.Groups);
        Assert.Equal([_first.Id, _second.Id], group.MemberIds.ToHashSet());
    }

    [Fact]
    public async Task EscapeEndsPortPickingThenTheModeThenTheEnteredGroupThenTheSelection()
    {
        var group = new Group([_first.Id, _second.Id]);
        _board.AddGroup(group);
        var canvas = RenderCanvas();
        canvas.Find(".group-tab-stop").Focus();
        await PressEnter(canvas);
        await PressSpace(canvas);
        Focus(canvas, _second);
        await PressEnter(canvas);

        await PressEscape(canvas);

        Assert.Empty(canvas.FindAll(".port-focused"));
        Assert.Equal([_first.Id], Selected(canvas));
        Assert.NotEmpty(canvas.FindAll(".entered-group-outline"));

        await PressEscape(canvas);

        Assert.Equal([_first.Id], Selected(canvas));
        Assert.NotEmpty(canvas.FindAll(".entered-group-outline"));

        await PressEscape(canvas);

        Assert.Empty(canvas.FindAll(".entered-group-outline"));
        Assert.Equal([_first.Id, _second.Id], Selected(canvas));
        Assert.Equal("true", canvas.Find(".group-tab-stop").GetAttribute("aria-selected"));

        await PressEscape(canvas);

        Assert.Empty(Selected(canvas));
    }

    [Fact]
    public void AFocusedUnselectedStopDrawsADashedAccentOutline()
    {
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, new Board()));
        var css = StyleBlockText(canvas);

        Assert.Contains(".component-container:focus-visible:not([aria-selected=\"true\"]),", css);
        Assert.Contains(".group-tab-stop:focus-visible:not([aria-selected=\"true\"]),", css);
        var rule = ExtractBlock(css, ".edge-tab-stop:focus-visible:not([aria-selected=\"true\"])");
        Assert.Contains("outline: 2px dashed var(--d12-accent)", rule);
    }

    [Fact]
    public void AFocusedSelectedShapeKeepsItsSelectionOutline()
    {
        var canvas = RenderCanvas();
        var css = StyleBlockText(canvas.FindComponent<ComponentContainer>());

        var focusRule = css.IndexOf(".component-container:focus {", StringComparison.Ordinal);
        var selectedRule = css.IndexOf(".component-container.selected {", StringComparison.Ordinal);
        Assert.InRange(focusRule, 0, selectedRule - 1);
    }
}
