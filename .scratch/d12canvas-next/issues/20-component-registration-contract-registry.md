# 20 — Component-type registration contract & registry

**What to build:** A component author registers a component type through DI under a stable string key — chosen independently of the CLR type name so persisted boards survive refactors. Registration requires `DisplayName`, `AccessibleName`, and `DefaultProps`; `Icon`, `Role` (defaulting to `"group"`), `DefaultSize`, and `Category` are optional. Registering a second type under an existing key fails fast at registration time. The registry resolves a key to its registration at runtime. (ADR 0001.)

**Blocked by:** None — can start immediately.

**Status:** ready-for-agent

- [ ] A component type registers via DI under a stable string key decoupled from its CLR type name
- [ ] Required metadata (`DisplayName`, `AccessibleName`, `DefaultProps`) is enforced; optional metadata defaults correctly (`Role` → `"group"`)
- [ ] Duplicate key registration is rejected at registration time with a clear error naming the key
- [ ] The registry resolves a key to its registration; an unknown key is a distinguishable, well-typed failure
- [ ] Props (`TProps`) are structurally separate from bounds in the contract
- [ ] xUnit coverage at the pure C# seam
