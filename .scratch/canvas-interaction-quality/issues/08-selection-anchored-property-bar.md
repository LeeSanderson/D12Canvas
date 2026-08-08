# Selection-anchored property bar

Type: prototype
Status: open
Blocked by: 05

## Question

Decide the shape and placement of a property-editing surface anchored to the selection, replacing or supplementing the docked `PropertyPanel`.

The seed note reports that editing properties — a sticky note's colour, for instance — is not possible, and asks for a bar that pops up above a selected component or group, or below when there is no room above.

The machinery already exists and is substantial: `PropertyPanel` builds editors generically from each component type's `EditableProperty` schema, with the full `EditorKind` set, a `Custom` escape hatch, and cross-type shared-property editing (ADR 0008). What does not exist is any placement other than host-positioned chrome, and `D12Canvas.App`'s `BoardEditor` never mounts it at all — which is why the note reads as "not possible" rather than "badly placed".

Build a prototype and decide:

- Whether this is a new positioning mode on `PropertyPanel`, a distinct component sharing its schema machinery, or a replacement. ADR 0002 keeps chrome separate from board content and positions chrome via the host's own CSS — a selection-anchored bar must track board coordinates through pan and zoom while remaining chrome, which that ADR did not anticipate. Note ADR 0002 is settled, so resolve this within it or establish why it genuinely cannot be.
- Placement rules: preferred side, flip conditions, viewport-edge clamping, and behaviour when the selection is larger than the viewport or partly off-screen.
- Behaviour during a gesture. Does it follow a moving selection live (requires ticket 05), hide for the duration, or freeze? Following looks best and costs the most.
- What it shows for a multi-selection and for a `Group` — ADR 0008 already decided cross-type edits touch only explicitly-tagged shared properties, so the question is presentation, not semantics.
- Whether the docked panel survives alongside it as a host-selectable option, and what `D12Canvas.App` should mount.
- How it interacts with the context menu (10) — both are selection-anchored popovers and must not fight over the same space or the same dismissal gesture.
- Keyboard and focus behaviour: reachability, whether it traps focus, and how Escape is shared with selection-clearing.

Amends ADR 0008.
