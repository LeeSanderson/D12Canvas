using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace D12Canvas.VisualTests;

// The picker opens only from a click that still counts as user activation once it has crossed
// into .NET and back, and a file arrives only through a real drop or paste event, none of which
// bUnit can see. Playwright reports a file chooser only when the browser really opened one.
public sealed class ImageRoutesProbes(PlaywrightFixture playwright, DemoAppFixture demoApp)
    : InteractionProbe(playwright, demoApp)
{
    private const string EmptyImageId = "f0000000-0000-0000-0000-000000000001";
    private const string FilledImageId = "f0000000-0000-0000-0000-000000000002";

    // A 160x80 PNG.
    private static readonly byte[] Picture = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAKAAAABQCAIAAAARP+ljAAAAlUlEQVR42u3RQQ0AAAjEsBODO3TjAxO8SJMpWDPVelwsACzAAizAAizAAgxYgAVYgAVYgAUYsAALsAALsAALsAADFmABFmABFmABBizAAizAAizAAgxYgAVYgAVYgAVYgAELsAALsAALsAADFmABFmABFmABBuwCYAEWYAEWYAEWYMACLMACLMACLMCABViABViAddUCswasOfQDXTgAAAAASUVORK5CYII="
    );

    protected override string ProbePagePath => "/image-routes-demo";

    protected override int ProbePageInstanceCount => 2;

    private ILocator Instances => Page.Locator(".component-container");

    private ILocator Instance(string id) =>
        Page.Locator($".component-container[data-d12-entity='{id}']");

    private ILocator PictureOf(string id) => Instance(id).Locator("img.d12-image");

    private static FilePayload PicturePayload =>
        new()
        {
            Name = "picture.png",
            MimeType = "image/png",
            Buffer = Picture,
        };

    private async Task<string> SourceOfAsync(ILocator picture) =>
        await picture.GetAttributeAsync("src") ?? "";

    // Dispatches a real drop carrying files built in the page, at a point in container pixels.
    private async Task DropFilesAsync(
        double x,
        double y,
        params (string Type, byte[] Bytes)[] files
    )
    {
        var box = await Page.Locator(".diagram-container").BoundingBoxAsync();
        await Page.EvaluateAsync(
            """
            ([clientX, clientY, files]) => {
                const transfer = new DataTransfer();
                for (const file of files) {
                    const bytes = Uint8Array.from(atob(file.base64), (c) => c.charCodeAt(0));
                    transfer.items.add(new File([bytes], "dropped", { type: file.type }));
                }
                const target = document.elementFromPoint(clientX, clientY);
                for (const type of ["dragenter", "dragover", "drop"]) {
                    target.dispatchEvent(new DragEvent(type, {
                        bubbles: true, cancelable: true, clientX, clientY, dataTransfer: transfer
                    }));
                }
            }
            """,
            new object[]
            {
                (double)(box!.X + x),
                (double)(box.Y + y),
                files
                    .Select(file => new
                    {
                        type = file.Type,
                        base64 = Convert.ToBase64String(file.Bytes),
                    })
                    .ToArray(),
            }
        );
        await SettleAsync();
    }

    // The instance's own bounds, which the inline style carries; a small shape renders larger
    // than its bounds, so its bounding box would not show them.
    private static double StyleLength(string? style, string property) =>
        double.Parse(
            Regex.Match(style ?? "", $@"(?<![-\w]){property}: ([\d.]+)px").Groups[1].Value,
            CultureInfo.InvariantCulture
        );

    [Fact]
    public async Task TheMenusChooseImageRowOpensThePickerAndFillsTheImage()
    {
        await Instance(EmptyImageId).ClickAsync();
        await Instance(EmptyImageId).ClickAsync(new() { Button = MouseButton.Right });

        var chooser = await Page.RunAndWaitForFileChooserAsync(
            () =>
                Page.Locator(".d12-context-menu-item", new() { HasText = "Choose image…" })
                    .ClickAsync()
        );
        await chooser.SetFilesAsync(PicturePayload);

        await Expect(PictureOf(EmptyImageId)).ToHaveCountAsync(1);
        Assert.StartsWith("data:image/png;base64,", await SourceOfAsync(PictureOf(EmptyImageId)));
        await Expect(Instances).ToHaveCountAsync(2);
    }

    [Fact]
    public async Task ThePanelsChooseFileButtonOpensThePickerAndFillsTheImage()
    {
        await Instance(EmptyImageId).ClickAsync();

        var chooser = await Page.RunAndWaitForFileChooserAsync(
            () =>
                Page.Locator(".d12-image-picture-button", new() { HasText = "Choose file…" })
                    .ClickAsync()
        );
        await chooser.SetFilesAsync(PicturePayload);

        await Expect(PictureOf(EmptyImageId)).ToHaveCountAsync(1);
        Assert.StartsWith("data:image/png;base64,", await SourceOfAsync(PictureOf(EmptyImageId)));
    }

    [Fact]
    public async Task ADroppedPictureFillsAnEmptyImageAndLeavesItsBoxAlone()
    {
        var before = await Instance(EmptyImageId).GetAttributeAsync("style");

        await DropFilesAsync(160, 150, ("image/png", Picture));

        await Expect(PictureOf(EmptyImageId)).ToHaveCountAsync(1);
        await Expect(Instances).ToHaveCountAsync(2);
        Assert.Equal(before, await Instance(EmptyImageId).GetAttributeAsync("style"));
    }

    [Fact]
    public async Task ADroppedPictureOnAFilledImageMakesANewImage()
    {
        var before = await SourceOfAsync(PictureOf(FilledImageId));

        await DropFilesAsync(460, 150, ("image/png", Picture));

        await Expect(Instances).ToHaveCountAsync(3);
        Assert.Equal(before, await SourceOfAsync(PictureOf(FilledImageId)));
    }

    [Fact]
    public async Task ADroppedPictureOnTheCanvasMakesAnImageInThePicturesOwnShape()
    {
        await DropFilesAsync(300, 380, ("image/png", Picture));

        await Expect(Instances).ToHaveCountAsync(3);
        var style = await Page.Locator(".component-container[aria-selected='true']")
            .GetAttributeAsync("style");
        Assert.Equal(2.0, StyleLength(style, "width") / StyleLength(style, "height"), 6);
    }

    [Fact]
    public async Task TwoDroppedPicturesOnTheCanvasMakeTwoImages()
    {
        await DropFilesAsync(300, 380, ("image/png", Picture), ("image/png", Picture));

        await Expect(Instances).ToHaveCountAsync(4);
        await Expect(Page.Locator(".component-container[aria-selected='true']"))
            .ToHaveCountAsync(2);
    }

    [Fact]
    public async Task ADroppedFileThatIsNotAnImageChangesNothing()
    {
        await DropFilesAsync(300, 380, ("text/plain", "hello"u8.ToArray()));

        Assert.Empty(await CallsToAsync("OnImageFilesDropped"));
        await Expect(Instances).ToHaveCountAsync(2);
    }

    [Fact]
    public async Task APastedBitmapMakesANewImageWithAnEmptyImageSelected()
    {
        await Instance(EmptyImageId).ClickAsync();

        await Page.EvaluateAsync(
            """
            (base64) => {
                const bytes = Uint8Array.from(atob(base64), (c) => c.charCodeAt(0));
                const transfer = new DataTransfer();
                transfer.items.add(new File([bytes], "pasted.png", { type: "image/png" }));
                document.activeElement.dispatchEvent(new ClipboardEvent("paste", {
                    bubbles: true, cancelable: true, clipboardData: transfer
                }));
            }
            """,
            Convert.ToBase64String(Picture)
        );

        await Expect(Instances).ToHaveCountAsync(3);
        await Expect(Instance(EmptyImageId).Locator(".d12-image-placeholder")).ToHaveCountAsync(1);
    }
}
