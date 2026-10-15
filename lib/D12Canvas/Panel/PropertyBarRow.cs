namespace D12Canvas.Panel;

// One cell of the property bar. The bar renders every row the same way and never learns whether a
// row edits instances or edges: the producer that built the row closes over its targets in Commit,
// which takes the raw value a control reports and writes it to every target in one history entry.
// Value is null on a mixed row, so no one target's value is shown as though it were the
// selection's.
public sealed record PropertyBarRow(
    string Id,
    PropertyRole Role,
    EditorKind Kind,
    IReadOnlyList<string>? Options,
    object? Value,
    bool IsMixed,
    bool CanHoldNull,
    Action<object?> Commit
)
{
    public string Label => PropertyRoleDeclarations.For(Role).Label;

    public bool IsThemed => CanHoldNull && !IsMixed && Value is null;
}
