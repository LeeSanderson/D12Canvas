# 21. Property bar lock states

**What to build:** the `Property bar` tells the user about locked shapes. A partly locked selection ends the bar with a lock glyph and a count such as "1/3", whose tooltip reads "1 of 3 locked; changes skip them". A fully locked selection no longer hides the bar: it stays where it would stand and holds a single Unlock button that unlocks the whole selection in one undo step.

See the spec's "Property bar" section.

**Blocked by:** 19 (Property bar value chips and choice flyout)

**Status:** resolved

- [x] A partly locked selection ends the bar with a divider, `Glyphs.Lock` and "N/M"; it isn't a button; the library tooltip above it reads "N of M locked; changes skip them"
- [x] The count appears only for a partial lock, the same rule as the panel's partial-lock line
- [x] A fully locked selection shows the bar in its usual place with only an Unlock button (`Glyphs.Unlock` and the word) and no role chips
- [x] Unlock unlocks the selection in one history entry, as the `PropertyPanel`'s Unlock does
- [x] bUnit: the count's partial, full and none cases; the lone Unlock button; one undo restores the lock
- [x] New screenshot cases: the partly locked bar and the fully locked bar, each in light and dark; the existing locked-shape case moves
- [x] The full visual suite passes with baselines updated in this commit

## Comments

- `LockTally(Locked, Total)` in `D12Canvas.Panel` holds the rule: `IsPartial` is some but not all locked, `IsFull` is all of at least one. It also gives the "1/3" text and the "1 of 3 locked; changes skip them" summary. The bar counts the expanded selection's instances plus its selected edges. The panel's `AllLocked` now reads `IsFull` from it, and ticket 18's partial-lock line should use `IsPartial` and `Summary`. It is public because `PropertyBar` takes it as a parameter (`Locks`), next to a new `OnUnlock` callback that `DiagramCanvas` wires to `OnUnlockPressed`.
- The lock count is a `role="img"` span named by the summary. It isn't a bar control and takes no focus, so its tooltip shows on hover only.
- A fully locked selection still gets no bar when its targets share no role, for example an instance and an edge together. The bar shows only where a bar with chips would stand.
- Unlock pressed from the keyboard (a click with `Detail` 0) moves focus to the first chip, because the button is replaced and focus would otherwise drop to the page. A pointer press leaves focus alone, since focus in the bar keeps the selection chrome hidden.
- Baselines: the four new cases are on the locking demo, partly locked (1/2) and fully locked, light and dark. The existing locked-shape case didn't move: it has the context menu open, which hides the bar. No other baseline moved. The `MultiSelectionMoveResize` `aria-selected` noise didn't appear.
