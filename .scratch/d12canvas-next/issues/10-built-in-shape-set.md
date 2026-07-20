# Built-in component/shape set

Type: grilling
Status: open

## Question

Given the resolved component registration contract (string key, `TComponent`/`TProps` pair, required `DisplayName`/`AccessibleName`/`DefaultProps`, optional `Icon`/`Role`/`DefaultSize`/`Category`), decide which component types ship pre-registered as D12Canvas's own built-ins (candidates raised while scoping this map: rectangle, sticky note, text, freehand pen, image, etc.). For each: its `TProps` shape, default size, and default props. These register through the exact same `AddD12Canvas`/`RegisterComponent<TComponent, TProps>` mechanism as any custom host-defined component — there is no separate built-in path.
