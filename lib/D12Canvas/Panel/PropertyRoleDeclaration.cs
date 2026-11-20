namespace D12Canvas.Panel;

public sealed record PropertyRoleDeclaration(
    EditorKind Kind,
    Type ClrType,
    string Label,
    Glyph? Glyph = null
);
