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

    // The canvas's own transform transition while a framing flight runs, so the bar travels with
    // the content instead of arriving ahead of it.
    [Parameter]
    public TimeSpan FlightDuration { get; set; }

    private ElementReference _barRef;
    private IJSObjectReference? _module;
    private IJSObjectReference? _registration;
    private double? _width;
    private string? _measuredContent;
    private (double Left, double Top) _shown;
    private string? _shownLayout;

    // A row still mixed after an edit renders the same empty value as before, so Blazor would keep
    // whatever was typed into it. Keying a mixed row on the edit count rebuilds it after every edit.
    // A choice chip holds nothing typed and keeps its plain id, so a pick from a mixed row leaves
    // the chip in place for the closing flyout to hand focus back to.
    private int _edits;

    // The native picker opens on the input's value, and confirming that value fires no change, so
    // a swatch with no single colour holds one nobody is likely to pick rather than black.
    private const string UnsetSwatchValue = "#010203";

    private string Layout => string.Join("|", Rows.Select(row => $"{row.Id}:{row.Kind}"));

    // A chip's width follows the value it shows, so the bar measures itself again whenever a shown
    // value changes, not only when its rows do.
    private string MeasuredContent =>
        string.Join("|", Rows.Select(row => $"{row.Id}:{row.Kind}:{ChipValue(row)}"));

    private const string MixedNumberPlaceholder = "–";

    private readonly Dictionary<string, ElementReference> _chipElements = new();
    private string? _openRowId;

    private string BarStyle
    {
        get
        {
            var placed = FormattableString.Invariant(
                $"left: {_shown.Left}px; top: {_shown.Top}px;"
            );
            if (_width is null)
            {
                return placed + " visibility: hidden;";
            }

            return FlightDuration > TimeSpan.Zero
                ? placed
                    + FormattableString.Invariant(
                        $" transition: left {FlightDuration.TotalMilliseconds}ms ease-out, top {FlightDuration.TotalMilliseconds}ms ease-out;"
                    )
                : placed;
        }
    }

    protected override void OnParametersSet()
    {
        if (_openRowId is not null && Rows.All(row => row.Id != _openRowId))
        {
            _openRowId = null;
        }

        foreach (
            var goneId in _chipElements.Keys.Where(id => Rows.All(row => row.Id != id)).ToList()
        )
        {
            _chipElements.Remove(goneId);
        }

        Reposition();
    }

    // Once measured, a bar keeps its place through a move smaller than the minimum, as long as its
    // rows are the same ones it was showing.
    private void Reposition()
    {
        var target = PropertyBarPlacement.Place(
            Anchor,
            _width ?? 0,
            ContainerWidth,
            ContainerHeight
        );
        var layout = _width is null ? null : Layout;
        _shown =
            layout is not null && layout == _shownLayout
                ? PropertyBarPlacement.Settle(
                    _shown,
                    target,
                    _width!.Value,
                    ContainerWidth,
                    ContainerHeight
                )
                : target;
        _shownLayout = layout;
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

        if (_module is null || _measuredContent == MeasuredContent)
        {
            return;
        }

        _measuredContent = MeasuredContent;
        var width = await _module.InvokeAsync<double>("measurePropertyBar", _barRef);
        if (width != _width)
        {
            _width = width;
            _shownLayout = null;
            Reposition();
            StateHasChanged();
        }
    }

    // The picker opens on the swatch's own value and the browser fires no change when a pick ends
    // on that value, so the last colour the picker reported is held and lands when the swatch is
    // left. A change commits it at once and drops the hold.
    private (string RowId, object? Value)? _heldPick;

    private void HoldPick(PropertyBarRow row, ChangeEventArgs args) =>
        _heldPick = (row.Id, args.Value);

    private void CommitPick(PropertyBarRow row, ChangeEventArgs args)
    {
        _heldPick = null;
        Commit(row, args);
    }

    private void CommitHeldPick(PropertyBarRow row)
    {
        if (_heldPick is { } held && held.RowId == row.Id)
        {
            _heldPick = null;
            Commit(row, new ChangeEventArgs { Value = held.Value });
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

    private bool IsOpen(PropertyBarRow row) => _openRowId == row.Id;

    private void ToggleFlyout(PropertyBarRow row) => _openRowId = IsOpen(row) ? null : row.Id;

    private void CloseFlyout() => _openRowId = null;

    private void PickOption(PropertyBarRow row, string option)
    {
        _openRowId = null;
        _edits++;
        row.Commit(option);
    }

    private static bool IsCurrent(PropertyBarRow row, string option) =>
        !row.IsMixed && TextValue(row) == option;

    private static Glyph? CurrentOptionGlyph(PropertyBarRow row) =>
        row.IsMixed ? null : row.OptionGlyphs?.GetValueOrDefault(TextValue(row));

    private static Glyph RoleGlyph(PropertyRole role) =>
        role switch
        {
            PropertyRole.Fill => Glyphs.Fill,
            PropertyRole.Stroke => Glyphs.Stroke,
            PropertyRole.TextColour => Glyphs.TextColour,
            PropertyRole.EdgeColour => Glyphs.EdgeColour,
            PropertyRole.StrokeWidth => Glyphs.StrokeWidth,
            PropertyRole.FontSize => Glyphs.FontSize,
            _ => throw new ArgumentOutOfRangeException(nameof(role), role, null),
        };

    private static string ColourCellClass(PropertyBarRow row) =>
        "d12-property-bar-cell d12-property-bar-colour"
        + (
            row.IsMixed ? " d12-property-bar-colour-mixed"
            : row.IsThemed ? " d12-property-bar-colour-themed"
            : ""
        );

    private static string ChoiceChipClass(PropertyBarRow row) =>
        "d12-property-bar-control d12-property-bar-chip"
        + (row.IsMixed ? " d12-property-bar-chip-mixed" : "");

    private static string? AccentStyle(PropertyBarRow row) =>
        row.IsMixed || row.IsThemed || InlineStyleValue.Safe(row.Value as string) is not { } ink
            ? null
            : $"--d12-icon-accent: {ink}";

    private static string ChipValue(PropertyBarRow row) =>
        row.IsMixed ? MixedValue.Label
        : row.Kind == EditorKind.Color
            ? row.IsThemed ? "Theme"
                : TextValue(row).ToUpperInvariant()
        : row.Kind == EditorKind.Dropdown ? OptionName.For(TextValue(row))
        : TextValue(row);

    private static string ChipName(PropertyBarRow row) => $"{row.Label}: {ChipValue(row)}";

    private static string NumberName(PropertyBarRow row) =>
        row.IsMixed ? $"{row.Label}: {MixedValue.Label}" : row.Label;

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
