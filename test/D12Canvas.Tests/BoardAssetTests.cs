using System.Security.Cryptography;
using System.Text;
using D12Canvas.Model;
using Xunit;

namespace D12Canvas.Tests;

// Binary content enters a board through AddAsset alone, is held once per distinct content, and is
// referenced from props by the string AddAsset hands back.
public class BoardAssetTests
{
    private static readonly byte[] PngBytes = AssetTestComponent.PngBytes;

    [Fact]
    public void AddAssetReturnsAReferenceWhoseIdIsTheSha256OfTheBytes()
    {
        var board = new Board();

        var reference = board.AddAsset(PngBytes, "image/png");

        var expectedId = "sha256-" + Convert.ToHexStringLower(SHA256.HashData(PngBytes));
        Assert.Equal($"asset:{expectedId}", reference);
        Assert.True(AssetReference.TryGetAssetId(reference, out var assetId));
        Assert.Equal(expectedId, assetId);
    }

    [Fact]
    public void AddingTheSameBytesTwiceYieldsOneAssetAndTheSameReference()
    {
        var board = new Board();

        var first = board.AddAsset(PngBytes, "image/png");
        var second = board.AddAsset(PngBytes.ToArray(), "image/png");

        Assert.Equal(first, second);
        Assert.Single(board.Assets);
    }

    [Fact]
    public void DifferentBytesAreDifferentAssets()
    {
        var board = new Board();

        var first = board.AddAsset(PngBytes, "image/png");
        var second = board.AddAsset(Encoding.ASCII.GetBytes("another picture"), "image/png");

        Assert.NotEqual(first, second);
        Assert.Equal(2, board.Assets.Count);
    }

    [Fact]
    public void GetAssetFindsAnAssetByItsIdAndEncodesADataUriFromItsMimeTypeAndBytes()
    {
        var board = new Board();
        AssetReference.TryGetAssetId(board.AddAsset(PngBytes, "image/png"), out var assetId);

        var asset = board.GetAsset(assetId!);

        Assert.NotNull(asset);
        Assert.Equal("image/png", asset!.MimeType);
        Assert.Equal(PngBytes, asset.Data);
        Assert.Equal($"data:image/png;base64,{Convert.ToBase64String(PngBytes)}", asset.DataUri);
        Assert.Null(board.GetAsset("sha256-0000"));
    }

    [Fact]
    public void AnOrdinaryUrlIsNotAnAssetReference()
    {
        Assert.False(AssetReference.TryGetAssetId("https://example.com/photo.png", out _));
        Assert.False(AssetReference.TryGetAssetId("", out _));
        Assert.False(AssetReference.TryGetAssetId(null, out _));
    }
}
