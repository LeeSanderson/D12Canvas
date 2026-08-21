# Mixed values across a multi-selection

Type: grilling
Status: open
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
