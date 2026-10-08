namespace D12Canvas;

// Checked is null for a row that is not a toggle. A glyph row is drawn as an icon in one strip with
// its neighbouring glyph rows, its Label becoming the icon's accessible name.
internal sealed record ContextMenuRow(
    ContextMenuCommand Command,
    string Label,
    string? Hint,
    bool? Checked,
    bool Glyph = false
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
        Func<ContextMenuContext, Chord?> Chord,
        Func<ContextMenuContext, bool?> Checked,
        bool Glyph = false
    );

    private static readonly Chord DeleteChord = new("Delete", AppleKey: "⌫");
    private static readonly Chord GroupChord = new("G", Primary: true);
    private static readonly Chord UngroupChord = new("G", Primary: true, Shift: true);
    private static readonly Chord BringToFrontChord = new("]", Primary: true, Shift: true);
    private static readonly Chord BringForwardChord = new("]", Primary: true);
    private static readonly Chord SendBackwardChord = new("[", Primary: true);
    private static readonly Chord SendToBackChord = new("[", Primary: true, Shift: true);
    private static readonly Chord SelectAllChord = new("A", Primary: true);
    private static readonly Chord SnapToGridChord = new("'", Primary: true);

    private static readonly Chord CutChord = new("X", Primary: true);
    private static readonly Chord CopyChord = new("C", Primary: true);
    private static readonly Chord PasteChord = new("V", Primary: true);
    private static readonly Chord DuplicateChord = new("D", Primary: true);

    private static readonly IReadOnlyList<IReadOnlyList<RowDefinition>> Sections =
    [
        [
            Row(
                ContextMenuCommand.Cut,
                "Cut",
                c => OnObject(c) && c.AsyncClipboard && c.CanCut,
                CutChord
            ),
            Row(
                ContextMenuCommand.Copy,
                "Copy",
                c => OnObject(c) && c.AsyncClipboard && c.CanCopy,
                CopyChord
            ),
            Row(ContextMenuCommand.Paste, "Paste", c => c.AsyncClipboard, PasteChord),
            Row(
                ContextMenuCommand.Duplicate,
                "Duplicate",
                c => OnObject(c) && c.CanCopy,
                DuplicateChord
            ),
        ],
        [Row(ContextMenuCommand.Delete, "Delete", OnObject, DeleteChord)],
        [
            Row(
                ContextMenuCommand.SelectAll,
                "Select All",
                c => OnCanvas(c) && c.CanSelectAll,
                SelectAllChord
            ),
        ],
        [
            Row(ContextMenuCommand.Group, "Group", c => OnObject(c) && c.CanGroup, GroupChord),
            Row(
                ContextMenuCommand.Ungroup,
                "Ungroup",
                c => OnObject(c) && c.CanUngroup,
                UngroupChord
            ),
        ],
        [
            Glyph(ContextMenuCommand.AlignLeft, "Align left", CanAlign),
            Glyph(ContextMenuCommand.AlignCentre, "Align centre", CanAlign),
            Glyph(ContextMenuCommand.AlignRight, "Align right", CanAlign),
            Glyph(ContextMenuCommand.AlignTop, "Align top", CanAlign),
            Glyph(ContextMenuCommand.AlignMiddle, "Align middle", CanAlign),
            Glyph(ContextMenuCommand.AlignBottom, "Align bottom", CanAlign),
            Glyph(
                ContextMenuCommand.DistributeHorizontally,
                "Distribute horizontally",
                CanDistribute
            ),
            Glyph(ContextMenuCommand.DistributeVertically, "Distribute vertically", CanDistribute),
            Row(ContextMenuCommand.BringToFront, "Bring to Front", CanArrange, BringToFrontChord),
            Row(ContextMenuCommand.BringForward, "Bring Forward", CanArrange, BringForwardChord),
            Row(ContextMenuCommand.SendBackward, "Send Backward", CanArrange, SendBackwardChord),
            Row(ContextMenuCommand.SendToBack, "Send to Back", CanArrange, SendToBackChord),
        ],
        [
            new RowDefinition(
                ContextMenuCommand.ToggleSnapToGrid,
                OnCanvas,
                "Snap to Grid",
                c => c.SnapToGridChordLive ? SnapToGridChord : null,
                c => c.SnapToGrid
            ),
            new RowDefinition(
                ContextMenuCommand.ToggleObjectSnapping,
                OnCanvas,
                "Object Snapping",
                _ => null,
                c => c.ObjectSnapping
            ),
        ],
        [
            Unhinted(ContextMenuCommand.ChooseImage, "Choose image…", CanChangePicture),
            Unhinted(ContextMenuCommand.RemoveImage, "Remove image", CanChangePicture),
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
            row.Chord(context) is { } chord
                ? ShortcutHint.Render(chord, context.ApplePlatform)
                : null,
            row.Checked(context),
            row.Glyph
        );

    private static RowDefinition Row(
        ContextMenuCommand command,
        string label,
        Func<ContextMenuContext, bool> isEligible,
        Chord chord
    ) => new(command, isEligible, label, _ => chord, _ => null);

    private static RowDefinition Unhinted(
        ContextMenuCommand command,
        string label,
        Func<ContextMenuContext, bool> isEligible
    ) => new(command, isEligible, label, _ => null, _ => null);

    private static RowDefinition Glyph(
        ContextMenuCommand command,
        string label,
        Func<ContextMenuContext, bool> isEligible
    ) => new(command, isEligible, label, _ => null, _ => null, Glyph: true);

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
