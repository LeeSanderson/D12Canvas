export function setIndeterminate(checkbox, indeterminate) {
    if (checkbox) {
        checkbox.indeterminate = indeterminate;
    }
}

const tileKeys = new Set(["ArrowUp", "ArrowDown", "ArrowLeft", "ArrowRight", "Home", "End", "Enter", " "]);

// The tiles move focus and commit on these keys themselves, so the page must not also scroll, and
// a button must not also click, on them.
export function registerTileKeys(panelElement) {
    panelElement?.addEventListener("keydown", (event) => {
        if (tileKeys.has(event.key) && event.target.closest?.("[role=radio]")) {
            event.preventDefault();
        }
    });
}
