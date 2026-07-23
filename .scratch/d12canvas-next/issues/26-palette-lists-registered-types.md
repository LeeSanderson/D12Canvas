# 26 — Palette lists registered types

**What to build:** An end user sees what they can place: the palette — canvas chrome, not board content — lists every registered component type with its icon and display name, grouped by category. It renders standalone, is reference-wired to the canvas, and is positioned entirely by the host's own CSS outside the pannable surface. (ADR 0002.)

**Blocked by:** 20 (Component-type registration contract & registry)

**Status:** ready-for-agent

- [ ] Every registered type appears with `DisplayName` and `Icon`, grouped by `Category`
- [ ] The palette renders standalone and is positioned by host CSS — it does not pan or zoom with the board
- [ ] Palette entries carry accessible names
- [ ] bUnit coverage; screenshot case for the rendered palette
