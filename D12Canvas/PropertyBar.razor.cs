using System.Globalization;
using D12Canvas.Model;
using D12Canvas.Panel;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace D12Canvas;

// Canvas-rendered chrome: DiagramCanvas mounts it only while it should show and hands it the rows
// and the selection's top edge in container pixels. The one thing the canvas cannot know is the
// bar's own width, so the bar measures itself whenever its rows change and places itself from that.
public partial class PropertyBar : IAsyncDisposable
{
    internal const string ModulePath = "./_content/D12Canvas/PropertyBar.razor.js";

    [Inject]
    private IJSRuntime JS { get; set; } = null!;

    [Parameter, EditorRequired]
    public IReadOnlyList<PropertyBarRow> Rows { get; set; } = [];

    [Parameter, EditorRequired]
    public PropertyBarAnchor Anchor { get; set; }

    [Parameter]
    public double ContainerWidth { get; set; }

    [Parameter]
    public double ContainerHeight { get; set; }

    private ElementReference _barRef;
    private IJSObjectReference? _module;
    private IJSObjectReference? _registration;
    private double? _width;
    private string? _measuredLayout;

    // A row still mixed after an edit renders the same empty value as before, so Blazor would keep
    // whatever was typed into it. Keying a mixed row on the edit count rebuilds it after every edit.
    private int _edits;

    // The native picker opens on the input's value, and confirming that value fires no change, so
    // a swatch with no single colour holds one nobody is likely to pick rather than black.
    private const string UnsetSwatchValue = "#010203";

    private string Layout => string.Join("|", Rows.Select(row => $"{row.Id}:{row.Kind}"));

    private string BarStyle
    {
        get
        {
            var (left, top) = PropertyBarPlacement.Place(
                Anchor,
                _width ?? 0,
                ContainerWidth,
                ContainerHeight
            );
            var placed = FormattableString.Invariant($"left: {left}px; top: {top}px;");
            return _width is null ? placed + " visibility: hidden;" : placed;
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _module = await JS.InvokeAsync<IJSObjectReference>("import", ModulePath);
            _registration = await _module.InvokeAsync<IJSObjectReference>(
                "registerPropertyBar",
                _barRef
            );
        }

        if (_module is null || _measuredLayout == Layout)
        {
            return;
        }

        _measuredLayout = Layout;
        var width = await _module.InvokeAsync<double>("measurePropertyBar", _barRef);
        if (width != _width)
        {
            _width = width;
            StateHasChanged();
        }
    }

    private void Commit(PropertyBarRow row, ChangeEventArgs args)
    {
        _edits++;
        row.Commit(args.Value);
    }

    private void UseThemeColour(PropertyBarRow row)
    {
        _edits++;
        row.Commit("");
    }

    private object RowKey(PropertyBarRow row) => row.IsMixed ? $"{row.Id}#{_edits}" : row.Id;

    private static string CellClass(PropertyBarRow row) =>
        row.Kind == EditorKind.Number
            ? "d12-property-bar-cell d12-property-bar-cell-number"
            : "d12-property-bar-cell";

    private static string Title(PropertyBarRow row) =>
        row.CanHoldNull && !row.IsThemed
            ? $"{row.Label}: {ValueText(row)} (Delete to use the theme colour)"
            : $"{row.Label}: {ValueText(row)}";

    private static string ValueText(PropertyBarRow row) =>
        row.IsMixed ? MixedValue.Label
        : row.IsThemed ? "Theme colour"
        : TextValue(row);

    private static string TextValue(PropertyBarRow row) =>
        row.Value switch
        {
            null => "",
            string text => text,
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            var value => value.ToString() ?? "",
        };

    private static string ColorValue(PropertyBarRow row) =>
        row.IsMixed || row.IsThemed ? UnsetSwatchValue : TextValue(row);

    private static string? Ink(PropertyBarRow row) =>
        row.IsMixed || row.IsThemed ? null : InlineStyleValue.Safe(row.Value as string);

    private static string? SwatchClass(PropertyBarRow row, string paint) =>
        row.IsMixed ? $"d12-property-bar-swatch-mixed-{paint}"
        : row.IsThemed ? $"d12-property-bar-swatch-themed-{paint}"
        : null;

    private static string TextAlignPath(PropertyBarRow row) =>
        row.Value switch
        {
            "center" => "M2.5 4 H13.5 M4.5 8 H11.5 M2.5 12 H13.5",
            "right" => "M2.5 4 H13.5 M6.5 8 H13.5 M2.5 12 H13.5",
            _ => "M2.5 4 H13.5 M2.5 8 H9.5 M2.5 12 H13.5",
        };

    private static string RoutingPath(PropertyBarRow row) =>
        row.Value switch
        {
            EdgeRouting.Straight => "M2.5 12.5 L13.5 3.5",
            EdgeRouting.Curved => "M2.5 12.5 C8 12.5 8 3.5 13.5 3.5",
            _ => "M2.5 12.5 H8 V3.5 H13.5",
        };

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_registration is not null)
            {
                await _registration.InvokeVoidAsync("dispose");
                await _registration.DisposeAsync();
            }

            if (_module is not null)
            {
                await _module.DisposeAsync();
            }
        }
        catch (JSDisconnectedException) { }
    }
}
