# Autosave in the page header

Status: resolved
Blocked by: 17

Spec: [App polish](../spec.md), "Autosave". Decision record: [Shared nav bar](07-shared-nav-bar.md), point 3.

## What to build

Autosave is on by default, remembered across boards, and its state shows in the editor's page header.

- The editor puts an Autosave switch (`.form-check.form-switch`) and the save status in the page header. They replace the toolbar row's Autosave checkbox, save state and Save button.
- The choice is stored through `IPreferences` as `d12canvas.autosave` (`"true"`/`"false"`), read when a board opens. Missing means on.
- On: no Save button and no save-state text. Each save shows "Saving…" (`cloud-arrow-up`), then "Saved" (`cloud-check`) for two seconds, then clears.
- Off: "Unsaved changes" or "Saved", and a Save button (`floppy`) that is disabled when nothing changed.
- Turning autosave on while the board has unsaved changes saves at once.
- The status is one always-rendered `role="status"` element. Below `sm` it shows the icon only.

## Acceptance criteria

- [x] Opening a board with no stored preference has autosave on, and an edit saves the board without pressing Save.
- [x] Turning autosave off writes `"false"`, shows the Save button, and the choice applies to the next board opened and survives a reload.
- [x] With autosave on, the header shows "Saving…" then "Saved", and "Saved" clears after two seconds.
- [x] With autosave off, Save is disabled until an edit, and saving returns the status to "Saved".
- [x] Turning autosave on with unsaved changes saves at once.
- [x] App tests through the page harness cover each of the above, counting saves on `InMemoryBoardStore` and reading `InMemoryPreferences`.

## Comments

Done. The Autosave switch, the save status and the Save button sit in the editor's page header Actions section. The toolbar row keeps Undo/Redo, zoom, snapping, Wheel and Shortcuts until ticket 20 removes it. The editor reads `d12canvas.autosave` from `IPreferences` when a board opens; anything but `"false"` means on.

With autosave on, the status reads "Saving…" from the first edit until the debounced save finishes, then "Saved" for two seconds. A save only marks the board clean if no edit arrived while it ran, so an edit made mid-save stays unsaved in both modes. Turning autosave off cancels a pending save, so the changes stay unsaved. The spec names no icon for "Unsaved changes", so I used a plain `cloud`. Below `sm` the status text is visually hidden, not removed, so screen readers still hear it.

The editor takes `TimeProvider` from DI (`TimeProvider.System` in the App). The spec named `IPreferences` as the only new seam, but `TimeProvider` is the BCL's own clock and it keeps the tests off real one- and two-second waits. Tests register a `FakeTimeProvider` as `Clock` on `AppTestContext` and step the debounce and the two-second notice with `Clock.Advance`.

A failed `SaveAsync` still goes unreported, as it did before: the header stays on "Saving…". No ticket covers save-failure UI yet.
