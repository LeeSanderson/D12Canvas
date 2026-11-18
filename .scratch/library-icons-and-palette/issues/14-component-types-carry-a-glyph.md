# 14. Component types carry a Glyph

**What to build:** a component type's icon becomes a `Glyph`, so no emoji or OS-dependent character can reach the board. The built-in Rectangle, Sticky note, Text and Image entries draw their library glyphs on the Palette and on the zoomed-out LOD placeholder. A host type with no icon shows the generic Component glyph in both places. The image placeholder shows the Image glyph above "No image" or "Image unavailable". The Palette keeps today's layout in this ticket; only its icons change.

**Blocked by:** 13 (Glyph set, D12Icon and the glyph gallery)

**Status:** resolved

- [x] `ComponentRegistrationBuilder.Icon` and `ComponentRegistration.Icon` are `Glyph?`; no string overload or fallback remains
- [x] The four built-ins use `Glyphs.Rectangle`, `Glyphs.StickyNote`, `Glyphs.Text` and `Glyphs.Image`
- [x] The Demo's two custom types use glyphs (an existing library glyph or one built from path data in the Demo)
- [x] The Palette and the LOD placeholder draw the registration's glyph through `D12Icon`, decorative, and draw `Glyphs.Component` when `Icon` is null, with no branch for a missing icon
- [x] The image placeholder draws `Glyphs.Image` above its text for both the empty and the unavailable state; there is no broken-image variant
- [x] bUnit: a null `Icon` draws `Glyphs.Component` on the Palette and the LOD placeholder; a host type's glyph shows on the Palette; the built-in and options tests assert glyphs, not strings
- [x] The full visual suite runs in the pinned container with `-parallel none`; Palette, LOD placeholder and image placeholder baselines move in this commit and nothing else moves

## Comments

- The Palette and the LOD placeholder draw `ComponentRegistration.IconOrDefault`, an internal `Icon ?? Glyphs.Component`, so neither site branches on a missing icon.
- The Demo note uses `Glyphs.StickyNote`, the same glyph as the built-in Sticky Note; the Stress Item uses a 2x2 grid glyph built from path data in the Demo.
- 46 screenshot cases moved (92 files). 39 of them are Demo pages that include the Palette; the rest are the LOD placeholder (plus two pages whose zoomed-out shapes render as placeholders) and the image placeholder routes. Every HTML diff is only the icon markup. Three dark-board baselines (image placeholder on a dark board, LOD dark, two themes) also pick up the dark canvas frame as `#444` instead of `#ccc`. That frame value predates this slice; the old baselines passed because the fuzzy comparer let a 1px border through.
- `ComponentTestBase.StyleBlockText` now joins every `<style>` block, because each `D12Icon` brings its own and the first block is no longer the component's.
- Still open from ticket 13: every `D12Icon` emits its own `<style>` copy, now once per Palette entry and LOD placeholder.
