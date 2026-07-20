# Layered testing strategy (unit + visual verification)

Type: grilling
Status: open
Blocked by: 03

## Question

Given the tooling options surfaced by ticket 03, decide the layered testing strategy for D12Canvas going forward: what stays bUnit-based unit/component testing, what becomes visual/render verification (and with which tool), and what's the expectation per future ticket (e.g., "any ticket touching rendering must add a visual-verification case")? The current strategy (bUnit-only) is judged insufficient for a visual, interactive canvas tool.
