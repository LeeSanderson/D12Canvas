# Selecting inside a Group

Type: grilling
Status: resolved

## Question

Decide how a user addresses one member of a `Group` without ungrouping it, and what that then makes possible.

Graduated from the map's fog while resolving ADR 0022, which supplied the last piece the question was missing. ADR 0006 punted this to implementation time; ADR 0013 removed the "select all" half of the old fog patch by taking top-level entities only.

Three inputs now exist that did not:

- **ADR 0018** carries a **press count** on the hit-target classification, so a double-press has somewhere to be expressed and dispatches from the same place as everything else. No `@ondblclick` binding survives on board content.
- **ADR 0022** fixed that membership is tested on the **outermost containing entity**, via the `EffectiveSelectionId` this codebase already has. That is precisely the rule a click-through must override, so the override has one named place to live rather than being a special case scattered across the selection paths.
- **ADR 0017** separated pointer participation from keyboard reachability and made a `Group` collapse to one tab stop, so whatever this decides for the pointer has to be answerable for the keyboard too, and cannot be keyed off pointer behaviour.

Decide:

- **The gesture.** Double-press is the obvious candidate (all four reference tools use it or an accel-press). tldraw models it as a *focused group* the selection is temporarily scoped inside, exiting on Escape or a press outside; Figma and Miro use a modified press to reach a nested layer directly with no scope state. These differ in whether anything persists after the press, which matters because ADR 0009 has no persistent tool modes and ADR 0018 makes a pointer gesture unable to outlive a press. A scope that survives the release is neither, so it needs a name and a home or it needs rejecting.
- **What the selection then holds.** `_selectedInstanceIds` holds top-level ids only, and ADR 0013 relies on that. A member id sitting in it "naked" is exactly what `UpdateMarqueeSelection`'s existing comment warns about, since a later `Ctrl+G` would create a second overlapping group. So either the invariant is relaxed with a stated consequence for every reader of the selection, or the scope is tracked separately from the selection.
- **Aligning within a group**, which ticket 12 attached to this question. ADR 0014 acts on top-level entities and treats a group as one rigid body, so there is currently no way to address its members at all. Confirm whether this gesture is sufficient to unlock that, or whether align needs its own notion of a scope.
- **The keyboard route**, per ADR 0017's separation rule and ADR 0010's single tab stop per group. Whatever the pointer does, a keyboard user needs an equivalent, and `Ctrl+Tab` plus `Space` already exists as the keyboard multi-select mechanism.
- **How it composes with locking.** ADR 0017 derives a group's locked state from its members. Entering a partly-locked group is a case that has no answer yet.
- **What Escape does**, given ADR 0018 made Escape "cancel the active pointer gesture, otherwise clear the selection". If a scope exists, Escape gains a third meaning and the precedence needs stating.

Confirms or amends ADR 0006's deferral. May amend ADR 0014.

## Answer

Recorded as [ADR 0044](../../../docs/adr/0044-entered-group.md).

**Entering leaves a scope, the `Entered group`,** held beside the selection as transient view state. It is not a tool mode under ADR 0009 and not a pointer gesture under ADR 0018. It is the same kind of thing as the selection, which also outlives every press. Direct reach with a modifier was rejected because it gives no marquee among members, no place for the keyboard to stand and no answer for nesting.

**One level per entry.** Groups nest, so a double-press on a member of G enters G and selects the member of G under the pointer, which may be a nested group H. Another double-press enters H. The scope is always one group id, never a path.

**Content responds only when its instance is addressable**, meaning top-level or a direct member of the `Entered group`. Elsewhere `author-content` classifies as `instance`, so on a grouped sticky note the first double-press enters the group and the second edits the text. C# renders a marker on each container that is not addressable, and ADR 0017's walk reads it. **Accepted cost:** every control inside an unentered group stops responding to a press. The rule "no selection state is mirrored into JavaScript" narrows to "JavaScript holds no copy of selection state; it reads only what C# has rendered", as it already does for `Locked`.

**The selection holds siblings**: direct members of the `Entered group`, or top-level ids when none is entered. `EffectiveSelectionId` resolves to the direct member of the scope. `Ctrl+G` inside a group nests the new group in it, and ungroup splices the members back into the parent. Both commands gain a parent-membership edit in one history entry, which also stops ungrouping a nested group from leaving a dangling id in its parent.

**Leaving:** Escape cancels a gesture, otherwise steps out one level and selects the group just left, otherwise clears the selection. A press on an entity outside pops the scope until the target is inside it. A press on empty canvas pops until the point is inside the scope's bounds: inside, a click clears the in-scope selection and a drag draws a marquee over members, and outside it exits. The scope pops if its group stops existing.

**Keyboard:** `Enter` on a group's tab stop enters it and focuses the first member. No focus trap: Tab out counts as focus landing outside. Escape returns focus to the group's tab stop. `Ctrl+Tab`+`Space` work over the members unchanged. Both focus writes fit ADR 0036's command-handoff category.

**Locking:** a group is locked when every member is, which nothing had stated. Inside, locked members follow the existing rules. Moving a partly-locked group at the top level is handed on to [Moving a partly-locked group](53-moving-a-partly-locked-group.md).

**Align** works on siblings, so entering is enough and ADR 0014 changes one word.

**Drawn** as a dashed `--d12-muted-text` outline on the innermost entered group, with a Demo page and Playwright case.

Resolves ADR 0006's deferral. Amends ADR 0014, ADR 0017 (lock derivation, JavaScript rule), ADR 0018 (double-press meaning, Escape), ADR 0022 (`author-content` primary cell) and ADR 0026 (`Enter` on a group stop). `CONTEXT.md` gains `Entered group` and updates `Selection`, `Hit target` and `Locked`. Updates added to [Reaching a buried instance](37-reaching-a-buried-instance.md) (double-press now descends a level) and [A Group left referencing deleted members](48-group-referencing-deleted-members.md) (deleting a member is now an ordinary action).
