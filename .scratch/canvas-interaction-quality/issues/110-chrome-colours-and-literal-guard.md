# 110 — Canvas and container chrome colours with the literal guard

**What to build:** Every visual the library paints on the canvas and the container reads a token, and a guard test keeps it that way (ADR 0063). The token boundary is who renders the pixels. New tokens land here or are confirmed where earlier tickets added them: `--d12-inline-edit-outline` (the editor outline currently vanishes on dark), `--d12-canvas-frame` as the escape hatch for the container border (the canvas's `--d12-border` is the grid-line colour), `--d12-shadow` gaining a dark value on the menu, the port fill reading `--d12-connector-preview`, and the selection chrome (selection box, handles, drag-over affordance, floating-endpoint ring exceptions aside) reading `--d12-accent`. The accent sweep moves two near-miss blues and breaks the byte-identical-light convention for about twenty-six baselines because nobody chose those blues. The orange on ports and floating endpoints and the white ring stay fixed by decision. A guard test asserts every colour literal in the canvas and container style blocks is a token declaration or on the fixed list. This ticket is last of ADRs 0016, 0041 and 0063 to land, so the guard ships here.

**Blocked by:** 92 (Border partition and ports on selection), 109 (Property bar and chrome suppression while judging)

**Status:** ready-for-agent

- [ ] The inline editor's outline is visible on a dark board; the container border is overridable through its own token
- [ ] Selection box, handles, marquee and drag-over affordance all read the accent in both themes
- [ ] The port and floating-endpoint signal colours are unchanged
- [ ] The guard test enumerates every colour literal in the two style blocks and fails on one that is neither a token declaration nor on the fixed list
- [ ] The README's token table lists every new token from this feature
- [ ] About twenty-six light baselines move by the accent sweep and no others; full visual suite run in the pinned image with `-parallel none`; baselines folded into the commit
- [ ] `CONTEXT.md`'s `Theme token` term describes what shipped
