namespace D12Canvas.Panel;

// The closed set of controls an editable property can render as. Custom is the escape hatch for
// anything the rest of the set can't express (e.g. Image's file picker).
public enum EditorKind
{
    Text,
    Number,
    Color,
    Checkbox,
    Dropdown,
    Custom,
}
