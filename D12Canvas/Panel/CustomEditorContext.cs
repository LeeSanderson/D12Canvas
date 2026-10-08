namespace D12Canvas.Panel;

// What a Custom-kind editor's RenderFragment receives - the property's current value, already
// unwrapped from Props via reflection (the same value every other EditorKind's control works off),
// plus a commit callback bound to that property. A Custom editor already produces a CLR-typed
// value itself, so Commit bypasses PropertyPanel's string-shaped ConvertValue path entirely.
// AddAsset stores bytes on the canvas's board and returns the reference to commit, for an editor
// whose value is a file; it is null while the panel has no board to store them on. Disabled is
// true while every instance the field edits is locked, when the editor should offer no edit.
public sealed record CustomEditorContext(
    object? Value,
    Action<object?> Commit,
    Func<byte[], string, string>? AddAsset = null,
    bool Disabled = false
);
