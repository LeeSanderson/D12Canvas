// The bar is a toolbar the keyboard enters on purpose (Ctrl+Enter from the canvas) and leaves by
// Escape or Tab. Every key pressed inside it stays there, so the canvas's window listener never
// nudges, deletes or undoes while a control has focus. Left and Right rove between the controls,
// wrapping at either end; Up and Down are left to the control itself. Delete or Backspace on a
// colour that can follow the theme returns it to the theme. Escape hands focus back to
// the one selected shape when there is one, otherwise to whatever held focus before the bar.
// "Custom…" in a colour flyout opens the native picker of the hidden colour input in its cell
// while the click is still a user gesture, which the browser requires; .NET closes the flyout.
// A visible bar that the canvas moves, outside a framing flight, hides until it has stood still
// for SETTLE_MS, so a wheel pan or a burst of nudges does not drag it jittering across the board.
const SETTLE_MS = 150;

export function registerPropertyBar(bar) {
    if (!bar?.isConnected) {
        return { dispose: () => {} };
    }

    const stopSettling = settleAfterMoves(bar);

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
                    .closest(".d12-property-bar-colour")
                    ?.querySelector(".d12-property-bar-use-theme");
                if (useTheme) {
                    event.preventDefault();
                    useTheme.click();
                }
                break;
            }
        }
    };

    const handleClick = (event) => {
        const picker = event.target
            .closest(".d12-property-bar-custom")
            ?.closest(".d12-property-bar-cell")
            ?.querySelector(".d12-property-bar-picker");
        if (picker) {
            openPicker(picker);
        }
    };

    bar.addEventListener("focusin", handleFocusIn);
    bar.addEventListener("keydown", handleKeyDown);
    bar.addEventListener("click", handleClick);

    return {
        dispose: () => {
            stopSettling();
            bar.removeEventListener("focusin", handleFocusIn);
            bar.removeEventListener("keydown", handleKeyDown);
            bar.removeEventListener("click", handleClick);
        }
    };
}

function settleAfterMoves(bar) {
    const placeOf = () => ({
        left: bar.style.left,
        top: bar.style.top,
        visible: bar.style.visibility !== "hidden",
        flying: bar.style.transitionProperty !== ""
    });
    let last = placeOf();
    let timer = null;

    const observer = new MutationObserver(() => {
        const current = placeOf();
        const moved = current.left !== last.left || current.top !== last.top;
        if (moved && last.visible && current.visible && !current.flying) {
            bar.classList.add("d12-property-bar-settling");
            clearTimeout(timer);
            timer = setTimeout(() => bar.classList.remove("d12-property-bar-settling"), SETTLE_MS);
        }

        last = current;
    });
    observer.observe(bar, { attributes: true, attributeFilter: ["style"] });

    return () => {
        observer.disconnect();
        clearTimeout(timer);
    };
}

export function measurePropertyBar(bar) {
    return bar?.isConnected ? bar.offsetWidth : 0;
}

function openPicker(picker) {
    picker.focus({ preventScroll: true });
    try {
        picker.showPicker();
    } catch {
        picker.click();
    }
}

function focusAdjacentControl(bar, direction) {
    const controls = Array.from(bar.querySelectorAll(".d12-property-bar-control"));
    if (controls.length === 0) {
        return;
    }

    const active = document.activeElement;
    const current = active?.classList.contains("d12-property-bar-picker")
        ? active.closest(".d12-property-bar-cell").querySelector(".d12-property-bar-control")
        : active;
    const currentIndex = controls.indexOf(current);
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
