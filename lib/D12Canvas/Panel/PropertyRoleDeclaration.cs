namespace D12Canvas.Panel;

// What a role expects of the property that declares it, plus the label the panel shows for a
// cross-type row merged on that role. Glyph is the role's own glyph, which the bar draws beside a
// colour or number value; a choice role has none, since its chip shows the current option's glyph.
public sealed record PropertyRoleDeclaration(
    EditorKind Kind,
    Type ClrType,
    string Label,
    Glyph? Glyph = null
);
