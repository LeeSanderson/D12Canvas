# 62 — Selection context menu

**What to build:** An end user right-clicks a selection and gets a context menu mirroring the baseline shortcut actions — delete, group/ungroup (as applicable), and the four layering commands — invoking exactly the same commands the shortcuts do. Right-clicking empty canvas shows no custom menu. The menu is keyboard-operable. (ADR 0009.)

**Blocked by:** 34 (Delete selection), 44 (Group/ungroup lifecycle), 60 (ZIndex commands + new-on-top)

**Status:** ready-for-agent

- [ ] Right-click on a selection opens the menu with delete, group/ungroup, and layering actions
- [ ] Group appears only for 2+ selections; ungroup only when a group is selected
- [ ] Menu actions invoke the same undoable commands as their shortcuts
- [ ] No custom menu appears on empty canvas
- [ ] The menu is keyboard-navigable and dismisses on Escape
- [ ] Screenshot case for the open menu
