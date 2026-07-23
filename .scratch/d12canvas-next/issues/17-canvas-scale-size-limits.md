# Canvas scale and size limits

Type: grilling

## Question

Is the board's coordinate space bounded or effectively infinite, and what governs the zoom range (min/max scale)? Specifically:

(a) should `Board` enforce a maximum extent on component placement (and on what basis), or is the coordinate space unbounded;
(b) what determines the minimum and maximum zoom scale exposed by `DiagramCanvas`, and should either bound be influenced by the DOM-rendering performance ceiling ([DOM-rendering performance ceiling for Blazor components](01-performance-ceiling-research.md)) or set purely on UX grounds (e.g., "zoomed out past X, individual components are illegible anyway");
(c) does windowing's overscan margin ([Virtualization/windowing mitigation stress test](02-virtualization-prototype.md)) fully absorb the risk of a very large or heavily zoomed-out board, or does scale need its own independent guardrail.
