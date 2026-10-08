using Xunit;

namespace D12Canvas.Tests;

// A board can come from a file or the clipboard, and its colours and keywords are written into
// inline styles. Ordinary CSS values pass; anything that could leave its declaration or fetch a
// resource is dropped.
public class InlineStyleValueTests
{
    [Theory]
    [InlineData("#c0392b")]
    [InlineData("rebeccapurple")]
    [InlineData("rgb(10, 20, 30)")]
    [InlineData("hsl(120 50% 50% / 0.5)")]
    [InlineData("var(--brand)")]
    [InlineData("bold")]
    public void AnOrdinaryValuePasses(string value) =>
        Assert.Equal(value, InlineStyleValue.Safe(value));

    [Theory]
    [InlineData("red; background: blue")]
    [InlineData("red } .x { color: blue")]
    [InlineData("url(https://example.com/x.png)")]
    [InlineData("URL(x)")]
    [InlineData("image-set(x 1x)")]
    [InlineData("expression(alert(1))")]
    [InlineData("red /* comment */")]
    [InlineData("\"quoted\"")]
    [InlineData("'quoted'")]
    [InlineData("u\\72l(x)")]
    [InlineData("red\nbackground: blue")]
    [InlineData("</style><script>")]
    public void AValueThatCouldEscapeItsDeclarationIsDropped(string value) =>
        Assert.Null(InlineStyleValue.Safe(value));

    [Fact]
    public void ADroppedValueWritesNoDeclaration() =>
        Assert.Equal("", InlineStyleValue.Declaration("color", "red; background: blue"));

    [Fact]
    public void ASafeValueWritesOneDeclaration() =>
        Assert.Equal("color: red;", InlineStyleValue.Declaration("color", "red"));
}
