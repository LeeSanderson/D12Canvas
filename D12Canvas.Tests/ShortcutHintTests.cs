using Xunit;

namespace D12Canvas.Tests;

public class ShortcutHintTests
{
    [Theory]
    [InlineData(false, "Ctrl+Shift+Z")]
    [InlineData(true, "⇧⌘Z")]
    public void APrimaryShiftChordFollowsEachPlatformsConvention(bool apple, string expected)
    {
        Assert.Equal(
            expected,
            ShortcutHint.Render(new Chord("Z", Primary: true, Shift: true), apple)
        );
    }

    [Theory]
    [InlineData(false, "Ctrl+Alt+Shift+K")]
    [InlineData(true, "⌥⇧⌘K")]
    public void ModifiersKeepTheirFixedOrder(bool apple, string expected)
    {
        Assert.Equal(
            expected,
            ShortcutHint.Render(new Chord("K", Primary: true, Shift: true, Alt: true), apple)
        );
    }

    [Theory]
    [InlineData(false, "Delete")]
    [InlineData(true, "⌫")]
    public void AKeyAMacLabelsDifferentlyUsesItsAppleName(bool apple, string expected)
    {
        Assert.Equal(expected, ShortcutHint.Render(new Chord("Delete", AppleKey: "⌫"), apple));
    }

    [Fact]
    public void AnAppleHintHasNoSeparator()
    {
        Assert.DoesNotContain("+", ShortcutHint.Render(new Chord("A", Primary: true), true));
    }
}
