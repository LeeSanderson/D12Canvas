const railKeys = new Set(["ArrowUp", "ArrowDown", "Home", "End"]);

export function registerRail(railElement) {
    railElement?.addEventListener("keydown", (event) => {
        if (railKeys.has(event.key)) {
            event.preventDefault();
        }
    });
}
