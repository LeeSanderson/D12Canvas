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
        string Label,
        Glyph Icon,
        Func<ContextMenuContext, bool> IsEligible,
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
                "Cut",
                Glyphs.Cut,
                c => OnObject(c) && c.AsyncClipboard && c.CanCut,
                CutChord
            ),
            Row(
                ContextMenuCommand.Copy,
                "Copy",
                Glyphs.Copy,
                c => OnObject(c) && c.AsyncClipboard && c.CanCopy,
                CopyChord
            ),
            Row(ContextMenuCommand.Paste, "Paste", Glyphs.Paste, c => c.AsyncClipboard, PasteChord),
            Row(
                ContextMenuCommand.Duplicate,
                "Duplicate",
                Glyphs.Duplicate,
                c => OnObject(c) && c.CanCopy,
                DuplicateChord
            ),
        ],
        [
            Row(
                ContextMenuCommand.Delete,
                "Delete",
                Glyphs.Delete,
                c => OnObject(c) && c.CanDelete,
                DeleteChord
            ),
        ],
        [
            Row(
                ContextMenuCommand.SelectAll,
                "Select All",
                Glyphs.SelectAll,
                c => OnCanvas(c) && c.CanSelectAll,
                SelectAllChord
            ),
        ],
        [
            Row(
                ContextMenuCommand.Group,
                "Group",
                Glyphs.Group,
                c => OnObject(c) && c.CanGroup,
                GroupChord
            ),
            Row(
                ContextMenuCommand.Ungroup,
                "Ungroup",
                Glyphs.Ungroup,
                c => OnObject(c) && c.CanUngroup,
                UngroupChord
            ),
        ],
        [
            Strip(ContextMenuCommand.AlignLeft, "Align left", Glyphs.AlignLeft, CanAlign),
            Strip(ContextMenuCommand.AlignCentre, "Align centre", Glyphs.AlignCentre, CanAlign),
            Strip(ContextMenuCommand.AlignRight, "Align right", Glyphs.AlignRight, CanAlign),
            Strip(ContextMenuCommand.AlignTop, "Align top", Glyphs.AlignTop, CanAlign),
            Strip(ContextMenuCommand.AlignMiddle, "Align middle", Glyphs.AlignMiddle, CanAlign),
            Strip(ContextMenuCommand.AlignBottom, "Align bottom", Glyphs.AlignBottom, CanAlign),
            Strip(
                ContextMenuCommand.DistributeHorizontally,
                "Distribute horizontally",
                Glyphs.DistributeHorizontally,
                CanDistribute
            ),
            Strip(
                ContextMenuCommand.DistributeVertically,
                "Distribute vertically",
                Glyphs.DistributeVertically,
                CanDistribute
            ),
            Row(
                ContextMenuCommand.BringToFront,
                "Bring to Front",
                Glyphs.BringToFront,
                CanArrange,
                BringToFrontChord
            ),
            Row(
                ContextMenuCommand.BringForward,
                "Bring Forward",
                Glyphs.BringForward,
                CanArrange,
                BringForwardChord
            ),
            Row(
                ContextMenuCommand.SendBackward,
                "Send Backward",
                Glyphs.SendBackward,
                CanArrange,
                SendBackwardChord
            ),
            Row(
                ContextMenuCommand.SendToBack,
                "Send to Back",
                Glyphs.SendToBack,
                CanArrange,
                SendToBackChord
            ),
        ],
        [
            Row(
                ContextMenuCommand.Lock,
                "Lock",
                Glyphs.Lock,
                c => OnObject(c) && !c.SelectionLocked,
                LockChord
            ),
            Row(
                ContextMenuCommand.Unlock,
                "Unlock",
                Glyphs.Unlock,
                c => OnObject(c) && c.SelectionLocked,
                LockChord
            ),
        ],
        [
            Row(
                ContextMenuCommand.ZoomToSelection,
                "Zoom to Selection",
                Glyphs.ZoomToSelection,
                OnObject,
                ZoomToSelectionChord
            ),
            Row(
                ContextMenuCommand.ZoomToFit,
                "Zoom to Fit",
                Glyphs.ZoomToFit,
                OnCanvas,
                ZoomToFitChord
            ),
            Row(
                ContextMenuCommand.ZoomTo100Percent,
                "Zoom to 100%",
                Glyphs.ZoomTo100Percent,
                OnCanvas,
                ZoomTo100PercentChord
            ),
        ],
        [
            new RowDefinition(
                ContextMenuCommand.ToggleSnapToGrid,
                "Snap to Grid",
                Glyphs.SnapToGrid,
                OnCanvas,
                c => c.SnapToGridChordLive ? SnapToGridChord : null,
                c => c.SnapToGrid
            ),
            new RowDefinition(
                ContextMenuCommand.ToggleObjectSnapping,
                "Object Snapping",
                Glyphs.ObjectSnapping,
                OnCanvas,
                _ => null,
                c => c.ObjectSnapping
            ),
        ],
        [
            Unhinted(
                ContextMenuCommand.UnlockAll,
                "Unlock All",
                Glyphs.Unlock,
                c => OnCanvas(c) && c.CanUnlockAll
            ),
        ],
        [
            Unhinted(
                ContextMenuCommand.AddPortHere,
                "Add port here",
                Glyphs.AddPort,
                c => OnObject(c) && c.CanAddPortHere
            ),
            Unhinted(
                ContextMenuCommand.PlacePort,
                "Add port…",
                Glyphs.AddPort,
                c => OnObject(c) && c.CanPlacePort
            ),
            Unhinted(
                ContextMenuCommand.RemovePort,
                "Remove port",
                Glyphs.RemovePort,
                c => OnObject(c) && c.CanRemovePort
            ),
        ],
        [
            Unhinted(
                ContextMenuCommand.ChooseImage,
                "Choose image…",
                Glyphs.ChooseImage,
                CanChangePicture
            ),
            Unhinted(
                ContextMenuCommand.RemoveImage,
                "Remove image",
                Glyphs.RemoveImage,
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
        string label,
        Glyph icon,
        Func<ContextMenuContext, bool> isEligible,
        Chord chord
    ) => new(command, label, icon, isEligible, _ => chord, _ => null);

    private static RowDefinition Unhinted(
        ContextMenuCommand command,
        string label,
        Glyph icon,
        Func<ContextMenuContext, bool> isEligible
    ) => new(command, label, icon, isEligible, _ => null, _ => null);

    private static RowDefinition Strip(
        ContextMenuCommand command,
        string label,
        Glyph icon,
        Func<ContextMenuContext, bool> isEligible
    ) => new(command, label, icon, isEligible, _ => null, _ => null, InStrip: true);

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
