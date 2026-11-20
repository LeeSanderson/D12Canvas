using System.Reflection;
using Microsoft.AspNetCore.Components;

namespace D12Canvas.Panel;

public sealed record EditableProperty(
    PropertyInfo Property,
    EditorKind Kind,
    IReadOnlyList<string>? Options = null,
    RenderFragment<CustomEditorContext>? CustomEditor = null,
    PropertyRole? Role = null,
    IReadOnlyDictionary<string, Glyph>? OptionGlyphs = null
)
{
    public bool CanHoldNull { get; } =
        new NullabilityInfoContext().Create(Property).WriteState == NullabilityState.Nullable;

    internal bool HasGlyphForEveryOption =>
        Kind == EditorKind.Dropdown
        && Options is { Count: > 0 }
        && OptionGlyphs is not null
        && Options.All(OptionGlyphs.ContainsKey);
}
