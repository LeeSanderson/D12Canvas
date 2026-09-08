# A colour default nobody chose is not author data, so it is null and resolves from a theme token

`TextProps.Color`, `RectangleProps.FillColor` and `RectangleProps.StrokeColor` become nullable and default to `null`. `null` means *no author opinion* and resolves to a `Theme token` in the component's own CSS at paint time, so a shape nobody has coloured is correct on both themes. A non-null value is an author's choice, taken literally in both themes with no legibility guarantee attached. This is ADR 0016's mechanism and ADR 0016's position, applied to the three properties its own trap section named and declined to decide.

The reported defect: `Text`'s default is `#000000` against a dark backdrop of `#1e1e1e`, which is not hard to read but invisible, and because `Edge.Label` defaults to a `Text` instance it reaches inside an `Edge`.

## The test is whether anyone chose the value

The ticket feared that a per-type judgement would produce an inconsistent result and a blanket rule would recolour a sticky note for no reason. Both follow from treating this as a question about colours. It is a question about **defaults**, and the discriminator is whether the literal in `BuiltInComponents.RegisterAll` records a decision or fills a constructor slot.

| Default | Reading |
|---|---|
| `TextProps.Color` `#000000` | A stand-in. Nobody chose black; text needs a colour. |
| `RectangleProps.FillColor` `#FFFFFF` | A stand-in. |
| `RectangleProps.StrokeColor` `#333333` | A stand-in. |
| `StickyNoteProps.Color` `#FFEB3B` | An opinion. Yellow is what makes it a sticky note. |
| `StickyNoteProps.TextColor` `#000000` | An opinion, and a correct one, judged against the yellow that travels with it. |

So the stand-ins become `null` and the opinions stay literals. `StickyNote` is untouched not because someone judged its yellow acceptable but because its yellow is a decision the library actually took.

`ImageProps` has no colour props at all.

## A colour is only fragile when its partner can change under it

Sorting the same properties by what each is judged against explains the damage pattern the ticket observed and reached for taste to describe:

- `TextProps.Color` is a foreground with no fill of its own, so it lands on whatever is beneath it.
- `RectangleProps.FillColor` and `StickyNoteProps.Color` are backgrounds. They are not judged against anything; they *are* what other things are judged against.
- `RectangleProps.StrokeColor` and `StickyNoteProps.TextColor` are judged against a fill that travels with them in the same `TProps`.

Only `Text` is judged against a surface it does not own, which is why it is the only one that breaks outright. A white rectangle on a dark board is legible. It is a white rectangle.

**That rule alone would scope this to `Text`, and doing so would ship a worse defect than it fixes.** `RectangleProps` has no text field, so the only way to label a box is to overlay a `Text` instance on it, and new instances default on top (ADR 0008). Under a `Text`-only change, the dark theme's default composition is near-white text on a white rectangle: invisible, reached by placing the two default shapes in the obvious order. The stand-in test and the partner rule agree once both are applied, because a `Rectangle` asserting a white fill nobody chose is the thing that breaks the pair.

## The fallback resolves in CSS, in each component

The mechanism is ADR 0016's, unchanged:

```css
.d12-text { color: var(--d12-board-text-override, var(--d12-board-text)); }
```

with `style="--d12-board-text-override: …"` emitted only when the prop is non-null. Absence of the override property makes the fallback win, and the fallback is a token already declared on `.diagram-container`, which board content inherits. A theme switch recolours every unset instance with no C# involved, because the token itself changes under the media query.

**This is why no registry tier is manufactured**, which is the ticket's first question. Nothing moves in the registration path. `BuiltInComponents.RegisterAll` keeps calling the same `options.RegisterComponent<>` a host calls, `ComponentRegistrationBuilder` gains no member, and ADR 0001 is confirmed. The change is two lines inside each component's own `<style>` and style emitter, against public tokens any author's component can read the same way. Theming a built-in specially would have manufactured a tier; theming it the way any author can does not.

**Resolving in C# was considered twice and fails twice.** Substituting a literal at `DiagramCanvas.GetComponentParameters`, the seam ADR 0032 uses for asset references, needs to know which theme is live, and ADR 0012 deliberately left no C# signal: theme is a media query and a `data-` attribute. Substituting the token *reference* instead (`"var(--d12-board-text)"` as the prop value) needs no theme knowledge and would theme an author's component that never heard of tokens, which is a real advantage. It is still rejected: it requires the library to know which props are colours and which token each falls back to, and the only vocabulary that could tell it is ADR 0021's `PropertyRole`, of which exactly one author-facing role has a token to fall back to. That is a role-to-token resolution layer serving a single property, and it works by writing a CSS expression into a value typed as author data, which breaks any component doing anything with that string other than pasting it into CSS.

The accepted cost: an author's own component gets no themed default for free. It gets the same public tokens and the same technique. Given ADR 0012 already made theming a CSS contract authors read rather than an API they call, that is consistent rather than mean.

## An authored colour has exactly one appearance

A stored colour is taken literally in both themes. A user who picks near-black text while on the light theme and later switches to dark sees it vanish, and owns that.

The alternatives were weighed and are worse. **A per-theme pair** doubles every colour field and the persistence, and fails on its own terms: the user picks while looking at one theme, so the other silently stays wrong unless they deliberately switch and re-pick. **A canvas-wide inversion filter**, Excalidraw's answer, needs no data change but inverts author images and reaches inside an author's own Razor component, which is exactly what ADR 0016's boundary forbids. **A closed named palette** resolving per theme, tldraw's answer, is clean and takes `EditorKind.Color`'s arbitrary picker away from every author. **A board backdrop that does not follow the theme** would remove the problem at the root and reverses ADR 0016, which committed `--d12-edge` to a per-theme value defended against `--d12-surface` in both themes.

One appearance is also ADR 0016's stated position verbatim, and the consistency is not cosmetic: `Edge.Label` **is** a `ComponentInstance` of type `Text`, so an edge and its label meet inside one entity. Two theming models in one edge would be indefensible.

## Absence has one representation

`null`, with an empty string normalising to `null` on write, following ADR 0016 so absence reads the same across edges and props.

`TextProps` and `RectangleProps` are positional records, so `string` becoming `string?` breaks no construction call. A host reading the field gets a nullable warning rather than a break, and `BoardEnvelope` does not change at all, since `Props` is opaque JSON to it.

## The panel gains a way back, because the control cannot express absence

`EditorKind.Color` renders `<input type="color">`, which by specification has no empty state: given `value=""` it displays `#000000`. Shipping nullability without a panel change would therefore make the panel **report the wrong colour** for every themed instance, in the direction that makes the user think nothing is wrong, and `@onchange` fires on a confirmed pick, so a user opening the picker to check and confirming the black they were shown writes `#000000` and freezes it. An accidental one-way door.

So a nullable `Color` property renders a **clear control beside its swatch**, writing `null`. `EditablePropertySchema.DiscoverFrom` already reflects over the CLR type, so detecting nullability costs nothing.

This **amends ADR 0008 in one place** and does not reopen it. Worth recording because this ADR's own ticket inherited the opposite belief and the next reader otherwise will too: **ADR 0008 never declined a theming model.** It contains no mention of theme, theming or token anywhere in its twenty-one lines. The claim originates in ADR 0012's characterisation of it. What ticket 12 actually declined was a separate style **data model**, a parallel object beside `Props`, which is a different question from what an unset field on `Props` resolves to. Nothing here introduces a second data model: the field stays exactly where ADR 0008 put it and gains one representable value.

**ADR 0021 is amended for the same reason.** A colour role paints its value into its own glyph, and a null value has no ink, so the property bar needs the same themed state.

## No migration

Instances already placed keep their literals. New instances get `null`.

A `SchemaVersion` bump is unavailable: `EnsureSupportedSchemaVersion` is strict equality, so moving to 2 makes every board ever saved throw. Rewriting on load without a bump, treating `#000000` on a `Text` as unset, is available and is refused: it cannot distinguish the default freezing there from a user deliberately choosing black, black text is a choice people make, and the loss would be written back on the next save with no undo, since ADR 0007 makes history session-scoped. That is the library second-guessing author data, which ADR 0016 refuses by name.

Recovery needs no new mechanism. The clear control above is the route, and board-wide it is Select All from ADR 0023's canvas menu followed by one clear.

The population this leaves stranded is developer-local test data. D12Canvas is not a shipped product, so the asymmetry the ticket named as its sharp constraint is bounded by a fact the ticket did not check.

## The tokens

Three new content-role tokens, following `--d12-edge` as the first of that class (which keeps its shorter name rather than being churned for tidiness):

| Token | Light | Dark |
|---|---|---|
| `--d12-board-text` | `#000000` | `#e8e8e8` |
| `--d12-board-fill` | `#FFFFFF` | `#2e2e2e` |
| `--d12-board-stroke` | `#333333` | `#8a8a8a` |

**Every light value is byte-identical to the literal it replaces**, following the precedent set when the grid, marquee and edge were tokenised, so no light-mode visual baseline moves and the diff is confined to the dark cases that are currently broken. `--d12-board-text`'s dark value equals `--d12-text`'s by coincidence rather than by reference, and they stay separate tokens: a host retuning its chrome label colour must not silently recolour every text instance on every board, which is ADR 0016's own argument for refusing to reuse `--d12-muted-text` for edges.

**`--d12-board-fill` deliberately is not `--d12-surface`.** A shape the colour of the board loses its occlusion cue, and two overlapping rectangles become indistinguishable from one.

The guarantees, stated as relationships so they are assertable in ADR 0025's shape rather than pinned as values:

- `--d12-board-text` holds at least 4.5:1 against both `--d12-surface` and `--d12-board-fill`, in both themes. WCAG's text threshold, and both partners are needed because a `Text` lands on either.
- `--d12-board-stroke` holds at least 3:1 against `--d12-board-fill`, in both themes. WCAG's non-text threshold, as ADR 0016 used for the edge.
- `--d12-board-fill` differs from `--d12-surface` by at least the separation the light pair already has.

No guarantee attaches to an authored colour, or to a `Text` overlaid on an author's own component, which the library cannot see inside.

## The library's own hard-coded colours in the built-ins

Four literals sit in built-in `<style>` blocks with no author, no prop and no data question, so they are on the library's side of ADR 0016's line outright. Ticket 74 claimed to have swept all remaining chrome; ticket 14 found three it missed and this found four more, neither while looking for them.

`Text.razor` and `StickyNote.razor` both draw the inline editor with `outline: 1px dashed rgba(0, 0, 0, 0.4)`, which against `#1e1e1e` means **the "you are editing this" affordance disappears on the dark theme**. That is an interaction defect rather than a cosmetic one. It takes a per-element escape hatch, `--d12-inline-edit-outline`, exactly `--d12-connector-preview`'s case: an element that must diverge from `--d12-border`, being four times stronger than it. Its light default is the existing `rgba(0, 0, 0, 0.4)` and its dark default the mirror, so nothing moves on light.

`Image.razor`'s placeholder is `#f0f0f0` background, `#999999` border and `#666666` text, which is `.lod-placeholder` styled differently by accident. It adopts the shared `--d12-surface` / `--d12-border` / `--d12-muted-text` trio. Only the background is byte-identical; the border and text are near-misses, so this one moves a light baseline slightly, and it does so as a correction that aligns two placeholders that should already have matched.

## Not colour specifically, but only colour today

The ticket asks whether the reasoning reaches `ImageProps.AltText` and `Fit`. It does not, and the test is sharper than "they are not colours": the rule needs a default that is a **stand-in** and a correct value that **varies by theme**. `AltText`'s `""` is a stand-in with nothing to resolve from, and nobody can guess alt text. `Fit`'s `"cover"` and `StrokeWidth`'s `2` look identical on both themes. So the rule is general and only colours satisfy it today, because the token layer is currently a colour vocabulary.

## Registration cannot express a theme-dependent default, and does not need to

The ticket's fourth bullet asks whether the door closes on the registration route. It does, and for a reason stronger than the one it gives. A `DefaultProps` evaluated once at startup cannot read a theme ADR 0012 exposes only to CSS. But even if it could, the value it produced would freeze into board data at placement, so a board authored on the light theme would stay light-shaped after the user switched. A default cannot be theme-dependent because a default becomes data. Only *absence* survives placement, which is why absence is the mechanism.

**Considered and rejected:**
- **Theming only `Text`** — the smallest change, and it ships near-white text on a white rectangle as the dark theme's default composition, because `RectangleProps` has no text field and a label is an overlaid instance.
- **Theming every colour prop** — sends `StickyNoteProps.TextColor` to a near-white token on a yellow note, strictly worse than today.
- **A per-theme colour pair on every property** — doubles the data to solve half the problem, since the user picks while looking at one theme.
- **A canvas-wide inversion filter for the dark theme** — Excalidraw's answer, no data change, and it reaches inside author components and inverts author images.
- **A closed named palette resolving per theme** — tldraw's answer, and it removes the arbitrary picker `EditorKind.Color` grants every author.
- **A board backdrop that does not follow the theme** — removes the problem at the root and reverses ADR 0016's per-theme `--d12-edge`, making the dark theme cosmetic for the surface it matters most on.
- **Resolving the token to a literal in C# at the bind seam** — ADR 0032's seam, and ADR 0012 left no C# theme signal for it to read.
- **Substituting the token reference as the prop value in C#** — needs no theme signal and would theme author components for free, at the cost of a `PropertyRole`-to-token layer serving one property and a CSS expression written into a field typed as author data.
- **A registration-time themed default** — impossible to read a CSS-only theme from C#, and self-defeating anyway, since the value would freeze into board data at placement.
- **An empty string as the sentinel rather than null** — gives absence two representations across edges and props, and buys nothing, since `<input type="color">` displays `#000000` for both.
- **Rewriting `#000000` to null on load** — cannot distinguish a frozen default from a deliberate choice, and writes the loss back with no undo.
- **A `SchemaVersion` bump with a migration pipeline** — the gate is strict equality, so every board ever saved would throw on load. The fourth refusal on this map.
- **Shipping nullability with no panel change** — makes the panel misreport every themed instance and turns an accidental confirm into a one-way door.
- **Reusing `--d12-text` for board text** — a host retuning its chrome label colour would silently recolour every text instance on every board, ADR 0016's argument against reusing `--d12-muted-text`.
- **`--d12-surface` as the rectangle's fill fallback** — a shape the colour of the board has no occlusion cue against another one.
- **Renaming `--d12-edge` into the new `--d12-board-*` family** — churns a settled ADR's published name for symmetry.
