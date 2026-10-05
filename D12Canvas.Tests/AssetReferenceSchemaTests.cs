using D12Canvas.Registration;
using Xunit;

namespace D12Canvas.Tests;

// [AssetReference] is discovered in the same registration-time pass as [PanelEditable], and a
// declaration on anything but a string fails there rather than when the reference is resolved.
public class AssetReferenceSchemaTests
{
    [Fact]
    public void DiscoversEveryStringPropertyDeclaredAsAnAssetReference()
    {
        var references = AssetReferenceSchema.DiscoverFrom(typeof(AssetTestProps));

        Assert.Equal(nameof(AssetTestProps.Src), Assert.Single(references).Name);
    }

    [Fact]
    public void ATypeDeclaringNoAssetReferenceHasAnEmptySchema()
    {
        Assert.Empty(AssetReferenceSchema.DiscoverFrom(typeof(TestProps)));
    }

    [Fact]
    public void ANonStringAssetReferenceThrowsNamingTheProperty()
    {
        var exception = Assert.Throws<AssetReferenceStringRequiredException>(
            () => AssetReferenceSchema.DiscoverFrom(typeof(PropsWithANonStringAssetReference))
        );

        Assert.Equal(typeof(PropsWithANonStringAssetReference), exception.PropsType);
        Assert.Equal(nameof(PropsWithANonStringAssetReference.Bytes), exception.PropertyName);
    }

    [Fact]
    public void RegisterComponentRecordsTheAssetReferencesOnTheRegistration()
    {
        var options = new D12CanvasOptions();

        AssetTestComponent.Register(options);

        var registration = options.Registry.Resolve(AssetTestComponent.Key);
        Assert.Equal(nameof(AssetTestProps.Src), Assert.Single(registration.AssetReferences!).Name);
    }

    [Fact]
    public void ReferencesInReturnsOnlyThePropertiesHoldingAReference()
    {
        var options = new D12CanvasOptions();
        AssetTestComponent.Register(options);
        var registration = options.Registry.Resolve(AssetTestComponent.Key);

        var referencing = AssetReferenceSchema.ReferencesIn(
            registration,
            new AssetTestProps("asset:sha256-abc")
        );
        var plainUrl = AssetReferenceSchema.ReferencesIn(
            registration,
            new AssetTestProps("https://example.com/photo.png")
        );

        var (property, assetId) = Assert.Single(referencing);
        Assert.Equal(nameof(AssetTestProps.Src), property.Name);
        Assert.Equal("sha256-abc", assetId);
        Assert.Empty(plainUrl);
    }

    private sealed record PropsWithANonStringAssetReference(
        [property: AssetReference] int Bytes = 0
    );
}
