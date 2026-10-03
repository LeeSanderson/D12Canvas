# Chrome layout for the acceptance surface

Type: prototype
Status: resolved

## Question

Decide what `D12Canvas.App`'s board editor should look like once every piece of chrome this effort produced has somewhere to live.

Graduated from the map's fog. It waited on the context menu, which is now ADR 0023, and it absorbs a second fog patch on the way out: where a host surfaces the wheel device profile, and whether it should at all.

`BoardEditor` today is a fixed 220px `Palette` rail beside a `DiagramCanvas`, with a header above. It mounts **no** `PropertyPanel` at all, which ADR 0021 identified as the entire cause of the reported "a sticky note's colour cannot be edited" defect: mounting the panel fixes it with no library code. This is also the effort's acceptance surface, so a decision that cannot be judged here has not really been judged.

Three ADRs have deposited chrome and each deliberately declined to place it, because ADR 0002 makes placement the host's CSS:

- **ADR 0015's minimap** is a second host-placed chrome component. That ADR also rejected a zoom control cluster and rejected siting fit and 100% buttons *on* the minimap, on the grounds that every new control is one more thing a host must position.
- **ADR 0021's property bar** needs nothing from the host, being canvas-rendered, but the same ADR keeps `PropertyPanel` as the long-tail surface and it still has nowhere to sit.
- **ADR 0019's `WheelDeviceProfile`** is a parameter with host-owned persistence and no surface anywhere. ADR 0023 explicitly refused it a menu row, drawing the line that the menu may flip state the library already has its own route to flip but does not become the first route to a host-owned preference. So if a user is ever to change it, this ticket is where that happens.

ADR 0023 shrank the patch twice on its way here, which is worth knowing before designing anything: the context menu now carries the snap-to-grid toggle and two of the three viewport commands, so neither needs host chrome, and the property bar is canvas-rendered so it is not something the host places.

Decide:

- **Whether the 220px rail plus header survives** three additions, or whether the editor wants a different arrangement entirely. Prototype it rather than argue it.
- **Where `PropertyPanel` docks**, and whether it is always present or appears with a selection. ADR 0021 kept it as the authoritative long-tail surface beside the transient bar, so it is not optional.
- **Where the minimap sits**, and whether it is always visible. ADR 0015 made its visibility the host's markup rather than board state.
- **Whether the wheel device profile gets a visible control at all.** `Auto` is meant to be right nearly always, so a permanently visible switch is chrome earning its place only on the rare miss, which is the objection ADR 0015 raised against a zoom cluster. A settings surface the user opens is the obvious middle answer and the App has none.
- **Whether the canvas needs to know what occludes it.** [Viewport inset for host-placed chrome](issues/24-viewport-inset-for-host-chrome.md) asks that separately and is answerable independently; this ticket is what will produce the overlapping chrome that makes the answer matter.

Type is `prototype` deliberately. Every question above is a layout judgement, and the map's own note says interaction quality resists paper specification.

No ADR is expected. ADR 0002 already assigns placement to the host, and `D12Canvas.App` is a host; a decision here is about the example app rather than the library. Say so explicitly if that turns out to be wrong, because it would mean ADR 0002's boundary is not carrying its weight.

## Answer

**Full-bleed canvas with every piece of chrome floating over it.** The dev picked this from three variants, built on the real editor: three docked rails, full-bleed with floating chrome, and a palette toolbar with a selection drawer and a status bar. The prototype is on the `prototype/chrome-layout` branch (commit `5b02846`), run with `?variant=A|B|C` on `/board/{id}`.

Per bullet:

- **The 220px rail does not survive.** The canvas takes the whole area under the header, and the header is unchanged.
- **`PropertyPanel` floats top-right and appears only while something is selected.** It leaves when the selection empties, so an empty board shows the palette and nothing else. This alone fixes "a sticky note's colour cannot be edited" (ADR 0021).
- **The minimap floats bottom-right and is always visible** once it exists. ADR 0015 leaves its visibility to host markup, and the App chooses always.
- **The wheel device profile gets a control, but only behind a settings gear** floating bottom-left. It opens a popover with the `Auto`/`Mouse`/`Trackpad` choice. This keeps ADR 0015's objection to permanent chrome that earns its place only on a miss, and still gives the preference a route, which ADR 0023 refused to provide from the menu.
- **The canvas still does not learn what occludes it.** This layout is the one that tests ADR 0033 on contact. The palette and panel sit in corners, so click-to-add's centre stays clear. The real test is ADR 0015's initial fit, which does not exist yet. ADR 0033's revisit section now says so, and its line about the App docking the rail is corrected.

**Folded in now:** the full-bleed body, the floating palette, and the selection-driven `PropertyPanel` in `BoardEditor.razor`. **Not folded in:** the minimap and the settings gear. Neither `Minimap` nor `WheelDeviceProfile` exists in the library yet, so each lands with its own implementation, in the position above.

**No ADR, as the ticket predicted.** ADR 0002's boundary held. Every choice here is the App's own CSS, and nothing in the library had to change to allow any of the three variants.
