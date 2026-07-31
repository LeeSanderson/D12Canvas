using D12Canvas.Model;
using D12Canvas.Persistence;
using D12Canvas.Registration;
using Xunit;

namespace D12Canvas.Tests;

// Partial deserialize never throws on unknown component-type keys or malformed entities — it
// skips them, warns, and loads everything else normally.
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

    // The partial path never fails the whole load over a group's problems either - a
    // structurally malformed group is skipped-and-warned like a malformed component, and a group
    // referencing a missing member is still loaded (its other, valid members still behave as a
    // group) with a warning recorded instead of the load failing.
    [Fact]
    public void RoundTripsGroupsWithNoWarningsWhenEveryMemberResolves()
    {
        var serializer = new BoardJsonSerializer(BuildRegistry());
        var board = new Board();
        var first = new ComponentInstance(
            TestComponentKey,
            new TestProps("a"),
            new Bounds(0, 0, 10, 10)
        );
        var second = new ComponentInstance(
            TestComponentKey,
            new TestProps("b"),
            new Bounds(20, 20, 10, 10)
        );
        board.AddComponent(first);
        board.AddComponent(second);
        var group = new Group(new[] { first.Id, second.Id });
        board.AddGroup(group);

        var result = serializer.DeserializePartial(serializer.Serialize(board));

        Assert.Empty(result.Warnings);
        var restoredGroup = Assert.Single(result.Board.Groups);
        Assert.Equal(group.Id, restoredGroup.Id);
        Assert.Equal(group.MemberIds, restoredGroup.MemberIds);
    }

    [Fact]
    public void RecordsAWarningForAGroupReferencingAMissingMemberButStillLoadsTheGroup()
    {
        var serializer = new BoardJsonSerializer(BuildRegistry());
        const string json = """
            {
              "SchemaVersion": 1,
              "Components": [],
              "Groups": [
                {
                  "Id": "55555555-5555-5555-5555-555555555555",
                  "MemberIds": [ "66666666-6666-6666-6666-666666666666" ]
                }
              ]
            }
            """;

        var result = serializer.DeserializePartial(json);

        var restoredGroup = Assert.Single(result.Board.Groups);
        Assert.Equal(Guid.Parse("55555555-5555-5555-5555-555555555555"), restoredGroup.Id);
        var warning = Assert.Single(result.Warnings);
        Assert.Equal("55555555-5555-5555-5555-555555555555", warning.Entity, ignoreCase: true);
        Assert.Contains(
            "66666666-6666-6666-6666-666666666666",
            warning.Reason,
            StringComparison.OrdinalIgnoreCase
        );
    }

    [Fact]
    public void SkipsAStructurallyMalformedGroupEntryAndRecordsAWarning()
    {
        var serializer = new BoardJsonSerializer(BuildRegistry());
        const string json = """
            {
              "SchemaVersion": 1,
              "Components": [],
              "Groups": [
                {
                  "Id": "99999999-9999-9999-9999-999999999999",
                  "MemberIds": "not-an-array"
                }
              ]
            }
            """;

        var result = serializer.DeserializePartial(json);

        Assert.Empty(result.Board.Groups);
        var warning = Assert.Single(result.Warnings);
        Assert.Equal("99999999-9999-9999-9999-999999999999", warning.Entity, ignoreCase: true);
    }

    [Fact]
    public void DoesNotWarnWhenAGroupIsDeclaredBeforeTheNestedGroupItReferences()
    {
        var serializer = new BoardJsonSerializer(BuildRegistry());
        const string json = """
            {
              "SchemaVersion": 1,
              "Components": [
                {
                  "Id": "11111111-1111-1111-1111-111111111111",
                  "ComponentTypeKey": "test-props",
                  "Props": { "Text": "leaf" },
                  "Bounds": { "X": 0, "Y": 0, "Width": 10, "Height": 10 },
                  "ZIndex": 0
                }
              ],
              "Groups": [
                {
                  "Id": "77777777-7777-7777-7777-777777777777",
                  "MemberIds": [ "88888888-8888-8888-8888-888888888888" ]
                },
                {
                  "Id": "88888888-8888-8888-8888-888888888888",
                  "MemberIds": [ "11111111-1111-1111-1111-111111111111" ]
                }
              ]
            }
            """;

        var result = serializer.DeserializePartial(json);

        Assert.Empty(result.Warnings);
        Assert.Equal(2, result.Board.Groups.Count);
    }

    [Fact]
    public void SkipsAGroupWithADuplicateIdAndRecordsAWarningInsteadOfFailingTheLoad()
    {
        var serializer = new BoardJsonSerializer(BuildRegistry());
        const string json = """
            {
              "SchemaVersion": 1,
              "Components": [],
              "Groups": [
                {
                  "Id": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
                  "MemberIds": []
                },
                {
                  "Id": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
                  "MemberIds": []
                }
              ]
            }
            """;

        var result = serializer.DeserializePartial(json);

        var restoredGroup = Assert.Single(result.Board.Groups);
        Assert.Equal(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), restoredGroup.Id);
        var warning = Assert.Single(result.Warnings);
        Assert.Equal("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa", warning.Entity, ignoreCase: true);
    }

    // The partial path never fails the whole load over an edge's problems either - a
    // structurally malformed edge is skipped-and-warned like a malformed component/group, and an
    // edge referencing a missing instance is still loaded (its other, valid endpoint still
    // resolves) with a warning recorded instead of the load failing.
    [Fact]
    public void RoundTripsEdgesWithNoWarningsWhenBothEndpointsResolve()
    {
        var serializer = new BoardJsonSerializer(BuildRegistry());
        var board = new Board();
        var first = new ComponentInstance(
            TestComponentKey,
            new TestProps("a"),
            new Bounds(0, 0, 10, 10)
        );
        var second = new ComponentInstance(
            TestComponentKey,
            new TestProps("b"),
            new Bounds(20, 20, 10, 10)
        );
        board.AddComponent(first);
        board.AddComponent(second);
        var edge = new Edge(
            new PortEndpoint(first.Id, PortId.Right),
            new PortEndpoint(second.Id, PortId.Left)
        );
        board.AddEdge(edge);

        var result = serializer.DeserializePartial(serializer.Serialize(board));

        Assert.Empty(result.Warnings);
        var restoredEdge = Assert.Single(result.Board.Edges);
        Assert.Equal(edge.Id, restoredEdge.Id);
    }

    [Fact]
    public void RecordsAWarningForAnEdgeReferencingAMissingInstanceButStillLoadsTheEdge()
    {
        var serializer = new BoardJsonSerializer(BuildRegistry());
        const string json = """
            {
              "SchemaVersion": 1,
              "Components": [],
              "Edges": [
                {
                  "Id": "77777777-7777-7777-7777-777777777777",
                  "Source": {
                    "ComponentId": "66666666-6666-6666-6666-666666666666",
                    "PortId": 0,
                    "X": null,
                    "Y": null
                  },
                  "Target": { "ComponentId": null, "PortId": null, "X": 10, "Y": 20 }
                }
              ]
            }
            """;

        var result = serializer.DeserializePartial(json);

        var restoredEdge = Assert.Single(result.Board.Edges);
        Assert.Equal(Guid.Parse("77777777-7777-7777-7777-777777777777"), restoredEdge.Id);
        var warning = Assert.Single(result.Warnings);
        Assert.Equal("77777777-7777-7777-7777-777777777777", warning.Entity, ignoreCase: true);
        Assert.Contains(
            "66666666-6666-6666-6666-666666666666",
            warning.Reason,
            StringComparison.OrdinalIgnoreCase
        );
    }

    // A dangling custom-port reference is tolerated exactly like a dangling standard port
    // reference (RecordsAWarningForAnEdgeReferencingAMissingInstanceButStillLoadsTheEdge above) -
    // a warning is recorded but the edge still loads.
    [Fact]
    public void RecordsAWarningForAnEdgeReferencingAMissingInstanceViaACustomPortButStillLoadsTheEdge()
    {
        var serializer = new BoardJsonSerializer(BuildRegistry());
        const string json = """
            {
              "SchemaVersion": 1,
              "Components": [],
              "Edges": [
                {
                  "Id": "dddddddd-dddd-dddd-dddd-dddddddddddd",
                  "Source": {
                    "ComponentId": "eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee",
                    "PortId": null,
                    "X": null,
                    "Y": null,
                    "CustomPortId": "ffffffff-ffff-ffff-ffff-ffffffffffff"
                  },
                  "Target": { "ComponentId": null, "PortId": null, "X": 10, "Y": 20 }
                }
              ]
            }
            """;

        var result = serializer.DeserializePartial(json);

        var restoredEdge = Assert.Single(result.Board.Edges);
        Assert.Equal(Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"), restoredEdge.Id);
        var warning = Assert.Single(result.Warnings);
        Assert.Equal("dddddddd-dddd-dddd-dddd-dddddddddddd", warning.Entity, ignoreCase: true);
        Assert.Contains(
            "eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee",
            warning.Reason,
            StringComparison.OrdinalIgnoreCase
        );
    }

    [Fact]
    public void SkipsAnEdgeWithADuplicateIdAndRecordsAWarningInsteadOfFailingTheLoad()
    {
        var serializer = new BoardJsonSerializer(BuildRegistry());
        const string json = """
            {
              "SchemaVersion": 1,
              "Components": [],
              "Edges": [
                {
                  "Id": "cccccccc-cccc-cccc-cccc-cccccccccccc",
                  "Source": { "ComponentId": null, "PortId": null, "X": 0, "Y": 0 },
                  "Target": { "ComponentId": null, "PortId": null, "X": 10, "Y": 10 }
                },
                {
                  "Id": "cccccccc-cccc-cccc-cccc-cccccccccccc",
                  "Source": { "ComponentId": null, "PortId": null, "X": 0, "Y": 0 },
                  "Target": { "ComponentId": null, "PortId": null, "X": 20, "Y": 20 }
                }
              ]
            }
            """;

        var result = serializer.DeserializePartial(json);

        var restoredEdge = Assert.Single(result.Board.Edges);
        Assert.Equal(Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"), restoredEdge.Id);
        var warning = Assert.Single(result.Warnings);
        Assert.Equal("cccccccc-cccc-cccc-cccc-cccccccccccc", warning.Entity, ignoreCase: true);
    }

    [Fact]
    public void SkipsAStructurallyMalformedEdgeEntryAndRecordsAWarning()
    {
        var serializer = new BoardJsonSerializer(BuildRegistry());
        const string json = """
            {
              "SchemaVersion": 1,
              "Components": [],
              "Edges": [
                {
                  "Id": "88888888-8888-8888-8888-888888888888",
                  "Source": { "ComponentId": null, "PortId": null, "X": null, "Y": null },
                  "Target": { "ComponentId": null, "PortId": null, "X": 10, "Y": 20 }
                }
              ]
            }
            """;

        var result = serializer.DeserializePartial(json);

        Assert.Empty(result.Board.Edges);
        var warning = Assert.Single(result.Warnings);
        Assert.Equal("88888888-8888-8888-8888-888888888888", warning.Entity, ignoreCase: true);
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
