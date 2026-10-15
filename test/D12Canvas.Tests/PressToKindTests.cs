using D12Canvas.Pointer;
using Xunit;

namespace D12Canvas.Tests;

// The middle button pans whatever it lands on, and so does the secondary button unless the Menu
// verdict gave the press to the browser; the primary button reads the role, and every role
// resolves to exactly one gesture.
public class PressToKindTests
{
    public static IEnumerable<object[]> EveryRole() =>
        new[]
        {
            HitRole.Canvas,
            HitRole.Instance,
            HitRole.ResizeHandle,
            HitRole.Port,
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

    [Theory]
    [InlineData(HitRole.AuthorContent, PointerPress.BrowserMenuVerdict, "Native")]
    [InlineData(HitRole.AuthorContent, PointerPress.CanvasMenuVerdict, "Pan")]
    [InlineData(HitRole.AuthorContent, null, "Pan")]
    [InlineData(HitRole.Instance, PointerPress.BrowserMenuVerdict, "Native")]
    [InlineData(HitRole.Instance, PointerPress.CanvasMenuVerdict, "Pan")]
    public void TheSecondaryButtonFollowsTheMenuVerdict(
        string role,
        string? verdict,
        string expected
    )
    {
        Assert.Equal(
            Enum.Parse<GestureKind>(expected),
            PressToKind.Resolve(
                PointerEvents.Press(role, PointerPress.SecondaryButton, 0, 0, menuVerdict: verdict)
            )
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
    [InlineData(HitRole.Port, "DragEdgeEnd")]
    [InlineData(HitRole.EdgeEndpoint, "DragEdgeEnd")]
    [InlineData(HitRole.Edge, "SelectEdge")]
    [InlineData(HitRole.EdgeLabel, "SelectEdge")]
    [InlineData(HitRole.AuthorContent, "Native")]
    public void ThePrimaryButtonOnBoardContentResolvesByRole(string role, string expected)
    {
        Assert.Equal(
            Enum.Parse<GestureKind>(expected),
            PressToKind.Resolve(PointerEvents.Press(role, PointerPress.PrimaryButton, 0, 0))
        );
    }

    [Theory]
    [MemberData(nameof(EveryRole))]
    public void ThePrimaryButtonOnALockedEntityIsTheMarqueeWhateverItsRole(string role)
    {
        Assert.Equal(
            GestureKind.MarqueeSelect,
            PressToKind.Resolve(
                PointerEvents.Press(role, PointerPress.PrimaryButton, 0, 0, locked: true)
            )
        );
    }

    [Theory]
    [InlineData(PointerPress.BrowserMenuVerdict)]
    [InlineData(PointerPress.CanvasMenuVerdict)]
    [InlineData(null)]
    public void TheSecondaryButtonOnALockedEntityPansWhateverTheMenuVerdict(string? verdict)
    {
        Assert.Equal(
            GestureKind.Pan,
            PressToKind.Resolve(
                PointerEvents.Press(
                    HitRole.AuthorContent,
                    PointerPress.SecondaryButton,
                    0,
                    0,
                    menuVerdict: verdict,
                    locked: true
                )
            )
        );
    }

    [Fact]
    public void TheMiddleButtonOnALockedEntityPans()
    {
        Assert.Equal(
            GestureKind.Pan,
            PressToKind.Resolve(
                PointerEvents.Press(HitRole.Instance, PointerPress.MiddleButton, 0, 0, locked: true)
            )
        );
    }
}
