// The bar is a toolbar the keyboard enters on purpose (Ctrl+Enter from the canvas) and leaves by
// Escape or Tab. Every key pressed inside it stays there, so the canvas's window listener never
// nudges, deletes or undoes while a control has focus. Left and Right rove between the controls,
// wrapping at either end; Up and Down are left to the control itself. Delete or Backspace on a
// colour that can follow the theme returns it to the theme. Escape hands focus back to
// the one selected shape when there is one, otherwise to whatever held focus before the bar.
export function registerPropertyBar(bar) {
    if (!bar?.isConnected) {
        return { dispose: () => {} };
    }

    const container = bar.closest(".diagram-container");
    let focusedBeforeBar = null;

    const handleFocusIn = (event) => {
        if (!bar.contains(event.relatedTarget)) {
            focusedBeforeBar = event.relatedTarget;
        }
    };

    const handleKeyDown = (event) => {
        event.stopPropagation();
        switch (event.key) {
            case "ArrowRight":
                event.preventDefault();
                focusAdjacentControl(bar, 1);
                break;
            case "ArrowLeft":
                event.preventDefault();
                focusAdjacentControl(bar, -1);
                break;
            case "Escape":
                event.preventDefault();
                returnFocus(container, focusedBeforeBar);
                break;
            case "Delete":
            case "Backspace": {
                const useTheme = event.target
                    .closest(".d12-property-bar-cell")
                    ?.querySelector(".d12-property-bar-use-theme");
                if (event.target.type === "color" && useTheme) {
                    event.preventDefault();
                    useTheme.click();
                }
                break;
            }
        }
    };

    bar.addEventListener("focusin", handleFocusIn);
    bar.addEventListener("keydown", handleKeyDown);

    return {
        dispose: () => {
            bar.removeEventListener("focusin", handleFocusIn);
            bar.removeEventListener("keydown", handleKeyDown);
        }
    };
}

export function measurePropertyBar(bar) {
    return bar?.isConnected ? bar.offsetWidth : 0;
}

function focusAdjacentControl(bar, direction) {
    const controls = Array.from(bar.querySelectorAll(".d12-property-bar-control"));
    if (controls.length === 0) {
        return;
    }

    const currentIndex = controls.indexOf(document.activeElement);
    const nextIndex =
        currentIndex === -1
            ? 0
            : (currentIndex + direction + controls.length) % controls.length;
    controls[nextIndex].focus({ preventScroll: true });
}

function returnFocus(container, focusedBeforeBar) {
    const selectedStops =
        container?.querySelectorAll(
            ".component-container[aria-selected=\"true\"][tabindex=\"0\"], .group-tab-stop[aria-selected=\"true\"], .edge-tab-stop[aria-selected=\"true\"]"
        ) ?? [];
    const target =
        selectedStops.length === 1
            ? selectedStops[0]
            : focusedBeforeBar?.isConnected && container?.contains(focusedBeforeBar)
                ? focusedBeforeBar
                : container?.querySelector(".diagram-canvas");
    target?.focus({ preventScroll: true });
}
