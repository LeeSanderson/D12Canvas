using D12Canvas.BuiltIns;
using D12Canvas.Model;
using D12Canvas.Registration;
using Xunit;

namespace D12Canvas.Tests;

public class ImagePictureTests
{
    private static readonly Bounds Viewport = new(0, 0, 800, 600);
    private static readonly ComponentSize Fallback = new(240, 180);

    private static ComponentInstance AddImage(Board board, string url)
    {
        var instance = new ComponentInstance(
            "image",
            new ImageProps(url, "", "cover"),
            new Bounds(0, 0, 240, 180)
        );
        board.AddComponent(instance);
        return instance;
    }

    private static ComponentInstance AddRectangle(Board board)
    {
        var instance = new ComponentInstance(
            "rectangle",
            new RectangleProps(null, null, 2),
            new Bounds(0, 0, 100, 50)
        );
        board.AddComponent(instance);
        return instance;
    }

    [Fact]
    public void ASmallPictureLandsAtItsOwnPixelSize()
    {
        Assert.Equal(new ComponentSize(64, 32), ImagePicture.SizeFor(64, 32, Viewport, Fallback));
    }

    [Fact]
    public void ALargePictureShrinksToHalfTheViewportKeepingItsAspect()
    {
        var size = ImagePicture.SizeFor(4000, 1000, Viewport, Fallback);

        Assert.Equal(Viewport.Width / 2, size.Width);
        Assert.Equal(4.0, size.Width / size.Height, 6);
    }

    [Fact]
    public void ATallPictureIsBoundedByHalfTheViewportsHeight()
    {
        var size = ImagePicture.SizeFor(500, 2000, Viewport, Fallback);

        Assert.Equal(Viewport.Height / 2, size.Height);
        Assert.Equal(0.25, size.Width / size.Height, 6);
    }

    [Fact]
    public void ZoomingOutBeforehandLetsThePictureLandLarger()
    {
        var zoomedOut = new Bounds(0, 0, 1600, 1200);

        Assert.True(
            ImagePicture.SizeFor(4000, 3000, zoomedOut, Fallback).Width
                > ImagePicture.SizeFor(4000, 3000, Viewport, Fallback).Width
        );
    }

    [Fact]
    public void APictureWithNoSizeTakesTheFallback()
    {
        Assert.Equal(Fallback, ImagePicture.SizeFor(0, 0, Viewport, Fallback));
    }

    [Fact]
    public void ADropFillsTheTopmostInstanceWhenItIsAnEmptyImage()
    {
        var board = new Board();
        var empty = AddImage(board, "");

        Assert.Same(empty, ImagePicture.FillTarget(board, [empty.Id]));
    }

    [Fact]
    public void ADropOnAFilledImageFillsNothing()
    {
        var board = new Board();
        var filled = AddImage(board, "https://example.com/a.png");

        Assert.Null(ImagePicture.FillTarget(board, [filled.Id]));
    }

    [Fact]
    public void ADropOnAnInstanceAboveAnEmptyImageFillsNothing()
    {
        var board = new Board();
        var empty = AddImage(board, "");
        var shape = AddRectangle(board);

        Assert.Null(ImagePicture.FillTarget(board, [shape.Id, empty.Id]));
    }

    [Fact]
    public void ADropOnAnEdgeDrawnOverAnEmptyImageFillsNothing()
    {
        var board = new Board();
        var empty = AddImage(board, "");
        var edge = new Edge(new FloatingEndpoint(0, 0), new FloatingEndpoint(100, 100));
        board.AddEdge(edge);

        Assert.Null(ImagePicture.FillTarget(board, [edge.Id, empty.Id]));
    }

    [Theory]
    [InlineData(double.NaN, 32)]
    [InlineData(double.PositiveInfinity, 32)]
    [InlineData(-64, 32)]
    public void APictureWithANonsenseSizeTakesTheFallback(double width, double height)
    {
        Assert.Equal(Fallback, ImagePicture.SizeFor(width, height, Viewport, Fallback));
    }

    [Fact]
    public void HitsThatAreNotBoardEntitiesAreSkipped()
    {
        var board = new Board();
        var empty = AddImage(board, "");

        Assert.Same(empty, ImagePicture.FillTarget(board, [Guid.NewGuid(), empty.Id]));
    }

    [Theory]
    [InlineData("image/png", true)]
    [InlineData("image/svg+xml", true)]
    [InlineData("image/vnd.microsoft.icon", true)]
    [InlineData("text/html", false)]
    [InlineData("image/png;base64,AAAA", false)]
    [InlineData("image/png\" onerror=\"x", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void OnlyAPlainImageTypeIsAccepted(string? mimeType, bool accepted)
    {
        Assert.Equal(accepted, ImagePicture.IsAcceptedMimeType(mimeType));
    }
}
