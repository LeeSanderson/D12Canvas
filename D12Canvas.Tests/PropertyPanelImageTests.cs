using Bunit;
using D12Canvas.BuiltIns;
using D12Canvas.Model;
using D12Canvas.Registration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace D12Canvas.Tests;

// The image's Url row is a Choose file button and a Remove image button, with no text field to
// type an address into.
public class PropertyPanelImageTests : ComponentTestBase
{
    private static readonly byte[] PictureBytes = [137, 80, 78, 71, 1, 2, 3];
    private static readonly Bounds ImageBox = new(40, 40, 240, 180);

    private readonly BunitJSModuleInterop _imageFiles;

    public PropertyPanelImageTests()
    {
        SetupDiagramCanvasJsModule();
        _imageFiles = JSInterop.SetupModule(ImagePictureEditor.ModulePath);
        var options = new D12CanvasOptions();
        BuiltInComponents.RegisterAll(options);
        Services.AddSingleton(options.Registry);
    }

    private static string UrlOf(ComponentInstance instance) => ((ImageProps)instance.Props).Url;

    private (
        IRenderedComponent<DiagramCanvas> Canvas,
        IRenderedComponent<PropertyPanel> Panel
    ) RenderSelected(Board board, ComponentInstance image)
    {
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        var panel = Render<PropertyPanel>(parameters =>
            parameters.Add(p => p.Canvas, canvas.Instance)
        );
        canvas.ClickOn(canvas.ContainerOf(image.Id));
        return (canvas, panel);
    }

    private static ComponentInstance AddImage(Board board, string url = "")
    {
        var instance = new ComponentInstance("image", new ImageProps(url, "", "cover"), ImageBox);
        board.AddComponent(instance);
        return instance;
    }

    private static Task Click(IRenderedComponent<PropertyPanel> panel, string label) =>
        panel
            .FindAll(".d12-image-picture-button")
            .Single(button => button.TextContent == label)
            .ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

    [Fact]
    public void TheUrlRowOffersTwoButtonsAndNoTextField()
    {
        var board = new Board();
        var image = AddImage(board);

        var (_, panel) = RenderSelected(board, image);

        var row = panel.Find("#d12-property-panel-field-Url");
        Assert.Empty(row.QuerySelectorAll("input"));
        Assert.Equal(
            ["Choose file…", "Remove image"],
            row.QuerySelectorAll("button").Select(button => button.TextContent)
        );
    }

    [Fact]
    public async Task ChooseFileFillsTheSelectedEmptyImageKeepingItsBoxAndUndoEmptiesIt()
    {
        var board = new Board();
        var image = AddImage(board);
        var (canvas, panel) = RenderSelected(board, image);
        _imageFiles
            .Setup<HeldImage?>("chooseImageFile", _ => true)
            .SetResult(HoldImage(_imageFiles, 1, PictureBytes));

        await Click(panel, "Choose file…");

        Assert.Equal(AssetReference.Format(Asset.IdFor(PictureBytes)), UrlOf(image));
        Assert.Equal(ImageBox, image.Bounds);
        Assert.Single(board.Assets);

        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());

        Assert.Equal("", UrlOf(image));
    }

    [Fact]
    public async Task CancellingThePickerCommitsNothing()
    {
        var board = new Board();
        var image = AddImage(board);
        var (_, panel) = RenderSelected(board, image);
        _imageFiles.Setup<HeldImage?>("chooseImageFile", _ => true).SetResult(null);

        await Click(panel, "Choose file…");

        Assert.Equal("", UrlOf(image));
        Assert.Empty(board.Assets);
    }

    [Fact]
    public async Task RemoveImageReturnsTheImageToThePlaceholderAndUndoRestoresIt()
    {
        var board = new Board();
        var reference = board.AddAsset(PictureBytes, "image/png");
        var image = AddImage(board, reference);
        var (canvas, panel) = RenderSelected(board, image);

        await Click(panel, "Remove image");

        Assert.Equal("", UrlOf(image));
        Assert.NotEmpty(canvas.FindAll(".d12-image-placeholder"));

        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());

        Assert.Equal(reference, UrlOf(image));
    }
}
