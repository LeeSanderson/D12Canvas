# 56 — Property panel + attribute schema + first editors

**What to build:** An end user selects an instance and the property panel — canvas chrome, host-positioned — shows its editable properties, built generically from attribute declarations on the component type's props record (overridable via the registration builder). This ticket ships the panel plus the Text and Number `EditorKind` controls. Each committed edit is one undoable `MutateEntity` gesture and re-renders the instance live. A component's `Text`-type *content* is excluded — that's inline editing's job. (ADR 0008.)

**Blocked by:** 29 (Click select, escape, and `aria-selected`), 37 (History core: undo/redo move & resize), 39 (Rectangle built-in)

**Status:** ready-for-agent

- [ ] Editable properties are declared via attributes on the props record; the registration builder can override the declared schema
- [ ] The panel renders Text and Number controls for the selection's editable properties
- [ ] Committing an edit updates the instance live and records exactly one history entry
- [ ] Content-text fields are excluded from the panel
- [ ] The panel is chrome: standalone, host-positioned, empty-state when nothing is selected
- [ ] bUnit coverage; screenshot case for the populated panel
