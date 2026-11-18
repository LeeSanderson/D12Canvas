const railKeys = new Set(["ArrowUp", "ArrowDown", "Home", "End"]);

// The rail moves focus on these keys itself, so the page must not also scroll on them.
export function registerRail(railElement) {
    railElement?.addEventListener("keydown", (event) => {
        if (railKeys.has(event.key)) {
            event.preventDefault();
        }
    });
}
