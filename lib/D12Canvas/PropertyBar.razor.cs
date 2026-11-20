using System.Globalization;
using D12Canvas.Model;
using D12Canvas.Panel;
using D12Canvas.Registration;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace D12Canvas;

public partial class PropertyBar : IAsyncDisposable
{
    internal const string ModulePath = "./_content/D12Canvas/PropertyBar.razor.js";

    [Inject]
    private IJSRuntime JS { get; set; } = null!;

    [Inject]
    private IServiceProvider Services { get; set; } = null!;

    private IReadOnlyList<ColourPreset> ColourPresets =>
        (Services.GetService(typeof(ColourPresetCatalog)) as ColourPresetCatalog)?.Presets
        ?? ColourPresetCatalog.Defaults;

    [Parameter, EditorRequired]
    public IReadOnlyList<PropertyBarRow> Rows { get; set; } = [];

    [Parameter, EditorRequired]
    public PropertyBarAnchor Anchor { get; set; }

    [Parameter]
    public int LockedCount { get; set; }

    [Parameter]
    public int TargetCount { get; set; }

    private LockTally Locks => new(LockedCount, TargetCount);

    [Parameter]
    public EventCallback OnUnlock { get; set; }

    [Parameter]
    public double ContainerWidth { get; set; }

    [Parameter]
    public double ContainerHeight { get; set; }

    [Parameter]
    public TimeSpan FlightDuration { get; set; }

    private ElementReference _barRef;
    private IJSObjectReference? _module;
    private IJSObjectReference? _registration;
    private double? _width;
    private string? _measuredContent;
    private (double Left, double Top) _shown;
    private string? _shownLayout;

    private int _edits;

    private const string UnsetSwatchValue = "#010203";

    private const string UnlockOnlyLayout = "unlock";

    private string Layout =>
        Locks.IsFull
            ? UnlockOnlyLayout
            : string.Join("|", Rows.Select(row => $"{row.Id}:{row.Kind}"));

    private string MeasuredContent =>
        Locks.IsFull
            ? UnlockOnlyLayout
            : string.Join("|", Rows.Select(row => $"{row.Id}:{row.Kind}:{ChipValue(row)}"))
                + (Locks.IsPartial ? $"|locks:{Locks.Fraction}" : "");

    private bool _focusFirstControlAfterUnlock;

    private async Task Unlock(MouseEventArgs args)
    {
        _focusFirstControlAfterUnlock = args.Detail == 0;
        await OnUnlock.InvokeAsync();
    }

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

        if (_focusFirstControlAfterUnlock)
        {
            _focusFirstControlAfterUnlock = false;
            if (_module is not null && !Locks.IsFull)
            {
                await _module.InvokeVoidAsync("focusFirstControl", _barRef);
            }
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

    private void PickThemeColour(PropertyBarRow row)
    {
        _openRowId = null;
        UseThemeColour(row);
    }

    private string FlyoutPlacement(PropertyBarRow row)
    {
        var index = Rows.ToList().FindIndex(candidate => candidate.Id == row.Id);
        return index * 2 >= Rows.Count
            ? "d12-property-bar-flyout d12-property-bar-flyout-end"
            : "d12-property-bar-flyout";
    }

    private static bool IsCurrentColour(PropertyBarRow row, ColourPreset preset) =>
        !row.IsMixed
        && !row.IsThemed
        && string.Equals(TextValue(row), preset.Hex, StringComparison.OrdinalIgnoreCase);

    private static bool IsCurrent(PropertyBarRow row, string option) =>
        !row.IsMixed && TextValue(row) == option;

    private static Glyph? CurrentOptionGlyph(PropertyBarRow row) =>
        row.IsMixed ? null : row.OptionGlyphs?.GetValueOrDefault(TextValue(row));

    private static Glyph? RoleGlyph(PropertyBarRow row) =>
        PropertyRoleDeclarations.For(row.Role).Glyph;

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

    private static string SwatchTooltip(ColourPreset preset) =>
        $"{preset.Name} {preset.Hex.ToUpperInvariant()}";

    private static string ChipName(PropertyBarRow row) => $"{row.Label}: {ChipValue(row)}";

    private static string CustomColourName(PropertyBarRow row) => $"{row.Label}: Custom colour";

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
