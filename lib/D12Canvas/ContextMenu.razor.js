import { focusedAtMenuKey } from "./DiagramCanvas.razor.js";

export function registerMenu(menuElement, dotNetHelper, options) {
    if (!menuElement?.isConnected) {
        return { dispose: () => {} };
    }

    const container = menuElement.closest(".diagram-container");
    const openedFromKeyboard = options?.openedFromKeyboard === true;
    const focusedAtOpen =
        (openedFromKeyboard && container !== null ? focusedAtMenuKey(container) : null) ??
        document.activeElement;

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
            case "ArrowRight":
                event.preventDefault();
                focusAdjacentGlyph(1);
                break;
            case "ArrowLeft":
                event.preventDefault();
                focusAdjacentGlyph(-1);
                break;
        }
    };

    document.addEventListener("pointerdown", handlePointerDown, true);
    menuElement.addEventListener("keydown", handleKeyDown);
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

function focusAdjacentItem(menuElement, direction) {
    const rows = Array.from(
        menuElement.querySelectorAll(":scope > .d12-context-menu-item, :scope > .d12-context-menu-strip")
    );
    if (rows.length === 0) {
        return;
    }

    const currentIndex = rows.findIndex((row) => row.contains(document.activeElement));
    const nextIndex =
        currentIndex === -1
            ? direction > 0
                ? 0
                : rows.length - 1
            : (currentIndex + direction + rows.length) % rows.length;
    const next = rows[nextIndex];
    (next.querySelector(".d12-context-menu-glyph") ?? next).focus();
}

function focusAdjacentGlyph(direction) {
    const strip = document.activeElement?.closest(".d12-context-menu-strip");
    if (!strip) {
        return;
    }

    const glyphs = Array.from(strip.querySelectorAll(".d12-context-menu-glyph"));
    const currentIndex = glyphs.indexOf(document.activeElement);
    glyphs[(currentIndex + direction + glyphs.length) % glyphs.length].focus();
}
