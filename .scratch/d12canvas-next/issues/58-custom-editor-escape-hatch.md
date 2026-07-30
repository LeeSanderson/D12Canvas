# 58 — `Custom` editor escape hatch

**What to build:** A component author whose property doesn't fit any built-in `EditorKind` supplies their own editor: `EditorKind.Custom` takes an author-provided render fragment over the props, which the panel hosts like any other control — committing through the same undoable path. (ADR 0008.)

**Blocked by:** 56 (Property panel + attribute schema + first editors)

**Status:** resolved

- [x] A property can declare `EditorKind.Custom` with an author-supplied render fragment
- [x] The custom editor renders inside the panel alongside built-in controls
- [x] Commits from a custom editor go through the same single-gesture undoable path
- [x] A Demo custom component exercises the escape hatch end to end
- [x] bUnit coverage

## Comments

Implemented as: `EditableProperty` gained a fourth field, `CustomEditor` (`RenderFragment<CustomEditorContext>?`,
only populated when `Kind == Custom`). `CustomEditorContext` is new (`D12Canvas.Panel`) - the property's
current value (already unwrapped via reflection, same value every other `EditorKind` control works off) plus
a `Commit` callback closed over that property. `PropertyPanel` gained a `case EditorKind.Custom:` that invokes
the author's fragment with a `CustomContext(property)` and a `CommitCustomEdit` method - identical to the
existing `CommitEdit` (same no-op-if-unchanged guard, same `CloneWithChange`/`CommitPropsChange` route to
exactly one `MutateEntityCommand`) except it skips `ConvertValue`/`ChangeEventArgs` parsing entirely, since a
Custom editor's value is already CLR-typed.

A `[PanelEditable(EditorKind.Custom)]` attribute can never supply the RenderFragment it needs (attribute
arguments must be compile-time constants), so `EditablePropertySchema.DiscoverFrom` now throws a new
`CustomEditorRequiredException` at discovery/registration time if it ever sees one - a `Custom`-kind property
must come from `ComponentRegistrationBuilder.EditableProperties` instead, the same escape hatch ticket 57's
`DropdownOptionsRequiredException` guards for missing `Options`.

Did not wire `ImageProps.Url` (ticket 57 had reserved it as "the file-picker escape hatch example") - a real
file-open/upload flow needs browser file-API JS interop that's out of this ticket's scope, and the checklist
asks for a *Demo* component exercising the mechanism, not a built-in. Instead extended the Demo app's existing
`demo-note` registration (`D12Canvas.Demo/Program.cs`): `Color` is now `EditorKind.Custom` via a new
`DemoNoteColorEditor.razor` - a small curated swatch picker (something the built-in Color/Dropdown kinds
can't express as cleanly), wrapped behind a static `RenderFragment<CustomEditorContext> Editor` factory so
`Program.cs`'s registration call only references that, not `RenderTreeBuilder` internals. `Text` was also
promoted to `EditorKind.Text` alongside it (previously undecorated), since a builder override replaces the
*whole* schema, not just the property being changed.

bUnit coverage: extended the shared `PanelTestProps` fixture with a `CustomValue` field (no attribute - a
`Custom`-kind property can't be attribute-declared) plus a new `PanelTestCustomEditor` fixture (a single
button, hand-built via `RenderTreeBuilder` since `D12Canvas.Tests` is a plain classlib with no `.razor`
support) for `PropertyPanelTests`' render/commit/undo/no-op-recommit cases, mirroring every other
`EditorKind`'s coverage shape. Added a `D12CanvasOptionsTests` case for the new
`CustomEditorRequiredException` throw path (new fixture `PropsWithCustomEditorAttribute`). Added a
`PropertyPanelVisualTests.PopulatedPanelWithCustomControl_MatchesBaseline` screenshot case (Demo Note's
swatch picker); the three pre-existing panel screenshot baselines also needed re-promoting since the new
`.d12-property-panel-custom` CSS rule changes `PropertyPanel`'s embedded `<style>` block content on every
render, not just Custom's own case (visually identical - `PopulatedPanel`/`PopulatedPanelWithDropdownControl`
matched the container's PNG byte-for-byte; only the HTML source's style block gained new lines). Baselines
promoted from a clean `mcr.microsoft.com/playwright/dotnet:v1.61.0-noble` container run, not the local dev
build, per the README's documented process.
