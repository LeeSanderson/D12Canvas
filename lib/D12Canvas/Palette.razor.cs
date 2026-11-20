using D12Canvas.Registration;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace D12Canvas;

public partial class Palette : IAsyncDisposable
{
    internal const int RailLimit = 8;

    internal const string EdgeName = "Edge";

    internal const string MoreName = "More components";

    internal const string ModulePath = "./_content/D12Canvas/Palette.razor.js";

    private const string UncategorizedLabel = "Uncategorized";

    internal static readonly Glyph MoreGlyph = new(
        new GlyphPart(
            "M2.25 8a1.25 1.25 0 1 0 2.5 0a1.25 1.25 0 1 0-2.5 0z"
                + " M6.75 8a1.25 1.25 0 1 0 2.5 0a1.25 1.25 0 1 0-2.5 0z"
                + " M11.25 8a1.25 1.25 0 1 0 2.5 0a1.25 1.25 0 1 0-2.5 0z",
            GlyphPaint.Fill
        )
    );

    private readonly Dictionary<int, ElementReference> _buttonElements = new();

    private ElementReference _railElement;

    private IJSObjectReference? _module;

    private int _activeIndex;

    private bool _flyoutOpen;

    [Inject]
    private IComponentRegistry Registry { get; set; } = null!;

    [Inject]
    private IJSRuntime JS { get; set; } = null!;

    [Parameter]
    public DiagramCanvas? Canvas { get; set; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _module = await JS.InvokeAsync<IJSObjectReference>("import", ModulePath);
            await _module.InvokeVoidAsync("registerRail", _railElement);
        }
    }

    private void HandleDragStart(string componentTypeKey) =>
        Canvas?.BeginPaletteDrag(componentTypeKey);

    private void HandleClick(string componentTypeKey) => Canvas?.ClickToAdd(componentTypeKey);

    private void HandleFlyoutPick(string componentTypeKey)
    {
        _flyoutOpen = false;
        HandleClick(componentTypeKey);
    }

    private void ToggleFlyout() => _flyoutOpen = !_flyoutOpen;

    private void CloseFlyout() => _flyoutOpen = false;

    private async Task HandleKeyDown(KeyboardEventArgs args)
    {
        var lastIndex = RailStopCount() - 1;
        int? target = args.Key switch
        {
            "ArrowDown" => _activeIndex >= lastIndex ? 0 : _activeIndex + 1,
            "ArrowUp" => _activeIndex <= 0 ? lastIndex : _activeIndex - 1,
            "Home" => 0,
            "End" => lastIndex,
            _ => null,
        };
        if (target is not { } index)
        {
            return;
        }

        _activeIndex = index;
        if (_buttonElements.TryGetValue(index, out var element))
        {
            await element.FocusAsync();
        }
    }

    private int RailStopCount() => RailButtons().Count + (OverflowCount() > 0 ? 1 : 0);

    private List<RailButton> RailButtons()
    {
        var buttons = new List<RailButton>
        {
            new(
                DiagramCanvas.EdgePaletteKey,
                EdgeName,
                EdgeName,
                Glyphs.Edge,
                IsEdge: true,
                StartsGroup: true
            ),
        };

        string? previousCategory = null;
        foreach (var registration in DisplayOrder().Take(RailLimit))
        {
            var category = CategoryOf(registration);
            buttons.Add(
                new(
                    registration.Key,
                    registration.DisplayName,
                    registration.AccessibleName,
                    registration.IconOrDefault,
                    IsEdge: false,
                    StartsGroup: category != previousCategory
                )
            );
            previousCategory = category;
        }

        return buttons;
    }

    private int OverflowCount() => Math.Max(0, Registry.All.Count - RailLimit);

    private IEnumerable<IGrouping<string, ComponentRegistration>> OverflowCategories() =>
        DisplayOrder().Skip(RailLimit).GroupBy(CategoryOf);

    private IEnumerable<ComponentRegistration> DisplayOrder() =>
        Registry.All.GroupBy(CategoryOf).SelectMany(category => category);

    private static string CategoryOf(ComponentRegistration registration) =>
        registration.Category ?? UncategorizedLabel;

    public async ValueTask DisposeAsync()
    {
        if (_module is not null)
        {
            await _module.DisposeAsync();
        }
    }

    private sealed record RailButton(
        string Key,
        string DisplayName,
        string AccessibleName,
        Glyph Glyph,
        bool IsEdge,
        bool StartsGroup
    );
}
