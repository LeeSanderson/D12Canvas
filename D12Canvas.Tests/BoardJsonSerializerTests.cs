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

    // Ticket 55/ADR 0005: a custom port round-trips alongside the rest of ComponentInstance's
    // own envelope - not a separate Board array, since it's instance-scoped state, not a
    // first-class board entity.
    [Fact]
    public void SerializeIncludesAnInstancesCustomPorts()
    {
        var serializer = new BoardJsonSerializer(BuildRegistry());
        var board = new Board();
        var instance = new ComponentInstance(
            TestComponentKey,
            new TestProps(),
            new Bounds(0, 0, 10, 10)
        );
        instance.CustomPorts.Add(new PortDef(0.25, 0));
        board.AddComponent(instance);

        var json = serializer.Serialize(board);

        using var document = JsonDocument.Parse(json);
        var componentElement = document.RootElement.GetProperty("Components")[0];
        var customPortsElement = componentElement.GetProperty("CustomPorts");
        Assert.Equal(1, customPortsElement.GetArrayLength());
        Assert.Equal(0.25, customPortsElement[0].GetProperty("FractionX").GetDouble());
    }

    [Fact]
    public void DeserializeRebuildsAnInstancesCustomPortsWithIdentityIntact()
    {
        var serializer = new BoardJsonSerializer(BuildRegistry());
        var board = new Board();
        var instance = new ComponentInstance(
            TestComponentKey,
            new TestProps(),
            new Bounds(0, 0, 10, 10)
        );
        var port = new PortDef(0.25, 0);
        instance.CustomPorts.Add(port);
        board.AddComponent(instance);

        var restored = serializer.Deserialize(serializer.Serialize(board));

        var restoredInstance = restored.GetComponent(instance.Id);
        Assert.NotNull(restoredInstance);
        Assert.Equal(new[] { port }, restoredInstance!.CustomPorts);
    }

    // A board saved before this ticket has no "CustomPorts" property on its components at all -
    // same "field didn't exist yet" tolerance every other ticket's own envelope addition relies on.
    [Fact]
    public void DeserializeDefaultsAnOlderInstanceMissingCustomPortsToAnEmptyList()
    {
        var serializer = new BoardJsonSerializer(BuildRegistry());
        const string json = """
            {
              "SchemaVersion": 1,
              "Components": [
                {
                  "Id": "11111111-1111-1111-1111-111111111111",
                  "ComponentTypeKey": "test-props",
                  "Props": {},
                  "Bounds": { "X": 0, "Y": 0, "Width": 10, "Height": 10 },
                  "ZIndex": 0
                }
              ]
            }
            """;

        var restored = serializer.Deserialize(json);

        var restoredInstance = restored.GetComponent(
            Guid.Parse("11111111-1111-1111-1111-111111111111")
        );
        Assert.NotNull(restoredInstance);
        Assert.Empty(restoredInstance!.CustomPorts);
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

    // Ticket 46: Groups join the envelope alongside Components, round-tripping identity and
    // MemberIds - including membership that nests one Group inside another.
    [Fact]
    public void SerializeProducesAnEnvelopeWithAGroupsArray()
    {
        var serializer = new BoardJsonSerializer(BuildRegistry());
        var board = new Board();
        var first = new ComponentInstance(
            TestComponentKey,
            new TestProps(),
            new Bounds(0, 0, 10, 10)
        );
        var second = new ComponentInstance(
            TestComponentKey,
            new TestProps(),
            new Bounds(20, 20, 10, 10)
        );
        board.AddComponent(first);
        board.AddComponent(second);
        var group = new Group(new[] { first.Id, second.Id });
        board.AddGroup(group);

        var json = serializer.Serialize(board);

        using var document = JsonDocument.Parse(json);
        var groupsElement = document.RootElement.GetProperty("Groups");
        Assert.Equal(1, groupsElement.GetArrayLength());
        var groupElement = groupsElement[0];
        Assert.Equal(group.Id, groupElement.GetProperty("Id").GetGuid());
        Assert.Equal(2, groupElement.GetProperty("MemberIds").GetArrayLength());
    }

    [Fact]
    public void DeserializeRebuildsAGroupWithIdentityAndMemberIdsIntact()
    {
        var serializer = new BoardJsonSerializer(BuildRegistry());
        var board = new Board();
        var first = new ComponentInstance(
            TestComponentKey,
            new TestProps(),
            new Bounds(0, 0, 10, 10)
        );
        var second = new ComponentInstance(
            TestComponentKey,
            new TestProps(),
            new Bounds(20, 20, 10, 10)
        );
        board.AddComponent(first);
        board.AddComponent(second);
        var group = new Group(new[] { first.Id, second.Id });
        board.AddGroup(group);

        var restored = serializer.Deserialize(serializer.Serialize(board));

        var restoredGroup = restored.GetGroup(group.Id);
        Assert.NotNull(restoredGroup);
        Assert.Equal(group.MemberIds, restoredGroup!.MemberIds);
    }

    [Fact]
    public void DeserializeRebuildsNestedGroupMembership()
    {
        var serializer = new BoardJsonSerializer(BuildRegistry());
        var board = new Board();
        var instance = new ComponentInstance(
            TestComponentKey,
            new TestProps(),
            new Bounds(0, 0, 10, 10)
        );
        board.AddComponent(instance);
        var inner = new Group(new[] { instance.Id });
        board.AddGroup(inner);
        var outer = new Group(new[] { inner.Id });
        board.AddGroup(outer);

        var restored = serializer.Deserialize(serializer.Serialize(board));

        var restoredOuter = restored.GetGroup(outer.Id);
        Assert.NotNull(restoredOuter);
        Assert.Equal(new[] { inner.Id }, restoredOuter!.MemberIds);
        Assert.NotNull(restored.GetGroup(inner.Id));
        Assert.Same(restoredOuter, restored.FindContainingGroup(instance.Id));
    }

    [Fact]
    public void ReloadedGroupsBehaveAsGroupsSelectingAnyMemberFindsTheSameGroup()
    {
        var serializer = new BoardJsonSerializer(BuildRegistry());
        var board = new Board();
        var first = new ComponentInstance(
            TestComponentKey,
            new TestProps(),
            new Bounds(0, 0, 10, 10)
        );
        var second = new ComponentInstance(
            TestComponentKey,
            new TestProps(),
            new Bounds(20, 20, 10, 10)
        );
        board.AddComponent(first);
        board.AddComponent(second);
        var group = new Group(new[] { first.Id, second.Id });
        board.AddGroup(group);

        var restored = serializer.Deserialize(serializer.Serialize(board));

        var foundViaFirst = restored.FindContainingGroup(first.Id);
        var foundViaSecond = restored.FindContainingGroup(second.Id);
        Assert.NotNull(foundViaFirst);
        Assert.Equal(group.Id, foundViaFirst!.Id);
        Assert.Same(foundViaFirst, foundViaSecond);
    }

    // Strict deserialize does no referential-integrity checking for Groups, same as it already
    // does none for Components (e.g. no check that a ComponentTypeKey's props shape is sane beyond
    // registry resolution) - a dangling member id is loaded as-is, never a throw. Only the tolerant
    // partial path (BoardJsonSerializerPartialDeserializeTests) turns this into a warning.
    [Fact]
    public void StrictDeserializeLoadsAGroupWithAMissingMemberWithoutThrowing()
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

        var restored = serializer.Deserialize(json);

        var restoredGroup = restored.GetGroup(Guid.Parse("55555555-5555-5555-5555-555555555555"));
        Assert.NotNull(restoredGroup);
        Assert.Equal(
            Guid.Parse("66666666-6666-6666-6666-666666666666"),
            Assert.Single(restoredGroup!.MemberIds)
        );
    }

    // Ticket 51: Edges join the envelope alongside Components and Groups. Attached endpoints
    // round-trip as instance + port references; floating endpoints as board points.
    [Fact]
    public void SerializeProducesAnEnvelopeWithAnEdgesArray()
    {
        var serializer = new BoardJsonSerializer(BuildRegistry());
        var board = new Board();
        var first = new ComponentInstance(
            TestComponentKey,
            new TestProps(),
            new Bounds(0, 0, 10, 10)
        );
        var second = new ComponentInstance(
            TestComponentKey,
            new TestProps(),
            new Bounds(20, 20, 10, 10)
        );
        board.AddComponent(first);
        board.AddComponent(second);
        var edge = new Edge(
            new PortEndpoint(first.Id, PortId.Right),
            new PortEndpoint(second.Id, PortId.Left)
        );
        board.AddEdge(edge);

        var json = serializer.Serialize(board);

        using var document = JsonDocument.Parse(json);
        var edgesElement = document.RootElement.GetProperty("Edges");
        Assert.Equal(1, edgesElement.GetArrayLength());
        Assert.Equal(edge.Id, edgesElement[0].GetProperty("Id").GetGuid());
    }

    [Fact]
    public void DeserializeRebuildsAnEdgeWithBothEndpointsAttachedToPorts()
    {
        var serializer = new BoardJsonSerializer(BuildRegistry());
        var board = new Board();
        var first = new ComponentInstance(
            TestComponentKey,
            new TestProps(),
            new Bounds(0, 0, 10, 10)
        );
        var second = new ComponentInstance(
            TestComponentKey,
            new TestProps(),
            new Bounds(20, 20, 10, 10)
        );
        board.AddComponent(first);
        board.AddComponent(second);
        var edge = new Edge(
            new PortEndpoint(first.Id, PortId.Right),
            new PortEndpoint(second.Id, PortId.Left)
        );
        board.AddEdge(edge);

        var restored = serializer.Deserialize(serializer.Serialize(board));

        var restoredEdge = restored.GetEdge(edge.Id);
        Assert.NotNull(restoredEdge);
        Assert.Equal(new PortEndpoint(first.Id, PortId.Right), restoredEdge!.Source);
        Assert.Equal(new PortEndpoint(second.Id, PortId.Left), restoredEdge.Target);
    }

    [Fact]
    public void DeserializeRebuildsAnEdgeWithAFloatingEndpoint()
    {
        var serializer = new BoardJsonSerializer(BuildRegistry());
        var board = new Board();
        var instance = new ComponentInstance(
            TestComponentKey,
            new TestProps(),
            new Bounds(0, 0, 10, 10)
        );
        board.AddComponent(instance);
        var edge = new Edge(
            new PortEndpoint(instance.Id, PortId.Right),
            new FloatingEndpoint(123, 456)
        );
        board.AddEdge(edge);

        var restored = serializer.Deserialize(serializer.Serialize(board));

        var restoredEdge = restored.GetEdge(edge.Id);
        Assert.NotNull(restoredEdge);
        Assert.Equal(new FloatingEndpoint(123, 456), restoredEdge!.Target);
    }

    // Ticket 52: RoutingStyle/SourceArrow/TargetArrow round-trip through the same envelope,
    // including non-default values - proving the round-trip isn't just an artifact of every edge
    // happening to keep its default style.
    [Fact]
    public void DeserializeRebuildsAnEdgesNonDefaultRoutingAndArrowStyles()
    {
        var serializer = new BoardJsonSerializer(BuildRegistry());
        var board = new Board();
        var first = new ComponentInstance(
            TestComponentKey,
            new TestProps(),
            new Bounds(0, 0, 10, 10)
        );
        var second = new ComponentInstance(
            TestComponentKey,
            new TestProps(),
            new Bounds(20, 20, 10, 10)
        );
        board.AddComponent(first);
        board.AddComponent(second);
        var edge = new Edge(
            new PortEndpoint(first.Id, PortId.Right),
            new PortEndpoint(second.Id, PortId.Left),
            routingStyle: EdgeRouting.Curved,
            sourceArrow: ArrowStyle.Arrow,
            targetArrow: ArrowStyle.None
        );
        board.AddEdge(edge);

        var restored = serializer.Deserialize(serializer.Serialize(board));

        var restoredEdge = restored.GetEdge(edge.Id);
        Assert.NotNull(restoredEdge);
        Assert.Equal(EdgeRouting.Curved, restoredEdge!.RoutingStyle);
        Assert.Equal(ArrowStyle.Arrow, restoredEdge.SourceArrow);
        Assert.Equal(ArrowStyle.None, restoredEdge.TargetArrow);
    }

    // Ticket 52: a board saved before this ticket has no RoutingStyle/SourceArrow/TargetArrow
    // properties in its JSON at all - same "field didn't exist yet" tolerance Groups/Edges
    // themselves already rely on (BoardEnvelope.Groups/Edges being nullable). Every edge from such
    // a board should come back with Edge's own ADR 0005 defaults, not throw or leave a null style.
    [Fact]
    public void DeserializeDefaultsAnOlderEdgesMissingRoutingAndArrowStylesToTheAdr0005Defaults()
    {
        var serializer = new BoardJsonSerializer(BuildRegistry());
        const string json = """
            {
              "SchemaVersion": 1,
              "Components": [],
              "Edges": [
                {
                  "Id": "88888888-8888-8888-8888-888888888888",
                  "Source": { "ComponentId": null, "PortId": null, "X": 1, "Y": 2 },
                  "Target": { "ComponentId": null, "PortId": null, "X": 3, "Y": 4 }
                }
              ]
            }
            """;

        var restored = serializer.Deserialize(json);

        var restoredEdge = restored.GetEdge(Guid.Parse("88888888-8888-8888-8888-888888888888"));
        Assert.NotNull(restoredEdge);
        Assert.Equal(EdgeRouting.Straight, restoredEdge!.RoutingStyle);
        Assert.Equal(ArrowStyle.None, restoredEdge.SourceArrow);
        Assert.Equal(ArrowStyle.Arrow, restoredEdge.TargetArrow);
    }

    // Ticket 53: an edge's embedded Label round-trips through the same envelope as an ordinary
    // ComponentInstanceEnvelope (Props/Bounds/ComponentTypeKey) - it's not a separate Board.Components
    // entry, so it isn't in the Components array at all, just nested on its owning edge.
    [Fact]
    public void SerializeIncludesTheEdgesLabelWhenPresent()
    {
        var serializer = new BoardJsonSerializer(BuildRegistry());
        var board = new Board();
        var first = new ComponentInstance(
            TestComponentKey,
            new TestProps(),
            new Bounds(0, 0, 10, 10)
        );
        var second = new ComponentInstance(
            TestComponentKey,
            new TestProps(),
            new Bounds(20, 20, 10, 10)
        );
        board.AddComponent(first);
        board.AddComponent(second);
        var label = new ComponentInstance(
            TestComponentKey,
            new TestProps("Label text"),
            new Bounds(5, 5, 80, 24)
        );
        var edge = new Edge(
            new PortEndpoint(first.Id, PortId.Right),
            new PortEndpoint(second.Id, PortId.Left),
            label: label
        );
        board.AddEdge(edge);

        var json = serializer.Serialize(board);

        using var document = JsonDocument.Parse(json);
        var edgeElement = document.RootElement.GetProperty("Edges")[0];
        Assert.True(edgeElement.TryGetProperty("Label", out var labelElement));
        Assert.Equal(label.Id, labelElement.GetProperty("Id").GetGuid());
    }

    [Fact]
    public void DeserializeRebuildsAnEdgesLabelAsAnEmbeddedComponentInstance()
    {
        var serializer = new BoardJsonSerializer(BuildRegistry());
        var board = new Board();
        var first = new ComponentInstance(
            TestComponentKey,
            new TestProps(),
            new Bounds(0, 0, 10, 10)
        );
        var second = new ComponentInstance(
            TestComponentKey,
            new TestProps(),
            new Bounds(20, 20, 10, 10)
        );
        board.AddComponent(first);
        board.AddComponent(second);
        var label = new ComponentInstance(
            TestComponentKey,
            new TestProps("Label text"),
            new Bounds(5, 5, 80, 24)
        );
        var edge = new Edge(
            new PortEndpoint(first.Id, PortId.Right),
            new PortEndpoint(second.Id, PortId.Left),
            label: label
        );
        board.AddEdge(edge);

        var restored = serializer.Deserialize(serializer.Serialize(board));

        var restoredEdge = restored.GetEdge(edge.Id);
        Assert.NotNull(restoredEdge);
        Assert.NotNull(restoredEdge!.Label);
        Assert.Equal(label.Id, restoredEdge.Label!.Id);
        Assert.Equal(TestComponentKey, restoredEdge.Label.ComponentTypeKey);
        Assert.Equal(new Bounds(5, 5, 80, 24), restoredEdge.Label.Bounds);
        Assert.Equal("Label text", ((TestProps)restoredEdge.Label.Props).Text);
    }

    [Fact]
    public void DeserializeRebuildsAnEdgeWithNoLabelAsNull()
    {
        var serializer = new BoardJsonSerializer(BuildRegistry());
        var board = new Board();
        var edge = new Edge(
            new PortEndpoint(Guid.NewGuid(), PortId.Right),
            new FloatingEndpoint(1, 2)
        );
        board.AddEdge(edge);

        var restored = serializer.Deserialize(serializer.Serialize(board));

        var restoredEdge = restored.GetEdge(edge.Id);
        Assert.NotNull(restoredEdge);
        Assert.Null(restoredEdge!.Label);
    }

    // A board saved before this ticket has no "Label" property on its edges at all - same
    // "field didn't exist yet" tolerance RoutingStyle/SourceArrow/TargetArrow already rely on.
    [Fact]
    public void DeserializeDefaultsAnOlderEdgesMissingLabelToNull()
    {
        var serializer = new BoardJsonSerializer(BuildRegistry());
        const string json = """
            {
              "SchemaVersion": 1,
              "Components": [],
              "Edges": [
                {
                  "Id": "99999999-9999-9999-9999-999999999999",
                  "Source": { "ComponentId": null, "PortId": null, "X": 1, "Y": 2 },
                  "Target": { "ComponentId": null, "PortId": null, "X": 3, "Y": 4 }
                }
              ]
            }
            """;

        var restored = serializer.Deserialize(json);

        var restoredEdge = restored.GetEdge(Guid.Parse("99999999-9999-9999-9999-999999999999"));
        Assert.NotNull(restoredEdge);
        Assert.Null(restoredEdge!.Label);
    }

    [Fact]
    public void ReloadedEdgesStayAttachedMovingAnEndpointInstanceAfterRoundTripStillDragsTheEdgeAlong()
    {
        var serializer = new BoardJsonSerializer(BuildRegistry());
        var board = new Board();
        var first = new ComponentInstance(
            TestComponentKey,
            new TestProps(),
            new Bounds(0, 0, 10, 10)
        );
        var second = new ComponentInstance(
            TestComponentKey,
            new TestProps(),
            new Bounds(20, 20, 10, 10)
        );
        board.AddComponent(first);
        board.AddComponent(second);
        board.AddEdge(
            new Edge(
                new PortEndpoint(first.Id, PortId.Right),
                new PortEndpoint(second.Id, PortId.Left)
            )
        );

        var restored = serializer.Deserialize(serializer.Serialize(board));
        var restoredFirst = restored.GetComponent(first.Id)!;
        restoredFirst.Bounds = new Bounds(100, 100, 10, 10);

        var restoredEdge = Assert.Single(restored.Edges);
        var sourcePoint = restored.ResolveEndpoint(restoredEdge.Source);
        Assert.Equal(restoredFirst.Bounds.PointAtFraction(1, 0.5), sourcePoint);
    }

    // Ticket 55/ADR 0005: an edge attached to a custom port round-trips exactly like one attached
    // to a standard port - same envelope shape, distinguished by which field pair is populated
    // (see EdgeEndpointEnvelope), and still tracks its instance's Bounds afterwards.
    [Fact]
    public void ACustomPortAttachedEdgeRoundTripsAndStaysAttached()
    {
        var serializer = new BoardJsonSerializer(BuildRegistry());
        var board = new Board();
        var first = new ComponentInstance(
            TestComponentKey,
            new TestProps(),
            new Bounds(0, 0, 10, 10)
        );
        var port = new PortDef(0.25, 0);
        first.CustomPorts.Add(port);
        board.AddComponent(first);
        var edge = new Edge(new CustomPortEndpoint(first.Id, port.Id), new FloatingEndpoint(1, 2));
        board.AddEdge(edge);

        var restored = serializer.Deserialize(serializer.Serialize(board));

        var restoredEdge = Assert.Single(restored.Edges);
        Assert.Equal(new CustomPortEndpoint(first.Id, port.Id), restoredEdge.Source);

        var restoredFirst = restored.GetComponent(first.Id)!;
        restoredFirst.Bounds = new Bounds(100, 100, 20, 20);
        Assert.Equal(
            restoredFirst.Bounds.PointAtFraction(0.25, 0),
            restored.ResolveEndpoint(restoredEdge.Source)
        );
    }

    // Strict deserialize does no referential-integrity checking for Edges either, same as Groups
    // (StrictDeserializeLoadsAGroupWithAMissingMemberWithoutThrowing above) - a dangling endpoint
    // componentId is loaded as-is. Only the tolerant partial path turns this into a warning.
    [Fact]
    public void StrictDeserializeLoadsAnEdgeWithAMissingEndpointInstanceWithoutThrowing()
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

        var restored = serializer.Deserialize(json);

        var restoredEdge = restored.GetEdge(Guid.Parse("77777777-7777-7777-7777-777777777777"));
        Assert.NotNull(restoredEdge);
        Assert.Equal(
            new PortEndpoint(Guid.Parse("66666666-6666-6666-6666-666666666666"), PortId.Top),
            restoredEdge!.Source
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
