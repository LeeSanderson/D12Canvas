# Chrome layout for the acceptance surface

Type: prototype
Status: open

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
