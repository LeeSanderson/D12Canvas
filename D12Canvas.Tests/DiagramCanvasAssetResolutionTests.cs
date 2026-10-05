using Bunit;
using D12Canvas.BuiltIns;
using D12Canvas.Model;
using D12Canvas.Registration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace D12Canvas.Tests;

// The canvas swaps an asset reference for the asset's data: URI before it binds props, so the
// component only ever sees a URL. A value that is not a reference, or a reference to an asset the
// board does not hold, reaches the component exactly as written, and an edit the component
// commits back never carries the data URI into the board.
public class DiagramCanvasAssetResolutionTests : ComponentTestBase
{
    private static readonly byte[] PngBytes = AssetTestComponent.PngBytes;

    public DiagramCanvasAssetResolutionTests()
    {
        SetupDiagramCanvasJsModule();
        SetupComponentContainerJsModule();

        var options = new D12CanvasOptions();
        AssetTestComponent.Register(options);
        options.RegisterComponent<Image, ImageProps>(
            "image",
            builder =>
            {
                builder.DisplayName = "Image";
                builder.AccessibleName = "Image";
                builder.DefaultProps = new ImageProps("", "", "cover");
            }
        );
        Services.AddSingleton(options.Registry);
    }

    private static ComponentInstance AddAssetComponent(Board board, string src)
    {
        var instance = new ComponentInstance(
            AssetTestComponent.Key,
            new AssetTestProps(src, "A caption"),
            new Bounds(0, 0, 100, 100)
        );
        board.AddComponent(instance);
        return instance;
    }

    [Fact]
    public void ADeclaredAssetReferenceIsBoundAsTheAssetsDataUri()
    {
        var board = new Board();
        var reference = board.AddAsset(PngBytes, "image/png");
        AddAssetComponent(board, reference);

        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        var image = canvas.Find(".asset-test-image");
        Assert.Equal(
            $"data:image/png;base64,{Convert.ToBase64String(PngBytes)}",
            image.GetAttribute("src")
        );
        Assert.Equal("A caption", image.GetAttribute("alt"));
    }

    [Fact]
    public void ResolvingLeavesTheInstancesOwnPropsUntouched()
    {
        var board = new Board();
        var reference = board.AddAsset(PngBytes, "image/png");
        var instance = AddAssetComponent(board, reference);

        Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        Assert.Equal(reference, ((AssetTestProps)instance.Props).Src);
    }

    [Fact]
    public void AnOrdinaryUrlInADeclaredPropertyIsBoundAsWritten()
    {
        var board = new Board();
        AddAssetComponent(board, "https://example.com/photo.png");

        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        Assert.Equal(
            "https://example.com/photo.png",
            canvas.Find(".asset-test-image").GetAttribute("src")
        );
    }

    [Fact]
    public void AReferenceToAnAssetTheBoardDoesNotHoldIsLeftUnresolved()
    {
        var board = new Board();
        AddAssetComponent(board, "asset:sha256-missing");

        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        Assert.Equal("asset:sha256-missing", canvas.Find(".asset-test-image").GetAttribute("src"));
    }

    [Fact]
    public void AnAssetAddedAfterTheFirstRenderResolvesOnTheNext()
    {
        var board = new Board();
        var expectedReference = AssetReference.Format(Asset.IdFor(PngBytes));
        AddAssetComponent(board, expectedReference);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        Assert.Equal(expectedReference, canvas.Find(".asset-test-image").GetAttribute("src"));

        board.AddAsset(PngBytes, "image/png");
        canvas.Render();

        Assert.StartsWith(
            "data:image/png;base64,",
            canvas.Find(".asset-test-image").GetAttribute("src")
        );
    }

    [Fact]
    public async Task AnEditCommittedWithTheBoundPropsWritesTheReferenceBackNotTheDataUri()
    {
        var board = new Board();
        var reference = board.AddAsset(PngBytes, "image/png");
        var instance = AddAssetComponent(board, reference);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        canvas.Find(".asset-test-edit").Click();

        var edited = (AssetTestProps)instance.Props;
        Assert.Equal("edited", edited.Caption);
        Assert.Equal(reference, edited.Src);
        Assert.StartsWith(
            "data:image/png;base64,",
            canvas.Find(".asset-test-image").GetAttribute("src")
        );

        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());

        var restored = (AssetTestProps)instance.Props;
        Assert.Equal("A caption", restored.Caption);
        Assert.Equal(reference, restored.Src);
    }

    [Fact]
    public void TheBuiltInImageRendersAnAssetReferenceAsItsPicture()
    {
        var board = new Board();
        var reference = board.AddAsset(PngBytes, "image/png");
        board.AddComponent(
            new ComponentInstance(
                "image",
                new ImageProps(reference, "A photo", "cover"),
                new Bounds(0, 0, 200, 150)
            )
        );

        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        Assert.StartsWith("data:image/png;base64,", canvas.Find(".d12-image").GetAttribute("src"));
    }
}
