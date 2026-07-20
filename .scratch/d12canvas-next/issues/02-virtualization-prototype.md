# Virtualization/windowing mitigation stress test

Type: prototype
Status: open
Blocked by: 01

## Question

Given the performance ceiling and candidate mitigations surfaced by ticket 01, build a rough, throwaway prototype that stress-tests the most promising mitigation (e.g., viewport-based virtualization/windowing of off-screen components) against a large synthetic set of components on the existing `DiagramCanvas`. Does the mitigation actually hold frame rate/responsiveness at the target scale? What does it demand of the state/data model (e.g., spatial indexing, viewport-aware queries) that the state/data model ticket needs to account for?
