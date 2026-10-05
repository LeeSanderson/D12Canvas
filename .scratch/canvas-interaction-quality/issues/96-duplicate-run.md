# 96 — Duplicate and the duplicate run

**What to build:** Ctrl+D duplicates without touching the clipboard, and a second Ctrl+D repeats the offset the first copy was moved to, so an end user lays out a row by duplicating, nudging once, then duplicating again (ADR 0039). `Duplicate run` is transient canvas state: while the selection is exactly what the last duplicate produced, the next lands at that selection's committed offset from its source, top-left to top-left, replayed unsnapped. The first step is +20,+20 board units. Any selection change ends the run. Ctrl+D is a keydown row and Duplicate is an object-menu row. The copy uses ticket 95's fragment builder, so a lone edge and a locked entity (once ticket 100 lands) duplicate the same way they copy.

**Blocked by:** 95 (System clipboard)

**Status:** ready-for-agent

- [ ] Ctrl+D with something copied earlier leaves the clipboard untouched
- [ ] Ctrl+D, drag the copy somewhere, Ctrl+D again lands the third at the same offset again, even off-grid
- [ ] Clicking elsewhere and pressing Ctrl+D starts a fresh run at +20,+20
- [ ] The duplicate is selected and focus does not move to it (no inline edit opens)
- [ ] The Duplicate menu row works and shows its chord
- [ ] Pure C# tests for the run's offset rule; bUnit for the keydown and menu routes
- [ ] `CONTEXT.md`'s `Duplicate run` term describes what shipped
