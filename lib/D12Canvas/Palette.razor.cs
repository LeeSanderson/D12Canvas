using D12Canvas.Registration;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace D12Canvas;

/// <summary>
/// The component palette: a vertical toolbar of icon buttons, Edge first and then up to
/// <see cref="RailLimit"/> registered types grouped by category. Clicking a button adds that type
/// to <see cref="Canvas"/>, and dragging one places it where it is dropped.
/// </summary>
public partial class Palette
{
    internal const int RailLimit = 8;

    internal const string EdgeName = "Edge";

    private const string UncategorizedLabel = "Uncategorized";

    private readonly Dictionary<int, ElementReference> _buttonElements = new();

    private int _activeIndex;

    [Inject]
    private IComponentRegistry Registry { get; set; } = null!;

    // Palette is a sibling of DiagramCanvas in host markup (chrome isn't nested inside
    // the canvas), so it can't reach it via the cascading "ParentCanvas" value ComponentContainer
    // uses; wiring is this explicit reference instead, set by the host.
    [Parameter]
    public DiagramCanvas? Canvas { get; set; }

    private void HandleDragStart(string componentTypeKey) =>
        Canvas?.BeginPaletteDrag(componentTypeKey);

    private void HandleClick(string componentTypeKey) => Canvas?.ClickToAdd(componentTypeKey);

    private async Task HandleKeyDown(KeyboardEventArgs args)
    {
        var lastIndex = RailButtons().Count - 1;
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

    private List<RailButton> RailButtons()
    {
        var buttons = new List<RailButton>
        {
            new(
                DiagramCanvas.ConnectorPaletteKey,
                EdgeName,
                EdgeName,
                Glyphs.Edge,
                IsEdge: true,
                StartsGroup: true
            ),
        };

        string? previousCategory = null;
        foreach (var registration in RailRegistrations())
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

    private IEnumerable<ComponentRegistration> RailRegistrations() =>
        Registry.All.GroupBy(CategoryOf).SelectMany(category => category).Take(RailLimit);

    private static string CategoryOf(ComponentRegistration registration) =>
        registration.Category ?? UncategorizedLabel;

    private sealed record RailButton(
        string Key,
        string DisplayName,
        string AccessibleName,
        Glyph Glyph,
        bool IsEdge,
        bool StartsGroup
    );
}
