namespace D12Canvas.Panel;

// ADR 0008: the closed set of controls an editable property can render as. Custom is the escape
// hatch for anything the rest of the set can't express (e.g. Image's file picker, ticket 58).
public enum EditorKind
{
    Text,
    Number,
    Color,
    Checkbox,
    Dropdown,
    Custom,
}
