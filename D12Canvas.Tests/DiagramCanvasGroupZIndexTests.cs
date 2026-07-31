using System.Threading.Tasks;
using Bunit;
using D12Canvas.Model;
using D12Canvas.Registration;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace D12Canvas.Tests;

// A Group has no z-position field of its own (Model/Group.cs carries none) - layering a
// persisted Group bulk-writes every member's ZIndex, preserving their relative order.
// ExpandedSelection() already flattens a selected Group (nested or not) down to its raw leaf
// ComponentInstance ids before RestackSelection/ApplyZIndexChange ever see the selection, so
// there is no separate group-layering code path to add - these tests pin that behaviour
// specifically for a *persisted* Group entity (including nesting), which the ad-hoc shift-click
// multi-selection tests never exercised.
public class DiagramCanvasGroupZIndexTests : ComponentTestBase
{
    private const string ComponentTypeKey = "test-props";

    public DiagramCanvasGroupZIndexTests()
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

    private static ComponentInstance AddInstance(Board board, double x, int zIndex)
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

    [Fact]
    public async Task BringToFrontPressedAppliesToEveryMemberOfAPersistedGroupPreservingTheirRelativeOrder()
    {
        var board = new Board();
        var first = AddInstance(board, 0, zIndex: 2);
        var second = AddInstance(board, 100, zIndex: 3);
        AddInstance(board, 200, zIndex: 9); // untouched, outside the group
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        SelectBoth(canvas);
        await canvas.InvokeAsync(() => canvas.Instance.OnGroupPressed());

        await canvas.InvokeAsync(() => canvas.Instance.OnBringToFrontPressed());

        // Both land above the untouched third instance (9), and first (originally the lower of
        // the two) stays below second - a persisted Group's bulk move never collapses its members
        // onto the same tied value, which would erase their relative stacking order.
        Assert.Equal(10, first.ZIndex);
        Assert.Equal(11, second.ZIndex);
    }

    [Fact]
    public async Task SendToBackPressedAppliesToEveryMemberOfAPersistedGroupPreservingTheirRelativeOrder()
    {
        var board = new Board();
        AddInstance(board, 0, zIndex: 0); // untouched, outside the group
        var first = AddInstance(board, 100, zIndex: 5);
        var second = AddInstance(board, 200, zIndex: 8);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        var containers = canvas.FindAll(".component-container");
        containers[1].Click();
        containers[2].Click(new MouseEventArgs { ShiftKey = true });
        await canvas.InvokeAsync(() => canvas.Instance.OnGroupPressed());

        await canvas.InvokeAsync(() => canvas.Instance.OnSendToBackPressed());

        Assert.Equal(-2, first.ZIndex);
        Assert.Equal(-1, second.ZIndex);
    }

    [Fact]
    public async Task BringForwardPressedRewritesEveryMemberOfAPersistedGroupInOneGesturePreservingTheirRelativeOrder()
    {
        var board = new Board();
        var first = AddInstance(board, 0, zIndex: 2); // group
        var second = AddInstance(board, 100, zIndex: 4); // group
        AddInstance(board, 200, zIndex: 6); // untouched, outside the group
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        SelectBoth(canvas);
        await canvas.InvokeAsync(() => canvas.Instance.OnGroupPressed());

        await canvas.InvokeAsync(() => canvas.Instance.OnBringForwardPressed());

        // Each member independently skips to its own next distinct rank, evaluated against the
        // board's state before either of this gesture's own writes - first lands tied with
        // second's prior rank, second lands tied with the untouched instance - and their relative
        // order (first below second) survives the move.
        Assert.Equal(4, first.ZIndex);
        Assert.Equal(6, second.ZIndex);
    }

    [Fact]
    public async Task SendBackwardPressedRewritesEveryMemberOfAPersistedGroupInOneGesturePreservingTheirRelativeOrder()
    {
        var board = new Board();
        AddInstance(board, 0, zIndex: 4); // untouched, outside the group
        var first = AddInstance(board, 100, zIndex: 6); // group
        var second = AddInstance(board, 200, zIndex: 8); // group
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        var containers = canvas.FindAll(".component-container");
        containers[1].Click();
        containers[2].Click(new MouseEventArgs { ShiftKey = true });
        await canvas.InvokeAsync(() => canvas.Instance.OnGroupPressed());

        await canvas.InvokeAsync(() => canvas.Instance.OnSendBackwardPressed());

        // Each member independently skips to its own next distinct rank below, evaluated
        // against the board's state before either of this gesture's own writes - second lands
        // tied with first's prior rank, first lands tied with the untouched instance - and their
        // relative order (first below second) survives the move.
        Assert.Equal(4, first.ZIndex);
        Assert.Equal(6, second.ZIndex);
    }

    // A nested group ([A, B] grouped into an inner Group, then [inner, C] grouped into an outer
    // one) exercises the ExpandedSelection recursion - layering the outer group's selection must
    // bulk-write every leaf (A, B, and C), not just the outer group's immediate members, still
    // preserving their relative order to each other.
    [Fact]
    public async Task LayeringANestedGroupRewritesEveryLeafMembersZIndexPreservingRelativeOrder()
    {
        var board = new Board();
        var a = AddInstance(board, 0, zIndex: 1);
        var b = AddInstance(board, 100, zIndex: 2);
        var c = AddInstance(board, 200, zIndex: 3);
        var untouched = AddInstance(board, 300, zIndex: 9);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        SelectBoth(canvas); // selects A and B
        await canvas.InvokeAsync(() => canvas.Instance.OnGroupPressed()); // inner group = {A, B}
        canvas.FindAll(".component-container")[2].Click(new MouseEventArgs { ShiftKey = true });
        await canvas.InvokeAsync(() => canvas.Instance.OnGroupPressed()); // outer group = {inner, C}
        Assert.Equal(2, board.Groups.Count);

        await canvas.InvokeAsync(() => canvas.Instance.OnBringToFrontPressed());

        Assert.Equal(10, a.ZIndex);
        Assert.Equal(11, b.ZIndex);
        Assert.Equal(12, c.ZIndex);
        Assert.Equal(9, untouched.ZIndex);
    }
}
