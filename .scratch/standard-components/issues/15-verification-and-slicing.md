# Verification and slicing

Type: grilling
Status: open
Blocked by: 01, 02, 03, 04, 05, 06, 07, 08, 09, 10, 11, 13, 14
Map: [Better standard components](../map.md)

## Question

How is the spec sliced for `/implement`, and how is each slice verified?

Decide:

- the slice order and which slices can run in parallel
- which behaviour bUnit covers, which Playwright probes cover (drag, fit measurement, editor round trip), and which new screenshot cases each slice adds in light and dark
- which existing baselines each slice expects to move
- tests for the markdown parser and serialiser, including the C# and JS sides agreeing
