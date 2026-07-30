namespace D12Canvas.Panel;

// ADR 0008: what a Custom-kind editor's RenderFragment receives - the property's current value,
// already unwrapped from Props via reflection (the same value every other EditorKind's control
// works off), plus a commit callback bound to that property. A Custom editor already produces a
// CLR-typed value itself, so Commit bypasses PropertyPanel's string-shaped ConvertValue path
// entirely (ticket 58).
public sealed record CustomEditorContext(object? Value, Action<object?> Commit);
