using System.Globalization;
using System.Reflection;
using D12Canvas.History;
using D12Canvas.Model;
using D12Canvas.Registration;

namespace D12Canvas.Panel;

// The two producers of property bar rows. Instance rows are the roles every selected instance's
// type declares, each committing through the props batch; edge rows are the four edge roles, each
// committing through the edge style. An instance role and an edge role never coincide, so a
// selection holding both kinds has no row in common and gets none. Rows follow the role order.
internal static class PropertyBarRows
{
    public static IReadOnlyList<PropertyBarRow> For(
        IReadOnlyList<ComponentInstance> instances,
        IReadOnlyList<Edge> edges,
        IComponentRegistry registry,
        Action<IReadOnlyList<(Guid InstanceId, object Before, object After)>> commitProps,
        Action<IReadOnlyList<(Guid EdgeId, EdgeStyle Before, EdgeStyle After)>> commitEdgeStyles
    ) =>
        (instances.Count > 0, edges.Count > 0) switch
        {
            (true, false) => ForInstances(instances, registry, commitProps),
            (false, true) => ForEdges(edges, commitEdgeStyles),
            _ => [],
        };

    public static IReadOnlyList<PropertyBarRow> ForInstances(
        IReadOnlyList<ComponentInstance> instances,
        IComponentRegistry registry,
        Action<IReadOnlyList<(Guid InstanceId, object Before, object After)>> commit
    )
    {
        var schemas = instances
            .Select(instance => instance.ComponentTypeKey)
            .Distinct()
            .ToDictionary(
                key => key,
                key => registry.Resolve(key).EditableProperties ?? Array.Empty<EditableProperty>()
            );

        var rows = new List<PropertyBarRow>();
        foreach (var role in Enum.GetValues<PropertyRole>())
        {
            var propertiesByType = schemas.ToDictionary(
                schema => schema.Key,
                schema => schema.Value.FirstOrDefault(property => property.Role == role)
            );
            if (propertiesByType.Values.Any(property => property is null))
            {
                continue;
            }

            var targets = instances
                .Select(instance =>
                    (Instance: instance, propertiesByType[instance.ComponentTypeKey]!.Property)
                )
                .ToList();
            var representative = propertiesByType.Values.First()!;
            rows.Add(
                InstanceRow(
                    role,
                    representative.Options,
                    targets,
                    propertiesByType.Values.All(property => property!.CanHoldNull),
                    commit
                )
            );
        }

        return rows;
    }

    private static PropertyBarRow InstanceRow(
        PropertyRole role,
        IReadOnlyList<string>? options,
        IReadOnlyList<(ComponentInstance Instance, PropertyInfo Property)> targets,
        bool canHoldNull,
        Action<IReadOnlyList<(Guid InstanceId, object Before, object After)>> commit
    )
    {
        var declaration = PropertyRoleDeclarations.For(role);
        var values = targets
            .Select(target => target.Property.GetValue(target.Instance.Props))
            .ToList();
        var isMixed = MixedValue.IsMixed(declaration.Kind, values);

        void Commit(object? raw)
        {
            if (!TryConvert(raw, declaration, canHoldNull, out var value))
            {
                return;
            }

            var changes = targets
                .Where(target =>
                    !MixedValue.AreEqual(
                        declaration.Kind,
                        target.Property.GetValue(target.Instance.Props),
                        value
                    )
                )
                .Select(target =>
                    (
                        target.Instance.Id,
                        target.Instance.Props,
                        PropsCopy.With(target.Instance.Props, target.Property, value)
                    )
                )
                .ToList();
            if (changes.Count > 0)
            {
                commit(changes);
            }
        }

        return new PropertyBarRow(
            RowId(role),
            role,
            declaration.Kind,
            options,
            isMixed ? null : values[0],
            isMixed,
            canHoldNull,
            Commit
        );
    }

    private sealed record EdgeRole(
        PropertyRole Role,
        Func<Edge, object?> Read,
        Func<EdgeStyle, object?, EdgeStyle> Write,
        IReadOnlyList<string>? Options,
        bool CanHoldNull
    );

    private static readonly IReadOnlyList<EdgeRole> EdgeRoles =
    [
        new(
            PropertyRole.EdgeRouting,
            edge => edge.RoutingStyle,
            (style, value) => style with { RoutingStyle = (EdgeRouting)value! },
            Enum.GetNames<EdgeRouting>(),
            CanHoldNull: false
        ),
        new(
            PropertyRole.EdgeSourceArrow,
            edge => edge.SourceArrow,
            (style, value) => style with { SourceArrow = (ArrowStyle)value! },
            Enum.GetNames<ArrowStyle>(),
            CanHoldNull: false
        ),
        new(
            PropertyRole.EdgeTargetArrow,
            edge => edge.TargetArrow,
            (style, value) => style with { TargetArrow = (ArrowStyle)value! },
            Enum.GetNames<ArrowStyle>(),
            CanHoldNull: false
        ),
        new(
            PropertyRole.EdgeColour,
            edge => edge.Color,
            (style, value) => style with { Color = (string?)value },
            null,
            CanHoldNull: true
        ),
    ];

    public static IReadOnlyList<PropertyBarRow> ForEdges(
        IReadOnlyList<Edge> edges,
        Action<IReadOnlyList<(Guid EdgeId, EdgeStyle Before, EdgeStyle After)>> commit
    ) => EdgeRoles.Select(edgeRole => EdgeRow(edgeRole, edges, commit)).ToList();

    private static PropertyBarRow EdgeRow(
        EdgeRole edgeRole,
        IReadOnlyList<Edge> edges,
        Action<IReadOnlyList<(Guid EdgeId, EdgeStyle Before, EdgeStyle After)>> commit
    )
    {
        var declaration = PropertyRoleDeclarations.For(edgeRole.Role);
        var values = edges.Select(edgeRole.Read).ToList();
        var isMixed = MixedValue.IsMixed(declaration.Kind, values);

        void Commit(object? raw)
        {
            if (!TryConvert(raw, declaration, edgeRole.CanHoldNull, out var value))
            {
                return;
            }

            var changes = edges
                .Where(edge => !MixedValue.AreEqual(declaration.Kind, edgeRole.Read(edge), value))
                .Select(edge =>
                {
                    var before = StyleOf(edge);
                    return (edge.Id, before, edgeRole.Write(before, value));
                })
                .ToList();
            if (changes.Count > 0)
            {
                commit(changes);
            }
        }

        return new PropertyBarRow(
            RowId(edgeRole.Role),
            edgeRole.Role,
            declaration.Kind,
            edgeRole.Options,
            isMixed ? null : values[0],
            isMixed,
            edgeRole.CanHoldNull,
            Commit
        );
    }

    private static EdgeStyle StyleOf(Edge edge) =>
        new(edge.RoutingStyle, edge.SourceArrow, edge.TargetArrow, edge.Color);

    private static string RowId(PropertyRole role) => $"d12-property-bar-{role}";

    // A control reports a string; a value that does not parse as the role's type commits nothing.
    // An empty colour on a row that can hold null is the themed state.
    private static bool TryConvert(
        object? raw,
        PropertyRoleDeclaration declaration,
        bool canHoldNull,
        out object? value
    )
    {
        value = null;
        var text = raw as string ?? Convert.ToString(raw, CultureInfo.InvariantCulture);
        if (declaration.ClrType == typeof(string))
        {
            if (string.IsNullOrEmpty(text) && declaration.Kind == EditorKind.Color && canHoldNull)
            {
                return true;
            }

            value = text ?? "";
            return true;
        }

        if (declaration.ClrType == typeof(double))
        {
            if (
                double.TryParse(
                    text,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out var number
                ) && double.IsFinite(number)
            )
            {
                value = number;
                return true;
            }

            return false;
        }

        if (
            declaration.ClrType.IsEnum
            && Enum.TryParse(declaration.ClrType, text, ignoreCase: false, out var parsed)
            && Enum.IsDefined(declaration.ClrType, parsed)
        )
        {
            value = parsed;
            return true;
        }

        return false;
    }
}
