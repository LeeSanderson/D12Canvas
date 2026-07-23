# 60 — ZIndex commands + new-on-top

**What to build:** An end user reorders stacking with four commands — bring to front, send to back, bring forward, send backward — each with its keyboard shortcut from the ADR 0009 table and each an undoable gesture. Newly placed instances always appear on top by default. (ADR 0008.)

**Blocked by:** 28 (Click-to-add placement), 37 (History core: undo/redo move & resize)

**Status:** ready-for-agent

- [ ] All four layering commands reorder the selection correctly, including tie handling among neighbours
- [ ] Each command has its baseline keyboard shortcut and is one undoable history entry
- [ ] Newly placed instances receive a `ZIndex` above all existing entities
- [ ] Stacking changes render immediately and persist with the instances
- [ ] Screenshot case for overlapping instances before/after a layering command
- [ ] xUnit coverage of the reordering semantics
