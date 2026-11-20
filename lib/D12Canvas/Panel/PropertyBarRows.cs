using System.Globalization;
using System.Reflection;
using D12Canvas.History;
using D12Canvas.Model;
using D12Canvas.Registration;

namespace D12Canvas.Panel;

internal static class PropertyBarRows
{
    public static IReadOnlyList<PropertyBarRow> For(
        IReadOnlyList<ComponentInstance> instances,
        IReadOnlyList<Edge> edges,
        IComponentRegistry registry,
        Action<IReadOnlyList<PropsChange>> commitProps,
        Action<IReadOnlyList<EdgeStyleChange>> commitEdgeStyles
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
        Action<IReadOnlyList<PropsChange>> commit
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
                    representative.OptionGlyphs,
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
        IReadOnlyDictionary<string, Glyph>? optionGlyphs,
        IReadOnlyList<(ComponentInstance Instance, PropertyInfo Property)> targets,
        bool canHoldNull,
        Action<IReadOnlyList<PropsChange>> commit
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

            var changes = TargetedEdit.ChangesFor(targets, declaration.Kind, value);
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
            Commit,
            optionGlyphs
        );
    }

    private sealed record EdgeRole(
        PropertyRole Role,
        Func<Edge, object?> Read,
        Func<EdgeStyle, object?, EdgeStyle> Write,
        IReadOnlyList<string>? Options,
        bool CanHoldNull,
        IReadOnlyDictionary<string, Glyph>? OptionGlyphs = null
    );

    private static readonly IReadOnlyDictionary<string, Glyph> RoutingGlyphs = new Dictionary<
        string,
        Glyph
    >
    {
        [nameof(EdgeRouting.Straight)] = Glyphs.RoutingStraight,
        [nameof(EdgeRouting.Orthogonal)] = Glyphs.RoutingOrthogonal,
        [nameof(EdgeRouting.Curved)] = Glyphs.RoutingCurved,
    };

    private static readonly IReadOnlyDictionary<string, Glyph> SourceArrowGlyphs = new Dictionary<
        string,
        Glyph
    >
    {
        [nameof(ArrowStyle.None)] = Glyphs.SourceArrowNone,
        [nameof(ArrowStyle.Arrow)] = Glyphs.SourceArrowArrow,
    };

    private static readonly IReadOnlyDictionary<string, Glyph> TargetArrowGlyphs = new Dictionary<
        string,
        Glyph
    >
    {
        [nameof(ArrowStyle.None)] = Glyphs.TargetArrowNone,
        [nameof(ArrowStyle.Arrow)] = Glyphs.TargetArrowArrow,
    };

    private static readonly IReadOnlyList<EdgeRole> EdgeRoles =
    [
        new(
            PropertyRole.EdgeRouting,
            edge => edge.RoutingStyle,
            (style, value) => style with { RoutingStyle = (EdgeRouting)value! },
            Enum.GetNames<EdgeRouting>(),
            CanHoldNull: false,
            RoutingGlyphs
        ),
        new(
            PropertyRole.EdgeSourceArrow,
            edge => edge.SourceArrow,
            (style, value) => style with { SourceArrow = (ArrowStyle)value! },
            Enum.GetNames<ArrowStyle>(),
            CanHoldNull: false,
            SourceArrowGlyphs
        ),
        new(
            PropertyRole.EdgeTargetArrow,
            edge => edge.TargetArrow,
            (style, value) => style with { TargetArrow = (ArrowStyle)value! },
            Enum.GetNames<ArrowStyle>(),
            CanHoldNull: false,
            TargetArrowGlyphs
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
        Action<IReadOnlyList<EdgeStyleChange>> commit
    ) => EdgeRoles.Select(edgeRole => EdgeRow(edgeRole, edges, commit)).ToList();

    private static PropertyBarRow EdgeRow(
        EdgeRole edgeRole,
        IReadOnlyList<Edge> edges,
        Action<IReadOnlyList<EdgeStyleChange>> commit
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
                    return new EdgeStyleChange(edge.Id, before, edgeRole.Write(before, value));
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
            Commit,
            edgeRole.OptionGlyphs
        );
    }

    private static EdgeStyle StyleOf(Edge edge) =>
        new(edge.RoutingStyle, edge.SourceArrow, edge.TargetArrow, edge.Color);

    private static string RowId(PropertyRole role) => $"d12-property-bar-{role}";

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
            value = TargetedEdit.Normalise(declaration.Kind, canHoldNull, text ?? "");
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
