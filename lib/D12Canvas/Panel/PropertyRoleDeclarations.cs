using D12Canvas.Model;

namespace D12Canvas.Panel;

// Validation runs a property against its own role's declaration rather than against whatever
// other type happens to be registered, so a wrong role fails on the registration that carries it.
public static class PropertyRoleDeclarations
{
    public static PropertyRoleDeclaration For(PropertyRole role) =>
        role switch
        {
            PropertyRole.Fill => new(EditorKind.Color, typeof(string), "Fill"),
            PropertyRole.Stroke => new(EditorKind.Color, typeof(string), "Stroke"),
            PropertyRole.StrokeWidth => new(EditorKind.Number, typeof(double), "Stroke width"),
            PropertyRole.TextColour => new(EditorKind.Color, typeof(string), "Text colour"),
            PropertyRole.FontSize => new(EditorKind.Number, typeof(double), "Font size"),
            PropertyRole.FontWeight => new(EditorKind.Dropdown, typeof(string), "Font weight"),
            PropertyRole.TextAlign => new(EditorKind.Dropdown, typeof(string), "Text align"),
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
            PropertyRole.EdgeColour => new(EditorKind.Color, typeof(string), "Colour"),
            _ => throw new ArgumentOutOfRangeException(nameof(role), role, null),
        };
}
