# Layered testing strategy (unit + visual verification)

Type: grilling
Status: resolved
Blocked by: 03

## Question

Given the tooling options surfaced by ticket 03, decide the layered testing strategy for D12Canvas going forward: what stays bUnit-based unit/component testing, what becomes visual/render verification (and with which tool), and what's the expectation per future ticket (e.g., "any ticket touching rendering must add a visual-verification case")? The current strategy (bUnit-only) is judged insufficient for a visual, interactive canvas tool.

## Answer

Adopts ticket 03's recommendation as-is: **Playwright for .NET** is the visual-verification layer, no further re-litigation of the tool choice.

- **Split**: bUnit stays the default for anything expressible without real rendering — component logic, markup structure, event wiring, state transitions, parameter binding. Playwright is reserved specifically for rendered visual output bUnit can't see: pixel layout, CSS-driven positioning/sizing, and mid-interaction frames (drag/resize/pan/zoom). Default to bUnit; escalate to Playwright only when the thing under test is inherently visual/rendered.
- **Initial baseline set** (now, nothing more): rendered nodes/edges, a zoomed/panned view, a mid-drag frame, a mid-resize frame. Everything else (selection, connectors, palette, tool modes) doesn't exist as a locked design yet, so there's nothing stable to baseline — each earns its own screenshot case once its own ticket resolves and ships.
- **CI**: wired in from day one via the official `mcr.microsoft.com/playwright/dotnet` Docker image, used both to generate baselines and to run CI diffing — this is what neutralizes cross-platform font/rendering drift; running only locally on ad hoc dev machines would reintroduce that noise.
- **Baseline updates**: when an intentional visual change breaks the diff, the developer regenerates snapshots locally using the same Docker image and commits the updated `.png` files in the same PR. Ordinary PR review is the approval gate — no separate baseline-approval tooling.
- **Rule for future tickets**: any ticket that introduces or changes a rendered visual state on the canvas must add or update a Playwright screenshot case. Purely internal tickets (data shape, serialization, non-visual state logic) don't need one — unless their resolution introduces a new visual state as a side effect (the selection-model ticket almost certainly will, since "selected" is a visual state; the persistence-format ticket almost certainly won't, since format ≠ rendering). The test is "does this render something new," not "which ticket number is this."
