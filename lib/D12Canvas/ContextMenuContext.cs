namespace D12Canvas;

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
    AddPortHere,
    PlacePort,
    RemovePort,
    ZoomToSelection,
    ZoomToFit,
    ZoomTo100Percent,
}

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
    IReadOnlyDictionary<string, string>? KeyLabels = null,
    bool CanCopy = false,
    bool CanCut = false,
    bool AsyncClipboard = false,
    bool CanChangePicture = false,
    bool CanAlign = false,
    bool CanDistribute = false,
    bool CanDelete = true,
    bool SelectionLocked = false,
    bool CanUnlockAll = false,
    bool CanAddPortHere = false,
    bool CanPlacePort = false,
    bool CanRemovePort = false
);
