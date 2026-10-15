using System.Collections.Generic;
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

    // The keydown table matches the physical key, so the hint names whatever the user's layout
    // prints on that key: on AZERTY the key Ctrl+A answers to is labelled Q.
    [Theory]
    [InlineData(false, "Ctrl+Q")]
    [InlineData(true, "⌘Q")]
    public void AChordNamesTheKeyTheLayoutPutsAtItsCode(bool apple, string expected)
    {
        var labels = new Dictionary<string, string> { ["KeyA"] = "q" };

        Assert.Equal(
            expected,
            ShortcutHint.Render(new Chord("A", Primary: true, Code: "KeyA"), apple, labels)
        );
    }

    [Fact]
    public void APunctuationKeyTakesTheLayoutsCharacter()
    {
        var labels = new Dictionary<string, string> { ["BracketRight"] = "+" };

        Assert.Equal(
            "Ctrl++",
            ShortcutHint.Render(new Chord("]", Primary: true, Code: "BracketRight"), false, labels)
        );
    }

    [Fact]
    public void WithoutALayoutTheHintUsesTheUsName()
    {
        Assert.Equal(
            "Ctrl+]",
            ShortcutHint.Render(new Chord("]", Primary: true, Code: "BracketRight"), false, null)
        );
    }

    [Fact]
    public void AChordWithoutACodeIgnoresTheLayout()
    {
        var labels = new Dictionary<string, string> { ["KeyC"] = "j" };

        Assert.Equal("Ctrl+C", ShortcutHint.Render(new Chord("C", Primary: true), false, labels));
    }
}
