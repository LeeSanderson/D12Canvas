# A secondary press on author content is decided once at press, by a fixed order of five rules, and an author can mark a region either way

A secondary press on `author-content` goes either to the browser, which shows its own menu, or to the canvas, which pans or opens the object menu. That verdict is decided once, at `pointerdown`, on the press target, and kept until that press's `contextmenu` event arrives. The first matching rule wins:

1. The instance is not addressable, because it sits inside a group that is not the `Entered group` → **canvas**
2. The target is editable, or a text selection is live inside the content → **browser**
3. The nearest ancestor carrying `data-d12-context-menu` → its value, `browser` or `canvas`
4. The target or an ancestor is `a[href]`, `video` or `audio` → **browser**
5. Otherwise → **canvas**

The walk runs from the press target up to the instance boundary. A `browser` verdict starts no gesture and suppresses nothing. A `canvas` verdict starts `Pan` in `pointing`, suppresses the browser's menu, and opens the object menu on a sub-threshold release, exactly as ADR 0022 describes for every other role.

This closes the gap ADR 0023 left open. Its narrowing gave the browser menu to editable targets and live text selections, and handed everything else to the object menu, which took "open in new tab", "save video" and "loop" away from every author who embedded a link or a media element on purpose.

## Inference and a marker, the same shape as ADR 0017

ADR 0017 already classifies `author-content` two ways: inference over natively-interactive elements, and an opt-in marker for what inference cannot reach. It rejected markers with no inference because they break the first case an author hits. This decision uses the same shape for the same reason.

**The inferred set is `a[href]`, `video` and `audio`.** Each has browser menu items available nowhere else, and none is embedded as decoration.

**`<img>` is left out deliberately.** The built-in `Image` shape is a bare `<img>` filling the whole instance, so inferring `<img>` would give every Image instance the browser's menu instead of the object menu. That is the sticky-note failure ADR 0023 fixed, on a second built-in. An author who wants a saveable image marks it.

## A separate marker, scoped to the secondary button

`data-d12-context-menu` is separate from ADR 0017's marker. ADR 0017's marker says a primary press belongs to the author. This one says who owns a menu request. The cases that need them do not overlap:

- a saveable `<img>` still wants a primary press to drag the shape
- an author's `<div>` button wants its click handler and has no browser menu worth keeping

One marker for both would give each of those authors a side effect they did not ask for. This marker changes nothing on the primary or middle button.

**It takes a value.** `browser` adds an element inference misses. `canvas` removes one inference caught: an author's bookmark card that is a single `<a href>` filling the instance would otherwise never show the object menu, and no author could fix it.

## The order is fixed, and two rules sit above the marker

**Editable targets and live text selections always go to the browser.** These two are not guesses about whether the browser menu is useful. When the user is typing or has text selected, the browser menu is what they asked for, and an author cannot know better at authoring time. So a `canvas` marker on a wrapper does not take spellcheck away from a `<textarea>` inside it. The marker competes only with the element-kind inference. An author who wants the object menu over a textarea still has `Shift+F10`.

**Addressability sits above everything.** ADR 0044 classifies `author-content` in an unentered group as `instance` for the primary button, because the press addresses the group. A member that is not addressable owns neither button, so a secondary press there opens the group's object menu. Editable and live selection cannot fire there anyway: under ADR 0044 a member's control in an unentered group cannot take focus and its text cannot be selected. The walk reads the addressability marker ADR 0044 already renders.

## One evaluation, at press, kept until its `contextmenu`

ADR 0022 had the `contextmenu` listener classify its own `event.target`. That is a second hit test, and the platforms disagree about where it happens:

- **macOS and Linux** fire `contextmenu` on press, so its target is the press target
- **Windows** fires it after `pointerup`, at whatever sits under the release point

On Windows the two evaluations can disagree in both directions. A right-click on a link that wobbles 3 pixels off it gets **no menu at all**: the press went to the browser, so the canvas started no gesture, and the release target is plain instance, so the browser menu is suppressed. A right-drag pan released over a `<video>` gets **the browser menu at the end of a pan**. The second happens today without this decision: ADR 0023's editable rule produces it whenever a pan ends over a `<textarea>`.

So the verdict is computed once, at `pointerdown`, and stored. **The first `contextmenu` after the press uses it up.** One value, keyed by pointer and button and replaced by every secondary press, is not the free-floating flag ADR 0017 refused when it deleted `wasPortDragging`. That flag leaked across gestures. This one cannot, because each press overwrites it and each `contextmenu` clears it.

Two `contextmenu` events have no press in front of them:

- **The keyboard.** The ContextMenu key and `Shift+F10` fire `contextmenu` on the focused element. With no stored verdict, the same five rules run on that target. A focused `<textarea>` gets spellcheck, and a focused instance tab stop gets the object menu.
- **A consumed dismissal.** ADR 0023 swallows a press in the capture phase while a menu is open, so the classifier never sees it. That press stores a `canvas` verdict, since the canvas did take it. Without this, a dismissing right-click on Windows would read whatever an earlier press left behind and could open the browser menu.

## Mixed cases have one answer

The rules decide who owns the press, not what the menu contains. A link inside an editable region gets `browser` from two rules at once, and the browser combines link items and spellcheck into one menu itself. The only real conflict was an author's intent against the inference, and the marker's position in the order settles it.

## Platform facts this rests on

Two claims about the browser are asserted by an `Interaction probe` rather than reasoned, per ADR 0025:

- on Windows, `contextmenu` fires after `pointerup` and targets the element under the release point
- whether `preventDefault` on the `Shift+F10` keydown stops `contextmenu` firing, per engine

If the first is wrong, the stored verdict is harmless: it agrees with a second evaluation that would have agreed anyway.

## What this amends

**ADR 0022 is amended in two places.** The `author-content` / Secondary cell is these five rules. And native-menu suppression stops classifying from the `contextmenu` event's own target, because on Windows that target is the release point.

**ADR 0023's open question is closed.** Links and media get the browser menu by inference, and an author's marker covers the rest. Its narrowing to editable and live selection survives as rule 2.

**ADR 0017 is amended by addendum.** `author-content` gains a second author-facing marker, scoped to the secondary button and keyboard menu requests.

**ADR 0018 is amended by addendum.** The fifth synchronous decision, native-menu suppression, is now taken at `pointerdown` like the other four and kept until its `contextmenu`, rather than being taken on the `contextmenu` event.

**ADR 0044's addressability rule gains a second reader.** Nothing in it changes.

## Considered and rejected

- **Pure enumeration with no marker**: wrong in both directions. A decorative `<img>` gets a save menu, and an author's custom control can never get the browser's.
- **Pure delegation with no inference**: ADR 0017's own reason for rejecting it. An author's link or video, the case they hit first, silently loses its menu.
- **Inferring `<img>`**: takes the object menu off every built-in Image instance.
- **Reusing ADR 0017's marker for both jobs**: a saveable image stops dragging and a custom button loses its object menu.
- **A one-way opt-in marker**: leaves an author whose component is one big link with no way to get the object menu back.
- **Letting the marker override editable and live selection**: an author's wrapper silently removes spellcheck from a textarea the user is typing in.
- **Ignoring addressability on the secondary button**: a member that cannot be clicked could still be right-clicked into the browser's menu, and the two buttons would disagree about whether the group is the thing addressed.
- **Two evaluations, at press and at `contextmenu`**: on Windows they read different targets, which produces a press with no menu and a pan that ends in one.
- **Treating the dismissing press as having no verdict**: it would read a stale one.
