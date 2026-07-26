using D12Canvas.Model;
using D12Canvas.Persistence;
using D12Canvas.Registration;
using Xunit;

namespace D12Canvas.Tests;

// Ticket 36: partial deserialize never throws on unknown component-type keys or malformed
// entities — it skips them, warns, and loads everything else normally.
public class BoardJsonSerializerPartialDeserializeTests
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
        return registry;
    }

    [Fact]
    public void ReturnsTheBoardWithNoWarningsWhenEveryComponentIsValid()
    {
        var serializer = new BoardJsonSerializer(BuildRegistry());
        var board = new Board();
        board.AddComponent(
            new ComponentInstance(TestComponentKey, new TestProps("hello"), new Bounds(1, 2, 3, 4))
        );

        var result = serializer.DeserializePartial(serializer.Serialize(board));

        Assert.Empty(result.Warnings);
        var restored = Assert.Single(result.Board.Components);
        Assert.Equal(new TestProps("hello"), restored.Props);
    }

    [Fact]
    public void SkipsAnUnknownComponentTypeAndRecordsAWarningIdentifyingTheEntity()
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

        var result = serializer.DeserializePartial(json);

        Assert.Empty(result.Board.Components);
        var warning = Assert.Single(result.Warnings);
        Assert.Equal("11111111-1111-1111-1111-111111111111", warning.Entity, ignoreCase: true);
        Assert.Contains("no-such-type", warning.Reason);
    }

    [Fact]
    public void SkipsAStructurallyMalformedComponentEntryAndRecordsAWarning()
    {
        var serializer = new BoardJsonSerializer(BuildRegistry());
        const string json = """
            {
              "SchemaVersion": 1,
              "Components": [
                {
                  "Id": "22222222-2222-2222-2222-222222222222",
                  "ComponentTypeKey": "test-props",
                  "Props": { "Text": "hello" },
                  "Bounds": "not-an-object",
                  "ZIndex": 0
                }
              ]
            }
            """;

        var result = serializer.DeserializePartial(json);

        Assert.Empty(result.Board.Components);
        var warning = Assert.Single(result.Warnings);
        Assert.Equal("22222222-2222-2222-2222-222222222222", warning.Entity, ignoreCase: true);
        Assert.False(string.IsNullOrWhiteSpace(warning.Reason));
    }

    [Fact]
    public void FallsBackToAPositionalLabelWhenTheEntityHasNoReadableId()
    {
        var serializer = new BoardJsonSerializer(BuildRegistry());
        const string json = """
            {
              "SchemaVersion": 1,
              "Components": [
                "not-an-object"
              ]
            }
            """;

        var result = serializer.DeserializePartial(json);

        Assert.Empty(result.Board.Components);
        var warning = Assert.Single(result.Warnings);
        Assert.Equal("Components[0]", warning.Entity);
    }

    [Fact]
    public void SkipsAComponentWhosePropsDoNotMatchTheRegisteredPropsTypeAndRecordsAWarning()
    {
        var serializer = new BoardJsonSerializer(BuildRegistry());
        const string json = """
            {
              "SchemaVersion": 1,
              "Components": [
                {
                  "Id": "33333333-3333-3333-3333-333333333333",
                  "ComponentTypeKey": "other-props",
                  "Props": { "Value": "not-a-number" },
                  "Bounds": { "X": 0, "Y": 0, "Width": 10, "Height": 10 },
                  "ZIndex": 0
                }
              ]
            }
            """;

        var result = serializer.DeserializePartial(json);

        Assert.Empty(result.Board.Components);
        var warning = Assert.Single(result.Warnings);
        Assert.Equal("33333333-3333-3333-3333-333333333333", warning.Entity, ignoreCase: true);
    }

    [Fact]
    public void LoadsEntitiesUnaffectedByProblemsWhileSkippingAndWarningAboutTheRest()
    {
        var serializer = new BoardJsonSerializer(BuildRegistry());
        const string json = """
            {
              "SchemaVersion": 1,
              "Components": [
                {
                  "Id": "11111111-1111-1111-1111-111111111111",
                  "ComponentTypeKey": "test-props",
                  "Props": { "Text": "good" },
                  "Bounds": { "X": 0, "Y": 0, "Width": 10, "Height": 10 },
                  "ZIndex": 0
                },
                {
                  "Id": "22222222-2222-2222-2222-222222222222",
                  "ComponentTypeKey": "no-such-type",
                  "Props": {},
                  "Bounds": { "X": 0, "Y": 0, "Width": 10, "Height": 10 },
                  "ZIndex": 0
                },
                {
                  "Id": "33333333-3333-3333-3333-333333333333",
                  "ComponentTypeKey": "test-props",
                  "Props": { "Text": "hello" },
                  "Bounds": "not-an-object",
                  "ZIndex": 0
                },
                {
                  "Id": "44444444-4444-4444-4444-444444444444",
                  "ComponentTypeKey": "other-props",
                  "Props": { "Value": "not-a-number" },
                  "Bounds": { "X": 0, "Y": 0, "Width": 10, "Height": 10 },
                  "ZIndex": 0
                }
              ]
            }
            """;

        var result = serializer.DeserializePartial(json);

        var restored = Assert.Single(result.Board.Components);
        Assert.Equal(Guid.Parse("11111111-1111-1111-1111-111111111111"), restored.Id);
        Assert.Equal(new TestProps("good"), restored.Props);
        Assert.Equal(
            new[]
            {
                "22222222-2222-2222-2222-222222222222",
                "33333333-3333-3333-3333-333333333333",
                "44444444-4444-4444-4444-444444444444",
            },
            result.Warnings.Select(w => w.Entity.ToLowerInvariant())
        );
    }

    [Fact]
    public void StillThrowsUnsupportedSchemaVersionExceptionForAWrongSchemaVersion()
    {
        var serializer = new BoardJsonSerializer(BuildRegistry());
        const string json = """
            {
              "SchemaVersion": 99,
              "Components": []
            }
            """;

        var exception = Assert.Throws<UnsupportedSchemaVersionException>(
            () => serializer.DeserializePartial(json)
        );
        Assert.Equal(99, exception.ActualSchemaVersion);
    }

    [Fact]
    public void StrictDeserializeBehaviourIsUnchangedAndStillThrowsForAnUnknownComponentType()
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

        Assert.Throws<UnknownComponentKeyException>(() => serializer.Deserialize(json));
    }
}
