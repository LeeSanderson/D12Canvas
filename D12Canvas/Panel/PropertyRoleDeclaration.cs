namespace D12Canvas.Panel;

// What a role expects of the property that declares it, plus the label the panel shows for a
// cross-type row merged on that role.
public sealed record PropertyRoleDeclaration(EditorKind Kind, Type ClrType, string Label);
