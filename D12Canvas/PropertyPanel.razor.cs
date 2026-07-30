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

    private ComponentInstance? SelectedInstance => Canvas?.SinglySelectedComponent;

    private IReadOnlyList<EditableProperty> EditableProperties =>
        SelectedInstance is null
            ? Array.Empty<EditableProperty>()
            : Registry.Resolve(SelectedInstance.ComponentTypeKey).EditableProperties
                ?? Array.Empty<EditableProperty>();

    private static string FieldId(EditableProperty property) =>
        $"d12-property-panel-field-{property.Property.Name}";

    private string CurrentValue(EditableProperty property) =>
        SelectedInstance is null
            ? ""
            : FormatValue(property.Property.GetValue(SelectedInstance.Props));

    // Checkbox binds via the "checked" DOM property, not "value" - a separate accessor rather than
    // routing bool through FormatValue/CurrentValue's string-shaped path.
    private bool CurrentBoolValue(EditableProperty property) =>
        SelectedInstance is not null && property.Property.GetValue(SelectedInstance.Props) is true;

    private static string FormatValue(object? value) =>
        value switch
        {
            null => "",
            string text => text,
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString() ?? "",
        };

    // Each commit is exactly one MutateEntityCommand (ADR 0007), routed through
    // Canvas.CommitPropsChange - same discipline as a built-in's own inline text edit (ticket 43).
    // An edit that fails to parse or that leaves the value unchanged commits nothing.
    private void CommitEdit(EditableProperty property, ChangeEventArgs args)
    {
        var instance = SelectedInstance;
        if (instance is null)
        {
            return;
        }

        object? newValue;
        try
        {
            newValue = ConvertValue(args.Value, property.Property.PropertyType);
        }
        catch (Exception ex)
            when (ex is FormatException or InvalidCastException or OverflowException)
        {
            return;
        }

        var before = instance.Props;
        var currentValue = property.Property.GetValue(before);
        if (Equals(currentValue, newValue))
        {
            return;
        }

        var after = CloneWithChange(before, property.Property, newValue);
        Canvas?.CommitPropsChange(instance.Id, before, after);
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
