using D12Canvas.Pointer;
using Xunit;

namespace D12Canvas.Tests;

// The secondary and middle buttons pan whatever they land on; only the primary button reads the
// role, and for now only the `canvas` role is owned by the spine on that button.
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
    [MemberData(nameof(EveryRole))]
    public void ThePrimaryButtonOnAnyOtherRoleIsNotYetOwned(string role)
    {
        if (role == HitRole.Canvas)
        {
            return;
        }

        Assert.Null(
            PressToKind.Resolve(PointerEvents.Press(role, PointerPress.PrimaryButton, 0, 0))
        );
    }
}
