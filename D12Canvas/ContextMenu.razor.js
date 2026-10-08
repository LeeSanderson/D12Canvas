// While a menu is open, the next press anywhere outside it closes it. Inside the canvas container
// that press does nothing else: it is stopped in the capture phase on document, before the canvas
// listener sees it, and the click or contextmenu it would go on to fire is swallowed too. A press
// on the host's own markup outside the container is left alone, so the host's button still acts.
// A menu opened from the keyboard starts on its first row and, once it closes, hands focus back to
// the element that held it at the keydown.
export function registerMenu(menuElement, dotNetHelper, options) {
    // The menu can close before its first render's registration runs, leaving no element.
    if (!menuElement?.isConnected) {
        return { dispose: () => {} };
    }

    const container = menuElement.closest(".diagram-container");
    const openedFromKeyboard = options?.openedFromKeyboard === true;
    const focusedAtOpen = document.activeElement;

    const handlePointerDown = (event) => {
        if (menuElement.contains(event.target)) {
            return;
        }

        if (container !== null && container.contains(event.target)) {
            event.preventDefault();
            event.stopPropagation();
            swallowWhatThePressFiresNext();
        }

        dotNetHelper.invokeMethodAsync("RequestClose");
    };

    // The canvas's shortcut listener is on window, so every key pressed inside the menu stops here
    // and none of them also acts on the board: Escape closes the menu and nothing more, the arrows
    // rove between rows instead of nudging the selection. A row's own activation by Enter or Space
    // is the button's default action, which stopping propagation leaves alone.
    const handleKeyDown = (event) => {
        event.stopPropagation();
        switch (event.key) {
            case "Escape":
                event.preventDefault();
                dotNetHelper.invokeMethodAsync("RequestClose");
                break;
            case "ArrowDown":
                event.preventDefault();
                focusAdjacentItem(menuElement, 1);
                break;
            case "ArrowUp":
                event.preventDefault();
                focusAdjacentItem(menuElement, -1);
                break;
        }
    };

    document.addEventListener("pointerdown", handlePointerDown, true);
    menuElement.addEventListener("keydown", handleKeyDown);
    // From a pointer the menu itself takes focus, so no row looks chosen before a key is pressed;
    // the first arrow lands on the first or last row.
    const firstItem = menuElement.querySelector(".d12-context-menu-item");
    if (openedFromKeyboard && firstItem !== null) {
        firstItem.focus({ preventScroll: true });
    } else {
        menuElement.focus({ preventScroll: true });
    }

    return {
        dispose: () => {
            document.removeEventListener("pointerdown", handlePointerDown, true);
            menuElement.removeEventListener("keydown", handleKeyDown);
            if (openedFromKeyboard) {
                returnFocus(menuElement, container, focusedAtOpen);
            }
        }
    };
}

// Only focus the menu took with it goes back: a press that moved focus elsewhere keeps it there. A
// row whose command removed the element that held focus, such as Delete, leaves it on the canvas.
function returnFocus(menuElement, container, focusedAtOpen) {
    const active = document.activeElement;
    const focusWasLost =
        active === null ||
        active === document.body ||
        active === document.documentElement ||
        menuElement.contains(active);
    if (!focusWasLost || focusedAtOpen === null || focusedAtOpen === document.body) {
        return;
    }

    const target = focusedAtOpen.isConnected
        ? focusedAtOpen
        : container?.querySelector(".diagram-canvas");
    target?.focus({ preventScroll: true });
}

// A consumed press still fires its click, or on the secondary button its contextmenu, once it is
// released. Each is swallowed once. The next press or key, or the press being cancelled, clears
// whichever never came, so a primary dismissal cannot go on to eat a ContextMenu key's menu or a
// keyboard click on a host button.
function swallowWhatThePressFiresNext() {
    const swallow = (event) => {
        event.preventDefault();
        event.stopPropagation();
    };
    const clear = () => {
        document.removeEventListener("click", swallow, true);
        document.removeEventListener("contextmenu", swallow, true);
        document.removeEventListener("pointerdown", clear, true);
        document.removeEventListener("pointercancel", clear, true);
        document.removeEventListener("keydown", clear, true);
    };

    document.addEventListener("click", swallow, { capture: true, once: true });
    document.addEventListener("contextmenu", swallow, { capture: true, once: true });
    document.addEventListener("pointerdown", clear, true);
    document.addEventListener("pointercancel", clear, true);
    document.addEventListener("keydown", clear, true);
}

// Wraps at either end.
function focusAdjacentItem(menuElement, direction) {
    const items = Array.from(menuElement.querySelectorAll(".d12-context-menu-item"));
    if (items.length === 0) {
        return;
    }

    const currentIndex = items.indexOf(document.activeElement);
    const nextIndex =
        currentIndex === -1
            ? direction > 0
                ? 0
                : items.length - 1
            : (currentIndex + direction + items.length) % items.length;
    items[nextIndex].focus();
}
