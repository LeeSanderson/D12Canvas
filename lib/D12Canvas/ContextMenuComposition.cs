namespace D12Canvas;

// Checked is null for a row that is not a toggle. A strip row is drawn as its icon alone in one
// strip with its neighbouring strip rows, its Label becoming the button's accessible name.
internal sealed record ContextMenuRow(
    ContextMenuCommand Command,
    string Label,
    Glyph Icon,
    string? Hint,
    bool? Checked,
    bool InStrip = false
);

// Both content sets come from one list of sections in one fixed order, and each row decides for
// itself whether it is eligible. A section with no eligible row is left out entirely, so the
// separators that sit between rendered sections never lead, trail or double up.
internal static class ContextMenuComposition
{
    private sealed record RowDefinition(
        ContextMenuCommand Command,
        Func<ContextMenuContext, bool> IsEligible,
        string Label,
        Glyph Icon,
        Func<ContextMenuContext, Chord?> Chord,
        Func<ContextMenuContext, bool?> Checked,
        bool InStrip = false
    );

    private static readonly Chord DeleteChord = new("Delete", AppleKey: "⌫");
    private static readonly Chord GroupChord = new("G", Primary: true, Code: "KeyG");
    private static readonly Chord UngroupChord = new("G", Primary: true, Shift: true, Code: "KeyG");
    private static readonly Chord BringToFrontChord = new(
        "]",
        Primary: true,
        Shift: true,
        Code: "BracketRight"
    );
    private static readonly Chord BringForwardChord = new("]", Primary: true, Code: "BracketRight");
    private static readonly Chord SendBackwardChord = new("[", Primary: true, Code: "BracketLeft");
    private static readonly Chord SendToBackChord = new(
        "[",
        Primary: true,
        Shift: true,
        Code: "BracketLeft"
    );
    private static readonly Chord SelectAllChord = new("A", Primary: true, Code: "KeyA");
    private static readonly Chord SnapToGridChord = new("'", Primary: true, Code: "Quote");

    private static readonly Chord CutChord = new("X", Primary: true);
    private static readonly Chord CopyChord = new("C", Primary: true);
    private static readonly Chord PasteChord = new("V", Primary: true);
    private static readonly Chord DuplicateChord = new("D", Primary: true, Code: "KeyD");
    private static readonly Chord LockChord = new("L", Primary: true, Shift: true, Code: "KeyL");
    private static readonly Chord ZoomToSelectionChord = new("2", Shift: true);
    private static readonly Chord ZoomToFitChord = new("1", Shift: true);
    private static readonly Chord ZoomTo100PercentChord = new("0", Shift: true);

    private static readonly IReadOnlyList<IReadOnlyList<RowDefinition>> Sections =
    [
        [
            Row(
                ContextMenuCommand.Cut,
                Glyphs.Cut,
                "Cut",
                c => OnObject(c) && c.AsyncClipboard && c.CanCut,
                CutChord
            ),
            Row(
                ContextMenuCommand.Copy,
                Glyphs.Copy,
                "Copy",
                c => OnObject(c) && c.AsyncClipboard && c.CanCopy,
                CopyChord
            ),
            Row(ContextMenuCommand.Paste, Glyphs.Paste, "Paste", c => c.AsyncClipboard, PasteChord),
            Row(
                ContextMenuCommand.Duplicate,
                Glyphs.Duplicate,
                "Duplicate",
                c => OnObject(c) && c.CanCopy,
                DuplicateChord
            ),
        ],
        [
            Row(
                ContextMenuCommand.Delete,
                Glyphs.Delete,
                "Delete",
                c => OnObject(c) && c.CanDelete,
                DeleteChord
            ),
        ],
        [
            Row(
                ContextMenuCommand.SelectAll,
                Glyphs.SelectAll,
                "Select All",
                c => OnCanvas(c) && c.CanSelectAll,
                SelectAllChord
            ),
        ],
        [
            Row(
                ContextMenuCommand.Group,
                Glyphs.Group,
                "Group",
                c => OnObject(c) && c.CanGroup,
                GroupChord
            ),
            Row(
                ContextMenuCommand.Ungroup,
                Glyphs.Ungroup,
                "Ungroup",
                c => OnObject(c) && c.CanUngroup,
                UngroupChord
            ),
        ],
        [
            Strip(ContextMenuCommand.AlignLeft, Glyphs.AlignLeft, "Align left", CanAlign),
            Strip(ContextMenuCommand.AlignCentre, Glyphs.AlignCentre, "Align centre", CanAlign),
            Strip(ContextMenuCommand.AlignRight, Glyphs.AlignRight, "Align right", CanAlign),
            Strip(ContextMenuCommand.AlignTop, Glyphs.AlignTop, "Align top", CanAlign),
            Strip(ContextMenuCommand.AlignMiddle, Glyphs.AlignMiddle, "Align middle", CanAlign),
            Strip(ContextMenuCommand.AlignBottom, Glyphs.AlignBottom, "Align bottom", CanAlign),
            Strip(
                ContextMenuCommand.DistributeHorizontally,
                Glyphs.DistributeHorizontally,
                "Distribute horizontally",
                CanDistribute
            ),
            Strip(
                ContextMenuCommand.DistributeVertically,
                Glyphs.DistributeVertically,
                "Distribute vertically",
                CanDistribute
            ),
            Row(
                ContextMenuCommand.BringToFront,
                Glyphs.BringToFront,
                "Bring to Front",
                CanArrange,
                BringToFrontChord
            ),
            Row(
                ContextMenuCommand.BringForward,
                Glyphs.BringForward,
                "Bring Forward",
                CanArrange,
                BringForwardChord
            ),
            Row(
                ContextMenuCommand.SendBackward,
                Glyphs.SendBackward,
                "Send Backward",
                CanArrange,
                SendBackwardChord
            ),
            Row(
                ContextMenuCommand.SendToBack,
                Glyphs.SendToBack,
                "Send to Back",
                CanArrange,
                SendToBackChord
            ),
        ],
        [
            Row(
                ContextMenuCommand.Lock,
                Glyphs.Lock,
                "Lock",
                c => OnObject(c) && !c.SelectionLocked,
                LockChord
            ),
            Row(
                ContextMenuCommand.Unlock,
                Glyphs.Unlock,
                "Unlock",
                c => OnObject(c) && c.SelectionLocked,
                LockChord
            ),
        ],
        [
            Row(
                ContextMenuCommand.ZoomToSelection,
                Glyphs.ZoomToSelection,
                "Zoom to Selection",
                OnObject,
                ZoomToSelectionChord
            ),
            Row(
                ContextMenuCommand.ZoomToFit,
                Glyphs.ZoomToFit,
                "Zoom to Fit",
                OnCanvas,
                ZoomToFitChord
            ),
            Row(
                ContextMenuCommand.ZoomTo100Percent,
                Glyphs.ZoomTo100Percent,
                "Zoom to 100%",
                OnCanvas,
                ZoomTo100PercentChord
            ),
        ],
        [
            new RowDefinition(
                ContextMenuCommand.ToggleSnapToGrid,
                OnCanvas,
                "Snap to Grid",
                Glyphs.SnapToGrid,
                c => c.SnapToGridChordLive ? SnapToGridChord : null,
                c => c.SnapToGrid
            ),
            new RowDefinition(
                ContextMenuCommand.ToggleObjectSnapping,
                OnCanvas,
                "Object Snapping",
                Glyphs.ObjectSnapping,
                _ => null,
                c => c.ObjectSnapping
            ),
        ],
        [
            Unhinted(
                ContextMenuCommand.UnlockAll,
                Glyphs.Unlock,
                "Unlock All",
                c => OnCanvas(c) && c.CanUnlockAll
            ),
        ],
        [
            Unhinted(
                ContextMenuCommand.AddPortHere,
                Glyphs.AddPort,
                "Add port here",
                c => OnObject(c) && c.CanAddPortHere
            ),
            Unhinted(
                ContextMenuCommand.PlacePort,
                Glyphs.AddPort,
                "Add port…",
                c => OnObject(c) && c.CanPlacePort
            ),
            Unhinted(
                ContextMenuCommand.RemovePort,
                Glyphs.RemovePort,
                "Remove port",
                c => OnObject(c) && c.CanRemovePort
            ),
        ],
        [
            Unhinted(
                ContextMenuCommand.ChooseImage,
                Glyphs.ChooseImage,
                "Choose image…",
                CanChangePicture
            ),
            Unhinted(
                ContextMenuCommand.RemoveImage,
                Glyphs.RemoveImage,
                "Remove image",
                CanChangePicture
            ),
        ],
    ];

    public static IReadOnlyList<IReadOnlyList<ContextMenuRow>> Compose(
        ContextMenuContext context
    ) =>
        Sections
            .Select(section =>
                (IReadOnlyList<ContextMenuRow>)
                    section
                        .Where(row => row.IsEligible(context))
                        .Select(row => Render(row, context))
                        .ToList()
            )
            .Where(section => section.Count > 0)
            .ToList();

    private static ContextMenuRow Render(RowDefinition row, ContextMenuContext context) =>
        new(
            row.Command,
            row.Label,
            row.Icon,
            row.Chord(context) is { } chord
                ? ShortcutHint.Render(chord, context.ApplePlatform, context.KeyLabels)
                : null,
            row.Checked(context),
            row.InStrip
        );

    private static RowDefinition Row(
        ContextMenuCommand command,
        Glyph icon,
        string label,
        Func<ContextMenuContext, bool> isEligible,
        Chord chord
    ) => new(command, isEligible, label, icon, _ => chord, _ => null);

    private static RowDefinition Unhinted(
        ContextMenuCommand command,
        Glyph icon,
        string label,
        Func<ContextMenuContext, bool> isEligible
    ) => new(command, isEligible, label, icon, _ => null, _ => null);

    private static RowDefinition Strip(
        ContextMenuCommand command,
        Glyph icon,
        string label,
        Func<ContextMenuContext, bool> isEligible
    ) => new(command, isEligible, label, icon, _ => null, _ => null, InStrip: true);

    private static bool CanAlign(ContextMenuContext context) =>
        OnObject(context) && context.CanAlign;

    private static bool CanDistribute(ContextMenuContext context) =>
        OnObject(context) && context.CanDistribute;

    private static bool CanChangePicture(ContextMenuContext context) =>
        OnObject(context) && context.CanChangePicture;

    private static bool OnObject(ContextMenuContext context) =>
        context.Set == ContextMenuSet.Object;

    private static bool OnCanvas(ContextMenuContext context) =>
        context.Set == ContextMenuSet.Canvas;

    private static bool CanArrange(ContextMenuContext context) =>
        OnObject(context) && context.CanArrange;
}
