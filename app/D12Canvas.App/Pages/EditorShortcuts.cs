namespace D12Canvas.App.Pages;

public static class EditorShortcuts
{
    public static readonly (string Heading, (string Keys, string Action)[] Rows)[] Sections =
    [
        (
            "Pointer",
            [
                ("Drag on empty canvas", "Marquee select"),
                ("Double-click", "Enter a group, then edit"),
                ("Alt+drag", "Clone the selection"),
                ("Alt+resize", "Resize from the centre"),
                ("Shift+drag", "Move in a straight line"),
                ("Click a port", "Duplicate and connect"),
                ("Right- or middle-drag", "Pan"),
                ("Right-click", "Context menu"),
                ("Wheel", "Zoom, or pan on a trackpad"),
            ]
        ),
        (
            "Keyboard",
            [
                ("Tab", "Move between shapes"),
                ("Space", "Add the focused shape to the selection"),
                ("Enter / Escape", "Enter / leave a group"),
                ("F2", "Edit text"),
                ("Arrows", "Nudge"),
                ("Ctrl+Arrow", "Duplicate and connect"),
                ("Ctrl+Shift+Arrow", "Focus the nearest shape"),
                ("Shift+F10", "Context menu"),
                ("Ctrl+Enter", "Focus the property bar"),
            ]
        ),
        (
            "Commands",
            [
                ("Ctrl+Z / Ctrl+Shift+Z", "Undo / redo"),
                ("Ctrl+C / X / V", "Copy / cut / paste"),
                ("Ctrl+D", "Duplicate"),
                ("Ctrl+A", "Select all"),
                ("Delete", "Delete"),
                ("Ctrl+G / Ctrl+Shift+G", "Group / ungroup"),
                ("Ctrl+Shift+L", "Lock or unlock"),
                ("Ctrl+] / Ctrl+[", "Forward / backward"),
                ("Ctrl+Shift+] / [", "To front / to back"),
                ("Ctrl+'", "Snap to grid"),
                ("PageUp / PageDown", "Zoom in / out"),
                ("Shift+1 / 2 / 0", "Fit / selection / 100%"),
            ]
        ),
    ];
}
