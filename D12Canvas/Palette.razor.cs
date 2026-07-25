using D12Canvas.Registration;
using Microsoft.AspNetCore.Components;

namespace D12Canvas;

public partial class Palette
{
    private const string UncategorizedLabel = "Uncategorized";

    [Inject]
    private IComponentRegistry Registry { get; set; } = null!;

    private IEnumerable<PaletteCategory> Categories =>
        Registry
            .All.GroupBy(registration => registration.Category ?? UncategorizedLabel)
            .Select(group => new PaletteCategory(group.Key, group.ToList()));

    private sealed record PaletteCategory(
        string Name,
        IReadOnlyList<ComponentRegistration> Entries
    );
}
