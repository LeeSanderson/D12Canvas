using D12Canvas.Panel;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace D12Canvas.Tests;

// A second props type alongside TestProps: PropertyPanelTests needs one shape that carries a
// Text-type *content* field (Content, no attribute - excluded from the panel, like StickyNote's
// Text), plus one field per EditorKind the panel supports (ticket 56: Label/Count; ticket 57:
// Tint/Flag/Mode), all at once. Trailing defaults keep every pre-ticket-57 3-arg call site intact.
internal sealed record PanelTestProps(
    string Content,
    [property: PanelEditable(EditorKind.Text)] string Label,
    [property: PanelEditable(EditorKind.Number)] double Count,
    [property: PanelEditable(EditorKind.Color)] string Tint = "#000000",
    [property: PanelEditable(EditorKind.Checkbox)] bool Flag = false,
    [property: PanelEditable(EditorKind.Dropdown, Options = new[] { "a", "b", "c" })]
        string Mode = "a"
);

internal sealed class PanelTestPropsComponent : ComponentBase
{
    [Parameter]
    public PanelTestProps Props { get; set; } = new("", "", 0);

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "span");
        builder.AddAttribute(1, "class", "panel-test-props-component");
        builder.AddContent(2, Props.Content);
        builder.CloseElement();
    }
}
