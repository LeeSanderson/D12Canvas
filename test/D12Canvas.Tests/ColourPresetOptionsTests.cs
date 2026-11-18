using D12Canvas.Registration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace D12Canvas.Tests;

public sealed class ColourPresetOptionsTests
{
    [Fact]
    public void TheDefaultsAreTheTenLibraryPresetsInOrder()
    {
        var options = new D12CanvasOptions();

        Assert.Equal(
            [
                new ColourPreset("Black", "#000000"),
                new ColourPreset("Grey", "#868E96"),
                new ColourPreset("White", "#FFFFFF"),
                new ColourPreset("Red", "#E03131"),
                new ColourPreset("Orange", "#F08C00"),
                new ColourPreset("Yellow", "#FFEB3B"),
                new ColourPreset("Green", "#2F9E44"),
                new ColourPreset("Blue", "#2F80ED"),
                new ColourPreset("Purple", "#7048E8"),
                new ColourPreset("Pink", "#D6336C"),
            ],
            options.ColourPresets
        );
    }

    [Theory]
    [InlineData("2F80ED")]
    [InlineData("#2F80E")]
    [InlineData("#2F80EDFF")]
    [InlineData("#2F80EG")]
    [InlineData("blue")]
    [InlineData("")]
    public void APresetWhoseHexIsNotRRGGBBThrows(string hex)
    {
        var options = new D12CanvasOptions();

        var exception = Assert.Throws<ArgumentException>(
            () => options.ColourPresets = [new ColourPreset("Brand", hex)]
        );
        Assert.Contains("Brand", exception.Message);
    }

    [Fact]
    public void APresetWithoutANameThrows()
    {
        var options = new D12CanvasOptions();

        Assert.Throws<ArgumentException>(
            () => options.ColourPresets = [new ColourPreset(" ", "#123456")]
        );
    }

    [Fact]
    public void ANullListThrows()
    {
        var options = new D12CanvasOptions();

        Assert.Throws<ArgumentNullException>(() => options.ColourPresets = null!);
    }

    [Fact]
    public void ABadPresetFailsAtRegistration()
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentException>(
            () =>
                services.AddD12Canvas(options =>
                    options.ColourPresets = [new ColourPreset("Brand", "#12345")]
                )
        );
    }

    [Fact]
    public void AnEmptyListIsAllowedAndLowerCaseHexIsKept()
    {
        var options = new D12CanvasOptions();

        options.ColourPresets = [];
        Assert.Empty(options.ColourPresets);

        options.ColourPresets = [new ColourPreset("Brand", "#abcdef")];
        Assert.Equal([new ColourPreset("Brand", "#abcdef")], options.ColourPresets);
    }

    [Fact]
    public void AddD12CanvasRegistersTheConfiguredListAndASecondCallChangesIt()
    {
        var services = new ServiceCollection();
        services.AddD12Canvas(options =>
            options.ColourPresets = [new ColourPreset("First", "#111111")]
        );
        services.AddD12Canvas(options =>
            options.ColourPresets = [.. options.ColourPresets, new("Second", "#222222")]
        );

        var presets = services
            .BuildServiceProvider()
            .GetRequiredService<ColourPresetCatalog>()
            .Presets;

        Assert.Equal(["First", "Second"], presets.Select(preset => preset.Name));
    }
}
