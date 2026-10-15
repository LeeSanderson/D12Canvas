using D12Canvas.Registration;
using Microsoft.AspNetCore.Components;

namespace D12Canvas;

public partial class Palette
{
    private const string UncategorizedLabel = "Uncategorized";

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

    private IEnumerable<PaletteCategory> Categories =>
        Registry
            .All.GroupBy(registration => registration.Category ?? UncategorizedLabel)
            .Select(group => new PaletteCategory(group.Key, group.ToList()));

    private sealed record PaletteCategory(
        string Name,
        IReadOnlyList<ComponentRegistration> Entries
    );
}
