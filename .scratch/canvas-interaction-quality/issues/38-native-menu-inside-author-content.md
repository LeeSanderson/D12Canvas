# When an author's content owns its own context menu

Type: grilling
Status: open

## Question

Decide how the canvas knows that a secondary press inside an author's component should reach the browser's menu rather than the object menu.

ADR 0022 made the whole `author-content` role `Native` on a secondary press, reasoning that suppressing the browser's menu would cost an author's `<input>` its spellcheck and its own clipboard items. ADR 0023 narrowed that to the cases where the reasoning actually bites: the press target is editable, or a text selection is live inside the content. Everything else in an author's component now opens the object menu, which is what makes a right-click on a sticky note work.

The narrowing leaves a gap it did not close. A component holding an `<a>`, an `<img>`, a `<video>` or an `<audio>` element has a genuinely useful browser menu — open in a new tab, save image, download, picture-in-picture, loop — and none of those targets is editable and none needs a selection. Under ADR 0023 those all get the object menu instead, which is a straight loss of capability for an author who embedded them deliberately.

Decide:

- **Enumerate or delegate.** A small closed set of element kinds the classifier treats as owning their own menu is free for authors and needs no contract, but it is the framework guessing, and the guess is wrong in both directions: a decorative `<img>` inside a shape does not want a save-image menu, and an author's custom control built from `<div>`s might genuinely want one. Delegating instead means an opt-out the author marks on a subtree, which is honest about who knows, at the cost of a new contract and of authors who never discover it. ADR 0017 was pleased to *delete* an author workaround rather than add one, which argues against a marker, but an opt-in capability is not the same thing as a workaround.
- **Whether this is one rule or two.** Suppressing the native menu is ADR 0022's fifth synchronous role-derived decision and the only one on an event other than `pointerdown`; the object menu opening is a release-time outcome. A single predicate has to serve both or they can disagree, and a press that suppressed the native menu and then declined to open the object menu leaves the user with no menu at all.
- **What a mixed case does.** An author's component that is an editable field *and* contains a link, or a media element inside an editable region, has two answers under any enumeration.

Note that the object menu is not fully unreachable even where the native menu wins: every role except `author-content` opens it on a sub-threshold secondary release, so a selected instance's resize handles are a route. ADR 0023 rejected that as the *primary* route on the grounds that nobody discovers it, which is the same objection here, weaker only because an author who embeds a `<video>` has chosen to hand that region over.

Amends ADR 0022's role table and ADR 0023's narrowing, and adds a registration or markup contract if delegation wins.
