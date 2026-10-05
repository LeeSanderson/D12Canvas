using Bunit;
using D12Canvas.Model;
using D12Canvas.Registration;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace D12Canvas.Tests;

// Full keyboard-accessibility interaction: reading-order tab stops (a persisted Group
// collapses to one), and focus-follows-selection in both directions. Native Tab/Shift+Tab
// traversal itself needs no test here - it falls out for free from correct tabindex + DOM order,
// neither of which bUnit's AngleSharp-backed DOM has any special-cased browser tab logic to
// bypass; these tests instead verify the two things that make that native behaviour correct
// (tabindex placement, DOM order) and the C# side of the onfocus wiring.
public class DiagramCanvasFocusFollowsSelectionTests : ComponentTestBase
{
    private const string ComponentTypeKey = "test-props";

    public DiagramCanvasFocusFollowsSelectionTests()
    {
        SetupDiagramCanvasJsModule();
        SetupComponentContainerJsModule();

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

    private static ComponentInstance AddInstance(Board board, string text, double x, double y)
    {
        var instance = new ComponentInstance(
            ComponentTypeKey,
            new TestProps(text),
            new Bounds(x, y, 50, 50)
        );
        board.AddComponent(instance);
        return instance;
    }

    private static string? TextOf(AngleSharp.Dom.IElement container) =>
        container.QuerySelector(".test-props-component")?.TextContent;

    [Fact]
    public void InstancesAreOrderedByCurrentOnScreenPositionNotCreationOrder()
    {
        var board = new Board();
        // Added in an order that's neither reading order nor sorted by any single axis.
        AddInstance(board, "BottomLeft", x: 100, y: 100);
        AddInstance(board, "TopLeft", x: 0, y: 0);
        AddInstance(board, "TopRight", x: 200, y: 0);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        var containers = canvas.FindAll(".component-container");

        Assert.Equal("TopLeft", TextOf(containers[0]));
        Assert.Equal("TopRight", TextOf(containers[1]));
        Assert.Equal("BottomLeft", TextOf(containers[2]));
    }

    [Fact]
    public void EveryUngroupedInstanceHasTabIndexZero()
    {
        var board = new Board();
        AddInstance(board, "Only", x: 0, y: 0);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        Assert.Equal("0", canvas.Find(".component-container").GetAttribute("tabindex"));
    }

    [Fact]
    public async Task GroupedMembersLoseTheirOwnTabStopInFavourOfTheGroupsSingleOne()
    {
        var board = new Board();
        AddInstance(board, "First", x: 0, y: 0);
        AddInstance(board, "Second", x: 100, y: 0);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        canvas.ClickOn(canvas.FindAll(".component-container")[0]);
        canvas.ClickOn(canvas.FindAll(".component-container")[1], shift: true);
        await canvas.InvokeAsync(() => canvas.Instance.OnGroupPressed());

        var containers = canvas.FindAll(".component-container");
        Assert.Null(containers[0].GetAttribute("tabindex"));
        Assert.Null(containers[1].GetAttribute("tabindex"));

        var groupStop = Assert.Single(canvas.FindAll(".group-tab-stop"));
        Assert.Equal("0", groupStop.GetAttribute("tabindex"));
        Assert.Equal("group", groupStop.GetAttribute("role"));
    }

    [Fact]
    public async Task AnUngroupedInstanceGetsItsTabStopBackAfterUngrouping()
    {
        var board = new Board();
        AddInstance(board, "First", x: 0, y: 0);
        AddInstance(board, "Second", x: 100, y: 0);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        canvas.ClickOn(canvas.FindAll(".component-container")[0]);
        canvas.ClickOn(canvas.FindAll(".component-container")[1], shift: true);
        await canvas.InvokeAsync(() => canvas.Instance.OnGroupPressed());

        await canvas.InvokeAsync(() => canvas.Instance.OnUngroupPressed());

        Assert.Empty(canvas.FindAll(".group-tab-stop"));
        var containers = canvas.FindAll(".component-container");
        Assert.Equal("0", containers[0].GetAttribute("tabindex"));
        Assert.Equal("0", containers[1].GetAttribute("tabindex"));
    }

    [Fact]
    public void FocusingAnInstanceSelectsItAndReplacesAnyPriorSelection()
    {
        var board = new Board();
        AddInstance(board, "First", x: 0, y: 0);
        AddInstance(board, "Second", x: 100, y: 0);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        canvas.ClickOn(canvas.FindAll(".component-container")[0]);

        canvas.FindAll(".component-container")[1].Focus();

        var containers = canvas.FindAll(".component-container");
        Assert.Null(containers[0].GetAttribute("aria-selected"));
        Assert.Equal("true", containers[1].GetAttribute("aria-selected"));
    }

    [Fact]
    public async Task FocusingAGroupsTabStopSelectsTheWholeGroup()
    {
        var board = new Board();
        AddInstance(board, "First", x: 0, y: 0);
        AddInstance(board, "Second", x: 100, y: 0);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        canvas.ClickOn(canvas.FindAll(".component-container")[0]);
        canvas.ClickOn(canvas.FindAll(".component-container")[1], shift: true);
        await canvas.InvokeAsync(() => canvas.Instance.OnGroupPressed());
        await canvas.ClickCanvas(400, 400); // clear the selection first

        canvas.Find(".group-tab-stop").Focus();

        var containers = canvas.FindAll(".component-container");
        Assert.Equal("true", containers[0].GetAttribute("aria-selected"));
        Assert.Equal("true", containers[1].GetAttribute("aria-selected"));
        Assert.Equal("true", canvas.Find(".group-tab-stop").GetAttribute("aria-selected"));
    }

    // Focus drives selection and never the reverse: the browser-side listener's one focus write
    // per press goes to the canvas, and no click outcome, plain or Shift, on a grouped member or
    // not, asks for any further focus move.
    [Fact]
    public async Task NoClickOnAnInstanceMovesDomFocus()
    {
        var board = new Board();
        AddInstance(board, "First", x: 0, y: 0);
        AddInstance(board, "Second", x: 100, y: 0);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        canvas.ClickOn(canvas.FindAll(".component-container")[0]);
        canvas.ClickOn(canvas.FindAll(".component-container")[1], shift: true);
        await canvas.InvokeAsync(() => canvas.Instance.OnGroupPressed());
        var focusCallsAfterGrouping = FocusCalls();
        canvas.ClickOn(canvas.FindAll(".component-container")[0]);

        Assert.Equal(["focusGroupTabStop"], focusCallsAfterGrouping);
        Assert.Equal(focusCallsAfterGrouping, FocusCalls());
    }

    private List<string> FocusCalls() =>
        JSInterop
            .Invocations.Select(invocation => invocation.Identifier)
            .Where(identifier => identifier.StartsWith("focus"))
            .ToList();

    [Fact]
    public async Task GroupingASelectionMovesDomFocusToTheNewGroupsOwnTabStop()
    {
        var board = new Board();
        AddInstance(board, "First", x: 0, y: 0);
        AddInstance(board, "Second", x: 100, y: 0);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        canvas.ClickOn(canvas.FindAll(".component-container")[0]);
        canvas.ClickOn(canvas.FindAll(".component-container")[1], shift: true);

        await canvas.InvokeAsync(() => canvas.Instance.OnGroupPressed());

        Assert.Single(board.Groups);
        Assert.Single(JSInterop.Invocations["focusGroupTabStop"]);
        Assert.Equal("true", canvas.Find(".group-tab-stop").GetAttribute("aria-selected"));
    }

    [Fact]
    public async Task AGroupTabStopIsPositionedByTheGroupsComputedBoundsAndInterleavesByReadingOrder()
    {
        var board = new Board();
        AddInstance(board, "Above", x: 0, y: 0);
        AddInstance(board, "GroupA", x: 0, y: 100);
        AddInstance(board, "GroupB", x: 100, y: 100);
        AddInstance(board, "Below", x: 0, y: 300);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        var containers = canvas.FindAll(".component-container");
        canvas.ClickOn(containers[1]);
        canvas.ClickOn(containers[2], shift: true);
        await canvas.InvokeAsync(() => canvas.Instance.OnGroupPressed());

        // Filtered to actually-tabbable elements: the now-grouped members' own containers still
        // render alongside the group's stop (just with no tabindex), and one of them can tie the
        // group stop's own sort key (its bounds start at the same top-left corner as its
        // leftmost member) - harmless for real Tab traversal, which skips non-tabbable elements
        // regardless of DOM position, but not a meaningful position to assert on here.
        var stops = canvas
            .FindAll(".component-container, .group-tab-stop")
            .Where(e => e.GetAttribute("tabindex") == "0")
            .ToList();

        Assert.Equal(3, stops.Count);
        Assert.Equal("Above", TextOf(stops[0]));
        Assert.Equal("group-tab-stop", stops[1].ClassList[0]);
        Assert.Equal("Below", TextOf(stops[2]));
    }
}
