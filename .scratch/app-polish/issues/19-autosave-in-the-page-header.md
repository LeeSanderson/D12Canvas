# Autosave in the page header

Status: ready-for-agent
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

- [ ] Opening a board with no stored preference has autosave on, and an edit saves the board without pressing Save.
- [ ] Turning autosave off writes `"false"`, shows the Save button, and the choice applies to the next board opened and survives a reload.
- [ ] With autosave on, the header shows "Saving…" then "Saved", and "Saved" clears after two seconds.
- [ ] With autosave off, Save is disabled until an edit, and saving returns the status to "Saved".
- [ ] Turning autosave on with unsaved changes saves at once.
- [ ] App tests through the page harness cover each of the above, counting saves on `InMemoryBoardStore` and reading `InMemoryPreferences`.
