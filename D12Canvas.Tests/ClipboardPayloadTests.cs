using D12Canvas.BuiltIns;
using D12Canvas.Model;
using D12Canvas.Persistence;
using D12Canvas.Registration;
using Xunit;

namespace D12Canvas.Tests;

// A board envelope is recognised by its shape, not by a marker, so a saved board file pastes as a
// merge; it loads as tolerantly as a saved file does. Any other text becomes a text shape.
public class ClipboardPayloadTests
{
    private static readonly IComponentRegistry Registry = BuildRegistry();
    private static readonly BoardJsonSerializer Serializer = new(Registry);

    private static IComponentRegistry BuildRegistry()
    {
        var options = new D12CanvasOptions();
        BuiltInComponents.RegisterAll(options);
        return options.Registry;
    }

    private static ComponentInstance Rectangle(double x) =>
        new("rectangle", new RectangleProps(null, null, 2), new Bounds(x, 0, 100, 50));

    [Fact]
    public void ASerialisedBoardIsRecognised()
    {
        var board = new Board();
        board.AddComponent(Rectangle(0));

        Assert.True(ClipboardPayload.IsBoard(Serializer.Serialize(board)));
    }

    [Theory]
    [InlineData("hello")]
    [InlineData("{\"SchemaVersion\": 1}")]
    [InlineData("{\"SchemaVersion\": 99, \"Components\": []}")]
    [InlineData("{\"SchemaVersion\": \"1\", \"Components\": []}")]
    [InlineData("{\"SchemaVersion\": 1, \"Components\": {}}")]
    [InlineData("[1, 2, 3]")]
    [InlineData("{\"SchemaVersion\": 1, \"Components\": [")]
    public void TextThatIsNotABoardEnvelopeIsNotRecognised(string text) =>
        Assert.False(ClipboardPayload.IsBoard(text));

    [Fact]
    public void AnEdgeOnlyEnvelopeIsRecognised() =>
        Assert.True(
            ClipboardPayload.IsBoard("{\"SchemaVersion\": 1, \"Components\": [], \"Edges\": []}")
        );

    [Fact]
    public void APastedBoardComesBackWithNewIds()
    {
        var board = new Board();
        var original = Rectangle(0);
        board.AddComponent(original);

        var read = ClipboardPayload.From(Serializer.Serialize(board), Serializer, Registry)!;

        var pasted = Assert.Single(read.Fragment.Components);
        Assert.NotEqual(original.Id, pasted.Id);
        Assert.Equal(original.Bounds, pasted.Bounds);
        Assert.Empty(read.Warnings);
    }

    [Fact]
    public void AnUnknownTypeCostsThatInstanceAndItsEdgeWithAWarningNamingTheType()
    {
        var known = Guid.NewGuid();
        var unknown = Guid.NewGuid();
        var json = $$"""
            {
              "SchemaVersion": 1,
              "Components": [
                { "Id": "{{known}}", "ComponentTypeKey": "rectangle", "Props": { "FillColor": null, "StrokeColor": null, "StrokeWidth": 2 }, "Bounds": { "X": 0, "Y": 0, "Width": 100, "Height": 50 }, "ZIndex": 0 },
                { "Id": "{{unknown}}", "ComponentTypeKey": "no-such-type", "Props": {}, "Bounds": { "X": 200, "Y": 0, "Width": 100, "Height": 50 }, "ZIndex": 0 }
              ],
              "Edges": [
                { "Id": "{{Guid.NewGuid()}}", "Source": { "ComponentId": "{{known}}", "PortId": 1, "X": null, "Y": null }, "Target": { "ComponentId": "{{unknown}}", "PortId": 3, "X": null, "Y": null } },
                { "Id": "{{Guid.NewGuid()}}", "Source": { "ComponentId": "{{known}}", "PortId": 1, "X": null, "Y": null }, "Target": { "ComponentId": null, "PortId": null, "X": 400, "Y": 10 } }
              ]
            }
            """;

        var read = ClipboardPayload.From(json, Serializer, Registry)!;

        Assert.Single(read.Fragment.Components);
        var kept = Assert.Single(read.Fragment.Edges);
        Assert.IsType<FloatingEndpoint>(kept.Target);
        Assert.Contains(read.Warnings, warning => warning.Reason.Contains("'no-such-type'"));
    }

    [Fact]
    public void PlainTextBecomesATextShape()
    {
        var read = ClipboardPayload.From("Buy milk", Serializer, Registry)!;

        var shape = Assert.Single(read.Fragment.Components);
        Assert.Equal("text", shape.ComponentTypeKey);
        Assert.Equal("Buy milk", ((TextProps)shape.Props).Text);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  \n\t")]
    public void BlankTextPastesNothing(string text) =>
        Assert.Null(ClipboardPayload.From(text, Serializer, Registry));

    [Fact]
    public void AnEnvelopeWhoseArraysAreNotArraysPastesNothing() =>
        Assert.Null(
            ClipboardPayload.From(
                "{\"SchemaVersion\": 1, \"Components\": [], \"Edges\": 5}",
                Serializer,
                Registry
            )
        );
}
