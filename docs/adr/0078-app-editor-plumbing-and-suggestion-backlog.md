# The App's editor works around Blazor with small helper types, and a suggestion lands as a numbered markdown file

`D12Canvas.App` hits four places where Blazor's rendering and event model does not give the editor what it needs. Each has one helper type or one structural choice, and each is easy to undo by accident because the code looks like it could be simpler. The App's suggestion box sits beside them. It sends a `SuggestionSubmission` to `D12Canvas.App.Api`, and `BacklogFolder` writes it to disk. Nothing else stores suggestions, so the file layout, the numbering and the folder's location are the whole contract.

**Escape is shared through `HandledEscape`, focus waits for the render through `PendingFocus`, `ZoomControl` is its own component to keep pan and zoom renders small, and `Autosave` returns its timers through the component's dispatcher. Each suggestion is one `NNN-slug.md` file with its screenshot beside it as `NNN-slug.<ext>`, and `NNN` is one past the highest number already in the folder.**

## One Escape, two handlers

Blazor cannot stop the propagation of one key and let every other key through, because `@onkeydown:stopPropagation` applies to all keys. A control inside the editor that acts on `Escape`, such as `Dropdown`, calls `HandledEscape.Mark()`. The outer `Escape` handler further up the same dispatch, in `MainLayout`, calls `TryTake()` and skips its own action when the mark is there. `TryTake` clears the mark, so a mark covers exactly one key press.

## Focus waits for the render

An element can take focus only after the render that shows it has reached the DOM, and that render is what assigns its `ElementReference`. So `PendingFocus.Request` takes a `Func<ElementReference>`, not a reference, and `ApplyAsync` calls it in `OnAfterRender`. A reference read at request time holds the value from before the render, which is unset or stale.

## The zoom readout is its own component

`ZoomControl` subscribes to the canvas's `ZoomOrPanChanged` itself. A pan or zoom then re-renders only that control, not `BoardEditor`, which holds the canvas. With the readout folded into the page, every pan would re-render the whole editor.

## Autosave runs on the renderer's thread

`Autosave` debounces saves with a `RestartableDelay` and shows a "Saved" notice for a fixed time. Both delays fire on a timer thread. They come back through the `dispatch` delegate, which `BoardEditor` passes as its `InvokeAsync`, so the save and the state change run on the renderer's thread.

## The theme key appears twice

`index.html` reads the stored theme before Blazor starts. That script cannot read `PreferenceKeys.Theme`, so it repeats `"d12canvas.theme"` as a literal. A change to the key changes both.

## The backlog file layout

`BacklogFolder.Save` writes `NNN-slug.md` for every suggestion. When the submission carries a screenshot, it also writes `NNN-slug.<ext>` with the same base name. The extension comes from the image's media type.

`SuggestionSubmission.Screenshot` is a data URL exactly as the browser's `FileReader` produces it, or null when nothing was pasted. The API decodes it, and the browser does no conversion of its own.

## Numbers only rise

`NNN` is one past the highest number that any entry in the folder already starts with. It is not a count of files. A file deleted or renamed by hand never frees its number, and a new suggestion never takes the number of one that was removed.

## The folder belongs to the repository

`Program.RepositoryRoot` in `D12Canvas.App.Api` walks up from the content root to the first directory that holds `D12Canvas.slnx`. The backlog is that directory's `.scratch/backlog` folder, unless the `Backlog:Directory` setting names another. The folder is the same however the server starts: from the solution root, from the project folder or from an IDE. When no solution file is found, `RepositoryRoot` returns the content root.

## The box keeps its draft

Closing the suggestion box does not clear it. `SuggestionBox.OpenAsync` reopens the dialog on the draft as it was left, so an accidental `Escape` loses nothing. Only a successful save clears the draft.

## Pasting a screenshot

`readClipboardImage` in `SuggestionBox.razor.js` reads the async clipboard for the first `image/*` item. When the browser refuses the read, or the API is missing, it returns null, and the caller reports that there was no image to paste. A refusal is not an error.

## Consequences

- A new control that handles `Escape` inside the editor marks `HandledEscape` and does not stop propagation.
- Code that focuses an element shown by the next render goes through `PendingFocus`.
- Moving the zoom readout into `BoardEditor` makes every pan re-render the editor. It is a performance regression, not a simplification.
- Deleting or renaming backlog files by hand never makes the numbering go backwards or collide.
- Running the API outside a checkout of this repository, with no `Backlog:Directory`, writes to `.scratch/backlog` under the content root.
