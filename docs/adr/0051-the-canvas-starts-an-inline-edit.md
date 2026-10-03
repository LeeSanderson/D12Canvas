# The canvas starts an inline edit through an interface on the component, and a new node opens ready to type

ADR 0030 made `Quick create` a true duplicate and stated the cost: chain five nodes off a sticky note reading "Login" and all five read "Login". The fix is for the new instance to open ready to type, and that was not available, because each built-in starts its own editor from a private `BeginEdit()` reached only by `@ondblclick` on its own paragraph. `DiagramCanvas` had no way to say "edit this one", and any route had to work for an author's component as well as the built-ins.

The same gap turned up from a second direction. ADR 0018 dispatches double-press from the press count, and the paragraph carrying `@ondblclick="BeginEdit"` is a plain `<p>`, neither natively interactive nor opt-in marked, so ADR 0017's walk classifies a press on it as `instance`. ADR 0044 line 31 describes the right behaviour (the first double-press enters the group, the second edits the text) on the assumption that the text classifies `author-content`, which it does not. So double-press-to-edit also needs the canvas to be the thing that starts the edit.

This decision covers both: what the contract member is, which gestures start an edit, what the keyboard does, and how the edit sits in history.

## ADR 0001 reopens for one seam

ADR 0001 is on the map's settled list. It reopens here, narrowly, for one seam: **the canvas can ask an instance to begin editing, and the instance can tell the canvas it has finished.** This is the shape ADR 0032 used when ADR 0004 reopened for asset storage alone. Everything else in ADR 0001 holds.

Declining was considered and had precedent: the map's Out of scope section rules out containers and named layers on exactly this ground. Those were whole features with models of their own. This is one seam that two existing decisions already depend on, ADR 0030 for the create case and ADR 0018 for double-press dispatch.

The reopening also records something ADR 0001 never said. Every registered component already receives two cascading values from `DiagramCanvas`, `ParentCanvas` and `InstanceId`, and the built-ins rely on both to commit their edits. They have been a convention. They are now part of the contract, alongside the fixed `Props` parameter ADR 0001's second addendum names.

## The member is an interface on `TComponent`

```csharp
public interface IInlineEditable
{
    void BeginEdit();
}
```

A component type opts in by implementing it. `RegisterComponent<TComponent, TProps>` records `typeof(IInlineEditable).IsAssignableFrom(typeof(TComponent))` on the registration at composition time. That is a check on one known generic type argument, not the attribute and assembly scanning ADR 0001 rejected for trimming.

The deciding fact is that **the instance being asked to edit is often not mounted when the request arrives.** `Quick create` can step off screen on a dense board, windowing mounts only what is visible, and below the LOD cutoff an instance is a placeholder. So the canvas has to know whether a type can edit before any component exists, and has to deliver the request after one does. The interface gives both. Editability is a property of the registration. Delivery reuses the shape ticket 67 of `d12canvas-next` built for placement focus: the canvas sets a `_pendingEditId`, and after the render that mounts the instance it calls `BeginEdit()` on the `DynamicComponent.Instance`.

The compiler checks the method name, which the existing `Props` convention does not. The cost is that `BeginEdit` becomes public on `StickyNote` and `Text`. Nothing outside the canvas holds a reference to a mounted board component, so nothing else can call it.

## Declining is a silent no-op

A type that does not implement `IInlineEditable` has declined, and every route that would start an edit does nothing extra. `Rectangle` and `Image` are two of the four built-ins that decline. A `Quick create` from a rectangle still selects and focuses the new instance as ADR 0030 decides. A double-press on an addressable rectangle has already run its press-count-1 outcome (select), and press-count-2 has nothing more to do.

A fallback such as focusing the property bar's first control was rejected. It gives one gesture two meanings chosen by the type, which the user cannot see, the ground on which ADR 0030 rejected Miro's conditional outcome.

## Which gestures start an edit

**A gesture that makes a new node begins editing it. A gesture that makes a copy does not.**

| Gesture | Begins editing |
|---|---|
| `Quick create` by pointer (ADR 0030) | yes |
| `Ctrl`+`Arrow` quick create | yes |
| Palette click-to-add, including `Enter` or `Space` on a palette entry | yes |
| Palette drag-and-drop | yes |
| Adding an edge label by double-press on the line | yes |
| Paste, `Ctrl`+`D`, `Alt`-drag | no |

Paste, duplicate and `Alt`-drag mean "this content again", and opening an editor on the copy works against that. `Quick create` is mechanically a duplicate as well, but ADR 0030 made it one for its shape and size, and the repeated label is the cost this decision exists to remove. Palette placement has the opposite problem: `DefaultProps` gives a sticky note and a `Text` empty text, so without an edit they sit empty until double-pressed. Both palette routes are included because they share ADR 0009's placement path, and deciding one without the other is how a rule stops being readable.

An edge label is created as an empty `Text` through the same `NewCenteredInstance`, so labelling an edge has taken two double-presses with an invisible box after the first. It now takes one. `ResolvePropsEntity` already finds a label through `FindEdgeLabel`, so the pending-edit lookup reaches it with no new path.

## Existing instances: double-press and `F2`

**A double-press on an addressable instance calls `BeginEdit()`. A double-press on an instance that is not addressable enters its group** (ADR 0044). Both are the canvas dispatching off one press count on the `instance` role. Each double-press goes one level deeper, so a grouped sticky note takes one double-press to enter the group and a second to edit, which is what ADR 0044 line 31 describes. The built-ins' `@ondblclick="BeginEdit"` is deleted, and nothing depends on whether the browser's `dblclick` survives a prevented `pointerdown`, the probe ADR 0018 asked for.

A double-press on an existing edge label calls `BeginEdit()` on the label in the same way.

**`F2` begins editing the focused instance.** `Enter` on an instance tab stop is port picking (ADR 0026), so it is not available. `F2` is free in the table and is the Windows and spreadsheet key for editing in place. It is a guarded row like the rest, and a no-op on a type that declines.

**A locked instance is never edited.** ADR 0017 takes it out of primary-press participation, so a double-press does not reach it. It stays keyboard-reachable, so `F2` on a locked instance is a no-op: locked means nothing modifies it. New instances are never locked, so edit-on-create never meets the case.

## Off screen and below the LOD cutoff

**When a create is about to begin editing and the new instance is not fully inside the viewport, the canvas pans the minimum distance to bring it in.** Pan only, never zoom, as one framing write through `ZoomPanTracker`, so ADR 0015's transition animates it. Typing never happens out of view. This applies only when an edit will begin: a `Quick create` from a type that declines keeps ADR 0030's behaviour exactly, with no pan and `Shift+2` as the recovery.

**Below the LOD cutoff the request is dropped.** Text at that zoom is unreadable, and an uninvited zoom is a much larger jump than a pan.

**The request lasts one render.** If the instance has not mounted as a real component after the render that follows the pan, the request is dropped. A pending edit that fires later, when the user happens to scroll to the instance, would take the keyboard without warning.

**Editors focus with `preventScroll: true`.** `.diagram-container` is `overflow: hidden`, and plain `focus()` scrolls it to reveal the element, which moves the board without `ZoomPanTracker` knowing. The built-ins call `FocusAsync()` with no options today. Any `IInlineEditable` author should pass `preventScroll` too.

## Select all on entry

**`BeginEdit()` selects all of the existing text, on every route.** For a quick-created node, typing then replaces the duplicated label and an arrow key keeps it, which is what makes the chain worth having. For an empty new node it makes no difference. For an existing node, `End` or an arrow key puts the caret where the user wants it.

One behaviour on every route keeps `BeginEdit()` parameterless. An `IInlineEditable` author should follow it.

## `Escape` commits and returns focus

Both built-in editors stop keydown propagation, so the canvas sees no keys while an edit is open. Until now `Escape` threw the edit away, and `Tab` committed through blur but moved focus on to the next tab stop. A keyboard user could not leave an editor and stay on the instance, so a keyboard chain broke at its second link.

**`Escape` in an editor commits the edit**, reversing the built-ins' discard. Miro, FigJam and tldraw all keep the text on `Escape` out of a text edit. Discarding is one `Ctrl`+`Z` away.

**The component then calls `ParentCanvas.EndInlineEdit(InstanceId)`, and the canvas returns focus to the instance's tab stop** with `preventScroll`, through the `focusTabStopAt` path ticket 67 built. A component cannot find its own tab stop, so the canvas owns the return. Blur by pointer or by `Tab` does not call it, because focus is already going somewhere the user chose.

A keyboard chain then reads `Ctrl`+`Arrow`, type, `Escape`, `Ctrl`+`Arrow`, type, `Escape`.

An edge label has no tab stop, so for a label `EndInlineEdit` returns focus to the canvas container, the same target ADR 0018's focus transfer uses. [Keyboard reach for edges](../../.scratch/canvas-interaction-quality/issues/49-keyboard-reach-for-edges.md) can redirect it to the edge once edges have tab stops.

The first `Escape` never reaches the canvas's own `Escape` row, because the editor stops propagation. The next one does, and steps out of the `Entered group` or clears the selection as ADR 0044 orders. Each press walks back one level.

## Two history entries

**Creating and then typing is two entries.** One `Ctrl`+`Z` after a chained-and-typed node restores the duplicated label, and a second removes the node and its edge. Nothing new is built.

ADR 0007 makes an entry one gesture. Creating and typing are two acts with a visible boundary between them, since the node is complete on screen before a key is pressed. Making them one entry needs either a coalesce-into-previous operation on `CommandHistory`, which is a mechanism outside ADR 0007's closed command set, or a creation held open across an editing session of any length, which breaks ADR 0020's rule that a creation commits at release and leaves an open entry that `CommandHistory.Changed` and autosave would have to special-case. An `Escape` straight after creation with no change records nothing, so that case is one entry anyway.

What an abandoned edit on new, empty content should do (an empty `Text` is invisible) is left to its own ticket, because the clean answer, no history entry at all, is the retraction mechanism this section declines to build.

## How this is verified

Per ADR 0025:

- An `Interaction probe` drives the real key path for `F2` and for `Escape` out of an editor, and a real double-press, rather than calling `BeginEdit` or a key handler directly. This is the fourth-instance rule from the map: a test that supplies the browser's step proves only the C#.
- Editability comes from the type: a table over the four built-ins asserts `StickyNote` and `Text` editable and `Rectangle` and `Image` not.
- History is asserted as counts: a create followed by an edit is two entries, a create followed by an unchanged `Escape` is one.
- A create whose instance lands outside the viewport ends with the instance inside it and the scale unchanged.
- A create below the LOD cutoff leaves no editor open.
- After `Escape`, the focused element is the instance's tab stop.
- A double-press on a member of an unentered group enters the group and opens no editor. A second double-press opens one.

## Amends, confirms

- **Amends ADR 0001** with a third addendum: the `IInlineEditable` seam and `EndInlineEdit`, and `ParentCanvas` and `InstanceId` recorded as part of the contract.
- **Amends ADR 0018** in two places. Double-press on the `instance` role dispatches `BeginEdit()` on an addressable instance and group entry on one that is not. ADR 0035's carve-out keeping the built-ins' `@ondblclick="BeginEdit"` is withdrawn, so ADR 0018's rule deleting every `@ondblclick` on board content applies to them as well.
- **Amends ADR 0026** by one row, `F2`.
- **Amends ADR 0044** line 31: the behaviour it describes stands, and the mechanism is canvas dispatch through `IInlineEditable` rather than `author-content` reaching the built-in's own `dblclick`.
- **Amends ADR 0030**: the cost it carried is resolved, and a `Quick create` of an editable type now opens the new instance for editing.
- **Confirms ADR 0007** and **ADR 0020.** Creation and edit are separate entries, and the creation still commits at release.
- **Confirms ADR 0009.** Both placement gestures stay on one path, and both now begin editing.
- **Confirms ADR 0010.** Focus stays on the tab stop after an edit, so focus drives selection as before.
- **Confirms ADR 0017.** Locked instances are never edited.

## Considered and rejected

- **Keeping ADR 0001 shut** and accepting both costs for the life of the effort. Leaves every chained node reading its source's label, and leaves double-press-to-edit as the one piece of the old `dblclick` layer ADR 0018 set out to remove, resting on an unprobed browser assumption.
- **A cascading parameter the component reads**, next to `InstanceId`. Handles late mounting for free, but the canvas cannot tell whether anything listens, so it cannot decide what a declining type does, and a misspelled parameter fails silently.
- **A flag or delegate on the registration.** Known before mount, but a flag delivers nothing and a delegate cannot reach a component's private editing state.
- **A fallback for declining types**, such as focusing the property bar. One gesture with two meanings chosen by something the user cannot see.
- **Beginning an edit on paste, duplicate and `Alt`-drag.** Fights the intent of a copy.
- **Beginning an edit on `Quick create` only.** Leaves palette placement producing empty notes, and makes the rule depend on which of two near-identical creation paths was used.
- **`Enter` to begin editing.** Taken by port picking on an instance tab stop.
- **Panning on every `Quick create`**, editable or not. Changes ADR 0030's settled behaviour for types where nothing follows the create.
- **Zooming to an instance below the LOD cutoff** so its edit can open. A far larger jump than the user asked for.
- **A pending edit that waits until the instance mounts.** Fires at an unpredictable later moment and takes the keyboard.
- **`BeginEdit(EditEntry)`** with select-all for creation and caret-at-end for an existing node. Doubles what every author implements for a one-keypress difference.
- **Keeping `Escape` as discard.** Leaves the keyboard no way out of an editor that keeps both the text and the user's place.
- **One history entry for create and edit.** Needs a coalescing operation or a creation held open, and neither fits ADR 0007 or ADR 0020.

## Amended by ADR 0054

Edges now have tab stops, so `EndInlineEdit` for an edge label returns focus to the edge's stop, which selects the edge. The canvas container is only the fallback, used when the edge's stop is not mounted because the viewport was panned during the edit.
