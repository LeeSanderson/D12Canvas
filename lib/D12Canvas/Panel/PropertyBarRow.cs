namespace D12Canvas.Panel;

public sealed record PropertyBarRow(
    string Id,
    PropertyRole Role,
    EditorKind Kind,
    IReadOnlyList<string>? Options,
    object? Value,
    bool IsMixed,
    bool CanHoldNull,
    Action<object?> Commit,
    IReadOnlyDictionary<string, Glyph>? OptionGlyphs = null
)
{
    public string Label => PropertyRoleDeclarations.For(Role).Label;

    public bool IsThemed => CanHoldNull && !IsMixed && Value is null;
}
