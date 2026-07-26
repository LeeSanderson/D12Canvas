using System.Text.Json;
using D12Canvas.Model;
using D12Canvas.Persistence;
using D12Canvas.Registration;
using Xunit;

namespace D12Canvas.Tests;

// Ticket 35: Board -> versioned JSON envelope -> Board. Props round-trip via two-phase
// deserialize (registry-resolved bind by ComponentTypeKey), never a CLR-type discriminator.
public class BoardJsonSerializerTests
{
    private const string TestComponentKey = "test-props";
    private const string OtherComponentKey = "other-props";

    private static ComponentRegistry BuildRegistry()
    {
        var registry = new ComponentRegistry();
        registry.Register(
            new ComponentRegistration(
                Key: TestComponentKey,
                ComponentType: typeof(TestComponentDouble),
                PropsType: typeof(TestProps),
                DisplayName: "Test Props",
                AccessibleName: "Test props component",
                DefaultProps: new TestProps(),
                Icon: null,
                Role: "group",
                DefaultSize: null,
                Category: null
            )
        );
        return registry;
    }

    [Fact]
    public void SerializeProducesAnEnvelopeWithSchemaVersionAndComponentsArray()
    {
        var serializer = new BoardJsonSerializer(BuildRegistry());
        var board = new Board();
        board.AddComponent(
            new ComponentInstance(TestComponentKey, new TestProps("hello"), new Bounds(1, 2, 3, 4))
        );

        var json = serializer.Serialize(board);

        using var document = JsonDocument.Parse(json);
        Assert.Equal(1, document.RootElement.GetProperty("SchemaVersion").GetInt32());
        Assert.Equal(1, document.RootElement.GetProperty("Components").GetArrayLength());
    }

    [Fact]
    public void DeserializeRebuildsAnEquivalentBoard()
    {
        var serializer = new BoardJsonSerializer(BuildRegistry());
        var board = new Board();
        var instance = new ComponentInstance(
            TestComponentKey,
            new TestProps("hello"),
            new Bounds(10, 20, 100, 50),
            zIndex: 3
        );
        board.AddComponent(instance);

        var restored = serializer.Deserialize(serializer.Serialize(board));

        var restoredInstance = restored.GetComponent(instance.Id);
        Assert.NotNull(restoredInstance);
        Assert.Equal(instance.ComponentTypeKey, restoredInstance!.ComponentTypeKey);
        Assert.Equal(instance.Bounds, restoredInstance.Bounds);
        Assert.Equal(instance.ZIndex, restoredInstance.ZIndex);
        Assert.Equal(new TestProps("hello"), restoredInstance.Props);
    }

    [Fact]
    public void PropsBindToTheirRegisteredClrTypeNotAJsonElement()
    {
        var serializer = new BoardJsonSerializer(BuildRegistry());
        var board = new Board();
        board.AddComponent(
            new ComponentInstance(
                TestComponentKey,
                new TestProps("payload"),
                new Bounds(0, 0, 10, 10)
            )
        );

        var restored = serializer.Deserialize(serializer.Serialize(board));

        var restoredProps = Assert.Single(restored.Components).Props;
        Assert.IsType<TestProps>(restoredProps);
    }

    [Fact]
    public void OutputContainsNoClrTypeDiscriminator()
    {
        var serializer = new BoardJsonSerializer(BuildRegistry());
        var board = new Board();
        board.AddComponent(
            new ComponentInstance(
                TestComponentKey,
                new TestProps("hello"),
                new Bounds(0, 0, 10, 10)
            )
        );

        var json = serializer.Serialize(board);

        Assert.DoesNotContain("$type", json);
        Assert.DoesNotContain(typeof(TestProps).FullName!, json);
        Assert.DoesNotContain(typeof(TestProps).AssemblyQualifiedName!, json);
    }

    [Fact]
    public void ComponentsOfDifferentTypesEachBindToTheirOwnPropsType()
    {
        var registry = BuildRegistry();
        registry.Register(
            new ComponentRegistration(
                Key: OtherComponentKey,
                ComponentType: typeof(TestComponentDouble),
                PropsType: typeof(OtherTestProps),
                DisplayName: "Other Props",
                AccessibleName: "Other props component",
                DefaultProps: new OtherTestProps(),
                Icon: null,
                Role: "group",
                DefaultSize: null,
                Category: null
            )
        );
        var serializer = new BoardJsonSerializer(registry);
        var board = new Board();
        board.AddComponent(
            new ComponentInstance(
                TestComponentKey,
                new TestProps("hello"),
                new Bounds(0, 0, 10, 10)
            )
        );
        board.AddComponent(
            new ComponentInstance(
                OtherComponentKey,
                new OtherTestProps(42),
                new Bounds(10, 10, 10, 10)
            )
        );

        var restored = serializer.Deserialize(serializer.Serialize(board));

        Assert.Contains(
            restored.Components,
            c => c.Props is TestProps props && props.Text == "hello"
        );
        Assert.Contains(
            restored.Components,
            c => c.Props is OtherTestProps props && props.Value == 42
        );
    }

    [Fact]
    public void DeserializeThrowsUnknownComponentKeyExceptionForAnUnregisteredComponentType()
    {
        var serializer = new BoardJsonSerializer(BuildRegistry());
        const string json = """
            {
              "SchemaVersion": 1,
              "Components": [
                {
                  "Id": "11111111-1111-1111-1111-111111111111",
                  "ComponentTypeKey": "no-such-type",
                  "Props": {},
                  "Bounds": { "X": 0, "Y": 0, "Width": 10, "Height": 10 },
                  "ZIndex": 0
                }
              ]
            }
            """;

        var exception = Assert.Throws<UnknownComponentKeyException>(
            () => serializer.Deserialize(json)
        );
        Assert.Equal("no-such-type", exception.Key);
    }
}

internal sealed record OtherTestProps(int Value = 0);
