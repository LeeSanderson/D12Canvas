namespace D12Canvas.Panel;

// The closed, library-owned set of roles a props property can declare. A role is declared, never
// inferred: neither a property's name nor its EditorKind can tell a sticky note's background
// colour from a text instance's foreground colour. The role is both what admits a property to the
// property bar and what merges it with another type's property in a cross-type multi-selection.
// The four Edge* roles are what the library itself uses for an edge's settable style.
public enum PropertyRole
{
    Fill,
    Stroke,
    StrokeWidth,
    TextColour,
    FontSize,
    FontWeight,
    TextAlign,
    EdgeRouting,
    EdgeSourceArrow,
    EdgeTargetArrow,
    EdgeColour,
}
