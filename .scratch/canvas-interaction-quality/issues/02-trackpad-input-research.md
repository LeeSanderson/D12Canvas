# What the browser actually delivers for trackpad input

Type: research
Status: open

## Question

Establish, from primary sources, what a browser actually delivers to a Blazor `@onwheel` / pointer handler for trackpad input — so the arbitration model (01) and the pan/zoom work are designed against real event streams rather than assumptions.

Today `DiagramCanvas` binds `@onwheel="HandleMouseWheel"` and treats every wheel event as zoom. `ctrlKey` is read in `DiagramCanvas.razor.js` only for keyboard shortcut matching; nothing distinguishes a pinch from a scroll, and there is no two-finger-pan or momentum handling anywhere.

Find out:

- How a trackpad pinch is reported. The convention is a `wheel` event with `ctrlKey: true` synthesised by the OS/browser — confirm this is real and current, and establish how it is distinguished from a genuine Ctrl+scroll by a mouse user.
- How two-finger pan is reported, and how `deltaX`/`deltaY`/`deltaMode` differ between a discrete mouse wheel and a continuous trackpad surface.
- Whether momentum/inertia scrolling is distinguishable from user-driven scrolling, and whether there is any reliable "gesture ended" signal.
- Differences across Chrome, Firefox and Safari, and across Windows and macOS. The repo's Playwright rig runs headless Chromium in Docker, so anything that only reproduces elsewhere needs flagging as untestable in CI.
- Whether Pointer Events, `wheel`, or the non-standard `gesturestart`/`gesturechange` family is the right foundation — including what each costs in browser support and what each implies for the touch non-foreclosure constraint in the map Notes.
- Whether `preventDefault` on `wheel` requires a non-passive listener, and what that means for a Blazor `@onwheel` binding specifically (this may force JS interop rather than a Razor binding).

Capture findings as a markdown file in the repo and link it from this ticket.
