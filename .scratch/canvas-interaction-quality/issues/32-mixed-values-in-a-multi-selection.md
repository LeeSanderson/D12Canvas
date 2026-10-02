# Mixed values across a multi-selection

Type: grilling
Status: resolved
Blocked by: 08

## Question

Decide what a property surface shows, and what committing does, when the selected entities disagree on a value.

`PropertyPanel` has no answer today and says so: a multi-target field "displays whichever target happens to be first as its representative current value - there's no 'mixed values' indicator". That was defensible for a docked panel where the control is a text field with a number in it. ADR 0021 makes it worse in two ways at once.

**The glyph *is* the value.** A `Fill` role paints the current colour into its own glyph, so three differently-coloured sticky notes show one arbitrary colour as though it were the truth, at 26 pixels, with nothing to signal otherwise. There is no field to read and disbelieve.

**ADR 0021 widened who can be in a multi-selection.** Reading the expanded selection means a selected `Group` is now editable through its members, so mixed values stop being an unusual shift-click case and become the ordinary case: grouping three notes of different colours is a thing users do deliberately.

Decide:

- What a mixed row looks like in the bar, where the glyph carries the value, and in the panel, where it does not. These may legitimately differ.
- What committing a mixed row does. Writing the chosen value to every target is the obvious answer and is what the existing bulk commit already does; confirm it against the alternative that a mixed row is read-only until explicitly resolved.
- Whether "mixed" is computed per role or per target set. The existing `Commit` already skips a target already holding the new value, so a partially-mixed commit is already a smaller history entry than it looks.
- Whether a mixed `Custom` editor can be represented at all. An author's `RenderFragment` receives a single `CustomEditorContext.Value` and has no way to express "several", so either the context grows or a mixed `Custom` row is suppressed.
- Whether the panel and the bar must agree. ADR 0021 deliberately let them differ on membership; this asks whether they may also differ on how a value is displayed.

Note the reference evidence is thin here and worth gathering before grilling: ticket 03's teardown covers placement and gesture behaviour for selection chrome but not mixed-value presentation.

Amends ADR 0008 (the first-target rule) and possibly ADR 0021.

## Answer

**A mixed row stays editable, shows that it is mixed by one rule per `EditorKind`, and a commit writes to every target.** Recorded as [ADR 0040](../../../docs/adr/0040-mixed-values.md). Reference evidence: branch `research/mixed-value-presentation`, file `.scratch/canvas-interaction-quality/research/mixed-value-presentation.md`, commit `fff2c27`.

- **Commit:** writes the new value to every target in one history entry, skipping targets already at it, changing only the edited property. No read-only state; tldraw, Excalidraw and FigJam all write to every target.
- **Per row or per target set:** per row, across that row's targets. A target that lacks the property removes the row rather than making it mixed.
- **`null` is its own value**, because ADR 0034's themed `null` and a literal look different under a theme switch.
- **Colour values compare case-insensitively** in the mixed test and `Commit`'s skip. Defaults are uppercase, `<input type="color">` reports lowercase, and stored data is not rewritten.
- **Bar and panel agree** on how mixed looks, one rule per kind: hatched swatch for `Color` (the role's own glyph shape in the bar, a swatch button opening the native picker in the panel), empty field with a "Mixed" placeholder for `Number`/`Text`, no active option plus a disabled "Mixed" option for `Dropdown`, native indeterminate for `Checkbox` via the JS module.
- **Inked, themed and mixed colour glyphs must be distinct in both themes**, recorded as a constraint on ADR 0034's undrawn themed state.
- **`Custom`:** `CustomEditorContext` gains a trailing `bool IsMixed = false`, with `Value` null when mixed. Not a `Values` list, not suppression.
- **ADR 0021's row** gains `IsMixed`, computed by each producer, including the edge producer now that ADR 0037 allows edge-only multi-selection.

Amends ADRs 0008 and 0021. One item added to the fog: relative arithmetic in mixed number fields.
