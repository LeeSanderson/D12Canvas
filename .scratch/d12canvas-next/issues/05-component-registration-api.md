# Component registration & palette API contract

Type: grilling
Status: open

## Question

Design the public contract for developer-defined canvas components: how does a host app register a custom component (what does the registration call/attribute look like — type, metadata, icon, default size?), what must a registered component implement or supply to be screen-reader-friendly by contract (even though full keyboard navigation is deferred), and what shape does the default out-of-the-box palette UI take (a fixed sidebar? a searchable list? categorized?)? This is the ADR for the extensibility model — the single mechanism both built-in and custom shapes register through.
