# Themed visual defaults for built-in component types

Type: grilling
Status: open

## Question

Decide whether the built-in component types get themed visual defaults, and if so how — given that they register through the same public path as a host's own types, and that their current defaults freeze into board data at placement time.

Surfaced by [Edge visibility and board-content theming](14-edge-visibility-and-board-content-theming.md), which ruled this out of its own scope and recorded ADR 0016's boundary — **a token may default anything the library itself paints**. That boundary implicates the built-ins rather than excusing them: `Rectangle.razor`, `Text.razor` and `StickyNote.razor` all ship inside `D12Canvas`, so the library does paint them.

The damage is not uniform, which is part of what needs deciding. `Text` defaults to `#000000`, **invisible** against the dark theme's `#1e1e1e` backdrop — and since an edge label defaults to a `Text` instance, that reaches inside an `Edge`. `Rectangle`'s `#FFFFFF` fill is legible but glaring. `StickyNote`'s yellow is arguably correct on both themes.

Decide:

- **Whether the two-tier problem is acceptable.** `BuiltInComponents.RegisterAll` calls the same `options.RegisterComponent<>` a host calls, with a comment stating outright that there is no separate built-in path (ADR 0001). Theming built-ins specially would manufacture the tier that design deliberately avoided. Is there a mechanism that applies to *any* registered type, or is a built-in exception justified?
- **What happens to already-placed instances.** This is the sharp constraint. `DefaultProps` is a concrete `TextProps("", "#000000", …)`, so the moment an instance is placed, `#000000` *is* its persisted prop value — beyond the reach of any theme, then and forever. A decision here cannot retroactively fix boards authored in the meantime, only change what new instances get. Whether that asymmetry is tolerable, or whether it argues for acting sooner rather than later, is part of the question.
- **Whether the null-means-no-opinion sentinel pushes down into `TProps`.** ADR 0016's mechanism generalises: a nullable colour field falling through to a token in the component's own CSS. But that changes the public shape of `TextProps`/`RectangleProps`/`StickyNoteProps` to nullable colours, breaking any host constructing them directly — and it is a theming model for instance props, which ADR 0008 explicitly declined. ADR 0008 is reopenable; establish whether it genuinely needs reopening or whether this fits within it.
- **Whether registration should be able to express a theme-dependent default at all.** A `DefaultProps` evaluated once at startup cannot depend on a theme the host may switch at runtime, and ADR 0012 makes theme switching pure CSS with no C# signal — so there is nothing for a C# default to read. Decide whether that closes the door on the registration route entirely.
- **Which types are actually in scope.** A per-type judgement (`Text` broken, `Rectangle` glaring, `StickyNote` fine) invites an inconsistent result; a blanket rule invites changing a sticky note's yellow for no reason.
- **Whether the same reasoning reaches `Image`'s `AltText`/`Fit`** — i.e. whether this is specifically about colour or about visual defaults generally.
