using D12Canvas.Model;
using Xunit;

namespace D12Canvas.Tests;

public class BoardTests
{
    [Fact]
    public void AddedComponentIsRetrievableByItsId()
    {
        var board = new Board();
        var instance = new ComponentInstance(
            "sticky-note",
            new TestProps(),
            new Bounds(0, 0, 100, 100)
        );

        board.AddComponent(instance);

        Assert.Same(instance, board.GetComponent(instance.Id));
    }

    [Fact]
    public void GetComponentForAnUnknownIdReturnsNull()
    {
        var board = new Board();

        Assert.Null(board.GetComponent(Guid.NewGuid()));
    }

    [Fact]
    public void RemovedComponentIsNoLongerRetrievable()
    {
        var board = new Board();
        var instance = new ComponentInstance(
            "sticky-note",
            new TestProps(),
            new Bounds(0, 0, 100, 100)
        );
        board.AddComponent(instance);

        board.RemoveComponent(instance.Id);

        Assert.Null(board.GetComponent(instance.Id));
    }

    [Fact]
    public void RemovingAnUnknownIdIsANoOp()
    {
        var board = new Board();
        var instance = new ComponentInstance(
            "sticky-note",
            new TestProps(),
            new Bounds(0, 0, 100, 100)
        );
        board.AddComponent(instance);

        board.RemoveComponent(Guid.NewGuid());

        Assert.Same(instance, board.GetComponent(instance.Id));
    }

    [Fact]
    public void ComponentsExposesEveryAddedInstance()
    {
        var board = new Board();
        var first = new ComponentInstance(
            "sticky-note",
            new TestProps(),
            new Bounds(0, 0, 100, 100)
        );
        var second = new ComponentInstance(
            "rectangle",
            new TestProps(),
            new Bounds(10, 10, 50, 50)
        );

        board.AddComponent(first);
        board.AddComponent(second);

        Assert.Equal(new[] { first, second }, board.Components, ReferenceEqualityComparer.Instance);
    }
}
