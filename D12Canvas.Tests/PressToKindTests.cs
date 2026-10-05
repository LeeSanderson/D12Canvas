using D12Canvas.Pointer;
using Xunit;

namespace D12Canvas.Tests;

// The secondary and middle buttons pan whatever they land on; only the primary button reads the
// role. The spine owns the primary button on bare canvas, on instances, the selection box and every
// resize handle, and on author content; the port and edge roles are still served by the old
// handlers.
public class PressToKindTests
{
    public static IEnumerable<object[]> EveryRole() =>
        new[]
        {
            HitRole.Canvas,
            HitRole.Instance,
            HitRole.ResizeHandle,
            HitRole.Port,
            HitRole.PortStrip,
            HitRole.Edge,
            HitRole.EdgeEndpoint,
            HitRole.EdgeLabel,
            HitRole.SelectionBounds,
            HitRole.SelectionHandle,
            HitRole.AuthorContent,
        }.Select(role => new object[] { role });

    [Theory]
    [MemberData(nameof(EveryRole))]
    public void TheSecondaryAndMiddleButtonsPanOnEveryRole(string role)
    {
        Assert.Equal(
            GestureKind.Pan,
            PressToKind.Resolve(PointerEvents.Press(role, PointerPress.SecondaryButton, 0, 0))
        );
        Assert.Equal(
            GestureKind.Pan,
            PressToKind.Resolve(PointerEvents.Press(role, PointerPress.MiddleButton, 0, 0))
        );
    }

    [Fact]
    public void ThePrimaryButtonOnEmptyCanvasIsTheMarquee()
    {
        Assert.Equal(
            GestureKind.MarqueeSelect,
            PressToKind.Resolve(
                PointerEvents.Press(HitRole.Canvas, PointerPress.PrimaryButton, 0, 0)
            )
        );
    }

    [Theory]
    [InlineData(HitRole.Instance, "MoveSelection")]
    [InlineData(HitRole.SelectionBounds, "MoveSelection")]
    [InlineData(HitRole.ResizeHandle, "ResizeSelection")]
    [InlineData(HitRole.SelectionHandle, "ResizeSelection")]
    [InlineData(HitRole.AuthorContent, "Native")]
    public void ThePrimaryButtonOnBoardContentResolvesByRole(string role, string expected)
    {
        Assert.Equal(
            Enum.Parse<GestureKind>(expected),
            PressToKind.Resolve(PointerEvents.Press(role, PointerPress.PrimaryButton, 0, 0))
        );
    }

    [Theory]
    [InlineData(HitRole.Port)]
    [InlineData(HitRole.PortStrip)]
    [InlineData(HitRole.Edge)]
    [InlineData(HitRole.EdgeEndpoint)]
    [InlineData(HitRole.EdgeLabel)]
    public void ThePrimaryButtonOnARoleTheOldHandlersStillServeIsNotOwned(string role)
    {
        Assert.Null(
            PressToKind.Resolve(PointerEvents.Press(role, PointerPress.PrimaryButton, 0, 0))
        );
    }
}
