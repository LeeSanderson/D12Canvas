# 67 — Property roles replace shared tags

**What to build:** A component author tags a props property with a `PropertyRole` from a closed, library-owned set (`Fill`, `Stroke`, `StrokeWidth`, `TextColour`, `FontSize`, `FontWeight`, `TextAlign`, plus `EdgeRouting`, `EdgeSourceArrow`, `EdgeTargetArrow`, `EdgeColour` reserved for edges) instead of a free-string shared tag (ADR 0021, registration half). Each role declares the `EditorKind` and CLR type it expects, so registering a property whose kind or type does not match its role fails at startup with a registration error naming the property. The property panel's cross-type merging keys on role rather than tag, so a cross-type multi-selection shows exactly the rows whose roles every selected type declares.

The bar itself (ticket 109) is not built here. This ticket is the prefactor that makes it possible: the role is both what admits a property to the bar and what merges it across types.

**Blocked by:** None — can start immediately

**Status:** resolved

- [x] `SharedTag` is gone from the attribute, the builder and the editable-property record; `PropertyRole` replaces it everywhere
- [x] Registering a property whose `EditorKind` or CLR type disagrees with its role throws at registration, naming the type and property
- [x] The built-in Text, Sticky Note and Rectangle registrations declare the roles their colour, font and alignment properties play
- [x] A cross-type multi-selection in the panel shows the role intersection, keyed and labelled by role
- [x] The existing shared-tag mismatch tests are rewritten against roles; the validator no longer compares pairs of existing schemas but each property against its own role's declaration
- [x] `CONTEXT.md`'s `Property role` and `Editable property` terms describe what shipped

## Comments

`PropertyRole` is a closed enum of eleven members (the seven author roles plus the four edge roles) and `PropertyRoleDeclarations.For(role)` returns the `EditorKind`, CLR type and panel label each one expects. An author writes `[PanelEditable(EditorKind.Color, PropertyRole.Fill)]`; the attribute's `Role` is a get-only nullable so it stays expressible as a constructor argument (a nullable enum cannot be a named attribute argument, which is why `Role` and `Options` are set two different ways). `EditableProperty` carries `Role` where it carried `SharedTag`. `PropertyRoleValidator.Validate` runs in `RegisterComponent` over the type's final schema, so a builder-supplied override is checked too, and throws `PropertyRoleMismatchException` carrying the offending property and the declaration it missed. It also refuses a type that declares one role on two properties (`PropertyRoleDeclaredTwiceException`), because the role is the key a cross-type row merges and commits on and the second property would be silently unreachable. `SharedPropertyValidator` and `SharedPropertyMismatchException` are deleted. The glossary's `Property role` entry drops its "owns its glyph" clause until the bar ships.

Known limit carried, not introduced: the panel's Dropdown editor commits through `Convert.ChangeType`, which cannot produce an enum, so an author who declares an edge role on their own `EdgeRouting` or `ArrowStyle` property gets a panel row that renders but never commits. Edge rows are produced through `EdgeStyle` by the bar (ticket 109), which is where the edge roles are meant to be used.

The panel's cross-type merge keys on role: the rows are the first selected type's roled properties, in its schema order, kept only when every other selected type declares the same role. The field id is `d12-property-panel-field-{Role}` and the label is the role's declared label. The built-ins now play roles, so a Sticky Note and a Rectangle selected together share a Fill row and a Sticky Note and a Text share Text colour and Font size, where before the panel showed nothing for any cross-type built-in selection.

Glyphs are not declared here; they belong to the bar (ticket 109), and declaring them with no consumer would be speculative. The four edge roles are declared because the decision and the glossary name them as part of the closed set, but nothing produces an edge row yet. The visual suite was run because the cross-type row's id and label changed; it covers same-type selections only, so no baseline moved.
