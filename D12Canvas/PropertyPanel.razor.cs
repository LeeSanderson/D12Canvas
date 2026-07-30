using System.Globalization;
using System.Reflection;
using D12Canvas.Model;
using D12Canvas.Panel;
using D12Canvas.Registration;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace D12Canvas;

// ADR 0008: a standalone chrome component, wired to its DiagramCanvas the same explicit way
// Palette is (ADR 0002 - chrome isn't nested inside the canvas, so no cascading parameter reaches
// it). Unlike Palette, the panel's content depends on transient selection state living inside
// DiagramCanvas, so it also subscribes to DiagramCanvas.SelectionChanged to know when to re-render.
public partial class PropertyPanel : IDisposable
{
    private static readonly MethodInfo MemberwiseCloneMethod = typeof(object).GetMethod(
        "MemberwiseClone",
        BindingFlags.NonPublic | BindingFlags.Instance
    )!;

    [Inject]
    private IComponentRegistry Registry { get; set; } = null!;

    [Parameter]
    public DiagramCanvas? Canvas { get; set; }

    private DiagramCanvas? _subscribedCanvas;

    protected override void OnParametersSet()
    {
        if (ReferenceEquals(_subscribedCanvas, Canvas))
        {
            return;
        }

        if (_subscribedCanvas is not null)
        {
            _subscribedCanvas.SelectionChanged -= HandleSelectionChanged;
        }

        _subscribedCanvas = Canvas;

        if (_subscribedCanvas is not null)
        {
            _subscribedCanvas.SelectionChanged += HandleSelectionChanged;
        }
    }

    private void HandleSelectionChanged(object? sender, EventArgs e) => StateHasChanged();

    public void Dispose()
    {
        if (_subscribedCanvas is not null)
        {
            _subscribedCanvas.SelectionChanged -= HandleSelectionChanged;
        }
    }

    private IReadOnlyList<ComponentInstance> SelectedInstances =>
        Canvas?.SelectedComponents ?? Array.Empty<ComponentInstance>();

    // ADR 0008/ticket 59: one rendered row. A same-type selection (1 or 2+ instances) maps every
    // target to the SAME reflected PropertyInfo, since they all share one TProps type. A cross-type
    // multi-selection instead maps each instance to whichever of ITS OWN type's properties carries
    // the matching SharedTag - the names can differ across types, but SharedPropertyValidator
    // guarantees every property under a given tag agrees in Kind/CLR type at registration time, so
    // the row can render (and bulk-commit) as if it were a single property.
    private sealed record PanelField(
        string FieldId,
        string Label,
        EditorKind Kind,
        IReadOnlyList<string>? Options,
        RenderFragment<CustomEditorContext>? CustomEditor,
        IReadOnlyList<(ComponentInstance Instance, PropertyInfo Property)> Targets
    );

    private IReadOnlyList<PanelField> Fields
    {
        get
        {
            var instances = SelectedInstances;
            if (instances.Count == 0)
            {
                return Array.Empty<PanelField>();
            }

            var typeKeys = instances
                .Select(instance => instance.ComponentTypeKey)
                .Distinct()
                .ToList();

            return typeKeys.Count == 1
                ? SameTypeFields(typeKeys[0], instances)
                : CrossTypeFields(typeKeys, instances);
        }
    }

    // Ticket 56/59: a single type's own full declared schema - the ticket-56 shape for one selected
    // instance, extended so a same-type 2+ multi-selection edits every member through the identical
    // PropertyInfo (ticket 59).
    private IReadOnlyList<PanelField> SameTypeFields(
        string typeKey,
        IReadOnlyList<ComponentInstance> instances
    ) =>
        (Registry.Resolve(typeKey).EditableProperties ?? Array.Empty<EditableProperty>())
            .Select(property => new PanelField(
                FieldId: $"d12-property-panel-field-{property.Property.Name}",
                Label: property.Property.Name,
                Kind: property.Kind,
                Options: property.Options,
                CustomEditor: property.CustomEditor,
                Targets: instances.Select(instance => (instance, property.Property)).ToList()
            ))
            .ToList();

    // Ticket 59/ADR 0008: only a tag every selected type's own schema carries surfaces at all -
    // never inferred from a shared property name, only from an explicit, matching SharedTag. Each
    // selected instance still reads/writes through its own type's PropertyInfo for that tag; the
    // field is keyed and labelled by the tag itself rather than any one type's property name, since
    // the two types are free to name it differently.
    private IReadOnlyList<PanelField> CrossTypeFields(
        IReadOnlyList<string> typeKeys,
        IReadOnlyList<ComponentInstance> instances
    )
    {
        var schemasByType = typeKeys.ToDictionary(
            key => key,
            key => Registry.Resolve(key).EditableProperties ?? Array.Empty<EditableProperty>()
        );

        var sharedTags = schemasByType
            .Values.Select(schema =>
                schema
                    .Where(property => property.SharedTag is not null)
                    .Select(property => property.SharedTag!)
                    .ToHashSet()
            )
            .Aggregate(
                (a, b) =>
                {
                    a.IntersectWith(b);
                    return a;
                }
            );

        return sharedTags
            .Select(tag =>
            {
                var representative = schemasByType[typeKeys[0]]
                    .First(property => property.SharedTag == tag);
                var targets = instances
                    .Select(instance =>
                        (
                            instance,
                            schemasByType[instance.ComponentTypeKey]
                                .First(property => property.SharedTag == tag)
                                .Property
                        )
                    )
                    .ToList();

                return new PanelField(
                    FieldId: $"d12-property-panel-field-{tag}",
                    Label: tag,
                    Kind: representative.Kind,
                    Options: representative.Options,
                    CustomEditor: representative.CustomEditor,
                    Targets: targets
                );
            })
            .ToList();
    }

    // ticket 59: a multi-target field displays whichever target happens to be first as its
    // representative current value - there's no "mixed values" indicator, matching how a same-type
    // multi-selection already has no per-instance display distinction.
    private static object? FirstTargetValue(PanelField field) =>
        field.Targets.Count == 0
            ? null
            : field.Targets[0].Property.GetValue(field.Targets[0].Instance.Props);

    private string CurrentValue(PanelField field) => FormatValue(FirstTargetValue(field));

    // Checkbox binds via the "checked" DOM property, not "value" - a separate accessor rather than
    // routing bool through FormatValue/CurrentValue's string-shaped path.
    private bool CurrentBoolValue(PanelField field) => FirstTargetValue(field) is true;

    // A Custom editor gets the current value of the field's first target plus a commit callback
    // closed over this same field - Commit directly, not via CommitEdit, since a Custom editor's
    // value is already CLR-typed and needs no ChangeEventArgs/ConvertValue parsing (ticket 58).
    private CustomEditorContext CustomContext(PanelField field) =>
        new(FirstTargetValue(field), newValue => Commit(field, newValue));

    private static string FormatValue(object? value) =>
        value switch
        {
            null => "",
            string text => text,
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString() ?? "",
        };

    // An edit that fails to parse commits nothing - Commit's own no-op-if-unchanged guard covers
    // the "same value again" case once parsing succeeds.
    private void CommitEdit(PanelField field, ChangeEventArgs args)
    {
        if (field.Targets.Count == 0)
        {
            return;
        }

        object? newValue;
        try
        {
            newValue = ConvertValue(args.Value, field.Targets[0].Property.PropertyType);
        }
        catch (Exception ex)
            when (ex is FormatException or InvalidCastException or OverflowException)
        {
            return;
        }

        Commit(field, newValue);
    }

    // Ticket 56/59: every target that would actually change becomes one MutateEntityCommand - a
    // target already at newValue contributes nothing, the same no-op guard ticket 56 applied to a
    // single instance, just checked per target instead. The whole edit still commits as one atomic
    // history entry via CommitPropsChangeBatch, whether it touches one instance or many, and
    // whether or not every target ends up changing.
    private void Commit(PanelField field, object? newValue)
    {
        var changes = new List<(Guid InstanceId, object Before, object After)>();
        foreach (var (instance, property) in field.Targets)
        {
            var before = instance.Props;
            var currentValue = property.GetValue(before);
            if (Equals(currentValue, newValue))
            {
                continue;
            }

            changes.Add((instance.Id, before, CloneWithChange(before, property, newValue)));
        }

        if (changes.Count == 0)
        {
            return;
        }

        Canvas?.CommitPropsChangeBatch(changes);
    }

    // A checkbox's ChangeEventArgs.Value arrives as a bool (Blazor reads the DOM element's
    // .checked property directly); every other control's arrives as a string.
    private static object? ConvertValue(object? raw, Type targetType)
    {
        if (targetType == typeof(string))
        {
            return raw as string ?? "";
        }

        if (targetType == typeof(bool))
        {
            return (bool)raw!;
        }

        return Convert.ChangeType(raw, targetType, CultureInfo.InvariantCulture);
    }

    // A TProps record is immutable - editing one field normally means a `with` expression, but the
    // panel only ever sees Props as an opaque object (ADR 0007), so it has no compile-time TProps
    // to write `with` against. MemberwiseClone + a single reflected overwrite is the generic
    // equivalent: it copies every field byte-for-byte (so unrelated properties are untouched) and
    // leaves the original object - which becomes MutateEntityCommand's "before" - unmodified.
    private static object CloneWithChange(object props, PropertyInfo property, object? newValue)
    {
        var clone = MemberwiseCloneMethod.Invoke(props, null)!;
        property.SetValue(clone, newValue);
        return clone;
    }
}
