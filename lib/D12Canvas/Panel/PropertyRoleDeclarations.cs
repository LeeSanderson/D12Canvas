using D12Canvas.Model;

namespace D12Canvas.Panel;

public static class PropertyRoleDeclarations
{
    public static PropertyRoleDeclaration For(PropertyRole role) =>
        role switch
        {
            PropertyRole.Fill => new(EditorKind.Color, typeof(string), "Fill", Glyphs.Fill),
            PropertyRole.Stroke => new(EditorKind.Color, typeof(string), "Stroke", Glyphs.Stroke),
            PropertyRole.StrokeWidth => new(
                EditorKind.Number,
                typeof(double),
                "Stroke width",
                Glyphs.StrokeWidth
            ),
            PropertyRole.TextColour => new(
                EditorKind.Color,
                typeof(string),
                "Text colour",
                Glyphs.TextColour
            ),
            PropertyRole.FontSize => new(
                EditorKind.Number,
                typeof(double),
                "Font size",
                Glyphs.FontSize
            ),
            PropertyRole.FontWeight => new(EditorKind.Dropdown, typeof(string), "Font weight"),
            PropertyRole.TextAlign => new(EditorKind.Dropdown, typeof(string), "Text align"),
            PropertyRole.VerticalAlign => new(
                EditorKind.Dropdown,
                typeof(string),
                "Vertical align"
            ),
            PropertyRole.EdgeRouting => new(EditorKind.Dropdown, typeof(EdgeRouting), "Routing"),
            PropertyRole.EdgeSourceArrow => new(
                EditorKind.Dropdown,
                typeof(ArrowStyle),
                "Source arrow"
            ),
            PropertyRole.EdgeTargetArrow => new(
                EditorKind.Dropdown,
                typeof(ArrowStyle),
                "Target arrow"
            ),
            PropertyRole.EdgeColour => new(
                EditorKind.Color,
                typeof(string),
                "Colour",
                Glyphs.EdgeColour
            ),
            _ => throw new ArgumentOutOfRangeException(nameof(role), role, null),
        };
}
