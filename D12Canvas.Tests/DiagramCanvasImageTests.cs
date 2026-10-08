using Bunit;
using D12Canvas.BuiltIns;
using D12Canvas.Model;
using D12Canvas.Pointer;
using D12Canvas.Registration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace D12Canvas.Tests;

// An image gets its picture from the object menu, a dropped file or a pasted bitmap. Filling
// keeps the box; an image made from a file takes the picture's own size, bounded to half the
// viewport; the bytes are stored once on the board.
public class DiagramCanvasImageTests : ComponentTestBase
{
    private static readonly byte[] PictureBytes = [137, 80, 78, 71, 1, 2, 3];
    private static readonly byte[] OtherPictureBytes = [137, 80, 78, 71, 4, 5, 6];
    private static readonly Bounds ImageBox = new(40, 40, 240, 180);

    public DiagramCanvasImageTests()
    {
        SetupDiagramCanvasJsModule();
        var options = new D12CanvasOptions();
        BuiltInComponents.RegisterAll(options);
        Services.AddSingleton(options.Registry);
    }

    private static ComponentInstance AddImage(Board board, string url = "", Bounds? bounds = null)
    {
        var instance = new ComponentInstance(
            "image",
            new ImageProps(url, "", "cover"),
            bounds ?? ImageBox
        );
        board.AddComponent(instance);
        return instance;
    }

    private static ComponentInstance AddShape(Board board, double x)
    {
        var instance = new ComponentInstance(
            "rectangle",
            new RectangleProps(null, null, 2),
            new Bounds(x, 300, 100, 50)
        );
        board.AddComponent(instance);
        return instance;
    }

    private static string UrlOf(ComponentInstance instance) => ((ImageProps)instance.Props).Url;

    private static string ReferenceTo(byte[] bytes) => AssetReference.Format(Asset.IdFor(bytes));

    private IRenderedComponent<DiagramCanvas> RenderCanvas(Board board) =>
        Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, board).Add(p => p.SnapToGrid, false)
        );

    private static Task Undo(IRenderedComponent<DiagramCanvas> canvas) =>
        canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());

    private static Task Drop(
        IRenderedComponent<DiagramCanvas> canvas,
        double x,
        double y,
        IEnumerable<Guid> hits,
        params HeldImage[] images
    ) =>
        canvas.InvokeAsync(
            () =>
                canvas.Instance.OnImageFilesDropped(
                    images,
                    x,
                    y,
                    hits.Select(id => id.ToString()).ToArray()
                )
        );

    private static Task PasteImages(
        IRenderedComponent<DiagramCanvas> canvas,
        double x,
        double y,
        params HeldImage[] images
    ) => canvas.InvokeAsync(() => canvas.Instance.OnImagesPasted(images, x, y));

    private static List<ComponentInstance> AddedSince(
        Board board,
        params ComponentInstance[] seeded
    ) => board.Components.Where(instance => !seeded.Contains(instance)).ToList();

    private static async Task OpenObjectMenu(
        IRenderedComponent<DiagramCanvas> canvas,
        ComponentInstance instance
    )
    {
        await canvas.Press(
            60,
            60,
            PointerPress.SecondaryButton,
            role: HitRole.Instance,
            entityId: instance.Id
        );
        await canvas.Release(60, 60, PointerPress.SecondaryButton);
    }

    private static string[] MenuLabels(IRenderedComponent<DiagramCanvas> canvas) =>
        canvas.FindAll(".d12-context-menu-label").Select(label => label.TextContent).ToArray();

    private static Task ChooseRow(IRenderedComponent<DiagramCanvas> canvas, string label) =>
        canvas
            .FindAll(".d12-context-menu-item")
            .Single(item => item.QuerySelector(".d12-context-menu-label")!.TextContent == label)
            .ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

    private void PickInThePicker(HeldImage? picked) =>
        CanvasModule.Setup<HeldImage?>("chooseImageFile", _ => true).SetResult(picked);

    [Fact]
    public async Task TheImageRowsShowWhenEveryEntitySelectedIsAnImage()
    {
        var board = new Board();
        var first = AddImage(board);
        AddImage(board, bounds: new Bounds(400, 40, 240, 180));
        var canvas = RenderCanvas(board);
        await canvas.InvokeAsync(() => canvas.Instance.OnSelectAllPressed());

        await OpenObjectMenu(canvas, first);

        Assert.Equal(["Choose image…", "Remove image"], MenuLabels(canvas).TakeLast(2));
    }

    [Fact]
    public async Task TheImageRowsStayAwayWhenAnythingElseIsSelectedToo()
    {
        var board = new Board();
        var image = AddImage(board);
        AddShape(board, 400);
        var canvas = RenderCanvas(board);
        await canvas.InvokeAsync(() => canvas.Instance.OnSelectAllPressed());

        await OpenObjectMenu(canvas, image);

        Assert.DoesNotContain("Choose image…", MenuLabels(canvas));
        Assert.DoesNotContain("Remove image", MenuLabels(canvas));
    }

    [Fact]
    public async Task TheCanvasMenuHasNoImageRows()
    {
        var board = new Board();
        AddImage(board);
        var canvas = RenderCanvas(board);

        await canvas.Press(600, 500, PointerPress.SecondaryButton);
        await canvas.Release(600, 500, PointerPress.SecondaryButton);

        Assert.DoesNotContain("Choose image…", MenuLabels(canvas));
    }

    [Fact]
    public async Task ChooseImageFillsTheEmptyImageKeepingItsBoxAndUndoEmptiesItAgain()
    {
        var board = new Board();
        var image = AddImage(board);
        var canvas = RenderCanvas(board);
        PickInThePicker(HoldImage(CanvasModule, 1, PictureBytes, width: 1000, height: 100));
        await OpenObjectMenu(canvas, image);

        await ChooseRow(canvas, "Choose image…");

        Assert.Equal(ReferenceTo(PictureBytes), UrlOf(image));
        Assert.Equal(ImageBox, image.Bounds);
        Assert.Empty(canvas.FindAll(".d12-image-placeholder"));

        await Undo(canvas);

        Assert.Equal("", UrlOf(image));
        Assert.NotEmpty(canvas.FindAll(".d12-image-placeholder"));
    }

    [Fact]
    public async Task CancellingThePickerChangesNothing()
    {
        var board = new Board();
        var image = AddImage(board);
        var canvas = RenderCanvas(board);
        PickInThePicker(null);
        await OpenObjectMenu(canvas, image);

        await ChooseRow(canvas, "Choose image…");

        Assert.Equal("", UrlOf(image));
        Assert.Empty(board.Assets);
    }

    [Fact]
    public async Task OnePictureGoesIntoEverySelectedImageAsOneHistoryEntry()
    {
        var board = new Board();
        var first = AddImage(board);
        var second = AddImage(board, "https://example.com/a.png", new Bounds(400, 40, 240, 180));
        var canvas = RenderCanvas(board);
        await canvas.InvokeAsync(() => canvas.Instance.OnSelectAllPressed());
        PickInThePicker(HoldImage(CanvasModule, 1, PictureBytes));
        await OpenObjectMenu(canvas, first);

        await ChooseRow(canvas, "Choose image…");

        Assert.Equal(ReferenceTo(PictureBytes), UrlOf(first));
        Assert.Equal(ReferenceTo(PictureBytes), UrlOf(second));

        await Undo(canvas);

        Assert.Equal("", UrlOf(first));
        Assert.Equal("https://example.com/a.png", UrlOf(second));
    }

    [Fact]
    public async Task RemoveImageReturnsTheImageToThePlaceholderAndUndoBringsThePictureBack()
    {
        var board = new Board();
        var reference = board.AddAsset(PictureBytes, "image/png");
        var image = AddImage(board, reference);
        var canvas = RenderCanvas(board);
        await OpenObjectMenu(canvas, image);

        await ChooseRow(canvas, "Remove image");

        Assert.Equal("", UrlOf(image));
        Assert.NotEmpty(canvas.FindAll(".d12-image-placeholder"));

        await Undo(canvas);

        Assert.Equal(reference, UrlOf(image));
    }

    [Fact]
    public async Task DroppingAPictureOnAnEmptyImageFillsItAndKeepsItsBox()
    {
        var board = new Board();
        var image = AddImage(board);
        var canvas = RenderCanvas(board);

        await Drop(canvas, 100, 100, [image.Id], HoldImage(CanvasModule, 1, PictureBytes));

        Assert.Equal(ReferenceTo(PictureBytes), UrlOf(image));
        Assert.Equal(ImageBox, image.Bounds);
        Assert.Single(board.Components);
    }

    [Fact]
    public async Task DroppingAPictureOnTheCanvasMakesAnImageAtItsPixelSizeCentredOnTheDrop()
    {
        var board = new Board();
        var canvas = RenderCanvas(board);

        await Drop(canvas, 300, 200, [], HoldImage(CanvasModule, 1, PictureBytes, 64, 32));

        var made = Assert.Single(board.Components);
        Assert.Equal(new Bounds(268, 184, 64, 32), made.Bounds);
        Assert.Equal(ReferenceTo(PictureBytes), UrlOf(made));
        Assert.Equal([made], canvas.Instance.SelectedComponents);
    }

    [Fact]
    public async Task ALargeDroppedPictureFitsHalfTheViewportAndKeepsItsAspect()
    {
        var board = new Board();
        var canvas = RenderCanvas(board);

        await Drop(canvas, 300, 200, [], HoldImage(CanvasModule, 1, PictureBytes, 4000, 3000));

        var made = Assert.Single(board.Components);
        Assert.True(made.Bounds.Width <= 800 / 2.0 && made.Bounds.Height <= 600 / 2.0);
        Assert.True(made.Bounds.Width == 800 / 2.0 || made.Bounds.Height == 600 / 2.0);
        Assert.Equal(4000.0 / 3000, made.Bounds.Width / made.Bounds.Height, 6);
    }

    [Fact]
    public async Task DroppingAPictureOnAFilledImageMakesANewImage()
    {
        var board = new Board();
        var filled = AddImage(board, "https://example.com/a.png");
        var canvas = RenderCanvas(board);

        await Drop(canvas, 100, 100, [filled.Id], HoldImage(CanvasModule, 1, PictureBytes));

        Assert.Equal("https://example.com/a.png", UrlOf(filled));
        Assert.Single(AddedSince(board, filled));
    }

    [Fact]
    public async Task DroppingAPictureOnALockedEmptyImageLeavesItEmptyAndMakesANewImage()
    {
        var board = new Board();
        var locked = AddImage(board);
        locked.Locked = true;
        var canvas = RenderCanvas(board);

        await Drop(canvas, 100, 100, [locked.Id], HoldImage(CanvasModule, 1, PictureBytes));

        Assert.Equal("", UrlOf(locked));
        Assert.Equal(ReferenceTo(PictureBytes), UrlOf(Assert.Single(AddedSince(board, locked))));
    }

    [Fact]
    public async Task TheImageRowsLeaveOutALockedImage()
    {
        var board = new Board();
        var locked = AddImage(board);
        locked.Locked = true;
        var canvas = RenderCanvas(board);

        await OpenObjectMenu(canvas, locked);

        Assert.DoesNotContain(
            canvas.FindAll(".d12-context-menu-label"),
            label => label.TextContent is "Choose image…" or "Remove image"
        );
    }

    [Fact]
    public async Task TwoFilesDroppedOnTheCanvasCascadeAndUndoAsOne()
    {
        var board = new Board();
        var canvas = RenderCanvas(board);

        await Drop(
            canvas,
            300,
            200,
            [],
            HoldImage(CanvasModule, 1, PictureBytes, 64, 32),
            HoldImage(CanvasModule, 2, OtherPictureBytes, 64, 32)
        );

        var made = board.Components.OrderBy(instance => instance.Bounds.X).ToList();
        Assert.Equal(2, made.Count);
        Assert.Equal(PasteCascade.Step, made[1].Bounds.X - made[0].Bounds.X);
        Assert.Equal(PasteCascade.Step, made[1].Bounds.Y - made[0].Bounds.Y);
        Assert.Equal(2, canvas.Instance.SelectedComponents.Count);

        await Undo(canvas);

        Assert.Empty(board.Components);
    }

    [Fact]
    public async Task ADropOnAnEmptyImageFillsItWithTheFirstFileAndMakesImagesOfTheRestInOneEntry()
    {
        var board = new Board();
        var image = AddImage(board);
        var canvas = RenderCanvas(board);

        await Drop(
            canvas,
            100,
            100,
            [image.Id],
            HoldImage(CanvasModule, 1, PictureBytes),
            HoldImage(CanvasModule, 2, OtherPictureBytes)
        );

        Assert.Equal(ReferenceTo(PictureBytes), UrlOf(image));
        Assert.Equal(
            ReferenceTo(OtherPictureBytes),
            UrlOf(Assert.Single(AddedSince(board, image)))
        );

        await Undo(canvas);

        Assert.Equal("", UrlOf(image));
        Assert.Single(board.Components);
    }

    [Fact]
    public async Task AFileThatIsNotAPlainImageTypeLeavesTheBoardAlone()
    {
        var board = new Board();
        var canvas = RenderCanvas(board);

        await Drop(
            canvas,
            300,
            200,
            [],
            HoldImage(CanvasModule, 1, PictureBytes, mimeType: "text/html")
        );

        Assert.Empty(board.Components);
        Assert.Empty(board.Assets);
    }

    [Fact]
    public async Task APastedBitmapMakesANewImageEvenWithAnEmptyImageSelected()
    {
        var board = new Board();
        var image = AddImage(board);
        var canvas = RenderCanvas(board);
        canvas.ClickOn(canvas.ContainerOf(image.Id));

        await PasteImages(canvas, 500, 400, HoldImage(CanvasModule, 1, PictureBytes, 64, 32));

        Assert.Equal("", UrlOf(image));
        var made = Assert.Single(AddedSince(board, image));
        Assert.Equal(new Bounds(468, 384, 64, 32), made.Bounds);
        Assert.Equal([made], canvas.Instance.SelectedComponents);
    }

    [Fact]
    public async Task PastingTheSamePictureTwiceStoresItsBytesOnce()
    {
        var board = new Board();
        var canvas = RenderCanvas(board);

        await PasteImages(canvas, 300, 200, HoldImage(CanvasModule, 1, PictureBytes));
        await PasteImages(canvas, 300, 200, HoldImage(CanvasModule, 2, PictureBytes));

        Assert.Equal(2, board.Components.Count);
        Assert.Single(board.Assets);
    }

    [Fact]
    public async Task AMenuPasteOfABitmapMakesAnImageAtThePress()
    {
        ReportPlatform(applePlatform: false, asyncClipboard: true);
        CanvasModule
            .Setup<HeldImage[]>("readClipboardImages", _ => true)
            .SetResult([HoldImage(CanvasModule, 1, PictureBytes, 64, 32)]);
        var board = new Board();
        var canvas = RenderCanvas(board);
        await canvas.Press(520, 410, PointerPress.SecondaryButton);
        await canvas.Release(520, 410, PointerPress.SecondaryButton);

        await ChooseRow(canvas, "Paste");

        var made = Assert.Single(board.Components);
        Assert.Equal(new Bounds(488, 394, 64, 32), made.Bounds);
    }
}
