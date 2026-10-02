# Entering a group is a scope beside the selection, one level at a time

A user reaches one member of a `Group` by **entering** it. A double-press on a member, or `Enter` on the group's tab stop, makes that group the **`Entered group`**: transient view state held beside the `Selection`, never persisted and never in `History`. While a group is entered, the selection holds only its direct members, and content outside it does not respond to presses. Escape steps back out one level, and a press or focus landing outside steps out as far as it needs to.

This resolves the deferral in ADR 0006, which left group entry "to be decided at implementation time once the interaction can be felt hands-on".

## A scope, not a mode and not a gesture

The two models in the reference tools differ in whether anything persists after the press. tldraw enters a *focused group* and stays there until Escape or a press outside. Figma and Miro reach a nested layer directly with a modified press, and nothing remains but the selection.

The scope is chosen. Direct reach looks cheaper, and it fails the three things this decision has to support:

- **Aligning members** needs several of them selected at once. Under direct reach that is a held modifier on every press, and no marquee.
- **The keyboard** needs somewhere to stand. "Step into this group, then Tab through its members" is a scope by definition.
- **Nested groups** would either jump to the leaf every time or need a second modifier.

A state that outlives the release is neither a tool mode (ADR 0009) nor a `Pointer gesture` (ADR 0018). It is the same kind of thing as the `Selection`, which is also transient, also outlives every press, and is not called a mode by any ADR. So it lives beside the selection, as canvas-owned view state, and has its own name in `CONTEXT.md`.

## One level at a time

Groups nest: `Board.FindContainingGroup` walks up to the outermost group, and `GetBounds` recurses. With G holding instance A and nested group H, and H holding B and C, a double-press on C enters G and selects H. A second double-press on C enters H and selects C.

Going straight to the leaf was rejected. One level per entry means the `Entered group` is always the parent of whatever is selected, so it is one `Group` id and never a path. A nested group behaves like any other member: entering G shows the H the user built, not its contents. Entering a nested group replaces the field with the child, and stepping out replaces it with the parent.

ADR 0018 dispatches double-press off the press count, and a press-count-1 outcome always commits first. Here that is harmless and in fact what the user needs: the first press selects the group, and the second refines it. One target carries one click meaning and one double-press meaning, which is within the limit the map records for ADR 0018's press count.

## Content responds only when its instance is addressable

An instance is **addressable** when it is top-level or a direct member of the `Entered group`. Outside that, its `author-content` classifies as `instance`.

Without this rule, a double-press on a grouped sticky note means two different things depending on where on the note it lands. ADR 0022 keeps primary on `author-content` as `Native`, so a double-press on the text reaches `BeginEdit` and the canvas never learns of it, while a double-press on the border enters the group. On a sticky note the text is most of the surface. With the rule, the first double-press enters the group and selects the note, and the second edits the text, which is what tldraw does.

The mechanism is a marker C# renders on each container that is not addressable. ADR 0017's walk reads it at press and classifies `author-content` under it as `instance`. No round-trip and no new synchronous decision: the walk reads one more attribute, as it already reads locking.

**The cost is accepted, not glossed: every control inside an unentered group stops responding to a press**, not only text. A custom component's button, checkbox or `<input>` does nothing until its group is entered. Grouping an author's interactive component therefore changes behaviour that works today. That is the meaning of a group: one rigid unit until the user steps inside it.

### The JavaScript rule is narrowed to its reason

`CONTEXT.md`'s `Hit target` entry, carrying ADRs 0017 and 0018, says "no selection state is ever mirrored into JavaScript". Read literally, the marker breaks it, because addressability derives from the `Entered group`. The rule exists so JavaScript never holds a copy of the selection that can go stale. Under the marker JavaScript holds nothing. It reads, at press, the DOM C# last rendered, exactly as it reads `Locked`. ADR 0017's own title says locking "rides the same seam". Deciding in C# after the hop is not available, because `preventDefault` and the `Native` decision must be made synchronously at press, which is why ADR 0017 resolves roles in JavaScript at all.

So the rule now reads: **JavaScript holds no copy of selection state; it reads only what C# has rendered.** The lag this allows is the one locking already has. A press arriving after the `Entered group` changes and before the re-render classifies against the old marker. Entry is a double-press followed by a deliberate further press, so that window is not one a user reaches.

## What the selection holds

**The selection holds only direct members of the `Entered group`, or top-level ids when no group is entered.** Every selected id is a sibling at one level.

`EffectiveSelectionId` becomes "the ancestor of this id that is a direct member of the `Entered group`, or the outermost containing group when none is entered". That is the one named place ADR 0022 left for this override. The marquee resolves through the same function, so a band drawn inside G selects G's members and nothing outside G.

Each reader of the selection, stated:

- **Move, resize, delete, copy** act on siblings. A selected nested group still flattens through `ExpandedSelection()` as it does today.
- **Align and distribute** act on siblings as rigid bodies. The rule ADR 0014 applies at the top level applies one level down, so the gesture is enough and align needs no scope of its own.
- **`Ctrl+G` inside G** creates a new group nested in G. The selected ids leave G's `MemberIds` and the new group's id takes their place. This is what stops a second, overlapping group from forming, the case `UpdateMarqueeSelection`'s comment warns about.
- **Ungroup of H inside G** splices H's members back into G's `MemberIds` where H was.

`GroupCommand` and `UngroupCommand` are thin `AddGroup`/`RemoveGroup` wrappers today and never touch a parent. Both gain a parent-membership edit, applied and undone with the group in one history entry. This also stops a defect that exists now: ungrouping a nested group leaves its id dangling in the parent's `MemberIds`. Whether a group tolerates dead member ids in general is not decided here.

## Leaving

1. **Escape**, in precedence order: cancel the active `Pointer gesture` (ADR 0031). Otherwise, if a group is entered, **step out one level and select the group just left**. Otherwise clear the selection. With H entered inside G, Escape goes to G with H selected, then to the top level with G selected, then to nothing. It walks back up the entries that got the user there.
2. **A press on an entity outside the `Entered group`** pops the scope until the target is a descendant of it, or until none is entered, and then resolves as normal through `EffectiveSelectionId`. With H entered, pressing A pops to G and selects A. Pressing anything outside G pops to the top. The rule is one loop and needs no case for "outside, but inside an ancestor".
3. **A press on empty canvas** pops the scope until the press point lies inside the `Entered group`'s `EffectiveBounds`, or until none is entered. Inside the bounds it stays in the scope: a click clears the in-scope selection, and a drag draws a marquee over the scope's members. Outside every entered bound it exits to the top level, and the marquee starts there. The `canvas` role's synchronous answers are the same either way, so the bounds test runs in C# after the hop and JavaScript needs no new marker.
4. **The scope cannot outlive its group.** If an undo, a delete or an ungroup removes the entered group, the scope pops to the nearest ancestor that still exists. A future remote change follows the same rule, so collaboration is not foreclosed.

An empty selection inside an entered group is visible, because the scope draws its own outline (below). That is what makes rule 3's in-scope click acceptable, and it is why Escape does not clear an in-scope selection first and exit only on a second press: without the outline that would be a state the user cannot see, and with it the extra press buys nothing.

## The keyboard route

ADR 0026 binds `Enter` only on an instance tab stop, where it commits a port attachment, so `Enter` on a group's tab stop is free.

- **`Enter` on a group's tab stop enters it.** The group's single stop (ADR 0010) is replaced, in the same reading-order position, by stops for its direct members. Focus moves to the first of them, and since focus drives selection under ADR 0010, that member is selected. A nested group among them keeps one stop until it is entered in turn.
- **No focus trap.** Tab past the last member lands on whatever follows in reading order. Focus landing outside the `Entered group` is rule 2 above, so the pointer and the keyboard share one exit.
- **Escape** follows rule 1, and focus returns to the tab stop of the group just left.
- **Multi-select inside the scope** is `Ctrl+Tab` and `Space`, unchanged. They work over whatever tab stops are rendered, which are now the scope's members, and they carry the open doubt about `Ctrl+Tab` with them.

Both focus writes are forced, because each unmounts the tab stop that holds focus. They fit ADR 0036's third category, a command handoff that names its target, as `Ctrl+G`'s `focusGroupTabStop` does. Escape after a pointer entry finds focus on `.diagram-canvas` and writes nothing.

This is keyed off the `Entered group`, which is selection state, and not off any pointer behaviour, so ADR 0017's separation of participation and reachability holds.

## Locking

ADR 0017 says a group's locked state is "derived from its members exactly as its bounds are", and nothing says how. **A group is locked when every member is, recursively.**

Any-member was rejected. Locking one background image inside a group of ten shapes would stop the whole group taking primary presses, so it could not be double-pressed into, and the user would need the keyboard or a secondary press to reach nine unlocked shapes.

Entering then needs no rule of its own:

- A primary press on an unlocked member's region selects the group, and a double-press enters it. A locked member's region does not participate, as today.
- Inside the scope, locked members behave exactly as locked top-level entities do: primary presses and the marquee skip them, a secondary press reaches one, and Unlock works on it.
- A fully locked group is entered the way any locked entity is reached, by `Enter` on its tab stop or by a secondary press and the menu.

What a rigid-body move of a partly-locked group does at the top level is handed on, not settled here.

## Drawing it

The `Entered group` draws a **dashed outline** around its `EffectiveBounds` in `--d12-muted-text` (ADR 0012), beneath the selection chrome and not part of it. It is quieter than the selection outline because it shows context, not the target, and reading `EffectiveBounds` keeps it on the live geometry while a member moves (ADR 0020).

Only the innermost entered group is drawn, so stepping out moves the outline up a level and the user can watch it happen. Content outside the scope keeps full opacity: dimming it would need theme work on every built-in, and a press out there exits anyway, so there is nothing to warn the user off.

It is a new visual state, so it gets a Demo page and a Playwright case, in the shape ADR 0025 sets.

The property bar and panel are untouched. They follow the selection, which already holds the right ids.

## What this amends

**ADR 0006's deferral is resolved** by this ADR. Its rule that a press on any member selects the whole group still holds whenever that member is not addressable.

**ADR 0014 is amended in one word**: its actions act on top-level entities, which now means members of the `Entered group` when one is entered.

**ADR 0017 is amended in two places.** A group is locked when every member is. And "no selection state is mirrored into JavaScript" narrows to "JavaScript holds no copy of selection state; it reads only what C# has rendered", with the addressability marker as the second thing it reads after `Locked`.

**ADR 0018 is amended by addendum**: a double-press on a member of a group that is not entered enters it, and Escape gains a meaning between cancelling a gesture and clearing the selection.

**ADR 0022 is amended in one cell's scope**: primary on `author-content` is `Native` only when its instance is addressable. Otherwise the press classifies as `instance`.

**ADR 0026 gains one row**: `Enter` on a group's tab stop enters the group.

**ADR 0003, ADR 0004 and ADR 0007 are untouched.** Nothing new is persisted, the commands gain an edit rather than the set gaining a command, and one history entry still covers one gesture.

## Considered and rejected

- **Direct reach with a modifier**: no marquee or multi-select among members without a held modifier, no place for the keyboard to stand, and no answer for nesting short of a second modifier.
- **Entering straight to the leaf**: the scope becomes a path, and a nested group the user built is skipped past rather than shown.
- **Leaving `author-content` live inside an unentered group**: a double-press on one sticky note does two different things depending on where it lands.
- **Member ids in the selection with no scope**: a later `Ctrl+G` creates a second group overlapping the first.
- **Empty canvas inside the group's bounds exiting the scope**: there would be no pointer route to a marquee over members, which is most of what aligning within a group needs.
- **Escape clearing an in-scope selection before exiting**: one more press for no gain once the scope is drawn.
- **A focus trap inside the entered group**: the keyboard would need an exit rule the pointer does not have.
- **A group locked when any member is**: one locked member freezes all its neighbours to the pointer.
- **Dimming content outside the scope**: theme work on every built-in to warn the user away from presses that exit anyway.
- **"Focused group", "group scope", "edit mode" or "isolation mode" as the name**: focus means DOM focus throughout this repo, "scoped" already means something else in ADR 0026, ADR 0035 deleted edit mode, and this is not a mode under ADR 0009.
