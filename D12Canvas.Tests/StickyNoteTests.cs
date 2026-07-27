using Bunit;
using D12Canvas.BuiltIns;
using Xunit;

namespace D12Canvas.Tests;

public class StickyNoteTests : ComponentTestBase
{
    [Fact]
    public void RendersTextColorAndFontSizeFromProps()
    {
        var stickyNote = Render<StickyNote>(parameters =>
            parameters.Add(
                p => p.Props,
                new StickyNoteProps("Remember the milk", "#ff0000", "#00ff00", 20)
            )
        );

        var note = stickyNote.Find(".d12-sticky-note");
        Assert.Contains("background-color: #ff0000", note.GetAttribute("style"));
        Assert.Contains("Remember the milk", note.TextContent);

        var text = stickyNote.Find(".d12-sticky-note-text");
        var textStyle = text.GetAttribute("style");
        Assert.Contains("color: #00ff00", textStyle);
        Assert.Contains("font-size: 20px", textStyle);
    }

    [Fact]
    public void RendersWithItsDefaultPropsWhenNoneSupplied()
    {
        var stickyNote = Render<StickyNote>();

        var note = stickyNote.Find(".d12-sticky-note");
        Assert.Contains("background-color: #FFEB3B", note.GetAttribute("style"));

        var text = stickyNote.Find(".d12-sticky-note-text");
        var textStyle = text.GetAttribute("style");
        Assert.Contains("color: #000000", textStyle);
        Assert.Contains("font-size: 14px", textStyle);
    }
}
