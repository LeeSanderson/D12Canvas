# Viewport inset for host-placed chrome

Type: grilling
Status: open

## Question

Decide whether `DiagramCanvas` learns which part of its container is occluded by chrome, or whether occlusion stays entirely the host's problem.

ADR 0015 frames content into the **full container rect**: zoom-to-fit centres board content in the viewport, inset by a fixed 0.9 fraction, and knows nothing about what is drawn on top of that viewport. ADR 0002 is why — canvas chrome is a standalone component the host places with its own CSS, deliberately given no `Position` parameter, so `DiagramCanvas` is ignorant of the palette by construction and of anything else a host floats over it.

Those two decisions collide in a real deployment. `D12Canvas.App`'s board editor puts a fixed 220px palette rail beside the canvas today, but a host that floats it *over* the canvas — which ADR 0002 explicitly permits — gets a fit that centres content underneath it. ADR 0015 added the minimap as a second occluder, and the selection-anchored property bar will add a third. No margin constant fixes this: the 0.9 inset is symmetric and the occlusion is not, so loosening it hides the problem in the common case while still cropping content behind a wide rail.

The same blind spot affects more than framing. ADR 0009's click-to-add places a new instance at the **viewport centre**, which is the same rect and the same lie; so is the paste anchor's viewport-centre fallback (ADR 0013), and so is the empty-selection case of any future centre-on-something behaviour. If an inset exists, the question is which of these read it.

Decide:

- **Whether an inset exists at all**, or whether a host that floats chrome accepts the consequence. "The host's problem" is a defensible answer — the host is the only party that knows its own layout — but it currently has no mechanism to act on it, so choosing it means saying that out loud rather than by omission.
- **If it exists, what shape it takes.** Four edge insets in CSS pixels as a parameter is the obvious form; a `RenderFragment`-free "reserved region" list is more general and probably more than anything needs. Whether it is one parameter or per-edge matters less than whether the value is a host-supplied constant or something measured from the DOM.
- **Whether it is measured rather than declared.** Chrome sizes are not constants — a palette rail collapses, a property bar's height depends on the selection. Measuring occluders means a JS round trip per layout change and a way to nominate which elements count, which is a substantially bigger mechanism than a declared inset and reintroduces exactly the container-size ordering hazard that produced this codebase's worst gesture leak.
- **Which behaviours read it.** Framing certainly; click-to-add and the paste anchor arguably, since a new instance placed under the palette is the same defect one layer down. Applying it to some and not others needs a stated reason.
- **Whether the minimap is a special case.** It is the one occluder the library itself introduces, so unlike the palette it *could* be known without a host parameter — which is either a useful shortcut or an inconsistency that makes the general mechanism harder to explain.

Answerable now; nothing blocks it. The property bar will add a third occluder and its geometry may sharpen the "measured versus declared" question, but the decision does not wait on it.
