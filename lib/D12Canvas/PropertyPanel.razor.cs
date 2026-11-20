using System.Globalization;
using System.Reflection;
using D12Canvas.Model;
using D12Canvas.Panel;
using D12Canvas.Registration;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace D12Canvas;

public partial class PropertyPanel : IAsyncDisposable
{
    [Inject]
    private IComponentRegistry Registry { get; set; } = null!;

    [Inject]
    private IJSRuntime JS { get; set; } = null!;

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

    private void HandleSelectionChanged(object? sender, EventArgs e)
    {
        _focusedTiles.Clear();
        StateHasChanged();
    }

    public async ValueTask DisposeAsync()
    {
        if (_subscribedCanvas is not null)
        {
            _subscribedCanvas.SelectionChanged -= HandleSelectionChanged;
        }

        if (_module is not null)
        {
            try
            {
                await _module.DisposeAsync();
            }
            catch (JSDisconnectedException) { }
        }
    }

    private IReadOnlyList<ComponentInstance> SelectedInstances =>
        Canvas?.SelectedComponents ?? Array.Empty<ComponentInstance>();

    private LockTally Locks => LockTally.Of(SelectedInstances.Select(instance => instance.Locked));

    private bool AllLocked => Locks.IsFull;

    private void Unlock() => Canvas?.OnUnlockPressed();

    private sealed record PanelField(
        string FieldId,
        string Label,
        EditorKind Kind,
        IReadOnlyList<string>? Options,
        RenderFragment<CustomEditorContext>? CustomEditor,
        IReadOnlyList<(ComponentInstance Instance, PropertyInfo Property)> Targets,
        bool CanHoldNull,
        IReadOnlyDictionary<string, Glyph>? OptionGlyphs
    )
    {
        public bool DrawsTiles => OptionGlyphs is not null;

        public string LabelId => $"{FieldId}-label";

        public int LongestOptionNameLength => Options!.Max(option => OptionName.For(option).Length);

        public bool IsMixed { get; } =
            MixedValue.IsMixed(
                Kind,
                Targets.Select(target => target.Property.GetValue(target.Instance.Props)).ToList()
            );

        public object? Value =>
            IsMixed || Targets.Count == 0
                ? null
                : Targets[0].Property.GetValue(Targets[0].Instance.Props);
    }

    private static bool IsThemed(PanelField field) =>
        field.CanHoldNull && !field.IsMixed && field.Value is null;

    private static string ColorInputClass(PanelField field) =>
        "d12-property-panel-input d12-property-panel-color"
        + (
            IsThemed(field) ? " d12-property-panel-color-themed"
            : field.IsMixed ? " d12-property-panel-color-mixed"
            : ""
        );

    private const string MixedSwatchValue = "#010203";

    private static string ColorValue(PanelField field) =>
        field.IsMixed ? MixedSwatchValue : CurrentValue(field);

    private static string? MixedPlaceholder(PanelField field) =>
        field.IsMixed ? MixedValue.Label : null;

    private int _edits;

    private object RowKey(PanelField field) =>
        field.IsMixed && !field.DrawsTiles ? $"{field.FieldId}#{_edits}" : field.FieldId;

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
                Targets: instances.Select(instance => (instance, property.Property)).ToList(),
                CanHoldNull: property.CanHoldNull,
                OptionGlyphs: TileGlyphs(property)
            ))
            .ToList();

    private IReadOnlyList<PanelField> CrossTypeFields(
        IReadOnlyList<string> typeKeys,
        IReadOnlyList<ComponentInstance> instances
    )
    {
        var schemasByType = typeKeys.ToDictionary(
            key => key,
            key => Registry.Resolve(key).EditableProperties ?? Array.Empty<EditableProperty>()
        );
        var otherSchemas = typeKeys.Skip(1).Select(key => schemasByType[key]).ToList();

        return schemasByType[typeKeys[0]]
            .Where(property => property.Role is not null)
            .Select(property => (Representative: property, Role: property.Role!.Value))
            .Where(entry =>
                otherSchemas.All(schema => schema.Any(other => other.Role == entry.Role))
            )
            .Select(entry =>
            {
                var (representative, role) = entry;
                var propertiesByType = schemasByType.ToDictionary(
                    schema => schema.Key,
                    schema => schema.Value.First(property => property.Role == role)
                );
                var targets = instances
                    .Select(instance =>
                        (instance, propertiesByType[instance.ComponentTypeKey].Property)
                    )
                    .ToList();

                return new PanelField(
                    FieldId: $"d12-property-panel-field-{role}",
                    Label: PropertyRoleDeclarations.For(role).Label,
                    Kind: representative.Kind,
                    Options: representative.Options,
                    CustomEditor: representative.CustomEditor,
                    Targets: targets,
                    CanHoldNull: propertiesByType.Values.All(property => property.CanHoldNull),
                    OptionGlyphs: TileGlyphs(representative)
                );
            })
            .ToList();
    }

    private static IReadOnlyDictionary<string, Glyph>? TileGlyphs(EditableProperty property) =>
        property.HasGlyphForEveryOption ? property.OptionGlyphs : null;

    private static string CurrentValue(PanelField field) => FormatValue(field.Value);

    private static bool CurrentBoolValue(PanelField field) => field.Value is true;

    private CustomEditorContext CustomContext(PanelField field) =>
        new(
            field.Value,
            newValue => Commit(field, newValue),
            Canvas?.Board is { } board ? board.AddAsset : null,
            AllLocked,
            field.IsMixed
        );

    internal const string ModulePath = "./_content/D12Canvas/PropertyPanel.razor.js";

    private readonly Dictionary<string, ElementReference> _checkboxes = new();
    private HashSet<string> _indeterminateCheckboxes = new();
    private IJSObjectReference? _module;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _module ??= await JS.InvokeAsync<IJSObjectReference>("import", ModulePath);
            await _module.InvokeVoidAsync("registerTileKeys", _panelElement);
        }

        var checkboxes = Fields.Where(field => field.Kind == EditorKind.Checkbox).ToList();
        var toWrite = checkboxes
            .Where(field => field.IsMixed || _indeterminateCheckboxes.Contains(field.FieldId))
            .Where(field => _checkboxes.ContainsKey(field.FieldId))
            .ToList();
        _indeterminateCheckboxes = checkboxes
            .Where(field => field.IsMixed)
            .Select(field => field.FieldId)
            .ToHashSet();

        if (toWrite.Count == 0)
        {
            return;
        }

        _module ??= await JS.InvokeAsync<IJSObjectReference>("import", ModulePath);
        foreach (var field in toWrite)
        {
            await _module.InvokeVoidAsync(
                "setIndeterminate",
                _checkboxes[field.FieldId],
                field.IsMixed
            );
        }
    }

    private ElementReference _panelElement;

    private readonly Dictionary<string, int> _focusedTiles = new();
    private readonly Dictionary<(string FieldId, int Index), ElementReference> _tileElements =
        new();

    private static int CheckedTileIndex(PanelField field) =>
        field.IsMixed ? -1 : field.Options!.ToList().IndexOf(CurrentValue(field));

    private int TabStopTileIndex(PanelField field) =>
        _focusedTiles.TryGetValue(field.FieldId, out var focused) ? focused
        : CheckedTileIndex(field) is var checkedIndex and >= 0 ? checkedIndex
        : 0;

    private void TileFocused(PanelField field, int index) => _focusedTiles[field.FieldId] = index;

    private void TilesLostFocus(PanelField field) => _focusedTiles.Remove(field.FieldId);

    private async Task HandleTileKeyDown(PanelField field, int index, KeyboardEventArgs args)
    {
        var lastIndex = field.Options!.Count - 1;
        int? target = args.Key switch
        {
            "ArrowRight" or "ArrowDown" => index >= lastIndex ? 0 : index + 1,
            "ArrowLeft" or "ArrowUp" => index <= 0 ? lastIndex : index - 1,
            "Home" => 0,
            "End" => lastIndex,
            _ => null,
        };

        if (args.Key is "Enter" or " ")
        {
            Commit(field, field.Options[index]);
            return;
        }

        if (target is not { } next)
        {
            return;
        }

        _focusedTiles[field.FieldId] = next;
        if (_tileElements.TryGetValue((field.FieldId, next), out var element))
        {
            await element.FocusAsync();
        }
    }

    private static string FormatValue(object? value) =>
        value switch
        {
            null => "",
            string text => text,
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString() ?? "",
        };

    private (string FieldId, object? Value)? _heldPick;

    private void HoldPick(PanelField field, ChangeEventArgs args) =>
        _heldPick = (field.FieldId, args.Value);

    private void CommitPick(PanelField field, ChangeEventArgs args)
    {
        _heldPick = null;
        CommitEdit(field, args);
    }

    private void CommitHeldPick(PanelField field)
    {
        if (_heldPick is { } held && held.FieldId == field.FieldId)
        {
            _heldPick = null;
            CommitEdit(field, new ChangeEventArgs { Value = held.Value });
        }
    }

    private void CommitEdit(PanelField field, ChangeEventArgs args)
    {
        _edits++;
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

    private void Commit(PanelField field, object? newValue)
    {
        _edits++;
        var changes = TargetedEdit.ChangesFor(
            field.Targets,
            field.Kind,
            TargetedEdit.Normalise(field.Kind, field.CanHoldNull, newValue)
        );
        if (changes.Count > 0)
        {
            Canvas?.CommitPropsChangeBatch(changes);
        }
    }

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
}
