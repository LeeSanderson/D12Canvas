namespace D12Canvas;

// Which content set a menu shows: the object menu when the opening press hit an entity, the
// canvas menu when it hit bare canvas.
public enum ContextMenuSet
{
    Object,
    Canvas,
}

public enum ContextMenuCommand
{
    Cut,
    Copy,
    Paste,
    Duplicate,
    Delete,
    Group,
    Ungroup,
    BringToFront,
    BringForward,
    SendBackward,
    SendToBack,
    SelectAll,
    ToggleSnapToGrid,
    ToggleObjectSnapping,
}

// What DiagramCanvas resolved at the moment a menu opened: everything a row needs to decide
// whether it is eligible, how it reads and which hint it shows, so the menu itself reads nothing
// from the board. AsyncClipboard is whether the browser offers the clipboard to a click, which it
// does only in a secure context; without it the clipboard rows are left out and the keys still
// work.
public sealed record ContextMenuContext(
    ContextMenuSet Set,
    bool CanGroup = false,
    bool CanUngroup = false,
    bool CanArrange = false,
    bool CanSelectAll = false,
    bool SnapToGrid = false,
    bool SnapToGridChordLive = true,
    bool ObjectSnapping = false,
    bool ApplePlatform = false,
    bool CanCopy = false,
    bool CanCut = false,
    bool AsyncClipboard = false
);
