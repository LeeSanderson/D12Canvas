using Bunit;
using D12Canvas.BuiltIns;
using Microsoft.AspNetCore.Components.Web;
using Xunit;

namespace D12Canvas.Tests;

// Text's own inline WYSIWYG text editor, rendered standalone (no ParentCanvas/InstanceId
// cascaded) - so these cover the editor's own UI mechanics only. The actual Board/History commit
// is covered end-to-end in DiagramCanvasInlineTextEditingTests.
public class TextTests : ComponentTestBase
{
    [Fact]
    public void RendersContentAndFontStylingFromProps()
    {
        var text = Render<Text>(parameters =>
            parameters.Add(
                p => p.Props,
                new TextProps("Hello, D12Canvas", "#ff0000", 24, "bold", "center")
            )
        );

        var element = text.Find(".d12-text");
        Assert.Contains("Hello, D12Canvas", element.TextContent);

        var style = element.GetAttribute("style");
        Assert.Contains("color: #ff0000", style);
        Assert.Contains("font-size: 24px", style);
        Assert.Contains("font-weight: bold", style);
        Assert.Contains("text-align: center", style);
    }

    [Fact]
    public void RendersWithItsDefaultPropsWhenNoneSupplied()
    {
        var text = Render<Text>();

        var element = text.Find(".d12-text");
        var style = element.GetAttribute("style");
        Assert.Contains("color: #000000", style);
        Assert.Contains("font-size: 16px", style);
        Assert.Contains("font-weight: normal", style);
        Assert.Contains("text-align: left", style);
    }

    [Fact]
    public void DoubleClickEntersEditModeRenderingATextEditorInPlaceOfTheParagraph()
    {
        var text = Render<Text>(parameters =>
            parameters.Add(p => p.Props, new TextProps("Original", "#000000", 16, "normal", "left"))
        );

        text.Find("p.d12-text").DoubleClick();

        Assert.Empty(text.FindAll("p.d12-text"));
        Assert.Single(text.FindAll("textarea.d12-text-editor"));
    }

    [Fact]
    public void EscapeExitsEditModeAndLeavesTheDisplayedTextUnchanged()
    {
        var text = Render<Text>(parameters =>
            parameters.Add(p => p.Props, new TextProps("Original", "#000000", 16, "normal", "left"))
        );
        text.Find("p.d12-text").DoubleClick();
        var editor = text.Find("textarea.d12-text-editor");
        editor.Input("Changed but discarded");

        editor.KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.Empty(text.FindAll("textarea.d12-text-editor"));
        Assert.Contains("Original", text.Find("p.d12-text").TextContent);
    }

    [Fact]
    public void BlurExitsEditModeWithoutSelfMutatingItsOwnPropsParameter()
    {
        var text = Render<Text>(parameters =>
            parameters.Add(p => p.Props, new TextProps("Original", "#000000", 16, "normal", "left"))
        );
        text.Find("p.d12-text").DoubleClick();
        var editor = text.Find("textarea.d12-text-editor");
        editor.Input("Edited locally");

        editor.Blur();

        Assert.Empty(text.FindAll("textarea.d12-text-editor"));
        Assert.Contains("Original", text.Find("p.d12-text").TextContent);
    }
}
