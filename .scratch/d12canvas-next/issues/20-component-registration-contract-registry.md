# 20 — Component-type registration contract & registry

**What to build:** A component author registers a component type through DI under a stable string key — chosen independently of the CLR type name so persisted boards survive refactors. Registration requires `DisplayName`, `AccessibleName`, and `DefaultProps`; `Icon`, `Role` (defaulting to `"group"`), `DefaultSize`, and `Category` are optional. Registering a second type under an existing key fails fast at registration time. The registry resolves a key to its registration at runtime. (ADR 0001.)

**Blocked by:** None — can start immediately.

**Status:** resolved

- [x] A component type registers via DI under a stable string key decoupled from its CLR type name
- [x] Required metadata (`DisplayName`, `AccessibleName`, `DefaultProps`) is enforced; optional metadata defaults correctly (`Role` → `"group"`)
- [x] Duplicate key registration is rejected at registration time with a clear error naming the key
- [x] The registry resolves a key to its registration; an unknown key is a distinguishable, well-typed failure
- [x] Props (`TProps`) are structurally separate from bounds in the contract
- [x] xUnit coverage at the pure C# seam

## Comments

Implemented under `D12Canvas/Registration/`: `IComponentRegistry`/`ComponentRegistry` (keyed store, `Resolve` throws `UnknownComponentKeyException`, `Register` throws `DuplicateComponentKeyException` — both name the key), `ComponentRegistration` (the resolved, non-generic record: `Key`, `ComponentType`, `PropsType`, `DisplayName`, `AccessibleName`, boxed `DefaultProps`, `Icon`, `Role`, `DefaultSize`, `Category`), `ComponentRegistrationBuilder<TProps>` (the mutable bag a registration lambda configures), and `D12CanvasOptions.RegisterComponent<TComponent, TProps>(key, configure)` (validates `DisplayName`/`AccessibleName`/`DefaultProps` are present, throwing `ComponentRegistrationException` naming the key and the missing field; defaults `Role` to `"group"`). `AddD12Canvas(Action<D12CanvasOptions>)` is the `IServiceCollection` extension matching ADR 0001's `services.AddD12Canvas(o => o.RegisterComponent<TComponent, TProps>("key", ...))` shape, registering the built registry as a singleton `IComponentRegistry`.

`DefaultSize` is a new minimal `ComponentSize(Width, Height)` value type — deliberately size-only, no position, since bounds/position is the board/component-instance model's concern (tickets 06/21), not the registration contract's.

Covered by `ComponentRegistryTests`, `D12CanvasOptionsTests`, and `ServiceCollectionExtensionsTests` (all pure C# seam, no rendering).

Hardened per code review: `ComponentRegistrationBuilder<TProps>`/`RegisterComponent` now constrain `TProps : class` — an unconstrained `TProps?` made the `DefaultProps`-required check a no-op for value-type props (`default(TProps)` isn't `null`); `RegisterComponent` rejects an empty/whitespace key (`ArgumentException`) since a blank key would defeat the whole point of a stable persisted-board identity; and `AddD12Canvas` now detects an already-registered `IComponentRegistry` and reuses it instead of shadowing it, so calling `AddD12Canvas` more than once (e.g. library built-ins plus a host's own extension method) accumulates registrations rather than silently dropping the earlier call's — consistent with ADR 0001's "should throw, not silently overwrite or coexist."

**Known doc tension, not acted on:** `CONTEXT.md`'s `LOD placeholder` entry describes the placeholder as built from data "the registration contract already requires (`DisplayName`/`Icon`)" — but `Icon` is explicitly optional per this ticket's checklist and ADR 0001. Left `Icon` optional here since that's the locked, explicit contract; ticket 70 (LOD placeholder) will need to reconcile that wording (e.g. falling back to no icon, or requiring one) when it's implemented.
