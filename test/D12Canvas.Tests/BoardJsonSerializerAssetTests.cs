using System.Text;
using System.Text.Json;
using D12Canvas.Model;
using D12Canvas.Persistence;
using D12Canvas.Registration;
using Xunit;

namespace D12Canvas.Tests;

// Assets ride inside the one document the host stores, as an optional envelope field: present
// only when some entity refers to an asset, absent otherwise, and tolerated when a reference
// points at bytes the file does not carry.
public class BoardJsonSerializerAssetTests
{
    private const string AssetComponentKey = AssetTestComponent.Key;
    private const string PlainComponentKey = "test-props";
    private static readonly byte[] PngBytes = AssetTestComponent.PngBytes;

    private static ComponentRegistry BuildRegistry()
    {
        var options = new D12CanvasOptions();
        AssetTestComponent.Register(options);
        options.RegisterComponent<TestComponentDouble, TestProps>(
            PlainComponentKey,
            builder =>
            {
                builder.DisplayName = "Test Props";
                builder.AccessibleName = "Test props component";
                builder.DefaultProps = new TestProps();
            }
        );
        return (ComponentRegistry)options.Registry;
    }

    private static ComponentInstance AssetComponentInstance(string src) =>
        new(AssetComponentKey, new AssetTestProps(src), new Bounds(0, 0, 10, 10));

    [Fact]
    public void ABoardWithNoAssetsSerialisesWithoutAnAssetsProperty()
    {
        var serializer = new BoardJsonSerializer(BuildRegistry());
        var board = new Board();
        board.AddComponent(
            new ComponentInstance(PlainComponentKey, new TestProps("hello"), new Bounds(1, 2, 3, 4))
        );

        var json = serializer.Serialize(board);

        using var document = JsonDocument.Parse(json);
        Assert.False(document.RootElement.TryGetProperty("Assets", out _));
    }

    [Fact]
    public void AReferencedAssetRoundTripsWithItsIdMimeTypeAndBytes()
    {
        var serializer = new BoardJsonSerializer(BuildRegistry());
        var board = new Board();
        var reference = board.AddAsset(PngBytes, "image/png");
        var instance = AssetComponentInstance(reference);
        board.AddComponent(instance);

        var json = serializer.Serialize(board);
        var restored = serializer.Deserialize(json);

        using var document = JsonDocument.Parse(json);
        var assetElement = Assert.Single(
            document.RootElement.GetProperty("Assets").EnumerateArray()
        );
        Assert.Equal(Asset.IdFor(PngBytes), assetElement.GetProperty("Id").GetString());
        Assert.Equal("image/png", assetElement.GetProperty("MimeType").GetString());
        Assert.Equal(
            Convert.ToBase64String(PngBytes),
            assetElement.GetProperty("Data").GetString()
        );

        var restoredAsset = Assert.Single(restored.Assets);
        Assert.Equal(Asset.IdFor(PngBytes), restoredAsset.Id);
        Assert.Equal(PngBytes, restoredAsset.Data);
        Assert.Equal(reference, ((AssetTestProps)restored.GetComponent(instance.Id)!.Props).Src);
    }

    [Fact]
    public void AnAssetNothingRefersToIsNotSerialised()
    {
        var serializer = new BoardJsonSerializer(BuildRegistry());
        var board = new Board();
        board.AddAsset(PngBytes, "image/png");
        board.AddComponent(AssetComponentInstance("https://example.com/photo.png"));

        var json = serializer.Serialize(board);

        using var document = JsonDocument.Parse(json);
        Assert.False(document.RootElement.TryGetProperty("Assets", out _));
    }

    [Fact]
    public void AnAssetReferencedOnlyByAnEdgeLabelIsSerialised()
    {
        var serializer = new BoardJsonSerializer(BuildRegistry());
        var board = new Board();
        var reference = board.AddAsset(PngBytes, "image/png");
        board.AddEdge(
            new Edge(
                new FloatingEndpoint(0, 0),
                new FloatingEndpoint(100, 100),
                label: AssetComponentInstance(reference)
            )
        );

        var restored = serializer.Deserialize(serializer.Serialize(board));

        Assert.Equal(Asset.IdFor(PngBytes), Assert.Single(restored.Assets).Id);
    }

    [Fact]
    public void TheSameAssetReferencedTwiceIsSerialisedOnce()
    {
        var serializer = new BoardJsonSerializer(BuildRegistry());
        var board = new Board();
        var reference = board.AddAsset(PngBytes, "image/png");
        board.AddComponent(AssetComponentInstance(reference));
        board.AddComponent(AssetComponentInstance(reference));

        var json = serializer.Serialize(board);

        using var document = JsonDocument.Parse(json);
        Assert.Equal(1, document.RootElement.GetProperty("Assets").GetArrayLength());
    }

    [Fact]
    public void AMissingAssetIsToleratedOnBothLoadPathsAndTheReferenceIsLeftInPlace()
    {
        var serializer = new BoardJsonSerializer(BuildRegistry());
        var board = new Board();
        var instance = AssetComponentInstance("asset:sha256-missing");
        board.AddComponent(instance);
        var json = serializer.Serialize(board);

        var strict = serializer.Deserialize(json);
        var partial = serializer.DeserializePartial(json);

        Assert.Equal(
            "asset:sha256-missing",
            ((AssetTestProps)strict.GetComponent(instance.Id)!.Props).Src
        );
        Assert.Equal(
            "asset:sha256-missing",
            ((AssetTestProps)partial.Board.GetComponent(instance.Id)!.Props).Src
        );
        var warning = Assert.Single(partial.Warnings);
        Assert.Equal(instance.Id.ToString(), warning.Entity, ignoreCase: true);
        Assert.Contains("sha256-missing", warning.Reason);
    }

    [Fact]
    public void PartialLoadRestoresAssetsAndWarnsOnNoneWhenEveryReferenceResolves()
    {
        var serializer = new BoardJsonSerializer(BuildRegistry());
        var board = new Board();
        board.AddComponent(AssetComponentInstance(board.AddAsset(PngBytes, "image/png")));

        var result = serializer.DeserializePartial(serializer.Serialize(board));

        Assert.Empty(result.Warnings);
        Assert.Equal(PngBytes, Assert.Single(result.Board.Assets).Data);
    }

    [Fact]
    public void ABoardSavedBeforeAssetsExistedLoadsUnchanged()
    {
        var serializer = new BoardJsonSerializer(BuildRegistry());
        const string json = """
            {
              "SchemaVersion": 1,
              "Components": [
                {
                  "Id": "11111111-1111-1111-1111-111111111111",
                  "ComponentTypeKey": "asset-component",
                  "Props": { "Src": "https://example.com/photo.png", "Caption": "" },
                  "Bounds": { "X": 0, "Y": 0, "Width": 10, "Height": 10 },
                  "ZIndex": 0
                }
              ]
            }
            """;

        var restored = serializer.Deserialize(json);
        var partial = serializer.DeserializePartial(json);

        Assert.Empty(restored.Assets);
        Assert.Empty(partial.Warnings);
        Assert.Single(restored.Components);
    }
}
