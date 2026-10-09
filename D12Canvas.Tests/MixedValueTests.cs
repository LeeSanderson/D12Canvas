using D12Canvas.Panel;
using Xunit;

namespace D12Canvas.Tests;

public class MixedValueTests
{
    public static TheoryData<EditorKind, object?[], bool> Rows =>
        new()
        {
            { EditorKind.Color, ["#FFEB3B", "#ffeb3b"], false },
            { EditorKind.Color, ["#ff0000", "#00ff00"], true },
            { EditorKind.Color, [null, "#ffffff"], true },
            { EditorKind.Color, [null, null], false },
            { EditorKind.Text, ["Hello", "hello"], true },
            { EditorKind.Text, ["Hello", "Hello", "Hello"], false },
            { EditorKind.Text, [null, ""], true },
            { EditorKind.Number, [2.0, 2.0], false },
            { EditorKind.Number, [2.0, 3.0], true },
            { EditorKind.Checkbox, [true, false], true },
            { EditorKind.Dropdown, ["bold", "normal"], true },
            { EditorKind.Custom, ["a", "a"], false },
            { EditorKind.Color, ["#ff0000"], false },
        };

    [Theory]
    [MemberData(nameof(Rows))]
    public void ARowIsMixedWhenItsTargetsDoNotAllHoldAnEqualValue(
        EditorKind kind,
        object?[] values,
        bool expected
    ) => Assert.Equal(expected, MixedValue.IsMixed(kind, values));

    [Fact]
    public void ANoneColourEqualsOnlyAnotherNone()
    {
        Assert.True(MixedValue.AreEqual(EditorKind.Color, null, null));
        Assert.False(MixedValue.AreEqual(EditorKind.Color, null, "#FFFFFF"));
        Assert.False(MixedValue.AreEqual(EditorKind.Color, "#FFFFFF", null));
    }
}
