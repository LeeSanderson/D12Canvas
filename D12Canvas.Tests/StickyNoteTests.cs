using Bunit;
using D12Canvas.BuiltIns;
using Microsoft.AspNetCore.Components.Web;
using Xunit;

namespace D12Canvas.Tests;

// Ticket 43: Sticky Note's own inline WYSIWYG text editor (ADR 0008), rendered standalone (no
// ParentCanvas/InstanceId cascaded) - so these cover the editor's own UI mechanics only. The
// actual Board/History commit is covered end-to-end in DiagramCanvasInlineTextEditingTests.
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

    [Fact]
    public void DoubleClickEntersEditModeRenderingATextEditorInPlaceOfTheParagraph()
    {
        var stickyNote = Render<StickyNote>(parameters =>
            parameters.Add(p => p.Props, new StickyNoteProps("Original", "#ff0000", "#00ff00", 20))
        );

        stickyNote.Find("p.d12-sticky-note-text").DoubleClick();

        Assert.Empty(stickyNote.FindAll("p.d12-sticky-note-text"));
        Assert.Single(stickyNote.FindAll("textarea.d12-sticky-note-editor"));
    }

    [Fact]
    public void EscapeExitsEditModeAndLeavesTheDisplayedTextUnchanged()
    {
        var stickyNote = Render<StickyNote>(parameters =>
            parameters.Add(p => p.Props, new StickyNoteProps("Original", "#ff0000", "#00ff00", 20))
        );
        stickyNote.Find("p.d12-sticky-note-text").DoubleClick();
        var editor = stickyNote.Find("textarea.d12-sticky-note-editor");
        editor.Input("Changed but discarded");

        editor.KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.Empty(stickyNote.FindAll("textarea.d12-sticky-note-editor"));
        Assert.Contains("Original", stickyNote.Find("p.d12-sticky-note-text").TextContent);
    }

    [Fact]
    public void BlurExitsEditModeWithoutSelfMutatingItsOwnPropsParameter()
    {
        var stickyNote = Render<StickyNote>(parameters =>
            parameters.Add(p => p.Props, new StickyNoteProps("Original", "#ff0000", "#00ff00", 20))
        );
        stickyNote.Find("p.d12-sticky-note-text").DoubleClick();
        var editor = stickyNote.Find("textarea.d12-sticky-note-editor");
        editor.Input("Edited locally");

        editor.Blur();

        Assert.Empty(stickyNote.FindAll("textarea.d12-sticky-note-editor"));
        Assert.Contains("Original", stickyNote.Find("p.d12-sticky-note-text").TextContent);
    }
}
