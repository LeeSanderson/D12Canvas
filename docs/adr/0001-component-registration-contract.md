# Component registration is DI-based, keyed by a stable string decoupled from the CLR type, with bounds kept separate from props

Host apps register component types via `IServiceCollection` (`AddD12Canvas(o => o.RegisterComponent<TComponent, TProps>("key", ...))`), not via Razor-markup composition or attribute/assembly-scanning discovery. Built-in and custom component types register through this identical call — there's no separate path for "built-in" shapes.

Identity is a developer-assigned string key, not the CLR type — chosen so a persisted board survives an internal rename/refactor of the component's class, which type-name-based identity would break.

A component type's `TProps` (its own serializable business data) is kept structurally separate from its bounds (position/size). Bounds are tracked uniformly on every instance regardless of component type, specifically so a viewport-query surface (informed by the DOM-rendering performance research) can scan bounds across a board without reflecting into each component type's own props shape.

**Considered and rejected:** declarative Razor-markup registration (a `<PaletteItem>`-style composition) and attribute + assembly-scanning discovery — both rejected in favor of DI because it centralizes registration in one place regardless of how many pages use the canvas, and avoids reflection/trimming concerns under Blazor WASM.
