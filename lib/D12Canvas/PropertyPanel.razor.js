export function setIndeterminate(checkbox, indeterminate) {
    if (checkbox) {
        checkbox.indeterminate = indeterminate;
    }
}

const tileKeys = new Set(["ArrowUp", "ArrowDown", "ArrowLeft", "ArrowRight", "Home", "End", "Enter", " "]);

export function registerTileKeys(panelElement) {
    panelElement?.addEventListener("keydown", (event) => {
        if (tileKeys.has(event.key) && event.target.closest?.("[role=radio]")) {
            event.preventDefault();
        }
    });
}
