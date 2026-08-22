# Selecting inside a Group

Type: grilling
Status: open

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
