using D12Canvas.Model;
using D12Canvas.Pointer;
using Xunit;

namespace D12Canvas.Tests;

// A press on author content selects its instance if the selection does not already hold it. A
// control inside an edge's label belongs to the edge, which is not an instance, so a press there
// leaves the selection as it was.
public class NativeGestureTests
{
    [Fact]
    public void APressInsideAnEdgesLabelLeavesTheEdgeSelected()
    {
        var board = new Board();
        var edge = new Edge(new FloatingEndpoint(0, 0), new FloatingEndpoint(100, 0));
        board.AddEdge(edge);
        var context = new FakeGestureContext(board);
        context.SelectEdge(edge.Id);

        new NativeGesture(
            PointerEvents.Press(HitRole.AuthorContent, PointerPress.PrimaryButton, 50, 0, edge.Id),
            context
        ).Begin();

        Assert.Equal([edge.Id], context.SelectedEdgeIds);
        Assert.Empty(context.SelectedInstanceIds);
    }

    [Fact]
    public void APressInsideAnInstanceAddsItToTheSelection()
    {
        var board = new Board();
        var instance = new ComponentInstance(
            "test-props",
            new TestProps(),
            new Bounds(0, 0, 50, 50)
        );
        board.AddComponent(instance);
        var context = new FakeGestureContext(board);

        new NativeGesture(
            PointerEvents.Press(
                HitRole.AuthorContent,
                PointerPress.PrimaryButton,
                10,
                10,
                instance.Id
            ),
            context
        ).Begin();

        Assert.Contains(instance.Id, context.SelectedInstanceIds);
    }
}
