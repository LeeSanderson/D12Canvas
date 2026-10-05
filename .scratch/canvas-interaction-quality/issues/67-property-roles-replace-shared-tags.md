# 67 — Property roles replace shared tags

**What to build:** A component author tags a props property with a `PropertyRole` from a closed, library-owned set (`Fill`, `Stroke`, `StrokeWidth`, `TextColour`, `FontSize`, `FontWeight`, `TextAlign`, plus `EdgeRouting`, `EdgeSourceArrow`, `EdgeTargetArrow`, `EdgeColour` reserved for edges) instead of a free-string shared tag (ADR 0021, registration half). Each role declares the `EditorKind` and CLR type it expects, so registering a property whose kind or type does not match its role fails at startup with a registration error naming the property. The property panel's cross-type merging keys on role rather than tag, so a cross-type multi-selection shows exactly the rows whose roles every selected type declares.

The bar itself (ticket 109) is not built here. This ticket is the prefactor that makes it possible: the role is both what admits a property to the bar and what merges it across types.

**Blocked by:** None — can start immediately

**Status:** ready-for-agent

- [ ] `SharedTag` is gone from the attribute, the builder and the editable-property record; `PropertyRole` replaces it everywhere
- [ ] Registering a property whose `EditorKind` or CLR type disagrees with its role throws at registration, naming the type and property
- [ ] The built-in Text, Sticky Note and Rectangle registrations declare the roles their colour, font and alignment properties play
- [ ] A cross-type multi-selection in the panel shows the role intersection, keyed and labelled by role
- [ ] The existing shared-tag mismatch tests are rewritten against roles; the validator no longer compares pairs of existing schemas but each property against its own role's declaration
- [ ] `CONTEXT.md`'s `Property role` and `Editable property` terms describe what shipped
