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
    AlignLeft,
    AlignCentre,
    AlignRight,
    AlignTop,
    AlignMiddle,
    AlignBottom,
    DistributeHorizontally,
    DistributeVertically,
    BringToFront,
    BringForward,
    SendBackward,
    SendToBack,
    SelectAll,
    ToggleSnapToGrid,
    ToggleObjectSnapping,
    ChooseImage,
    RemoveImage,
    Lock,
    Unlock,
    UnlockAll,
}

// What DiagramCanvas resolved at the moment a menu opened: everything a row needs to decide
// whether it is eligible, how it reads and which hint it shows, so the menu itself reads nothing
// from the board. AsyncClipboard is whether the browser offers the clipboard to a click, which it
// does only in a secure context; without it the clipboard rows are left out and the keys still
// work. CanChangePicture is whether every selected entity is an image. CanAlign and
// CanDistribute count the selection's instances and groups, never its edges. CanDelete and CanCut
// are whether a delete would remove anything, which nothing locked is. SelectionLocked is whether
// every top-level selected entity is locked, which makes the lock row read Unlock. CanUnlockAll is
// whether anything on the board is locked.
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
    bool AsyncClipboard = false,
    bool CanChangePicture = false,
    bool CanAlign = false,
    bool CanDistribute = false,
    bool CanDelete = true,
    bool SelectionLocked = false,
    bool CanUnlockAll = false
);
